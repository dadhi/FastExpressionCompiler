using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;


#if LIGHT_EXPRESSION
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue475_Reuse_DynamicMethod_if_possible : ITestX
{
    public void Run(TestRun t)
    {
        TryToReuseTheDynamicMethod(t);
    }

    internal static FieldInfo IlGeneratorField = typeof(DynamicMethod).GetField("_ilGenerator", BindingFlags.Instance | BindingFlags.NonPublic);
    internal static FieldInfo MethodHandleField = typeof(DynamicMethod).GetField("_methodHandle", BindingFlags.Instance | BindingFlags.NonPublic);
    internal static MethodInfo GetMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
    internal static MethodInfo CreateDelegateMethod = typeof(Delegate).GetMethod("CreateDelegateNoSecurityCheck", BindingFlags.Static | BindingFlags.NonPublic);

    void TryToReuseTheDynamicMethod(TestContext t)
    {
        // Let's say there is a DynamicMethod of Func<int, int> returning its argument, 
        // at the end I want to reuse the created DynamicMethod for another Func<int, int> returning arg + 42

        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        var runtimeMethodHandle = GetMethodDescriptorMethod.Invoke(dynMethod, null);
        var func = (Func<int, int>)CreateDelegateMethod.Invoke(null, [typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure, runtimeMethodHandle]);

        // var func = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        t.AreEqual(41, func(41));

        // Debug.WriteLine($"Field_initLocals: {Field_initLocals.GetValue(dynMethod)}");
        IlGeneratorField.SetValue(dynMethod, null);

        // required: Keeps the old delegate if not set to null
        MethodHandleField.SetValue(dynMethod, null);

        var il2 = dynMethod.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        t.AreEqual(41, func(41));

        // CreateDelegate does:
        // RuntimeMethodHandle runtimeMethodHandle = GetMethodDescriptor();
        // MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, runtimeMethodHandle);
        // // stash this MethodInfo by brute force.
        // d.StoreDynamicMethod(this);
        // return d;

        // todo: @wip Calling this second time in `-c:Release` will produce `Fatal error. Internal CLR error. (0x80131506) ## :-( Failed with ERROR: -1073741819` (the -c:Debug is working fine)
        // runtimeMethodHandle = GetMethodDescriptorMethod.Invoke(dynMethod, null);

        // var func2 = (Func<int, int>)CreateDelegateMethod.Invoke(null, [typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure, runtimeMethodHandle]);

        // var func2 = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        // t.AreEqual(83, func2(41));
    }

    public static object Reuse()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        _ = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);

        IlGeneratorField.SetValue(dynMethod, null);
        // Field_initLocals.SetValue(dynMethod, true);

        var il2 = dynMethod.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        return (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
    }

    public static object NoReuse()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        _ = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);

        var dynMethod2 = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il2 = dynMethod2.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        return (Func<int, int>)dynMethod2.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
    }

    // internal static FieldInfo Field_initLocals = typeof(DynamicMethod).GetField("_initLocals", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static Action<DynamicMethod> GetDynamicMethodResetDelegate()
    {
        // Reset the DynamicMethod internals to enable its reuse
        // _ilGenerator = null;
        // _initLocals = true;
        // _methodHandle = null; // but this reset breaks the CLR

        var dynMethod = new DynamicMethod(string.Empty,
            typeof(void),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(DynamicMethod)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Stfld, IlGeneratorField);
        // il.Emit(OpCodes.Ldarg_1);
        // il.Emit(OpCodes.Ldc_I4_1);
        // il.Emit(OpCodes.Stfld, Field_initLocals);
        il.Emit(OpCodes.Ret);

        return (Action<DynamicMethod>)dynMethod.CreateDelegate(typeof(Action<DynamicMethod>), ExpressionCompiler.EmptyArrayClosure);
    }

    internal static Lazy<Action<DynamicMethod>> ResetDynamicMethod = new(GetDynamicMethodResetDelegate);
}