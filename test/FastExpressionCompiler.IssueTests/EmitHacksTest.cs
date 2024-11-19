#if NET8_0_OR_GREATER && !LIGHT_EXPRESSION
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class EmitHacksTest : ITest
    {
        public int Run()
        {
            DynamicMethod_Emit_Hack();
            // DynamicMethod_Emit_Newobj();
            // DynamicMethod_Hack_Emit_Newobj();
            return 3;
        }

        [Test]
        public void DynamicMethod_Emit_Hack()
        {
            var f = Get_DynamicMethod_Emit_Hack();
            var a = f(41);
            Assert.AreEqual(42, a);
        }

        static Type ilType = typeof(ILGenerator).Assembly.GetType("System.Reflection.Emit.DynamicILGenerator");
        static FieldInfo mScopeField = ilType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);

        static Type scopeType = ilType.Assembly.GetType("System.Reflection.Emit.DynamicScope");
        static FieldInfo mTokensField = scopeType.GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);

        static FieldInfo mLengthField = typeof(ILGenerator).GetField("m_length", BindingFlags.Instance | BindingFlags.NonPublic);
        static FieldInfo mILStreamField = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo updateStackSize = typeof(ILGenerator).GetMethod("UpdateStackSize", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Func<ILGenerator, IList<object>> GetScopeTokens()
        {
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
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(TField).MakeByRefType(), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(TFieldHolder) },
                typeof(TFieldHolder), skipVisibility: true);

            var il = dynMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldflda, field);
            il.Emit(OpCodes.Ret);

            return (GetFieldRefDelegate<TFieldHolder, TField>)dynMethod.CreateDelegate(typeof(GetFieldRefDelegate<TFieldHolder, TField>));
        }

        static GetFieldRefDelegate<ILGenerator, int> mLengthFieldAccessor = CreateFieldAccessor<ILGenerator, int>(mLengthField);
        static GetFieldRefDelegate<ILGenerator, byte[]> mILStreamAccessor = CreateFieldAccessor<ILGenerator, byte[]>(mILStreamField);

        static Action<ILGenerator, OpCode, int> updateStackSizeDelegate =
            (Action<ILGenerator, OpCode, int>)Delegate.CreateDelegate(typeof(Action<ILGenerator, OpCode, int>), null, updateStackSize);

        public static Func<int, int> Get_DynamicMethod_Emit_Hack()
        {
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

        [Test]
        public void DynamicMethod_Emit_OpCodes_Call()
        {
            var f = Get_DynamicMethod_Emit_OpCodes_Call();
            var a = f(41);
            Assert.AreEqual(42, a);
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

        [Test]
        public void DynamicMethod_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Emit_Newobj();
            var a = f();
            Assert.IsInstanceOf<A>(a);
        }

        // [Test]
        public void DynamicMethod_Hack_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Hack_Emit_Newobj();
            var a = f();
            Assert.IsInstanceOf<A>(a);
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

            var mScopeField = ilType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mScopeField == null)
                return null;
            var mScope = mScopeField.GetValue(il);

            var mTokensField = mScope.GetType().GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mTokensField == null)
                return null;
            var mTokens = mTokensField.GetValue(mScope);


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