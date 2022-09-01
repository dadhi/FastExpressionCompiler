#if !LIGHT_EXPRESSION && !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Buffers.Binary;
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
            return 2;
        }

        [Test]
        public void DynamicMethod_Emit_Hack()
        {
            var f = Get_DynamicMethod_Emit_Hack();
            var a = f();
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

        private static Func<ILGenerator, int, int> IncLength()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure), typeof(ILGenerator), typeof(int) },
                typeof(ExpressionCompiler), skipVisibility: true);
            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, mLengthField);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stfld, mLengthField);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            return (Func<ILGenerator, int, int>)dynMethod.CreateDelegate(typeof(Func<ILGenerator, int, int>), ExpressionCompiler.EmptyArrayClosure);
        }
        static readonly Func<ILGenerator, int, int> incLength = IncLength();

        static Action<ILGenerator, OpCode, int> updateStackSizeDelegate =
            (Action<ILGenerator, OpCode, int>)Delegate.CreateDelegate(typeof(Action<ILGenerator, OpCode, int>), null, updateStackSize);

        public static Func<int> Get_DynamicMethod_Emit_Hack()
        {
            var opCode = OpCodes.Call;
            var meth = MethodStaticNoArgs;
            var paramCount = 0;

            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            // var mScope = mScopeField.GetValue(il);
            // var mTokens = (IList<object?>)mTokensField.GetValue(mScope);
            var mTokens = getScopeTokens(il);
            mTokens.Add(meth.MethodHandle);

            var token = mTokens.Count - 1 | (int)0x06000000; // MetadataTokenType.MethodDef

            // todo: @perf read field of int
            var mLength = (int)mLengthField.GetValue(il);
            // var mLength = incLength(il, 5);

            // todo: @perf read field if bytes array
            var mILStream = (byte[])mILStreamField.GetValue(il);
            if (mILStream.Length < mLength + 7)
                Array.Resize(ref mILStream, Math.Max(mILStream.Length * 2, mLength + 7));

            mILStream[mLength++] = (byte)opCode.Value;

            // todo: @wip  we don't need it as the value set again later
            // mLengthField.SetValue(il, mLength);
            // todo: @wip check that we need this
            // updateStackSize.Invoke(il, new object[] { opCode, 0 });

            var stackExchange = CalcStackChange(meth, paramCount);

            // todo: @perf call method
            // updateStackSize.Invoke(il, new object[] { opCode, stackExchange });
            updateStackSizeDelegate(il, opCode, stackExchange);

            BinaryPrimitives.WriteInt32LittleEndian(mILStream.AsSpan(mLength), token);

            // todo: @perf sets the value of int
            mLengthField.SetValue(il, mLength + 4);

            il.Emit(OpCodes.Ret);

            return (Func<int>)dynMethod.CreateDelegate(typeof(Func<int>), ExpressionCompiler.EmptyArrayClosure);
        }

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
            var a = f();
            Assert.AreEqual(42, a);
        }

        public static Func<int> Get_DynamicMethod_Emit_OpCodes_Call()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(int), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Call, MethodStaticNoArgs);
            il.Emit(OpCodes.Ret);

            return (Func<int>)dynMethod.CreateDelegate(typeof(Func<int>), ExpressionCompiler.EmptyArrayClosure);
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
        }

        private static readonly ConstructorInfo _ctor = typeof(A).GetConstructor(Type.EmptyTypes);
        public static readonly MethodInfo MethodStaticNoArgs = typeof(A).GetMethod(nameof(A.M));
    }
}
#endif