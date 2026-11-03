#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
#if NET10_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

// These are intentionally strict contracts for the CoreCLR versions exercised by CI.
// A runtime update must not silently disable an optimization while correctness tests pass.
public struct RuntimeAccessorContractTests : ITestX
{
    private const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public void Run(TestRun t)
    {
        Local_allocation_and_generator_reuse_remain_available(t);
#if NET10_0_OR_GREATER
        Accessor_declarations_match_the_runtime_members(t);
        Production_accessors_bind_and_emit_a_working_method(t);
#endif
    }

    public void Local_allocation_and_generator_reuse_remain_available(TestContext t)
    {
        var hacks = typeof(DynamicMethodHacks);
        t.IsNotNull(hacks.GetField("GetNextLocalVarLocation", NonPublicStatic).GetValue(null));
        t.IsNotNull(hacks.GetField("ReuseDynamicILGenerator", NonPublicStatic).GetValue(null));
        var pool = hacks.GetField("_pooledILGenerator", NonPublicStatic);
        var previous = pool.GetValue(null);
        pool.SetValue(null, null);
        try
        {
            var first = new DynamicMethod("first", typeof(int), new[] { typeof(int) }, typeof(RuntimeAccessorContractTests), true);
            var il = DynamicMethodHacks.RentPooledOrNewILGenerator(first, typeof(int), new[] { typeof(int) });
            t.AreEqual(0, il.GetNextLocalVarIndex(typeof(int)));
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }));
            il.Emit(OpCodes.Stloc_0);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Nop);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            var f = (Func<int, int>)first.CreateDelegate(typeof(Func<int, int>));
            t.AreEqual(42, f(-42));
            DynamicMethodHacks.FreePooledILGenerator(first, il);

            // Change the signature and local type after tokens, exception regions and locals were used.
            var second = new DynamicMethod("second", typeof(string), new[] { typeof(string) }, typeof(RuntimeAccessorContractTests), true);
            var reused = DynamicMethodHacks.RentPooledOrNewILGenerator(second, typeof(string), new[] { typeof(string) });
            t.IsTrue(ReferenceEquals(il, reused));
            t.AreEqual(0, reused.GetNextLocalVarIndex(typeof(string)));
            reused.Emit(OpCodes.Ldarg_0);
            reused.Emit(OpCodes.Stloc_0);
            reused.Emit(OpCodes.Ldloc_0);
            reused.Emit(OpCodes.Ret);
            var g = (Func<string, string>)second.CreateDelegate(typeof(Func<string, string>));
            t.AreEqual("second", g("second"));
            t.AreEqual(17, f(-17)); // Reuse must not change the already-created method.
            DynamicMethodHacks.FreePooledILGenerator(second, reused);
        }
        finally
        {
            pool.SetValue(null, previous);
        }
    }

#if NET10_0_OR_GREATER
    public void Accessor_declarations_match_the_runtime_members(TestContext t)
    {
        var count = 0;
        foreach (var accessor in typeof(DynamicMethodHacks).GetMethods(NonPublicStatic))
        {
            var attribute = accessor.GetCustomAttribute<UnsafeAccessorAttribute>();
            if (attribute == null)
                continue;
            ++count;
            var parameters = accessor.GetParameters();
            var ownerName = parameters[0].GetCustomAttribute<UnsafeAccessorTypeAttribute>().TypeName;
            var owner = typeof(ILGenerator).Assembly.GetType(ownerName, throwOnError: true);
            // UnsafeAccessor does not search base classes. Check the exact declaring type.
            if (attribute.Kind == UnsafeAccessorKind.Field)
            {
                var field = owner.GetField(attribute.Name, InstanceMembers | BindingFlags.DeclaredOnly);
                if (field == null)
                    throw new MissingFieldException(owner.FullName, attribute.Name);
                t.IsTrue(!field.IsStatic);
                t.AreEqual(field.FieldType.MakeByRefType(), accessor.ReturnType);
                t.AreEqual(1, parameters.Length);
            }
            else
            {
                t.AreEqual(UnsafeAccessorKind.Method, attribute.Kind);
                var argumentTypes = new Type[parameters.Length - 1];
                for (var i = 1; i < parameters.Length; ++i)
                    argumentTypes[i - 1] = parameters[i].ParameterType;
                var method = owner.GetMethod(attribute.Name, InstanceMembers | BindingFlags.DeclaredOnly, null, argumentTypes, null);
                if (method == null)
                    throw new MissingMethodException(owner.FullName, attribute.Name);
                t.IsTrue(!method.IsStatic);
                t.AreEqual(method.ReturnType, accessor.ReturnType);
            }
        }
        t.AreEqual(6, count);
    }

    private delegate ref T FieldAccessor<T>(object instance);

    private static T Bind<T>(string name) where T : Delegate =>
        (T)typeof(DynamicMethodHacks).GetMethod(name, NonPublicStatic).CreateDelegate(typeof(T));

    public void Production_accessors_bind_and_emit_a_working_method(TestContext t)
    {
        // Bind the production declarations, not duplicate UnsafeAccessor stubs in the test assembly.
        var signature = Bind<FieldAccessor<SignatureHelper>>("GetMLocalSignature");
        var localCount = Bind<FieldAccessor<int>>("GetMLocalCount");
        var length = Bind<FieldAccessor<int>>("GetMLength");
        var stream = Bind<FieldAccessor<byte[]>>("GetMILStream");
        var tokens = Bind<FieldAccessor<List<object>>>("GetMTokens");
        var updateStack = Bind<Action<object, OpCode, int>>("UpdateStackSize");
        var method = new DynamicMethod("accessors", typeof(int), Type.EmptyTypes, typeof(RuntimeAccessorContractTests), true);
        var il = method.GetILGenerator(32);
        t.AreEqual(0, localCount(il));
        signature(il).AddArgument(typeof(int), false);
        ++localCount(il);
        t.AreEqual(1, localCount(il));

        var scope = il.GetType().GetField("m_scope", InstanceMembers).GetValue(il);
        var table = tokens(scope);
        table.Add(typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }).MethodHandle);
        var token = (table.Count - 1) | 0x06000000;
        var bytes = stream(il);
        ref var position = ref length(il);
        t.AreEqual(0, position);
        bytes[position++] = (byte)OpCodes.Ldc_I4_S.Value;
        bytes[position++] = unchecked((byte)-42);
        updateStack(il, OpCodes.Ldc_I4_S, 1);
        bytes[position++] = (byte)OpCodes.Call.Value;
        for (var shift = 0; shift != 32; shift += 8)
            bytes[position++] = (byte)(token >> shift);
        updateStack(il, OpCodes.Call, 0); // Abs consumes and returns one value.
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);
        t.AreEqual(42, ((Func<int>)method.CreateDelegate(typeof(Func<int>)))());
    }
#endif
}
#endif
