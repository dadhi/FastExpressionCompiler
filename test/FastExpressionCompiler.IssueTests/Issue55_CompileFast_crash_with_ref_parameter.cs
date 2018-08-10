using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using NUnit.Framework;
using System.Collections.Generic;

namespace FastExpressionCompiler.IssueTests
{
    // TODO:
    // 1. multi ref set values
    // 2. `out` (out must be set - validate?)
    // 3. IntPtr and object ref tests
    // 4. check support of `in` https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-parameter-modifier
    [TestFixture]
    public class Issue55_CompileFast_crash_with_ref_parameter
    {
        delegate TResult FuncRef<T, out TResult>(ref T a1);
        delegate TResult FuncRefIn<T1, in T2, out TResult>(ref T1 a1, T2 a2);
        delegate void ActionRef<T>(ref T a1);
        delegate void ActionRefIn<T1, in T2>(ref T1 obj, T2 value);
        delegate void ActionRefRef<T1, T2>(ref T1 obj, ref T2 value);

        struct StructWithIntField { public int IntField; }

        [Test]
        public void RefDoNothingShouldNoCrash()
        {
            void DoNothing(ref int igonre) { };
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

            var compiledA = lambda.Compile();
            var exampleA = default(StructWithIntField);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA.IntField);

            var compiledB = lambda.CompileFast<ActionRefIn<StructWithIntField, int>>(true);
            Assert.IsNull(compiledB);
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
            Assert.IsNull(compiledB);
        }
    }
}
