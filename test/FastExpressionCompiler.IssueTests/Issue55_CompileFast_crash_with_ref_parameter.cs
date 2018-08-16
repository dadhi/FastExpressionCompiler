using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
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
        delegate TResult FuncRefRef<T1, T2, out TResult>(ref T1 obj, ref T2 value);

        struct StructWithIntField { public int IntField; }

        [Test]
        public void RefDoNothingShouldNoCrash()
        {
            void DoNothing(ref int ignore) { };
            var lambda = Lambda<ActionRef<int>>(Empty(), Parameter(typeof(int).MakeByRefType()));

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(0, exampleA);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(0, exampleB);

            ActionRef<int> direct = DoNothing;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(0, exampleC);
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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(3, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(-1, exampleA);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(-1, exampleB);

            ActionRef<int> direct = CallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(-1, exampleC);
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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(0, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(0, exampleA);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(0, exampleB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(0, exampleC);
        }



        private static void Set1AndMinus1(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1;  }

        [Test]
        public void RefMethodCallingRefMethodWithLocal2()
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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            Assert.AreEqual(-1, compiledA(ref exampleA));
            Assert.AreEqual(1, exampleA);

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            var exampleB = default(int);
            Assert.AreEqual(-1, compiledB(ref exampleB));
            Assert.AreEqual(1, exampleB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            Assert.AreEqual(-1, direct(ref exampleC));
            Assert.AreEqual(1, exampleC);
        }

        [Test]
        public void RefMethodCallingRefMethodWithLoc123123al2()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal1 = localByRef;
                var objVal2 = localByRef;
                Set1AndMinus1(ref localByRef, ref objVal2);
                return -1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set1AndMinus1));
            var lambda = Lambda<FuncRef<int, int>>(Block(new[] { variable1, variable2 },
                                                    Assign(variable1, objRef),
                                                    Assign(variable2, objRef),
                                                    Call(call, objRef, variable2),
                                                    Constant(-1)
                                                    ),
                                                objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            Assert.AreEqual(-1, compiledA(ref exampleA));
            Assert.AreEqual(1, exampleA);

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            var exampleB = default(int);
            Assert.AreEqual(-1, compiledB(ref exampleB));
            Assert.AreEqual(1, exampleB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            Assert.AreEqual(-1, direct(ref exampleC));
            Assert.AreEqual(1, exampleC);
        }


        [Test]
        public void AAARefMeasdsdasdthodCallingRefMeth1312331231231231WithLoc123123al2()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var x = localByRef;
                var z = localByRef;
                Set1AndMinus1(ref x, ref localByRef);
                return -1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set1AndMinus1));
            var lambda = Lambda<FuncRef<int, int>>(Block(new ParameterExpression[] { variable1, variable2 },
                                                    Assign(variable1, objRef),
                                                    Assign(variable2, objRef),
                                                    Call(call, variable1, objRef),
                                                    Constant(-1)
                                                    ),
                                                objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            Assert.AreEqual(-1, compiledA(ref exampleA));
            Assert.AreEqual(1, exampleA);

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            var exampleB = default(int);
            Assert.AreEqual(-1, compiledB(ref exampleB));
            Assert.AreEqual(1, exampleB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            Assert.AreEqual(-1, direct(ref exampleC));
            Assert.AreEqual(1, exampleC);
        }

        [Test]
        public void AAARefMethodCallingRefMeth1312331231231231WithLoc123123al2()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                Set1AndMinus1(ref localByRef, ref localByRef);
                return -1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set1AndMinus1));
            var lambda = Lambda<FuncRef<int, int>>(Block(new ParameterExpression[] { },
                                                    Call(call, objRef, objRef),
                                                    Constant(-1)
                                                    ),
                                                objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            Assert.AreEqual(-1, compiledA(ref exampleA));
            Assert.AreEqual(1, exampleA);

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            var exampleB = default(int);
            Assert.AreEqual(-1, compiledB(ref exampleB));
            Assert.AreEqual(1, exampleB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            Assert.AreEqual(-1, direct(ref exampleC));
            Assert.AreEqual(1, exampleC);
        }

        private static void Set1AndMinus122(int ref1, ref int ref2) { ref2 = -1; }

        [Test]
        public void RefMethodCallingRefMethodWithLocal3()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                Set1AndMinus122(localByRef, ref localByRef);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());

            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set1AndMinus122));
            var lambda = Lambda<ActionRef<int>>(Call(call, objRef, objRef), objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(-1, exampleA);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(-1, exampleB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(-1, exampleC);
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

            var compiledA = lambda.Compile();
            var exampleA = default(byte);
            compiledA(ref exampleA);
            Assert.AreEqual(3, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(uint);
            Assert.AreEqual(7, compiledA(ref exampleA));
            Assert.AreEqual(3, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(uint);
            compiledA(ref exampleA);
            Assert.AreEqual(3, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(uint);
            compiledA(ref exampleA);
            Assert.AreEqual(3, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            var exampleA2 = default(float);
            compiledA(ref exampleA, ref exampleA2);
            Assert.AreEqual(0, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            var exampleA2 = default(float);
            compiledA(ref exampleA, ref exampleA2);
            Assert.AreEqual(0, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(IntPtr);
            var exampleA2 = default(object);
            compiledA(ref exampleA, ref exampleA2);
            Assert.IsNotNull(exampleA2);

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
        [Ignore("Maksim V. - may think about the case - should we support WRONG trees")]
        public void ConstantFromStaticField()
        {
            // WRONG - not constant, but still works with LINQ, just do not support `wrong` in FEC, 
            // C# generates for Zero proper ldsfld with static call
            var lambda = Lambda<Func<IntPtr>>(Block(Constant(IntPtr.Zero)));
            var compiledA = lambda.Compile();
            Assert.AreEqual(IntPtr.Zero, compiledA());
            var compiledB = lambda.CompileFast<Func<IntPtr>>(true);
            Assert.IsNull(compiledB);
        }

        [Test]
        public void RefDoNothingReturnCostant()
        {
            int DoNothing(ref int localByRef) => default(int);
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<FuncRef<int, int>>(Constant(default(int)), objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            Assert.AreEqual(0, compiledA(ref exampleA));
            Assert.AreEqual(0, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(42_123_666, exampleA);

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

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA);

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

            //var compiledA = lambda.Compile();
            //var exampleA = default(StructWithIntField);
            //compiledA(ref exampleA, 7);
            //Assert.AreEqual(7, exampleA.IntField);

            var compiledB = lambda.CompileFast<ActionRefIn<StructWithIntField, int>>(true);
            var exampleB = default(StructWithIntField);
            compiledB(ref exampleB, 7);
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

            var compiledA = lambda.Compile();
            var exampleA = default(StructWithIntField);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA.IntField);

            var compiledB = lambda.CompileFast<ActionRefIn<StructWithIntField, int>>(true);
            var exampleB = default(StructWithIntField);
            compiledB(ref exampleB, 7);
            Assert.AreEqual(7, exampleB.IntField);
        }
    }
}
