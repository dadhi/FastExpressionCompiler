using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;
#pragma warning disable 649
#pragma warning disable 219

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    // considers in/out/ref in C# represented by ByRef in expressions (i.e. single representation for 3 C# keywords)
    [TestFixture]
    public class Issue55_CompileFast_crash_with_ref_parameter
    {
        delegate TResult FuncRef<T, out TResult>(ref T a1);
        delegate TResult FuncRefIn<T1, in T2, out TResult>(ref T1 a1, T2 a2);
        delegate void ActionRef<T>(ref T a1);
        delegate void ActionRefIn<T1, in T2>(ref T1 obj, T2 value);
        delegate void ActionRefRef<T1, T2>(ref T1 obj, ref T2 value);
        delegate void ActionRefRefRef<T1, T2, T3>(ref T1 obj, ref T2 value, ref T3 ref3);
        delegate TResult FuncRefRef<T1, T2, out TResult>(ref T1 obj, ref T2 value);

        struct StructWithIntField { public int IntField; }

        [Test]
        public void RefDoNothingShouldNoCrash()
        {
            void DoNothing(ref int ignore) { }
            var lambda = Lambda<ActionRef<int>>(Empty(), Parameter(typeof(int).MakeByRefType()));

            void LocalAssert(ActionRef<int> invoke)
            {
                var exampleA = default(int);
                invoke(ref exampleA);
                Assert.AreEqual(0, exampleA);
            }
            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            LocalAssert(compiledB);

            ActionRef<int> direct = DoNothing;
            LocalAssert(direct);
        }

        [Test]
        public void RefDoNothingShouldNoCrashCustomStruct()
        {
            void DoNothing(ref BigInteger ignore) { }
            var lambda = Lambda<ActionRef<BigInteger>>(Empty(), Parameter(typeof(BigInteger).MakeByRefType()));

            void LocalAssert(ActionRef<BigInteger> invoke)
            {
                var exampleA = default(BigInteger);
                invoke(ref exampleA);
                Assert.AreEqual(new BigInteger(0), exampleA);
            }
            var compiledB = lambda.CompileFast<ActionRef<BigInteger>>(true);
            LocalAssert(compiledB);

            ActionRef<BigInteger> direct = DoNothing;
            LocalAssert(direct);
        }

        [Test]
        public void RefFromConstant()
        {
            void SetSmallConstant(ref int localByRef)
            {
                const int objVal = 3;
                localByRef = objVal;
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<ActionRef<int>>(Assign(objRef, Constant(3)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(3, exampleB);

            ActionRef<int> direct = SetSmallConstant;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(3, exampleC);
        }
        private static void SetMinus1(ref int localByRef) { localByRef = -1; }

        [Test]
        public void RefMethodCallingRefMethod()
        {
            void CallOtherRef(ref int localByRef) => SetMinus1(ref localByRef);
            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinus1));
            var lambda = Lambda<ActionRef<int>>(Call(call, objRef), objRef);

            void LocalAssert(ActionRef<int> invoke)
            {
                var exampleA = default(int);
                invoke(ref exampleA);
                Assert.AreEqual(-1, exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            LocalAssert(compiledB);

            ActionRef<int> direct = CallOtherRef;
            LocalAssert(direct);
        }

        private static void SetMinusBigInteger1(ref BigInteger localByRef) { localByRef = new BigInteger(-1); }

        [Test]
        public void RefMethodCallingRefMethodCustomStuct()
        {
            void CallOtherRef(ref BigInteger localByRef) => SetMinusBigInteger1(ref localByRef);
            var objRef = Parameter(typeof(BigInteger).MakeByRefType());
            var variable = Variable(typeof(BigInteger));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinusBigInteger1));
            var lambda = Lambda<ActionRef<BigInteger>>(Call(call, objRef), objRef);

            void LocalAssert(ActionRef<BigInteger> invoke)
            {
                var exampleA = default(BigInteger);
                invoke(ref exampleA);
                Assert.AreEqual(new BigInteger(-1), exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<BigInteger>>(true);
            LocalAssert(compiledB);

            ActionRef<BigInteger> direct = CallOtherRef;
            LocalAssert(direct);
        }

        [Test]
        public void RefMethodCallingRefMethodWithLocal()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal = localByRef;
                SetMinus1(ref objVal);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinus1));
            var lambda = Lambda<ActionRef<int>>(Block(new[] { variable }, Assign(variable, objRef), Call(call, variable)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(0, exampleB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(0, exampleC);
        }
        private static void OutSetMinus1(out int localByRef) { localByRef = -1; }

        [Test]
        public void OutRefMethodCallingRefMethodWithLocal()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal = localByRef;
                OutSetMinus1(out objVal);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(OutSetMinus1));
            var lambda = Lambda<ActionRef<int>>(Block(new[] { variable }, Assign(variable, objRef), Call(call, variable)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(0, exampleB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(0, exampleC);
        }



        private static void Set1AndMinus1(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1; }

        [Test]
        public void RefMethodCallingRefMethodWithLocalReturnLocalCalled()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal1 = localByRef;
                var objVal2 = localByRef;
                Set1AndMinus1(ref localByRef, ref objVal2);
                return objVal2;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set1AndMinus1));
            var lambda = Lambda<FuncRef<int, int>>(Block(new[] { variable1, variable2 },
                                                    Assign(variable1, objRef),
                                                    Assign(variable2, objRef),
                                                    Call(call, objRef, variable2),
                                                    variable2
                                                    ),
                                                objRef);

            void LocalAssert(FuncRef<int, int> invoke)
            {
                var exampleA = default(int);
                Assert.AreEqual(-1, invoke(ref exampleA));
                Assert.AreEqual(1, exampleA);
            }

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            LocalAssert(compiledB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }

        private static void SetVariableOneAndMinusForParameter(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1; }

        [Test]
        public void VariableVariableRefVariableRefParameterReturn()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var x = localByRef;
                var z = localByRef;
                SetVariableOneAndMinusForParameter(ref x, ref localByRef);
                return 1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetVariableOneAndMinusForParameter));
            var lambda = Lambda<FuncRef<int, int>>(Block(new ParameterExpression[] { variable1, variable2 },
                                                    Assign(variable1, objRef),
                                                    Assign(variable2, objRef),
                                                    Call(call, variable1, objRef),
                                                    Constant(1)
                                                    ),
                                                objRef);

            void LocalAssert(FuncRef<int, int> invoke)
            {
                var exampleA = default(int);
                Assert.AreEqual(1, invoke(ref exampleA));
                Assert.AreEqual(-1, exampleA);
            }

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            LocalAssert(compiledB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }

        private static void Set123(ref long ref1, ref byte ref2, ref short ref3) { ref1 = 1; ref2 = 2; ref3 = 3; }



        [Test]
        public void Ref1Ref2Ref3()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref long ref1, ref byte ref2, ref short ref3)
            {
                Set123(ref ref1, ref ref2, ref ref3);
            }

            var ref1E = Parameter(typeof(long).MakeByRefType());
            var ref2E = Parameter(typeof(byte).MakeByRefType());
            var ref3E = Parameter(typeof(short).MakeByRefType());

            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set123));
            var lambda = Lambda<ActionRefRefRef<long, byte, short>>(Call(call, ref1E, ref2E, ref3E), ref1E, ref2E, ref3E);


            void AssertLocal(ActionRefRefRef<long, byte, short> sut)
            {
                var example1 = default(long);
                var example2 = default(byte);
                var example3 = default(short);
                sut(ref example1, ref example2, ref example3);
                Assert.AreEqual(1, example1);
                Assert.AreEqual(2, example2);
                Assert.AreEqual(3, example3);
            }


            var compiledB = lambda.CompileFast<ActionRefRefRef<long, byte, short>>(true);
            AssertLocal(compiledB);

            ActionRefRefRef<long, byte, short> direct = SetIntoLocalVariableAndCallOtherRef;
            AssertLocal(direct);
        }

        private static void SetMinusOneAndOneForDoubleRefParameterInCallCall(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1; }

        [Test]
        public void SetMinusOneAndOneForDoubleRefParameterInCall()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                Set1AndMinus1(ref localByRef, ref localByRef);
                return -1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinusOneAndOneForDoubleRefParameterInCallCall));
            var lambda = Lambda<FuncRef<int, int>>(Block(new ParameterExpression[] { },
                                                    Call(call, objRef, objRef),
                                                    Constant(-1)
                                                    ),
                                                objRef);

            void LocalAssert(FuncRef<int, int> invoke)
            {
                var exampleA = default(int);
                Assert.AreEqual(-1, invoke(ref exampleA));
                Assert.AreEqual(1, exampleA);
            }

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            LocalAssert(compiledB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }

        private static void AsValueAndSetMinusOneAsRefCall(int ref1, ref int ref2) { ref2 = -1; }

        [Test]
        public void AsValueAndAsRef()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                AsValueAndSetMinusOneAsRefCall(localByRef, ref localByRef);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());

            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(AsValueAndSetMinusOneAsRefCall));
            var lambda = Lambda<ActionRef<int>>(Call(call, objRef, objRef), objRef);

            void LocalAssert(ActionRef<int> invoke)
            {
                var exampleA = default(int);
                invoke(ref exampleA);
                Assert.AreEqual(-1, exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            LocalAssert(compiledB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }

        [Test]
        public void GenericRefFromConstant()
        {
            void SetSmallConstant(ref byte localByRef)
            {
                const byte objVal = 3;
                localByRef = objVal;
            }
            var objRef = Parameter(typeof(byte).MakeByRefType());
            var lambda = Lambda<ActionRef<byte>>(Assign(objRef, Constant((byte)3)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<byte>>(true);
            var exampleB = default(byte);
            compiledB(ref exampleB);
            Assert.AreEqual(3, exampleB);

            ActionRef<byte> direct = SetSmallConstant;
            var exampleC = default(byte);
            direct(ref exampleC);
            Assert.AreEqual(3, exampleC);
        }

        [Test]
        public void GenericRefFromConstantReturn()
        {
            ushort SetSmallConstant(ref uint localByRef)
            {
                localByRef = 3;
                return 7;
            }
            var objRef = Parameter(typeof(uint).MakeByRefType());
            var lambda = Lambda<FuncRef<uint, ushort>>(Block(Assign(objRef, Constant((uint)3)), Constant((ushort)7)), objRef);

            var compiledB = lambda.CompileFast<FuncRef<uint, ushort>>(true);
            var exampleB = default(uint);
            Assert.AreEqual(7, compiledB(ref exampleB));
            Assert.AreEqual(3, exampleB);

            FuncRef<uint, ushort> direct = SetSmallConstant;
            var exampleC = default(uint);
            Assert.AreEqual(7, direct(ref exampleC));
            Assert.AreEqual(3, exampleC);
        }



        [Test]
        public void BlockWithNonRefStatementLast()
        {
            void SetSmallConstant(ref uint localByRef)
            {
                localByRef = 3;
                var x = 0.0;
            }
            var objRef = Parameter(typeof(uint).MakeByRefType());
            var variable = Variable(typeof(double));
            var lambda = Lambda<ActionRef<uint>>(Block(new[] { variable }, Assign(objRef, Constant((uint)3)), Assign(variable, Constant(0.0))), objRef);

            var compiledB = lambda.CompileFast<ActionRef<uint>>(true);
            var exampleB = default(uint);
            compiledB(ref exampleB);
            Assert.AreEqual(3, exampleB);

            ActionRef<uint> direct = SetSmallConstant;
            var exampleC = default(uint);
            direct(ref exampleC);
            Assert.AreEqual(3, exampleC);
        }

        [Test]
        public void RefReturnToVoid()
        {
            ushort SetSmallConstant(ref uint localByRef)
            {
                localByRef = 3;
                return 7;
            }
            var objRef = Parameter(typeof(uint).MakeByRefType());
            var lambda = Lambda<ActionRef<uint>>(Block(Assign(objRef, Constant((uint)3)), Constant((ushort)7)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<uint>>(true);
            var exampleB = default(uint);
            compiledB(ref exampleB);
            Assert.AreEqual(3, exampleB);

            ActionRef<uint> direct = (ref uint x) => SetSmallConstant(ref x);
            var exampleC = default(uint);
            direct(ref exampleC);
            Assert.AreEqual(3, exampleC);
        }

        [Test]
        public void RefRefVoid()
        {
            void SetSmallConstant(ref int a1, ref float a2)
            {
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objRef2 = Parameter(typeof(float).MakeByRefType());
            var lambda = Lambda<ActionRefRef<int, float>>(Constant(default(object)), objRef, objRef2);

            var compiledB = lambda.CompileFast<ActionRefRef<int, float>>(true);
            var exampleB = default(int);
            var exampleB2 = default(float);
            compiledB(ref exampleB, ref exampleB2);
            Assert.AreEqual(0, exampleB);

            ActionRefRef<int, float> direct = SetSmallConstant;
            var exampleC = default(int);
            var exampleC2 = default(float);
            direct(ref exampleC, ref exampleC2);
            Assert.AreEqual(0, exampleC);
        }


        [Test]
        public void RefRefReturnToVoid()
        {
            string SetSmallConstant(ref int a1, ref float a2)
            {
                return default(string);
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objRef2 = Parameter(typeof(float).MakeByRefType());
            var lambda = Lambda<ActionRefRef<int, float>>(Constant(default(object)), objRef, objRef2);

            var compiledB = lambda.CompileFast<ActionRefRef<int, float>>(true);
            var exampleB = default(int);
            var exampleB2 = default(float);
            compiledB(ref exampleB, ref exampleB2);
            Assert.AreEqual(0, exampleB);

            ActionRefRef<int, float> direct = (ref int x, ref float y) => SetSmallConstant(ref x, ref y);
            var exampleC = default(int);
            var exampleC2 = default(float);
            direct(ref exampleC, ref exampleC2);
            Assert.AreEqual(0, exampleC);
        }

#if !LIGHT_EXPRESSION
        [Test]
        public void IntPtrZeroReturn()
        {
            Expression<Func<IntPtr>> lambda = () => IntPtr.Zero;
            var compiled = lambda.CompileFast<Func<IntPtr>>(true);
            Assert.AreEqual(IntPtr.Zero, compiled());
        }

        [Test]
        public void NewIntPtr13Return()
        {
            Expression<Func<IntPtr>> lambda = () => new IntPtr(13);
            var compiled = lambda.CompileFast<Func<IntPtr>>(true);
            Assert.AreEqual(new IntPtr(13), compiled());
        }
#endif

        [Test]

        public void RefSetSetForFields()
        {
            UIntPtr Set2RefsWithPtrAndNewObject(ref IntPtr a1, ref object a2)
            {
                a1 = IntPtr.Zero;
                a2 = new object();
                return UIntPtr.Zero;
            }
            var objRef1 = Parameter(typeof(IntPtr).MakeByRefType());
            var objRef2 = Parameter(typeof(object).MakeByRefType());
            var intPtrZero = typeof(IntPtr).GetTypeInfo().DeclaredFields.First(m => m.Name == nameof(IntPtr.Zero));
            var uIntPtrZero = typeof(UIntPtr).GetTypeInfo().DeclaredFields.First(m => m.Name == nameof(UIntPtr.Zero));

            var lambda = Lambda<FuncRefRef<IntPtr, object, UIntPtr>>(Block(
                                                                Assign(objRef1, Field(null, intPtrZero)),
                                                                Assign(objRef2, New(typeof(object))),
                                                                Field(null, uIntPtrZero)
                                                            ), objRef1, objRef2
                                                       );

            var compiledB = lambda.CompileFast<FuncRefRef<IntPtr, object, UIntPtr>>(true);
            var exampleB = default(IntPtr);
            var exampleB2 = default(object);
            compiledB(ref exampleB, ref exampleB2);
            Assert.IsNotNull(exampleB2);

            var exampleC = default(IntPtr);
            var exampleC2 = default(object);
            Set2RefsWithPtrAndNewObject(ref exampleC, ref exampleC2);
            Assert.IsNotNull(exampleC2);
        }

        [Test]
        public void RefDoNothingReturnCostant()
        {
            int DoNothing(ref int localByRef) => default(int);
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<FuncRef<int, int>>(Constant(default(int)), objRef);

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            var exampleB = default(int);
            Assert.AreEqual(0, compiledB(ref exampleB));
            Assert.AreEqual(0, exampleB);

            FuncRef<int, int> direct = DoNothing;
            var exampleC = default(int);
            Assert.AreEqual(0, direct(ref exampleC));
            Assert.AreEqual(0, exampleC);
        }

        [Test]
        public void RefFromLargeConstant()
        {
            void SetSmallConstant(ref int localByRef)
            {
                const int objVal = 42_123_666;
                localByRef = objVal;
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<ActionRef<int>>(Assign(objRef, Constant(42_123_666)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(42_123_666, exampleB);

            ActionRef<int> direct = SetSmallConstant;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(42_123_666, exampleC);
        }

        [Test]
        public void RefSetFromParameter()
        {
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var body = Assign(objRef, objVal);
            var lambda = Lambda<ActionRefIn<int, int>>(body, objRef, objVal);

            var compiledB = lambda.CompileFast<ActionRefIn<int, int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB, 7);
            Assert.AreEqual(7, exampleB);
        }

        [Test]
        public void NonGenericSetterFieldShould_not_crash()
        {
            var objRef = Parameter(typeof(StructWithIntField).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var prop = PropertyOrField(objRef, nameof(StructWithIntField.IntField));
            var body = Assign(prop, objVal);
            var lambda = Lambda<ActionRefIn<StructWithIntField, int>>(body, objRef, objVal);

            var fastCompiled = lambda.CompileFast<ActionRefIn<StructWithIntField, int>>(true);
            var exampleB = default(StructWithIntField);
            fastCompiled(ref exampleB, 7);
            Assert.AreEqual(7, exampleB.IntField);
        }


        [Test]
        public void GenericRefStructFieldShould_not_crash()
        {
            var objRef = Parameter(typeof(StructWithIntField).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var prop = PropertyOrField(objRef, nameof(StructWithIntField.IntField));
            var body = Assign(prop, objVal);
            var lambda = Lambda<ActionRefIn<StructWithIntField, int>>(body, objRef, objVal);

            var compiledB = lambda.CompileFast<ActionRefIn<StructWithIntField, int>>(true);
            var exampleB = default(StructWithIntField);
            compiledB(ref exampleB, 7);
            Assert.AreEqual(7, exampleB.IntField);
        }

        [Test]
        public void RefAssign()
        {
            void AddSet(ref double byRef)
            {
                byRef = byRef + 3.0;
            }
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(Assign(objRef, Add(objRef, Constant(3.0))), objRef);

            void LocalAssert(ActionRef<double> invoke)
            {
                var exampleA = 5.0;
                invoke(ref exampleA);
                Assert.AreEqual(8.0, exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<double>>(true);
            LocalAssert(compiledB);

            ActionRef<double> direct = AddSet;
            LocalAssert(direct);
        }

        [Test]
        public void RefAssignCustomStruct()
        {
            void AddSet(ref BigInteger byRef)
            {
                byRef = byRef + new BigInteger(3);
            }
            var objRef = Parameter(typeof(BigInteger).MakeByRefType());
            var ctor = typeof(BigInteger).GetTypeInfo().DeclaredConstructors
                .Single(x => x.GetParameters().Count() == 1 && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(int));
            var lambda = Lambda<ActionRef<BigInteger>>(Assign(objRef, Add(objRef, New(ctor, Constant(3)))), objRef);

            void LocalAssert(ActionRef<BigInteger> invoke)
            {
                BigInteger exampleA = 5;
                invoke(ref exampleA);
                Assert.AreEqual(new BigInteger(8), exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<BigInteger>>(true);
            LocalAssert(compiledB);

            ActionRef<BigInteger> direct = AddSet;
            LocalAssert(direct);
        }

        [Test]
        public void CoalesceIntoByRef()
        {
            void DynamicDeserializer(ref object value) =>
                value = value ?? new object();

            void DynamicDeserializerGeneric<T>(ref T value) where T : class, new() =>
                value = value ?? new T();

            var specificType = typeof(object);
            var valueRef = Parameter(typeof(object).MakeByRefType(), "value");
            var newObj = Coalesce(valueRef, New(specificType.GetConstructor(Type.EmptyTypes)));
            var xVar = Variable(specificType, "x");
            var lambda = Lambda<ActionRef<object>>(
                    Block(new[] { xVar }, Assign(valueRef, newObj)),
                    valueRef
                );

            var compiledFast = lambda.CompileFast(true);

            void LocalAssert(ActionRef<object> invoke)
            {
                var obj = default(object);
                invoke(ref obj);
                Assert.AreNotEqual(default(object), obj);
            }

            LocalAssert(DynamicDeserializer);
            LocalAssert(DynamicDeserializerGeneric);
            LocalAssert(compiledFast);
        }

        [Test]
        public void ConstantOutInCondition()
        {
            int TryParseCondition()
            {
                int intValue;
                return int.TryParse("123", out intValue) ? intValue : default(int);
            }

            var intValueParameter = Parameter(typeof(int), "intValue");
            var tryParseMethod = typeof(int)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "TryParse" && m.GetParameters().Length == 2);
            var tryParseCall = Call(
                tryParseMethod,
                Constant("123", typeof(string)),
                intValueParameter);
            var parsedValueOrDefault = Condition(
                tryParseCall,
                intValueParameter,
                Default(typeof(int)));
            var conditionBlock = Block(new[] { intValueParameter }, parsedValueOrDefault);
            var conditionLambda = Lambda<Func<int>>(conditionBlock);

            var conditionFunc = conditionLambda.CompileSys();
            var conditionFuncFast = conditionLambda.CompileFast();

            void LocalAssert(Func<int> invoke)
            {
                var x = invoke();
                Assert.AreEqual(123, x);
            }
            LocalAssert(conditionFunc);
            LocalAssert(conditionFuncFast);
            LocalAssert(TryParseCondition);
        }

        [Test]
        public void ConstantOut()
        {
            int TryParseReturn()
            {
                int intValue;
                int.TryParse("123", out intValue);
                return intValue;
            }

            var intValueParameter = Parameter(typeof(int), "intValue");
            var tryParseMethod = typeof(int)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "TryParse" && m.GetParameters().Length == 2);
            var tryParseCall = Call(
                tryParseMethod,
                Constant("123", typeof(string)),
                intValueParameter);
            var conditionBlock = Block(new[] { intValueParameter }, tryParseCall, intValueParameter);
            var conditionLambda = Lambda<Func<int>>(conditionBlock);

            void LocalAssert(Func<int> invoke) => Assert.AreEqual(123, invoke());
            var func = conditionLambda.CompileSys();
            var funcFast = conditionLambda.CompileFast();

            LocalAssert(func);
            LocalAssert(funcFast);
            LocalAssert(TryParseReturn);
        }
    }
}
