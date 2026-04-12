#if NET8_0_OR_GREATER && !LIGHT_EXPRESSION
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace FastExpressionCompiler.IssueTests
{
    
    public class EmitHacksTest : ITest
    {
        public int Run()
        {
            DynamicMethod_Emit_Hack();
#if NET10_0_OR_GREATER
            DynamicMethod_Emit_Hack_Net10();
            return 4;
#else
            return 3;
#endif
        }

        public void DynamicMethod_Emit_Hack()
        {
            var f = Get_DynamicMethod_Emit_Hack();
            var a = f(41);
            Asserts.AreEqual(42, a);
        }

        static readonly Type ilType = typeof(ILGenerator).Assembly.GetType("System.Reflection.Emit.DynamicILGenerator");

        // m_scope field is on DynamicILGenerator (internal class) - accessed via reflection since
        // the field type DynamicScope is also internal (UnsafeAccessorType can't return non-public types).
        static readonly FieldInfo mScopeField = ilType?.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly Type scopeType = ilType?.Assembly.GetType("System.Reflection.Emit.DynamicScope");
        static readonly FieldInfo mTokensField = scopeType?.GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);

        // m_length, m_ILStream, and UpdateStackSize are on RuntimeILGenerator (the internal base class of DynamicILGenerator),
        // NOT on the public ILGenerator class. Look up the fields on the correct type.
        static readonly Type runtimeILGenType = ilType?.BaseType; // System.Reflection.Emit.RuntimeILGenerator
        static readonly FieldInfo mLengthField = runtimeILGenType?.GetField("m_length", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        static readonly FieldInfo mILStreamField = runtimeILGenType?.GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        static readonly MethodInfo updateStackSizeMethod = runtimeILGenType?.GetMethod("UpdateStackSize", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Func<ILGenerator, IList<object>> GetScopeTokens()
        {
            if (mScopeField == null || mTokensField == null)
                return null;

            var dynMethod = new DynamicMethod(string.Empty,
                typeof(IList<object>), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(ILGenerator) },
                typeof(ExpressionCompiler), skipVisibility: true);
            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, mScopeField);
            il.Emit(OpCodes.Ldfld, mTokensField);
            il.Emit(OpCodes.Ret);

            return (Func<ILGenerator, IList<object>>)dynMethod.CreateDelegate(typeof(Func<ILGenerator, IList<object>>), ExpressionCompiler.EmptyArrayClosure);
        }
        static readonly Func<ILGenerator, IList<object>> getScopeTokens = GetScopeTokens();

        private delegate ref TField GetFieldRefDelegate<TFieldHolder, TField>(TFieldHolder holder);

        private static GetFieldRefDelegate<TFieldHolder, TField> CreateFieldAccessor<TFieldHolder, TField>(FieldInfo field)
        {
            if (field == null) return null;

            var dynMethod = new DynamicMethod(string.Empty,
                typeof(TField).MakeByRefType(), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(TFieldHolder) },
                typeof(TFieldHolder), skipVisibility: true);

            var il = dynMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldflda, field);
            il.Emit(OpCodes.Ret);

            return (GetFieldRefDelegate<TFieldHolder, TField>)dynMethod.CreateDelegate(typeof(GetFieldRefDelegate<TFieldHolder, TField>));
        }

        static readonly GetFieldRefDelegate<ILGenerator, int> mLengthFieldAccessor = CreateFieldAccessor<ILGenerator, int>(mLengthField);
        static readonly GetFieldRefDelegate<ILGenerator, byte[]> mILStreamAccessor = CreateFieldAccessor<ILGenerator, byte[]>(mILStreamField);

        static readonly Action<ILGenerator, OpCode, int> updateStackSizeDelegate = GetUpdateStackSizeDelegate();

        private static Action<ILGenerator, OpCode, int> GetUpdateStackSizeDelegate()
        {
            if (updateStackSizeMethod == null) return null;
            // Cannot use Delegate.CreateDelegate with a method from a non-public declaring type (RuntimeILGenerator).
            // Instead, wrap the call in a DynamicMethod with skipVisibility: true.
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(void), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(ILGenerator), typeof(OpCode), typeof(int) },
                typeof(ExpressionCompiler), skipVisibility: true);
            var il = dynMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);  // ILGenerator (runtime type: DynamicILGenerator : RuntimeILGenerator)
            il.Emit(OpCodes.Ldarg_2);  // OpCode
            il.Emit(OpCodes.Ldarg_3);  // int stackchange
            il.Emit(OpCodes.Call, updateStackSizeMethod);
            il.Emit(OpCodes.Ret);
            return (Action<ILGenerator, OpCode, int>)dynMethod.CreateDelegate(
                typeof(Action<ILGenerator, OpCode, int>), ExpressionCompiler.EmptyArrayClosure);
        }

        public static Func<int, int> Get_DynamicMethod_Emit_Hack()
        {
            if (mLengthFieldAccessor == null || mILStreamAccessor == null || updateStackSizeDelegate == null || getScopeTokens == null)
                return null;

            var meth = MethodStatic1Arg;
            var paramCount = 1;

            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(int) },
                typeof(ExpressionCompiler),
                skipVisibility: true);

            // Ensuring the size of stream upfront, otherwise we would need this code
            // if (mILStream.Length < mLength + 13)
            //     Array.Resize(ref mILStream, Math.Max(mILStream.Length * 2, mLength + 13));
            // Ldarg_1(3) + Call(7) + Ret(3) = 13
            var il = dynMethod.GetILGenerator(16); // todo: @perf #351 how to reuse the mILStream - we may either set it initially or reuse when expanding it

            // current IL stream extension
            // internal void EnsureCapacity(int size)
            // {
            //     if (m_length + size >= m_ILStream.Length)
            //         IncreaseCapacity(size);
            // }
            // private void IncreaseCapacity(int size)
            // {
            //     byte[] temp = new byte[Math.Max(m_ILStream.Length * 2, m_length + size)]; // todo: @perf #351 how to use existing ILStream here
            //     Array.Copy(m_ILStream, temp, m_ILStream.Length);
            //     m_ILStream = temp;
            // }

            ref var mLength = ref mLengthFieldAccessor(il);
            ref var mILStream = ref mILStreamAccessor(il);

            // il.Emit(OpCodes.Ldarg_1);
            mILStream[mLength++] = (byte)OpCodes.Ldarg_1.Value;
            updateStackSizeDelegate(il, OpCodes.Ldarg_1, 1);

            // il.Emit(OpCodes.Call, meth);
            mILStream[mLength++] = (byte)OpCodes.Call.Value;
            updateStackSizeDelegate(il, OpCodes.Call, CalcStackChange(meth, paramCount));

            var mTokens = getScopeTokens(il);
            mTokens.Add(meth.MethodHandle);
            var token = mTokens.Count - 1 | (int)0x06000000; // MetadataTokenType.MethodDef
            BinaryPrimitives.WriteInt32LittleEndian(mILStream.AsSpan(mLength), token);
            mLength += 4;

            // il.Emit(OpCodes.Ret);
            mILStream[mLength++] = (byte)OpCodes.Ret.Value;
            updateStackSizeDelegate(il, OpCodes.Ret, 0);

            return (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        }

#if NET10_0_OR_GREATER
        // In .NET 10+, use UnsafeAccessorType to access the private fields of non-public types directly,
        // without the DynamicMethod-based delegation used in earlier .NET versions.
        // RuntimeILGenerator is the internal base class of DynamicILGenerator that holds the IL stream state.

        /// <summary>Directly accesses m_length on RuntimeILGenerator via UnsafeAccessorType (NET10+).</summary>
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_length")]
        private static extern ref int GetMLength_Net10(
            [UnsafeAccessorType("System.Reflection.Emit.RuntimeILGenerator")] object il);

        /// <summary>Directly accesses m_ILStream on RuntimeILGenerator via UnsafeAccessorType (NET10+).</summary>
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_ILStream")]
        private static extern ref byte[] GetMILStream_Net10(
            [UnsafeAccessorType("System.Reflection.Emit.RuntimeILGenerator")] object il);

        /// <summary>Directly calls UpdateStackSize on RuntimeILGenerator via UnsafeAccessorType (NET10+).</summary>
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdateStackSize")]
        private static extern void UpdateStackSize_Net10(
            [UnsafeAccessorType("System.Reflection.Emit.RuntimeILGenerator")] object il,
            OpCode opcode, int stackchange);

        /// <summary>Directly accesses m_tokens on DynamicScope via UnsafeAccessorType (NET10+).</summary>
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_tokens")]
        private static extern ref List<object> GetMTokens_Net10(
            [UnsafeAccessorType("System.Reflection.Emit.DynamicScope")] object scope);

        public void DynamicMethod_Emit_Hack_Net10()
        {
            var f = Get_DynamicMethod_Emit_Hack_Net10();
            var a = f(41);
            Asserts.AreEqual(42, a);
        }

        /// <summary>
        /// Demonstrates using UnsafeAccessorType (NET10+) to directly access private fields
        /// of non-public types (RuntimeILGenerator, DynamicScope) for fast IL emission.
        /// Replaces the DynamicMethod-based delegation approach used in earlier .NET versions.
        /// </summary>
        public static Func<int, int> Get_DynamicMethod_Emit_Hack_Net10()
        {
            var meth = MethodStatic1Arg;
            var paramCount = 1;

            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(int) },
                typeof(ExpressionCompiler),
                skipVisibility: true);

            var il = dynMethod.GetILGenerator(16);

            // Use UnsafeAccessorType to get refs to the internal IL stream fields directly
            ref var mLength = ref GetMLength_Net10(il);
            ref var mILStream = ref GetMILStream_Net10(il);

            // il.Emit(OpCodes.Ldarg_1);
            mILStream[mLength++] = (byte)OpCodes.Ldarg_1.Value;
            UpdateStackSize_Net10(il, OpCodes.Ldarg_1, 1);

            // il.Emit(OpCodes.Call, meth);
            mILStream[mLength++] = (byte)OpCodes.Call.Value;
            UpdateStackSize_Net10(il, OpCodes.Call, CalcStackChange(meth, paramCount));

            // Access m_scope via reflection (DynamicILGenerator.m_scope returns DynamicScope which is a non-public type,
            // so UnsafeAccessorType cannot currently be used for the return value).
            // Then use UnsafeAccessorType to access m_tokens on the DynamicScope instance directly.
            if (mScopeField == null) return null;
            var scope = mScopeField.GetValue(il);
            ref var mTokens = ref GetMTokens_Net10(scope);
            mTokens.Add(meth.MethodHandle);
            var token = mTokens.Count - 1 | (int)0x06000000; // MetadataTokenType.MethodDef
            BinaryPrimitives.WriteInt32LittleEndian(mILStream.AsSpan(mLength), token);
            mLength += 4;

            // il.Emit(OpCodes.Ret);
            mILStream[mLength++] = (byte)OpCodes.Ret.Value;
            UpdateStackSize_Net10(il, OpCodes.Ret, 0);

            return (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcStackChange(MethodInfo meth, int paramCount)
        {
            var stackChange = 0;
            if (meth.ReturnType != typeof(void))
                stackChange++;
            stackChange -= paramCount;
            if (!meth.IsStatic)
                stackChange--;
            return stackChange;
        }

        
        public void DynamicMethod_Emit_OpCodes_Call()
        {
            var f = Get_DynamicMethod_Emit_OpCodes_Call();
            var a = f(41);
            Asserts.AreEqual(42, a);
        }

        public static Func<int, int> Get_DynamicMethod_Emit_OpCodes_Call()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(int) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Call, MethodStatic1Arg);
            // il.Emit(OpCodes.Call, MethodStaticNoArgs);
            il.Emit(OpCodes.Ret);

            return (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        }

        
        public void DynamicMethod_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Emit_Newobj();
            var a = f();
            Asserts.IsInstanceOf<A>(a);
        }

        
        public void DynamicMethod_Hack_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Hack_Emit_Newobj();
            var a = f();
            Asserts.IsInstanceOf<A>(a);
        }

        public static Func<A> Get_DynamicMethod_Emit_Newobj()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(A), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Newobj, _ctor);
            il.Emit(OpCodes.Ret);

            return (Func<A>)dynMethod.CreateDelegate(typeof(Func<A>), ExpressionCompiler.EmptyArrayClosure);
        }

        public static Func<A> Get_DynamicMethod_Hack_Emit_Newobj()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(A), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();
            var ilType = il.GetType();

            il.Emit(OpCodes.Newobj, _ctor);

            Debug.Assert(_ctor.DeclaringType != null && !_ctor.DeclaringType.IsGenericType);
            // var rtConstructor = con as RuntimeConstructorInfo;
            var methodHandle = _ctor.MethodHandle;

            // m_tokens.Add(rtConstructor.MethodHandle);
            // var tk = m_tokens.Count - 1 | (int)MetadataTokenType.MethodDef;

            var scopeField = ilType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scopeField == null)
                return null;
            var mScope = scopeField.GetValue(il);

            var tokensField = mScope.GetType().GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);
            if (tokensField == null)
                return null;
            var mTokens = tokensField.GetValue(mScope);


            il.Emit(OpCodes.Ret);

            return (Func<A>)dynMethod.CreateDelegate(typeof(Func<A>), ExpressionCompiler.EmptyArrayClosure);
        }

        public class A
        {
            public A() { }

            public static int M() => 42;
            public static int M1(int q) => q + 1;
        }

        private static readonly ConstructorInfo _ctor = typeof(A).GetConstructor(Type.EmptyTypes);
        public static readonly MethodInfo MethodStaticNoArgs = typeof(A).GetMethod(nameof(A.M));
        public static readonly MethodInfo MethodStatic1Arg = typeof(A).GetMethod(nameof(A.M1));
    }
}
#endif