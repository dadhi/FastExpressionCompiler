using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using NUnit.Framework;
using System.Collections.Generic;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue55_CompileFast_crash_with_ref_parameter
    {
        delegate void VoidReturnByRefIntIn(ref int obj);
        delegate int IntRetrunByRefIntInt(ref int it);
        delegate void VoidReturnByRefIntAndOtherIntIn(ref int obj, int val);
        delegate void VoidReturnByRefTAndOtherIntIn<T>(ref T obj, int value);
        struct StructWithIntField { public int IntField; }
        delegate void VoidReturnByRefStructWithIntFieldAndIntIn(ref StructWithIntField obj, int value);



        [Test]
        public void RefDoNothingShouldNoCrash()
        {
            void DoNothing(ref int igonre) { };
            var lambda = Lambda<VoidReturnByRefIntIn>(Empty(), Parameter(typeof(int).MakeByRefType()));

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(0, exampleA);

            var compiledB = lambda.CompileFast<VoidReturnByRefIntIn>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(0, exampleB);

            VoidReturnByRefIntIn direct = DoNothing;
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
            var lambda = Lambda<VoidReturnByRefIntIn>(Assign(objRef, Constant(7)), objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(3, exampleA);

            var compiledB = lambda.CompileFast<VoidReturnByRefIntIn>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(3, exampleB);

            VoidReturnByRefIntIn direct = SetSmallConstant;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(3, exampleC);
        }

        [Test]
        //[Ignore("Fails on IsNotNull")]
        public void ConstanRetrunIsSupported()
        {
            var varr = Variable(typeof(int), "xxx");
            var assign = Assign(varr, Constant(7));
            var lambda = Lambda<Func<int>>(Block(new List<ParameterExpression> { varr }, assign, Label(Label(typeof(int)), varr)));
            var compiled = lambda.Compile();
            var value1 = compiled();
            Assert.AreEqual(7, value1);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
        }

        [Test]
        public void RefFromConst21ant()
        {


            int SetSmallConstant(ref int localByRef) => default(int);
            var objRef = Parameter(typeof(int).MakeByRefType());
            var ret = Block(Label(Label(typeof(int)), Constant(default(int))));
            var lambda = Lambda<IntRetrunByRefIntInt>(ret, objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            Assert.AreEqual(0, compiledA(ref exampleA));
            Assert.AreEqual(0, exampleA);

            var compiledB = lambda.CompileFast<IntRetrunByRefIntInt>(true);
            var exampleB = default(int);
            Assert.AreEqual(0, compiledB(ref exampleB));
            Assert.AreEqual(0, exampleB);

            IntRetrunByRefIntInt direct = SetSmallConstant;
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
            var lambda = Lambda<VoidReturnByRefIntIn>(Assign(objRef, Constant(42_123_666)), objRef);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA);
            Assert.AreEqual(42_123_666, exampleA);

            var compiledB = lambda.CompileFast<VoidReturnByRefIntIn>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Assert.AreEqual(42_123_666, exampleB);

            VoidReturnByRefIntIn direct = SetSmallConstant;
            var exampleC = default(int);
            direct(ref exampleC);
            Assert.AreEqual(42_123_666, exampleC);
        }



        // will retain next methods under well defined names
        // 1. newcomvers may look into il
        // 2. may call thes for test comparison
        // 3. as documentation of what is covered


        private void Set1124144112Constant(ref int objRef)
        {
            const int objVal = 1124144112;
            objRef = objVal;
        }

        private static void SetXXXXXConstant(ref int objRef)
        {
            const int objVal = 1124144112;
            objRef = objVal;
        }

        private void Set1124144112Constant(ref int objRef, ref int objRef2)
        {
            const int objVal = 1124144112;
            objRef = objVal;
            objRef2 = 7;
        }

        private int ReadIntoVar(ref int objRef)
        {
            var read = objRef;
            return read;
        }

        private void ReadIntoVassr(int objRef)
        {
            var x = 0;
        }

        private int ReadIntoVas131231231312sr(int objRef)
        {
            var x = objRef;
            return x;
        }




        [Test]
        public void RefSetFromParameter()
        {
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var body = Assign(objRef, objVal);
            var lambda = Lambda<VoidReturnByRefIntAndOtherIntIn>(body, objRef, objVal);

            var compiledA = lambda.Compile();
            var exampleA = default(int);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA);

            var compiledB = lambda.CompileFast<VoidReturnByRefIntAndOtherIntIn>(true);
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
            var lambda = Lambda<VoidReturnByRefStructWithIntFieldAndIntIn>(body, objRef, objVal);

            var compiledA = lambda.Compile();
            var exampleA = default(StructWithIntField);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA.IntField);

            var compiledB = lambda.CompileFast<VoidReturnByRefStructWithIntFieldAndIntIn>(true);
            Assert.IsNull(compiledB);
        }


        [Test]
        public void GenericRefStructFieldShould_not_crash()
        {
            var objRef = Parameter(typeof(StructWithIntField).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var prop = PropertyOrField(objRef, nameof(StructWithIntField.IntField));
            var body = Assign(prop, objVal);
            var lambda = Lambda<VoidReturnByRefTAndOtherIntIn<StructWithIntField>>(body, objRef, objVal);

            var compiledA = lambda.Compile();
            var exampleA = default(StructWithIntField);
            compiledA(ref exampleA, 7);
            Assert.AreEqual(7, exampleA.IntField);

            var compiledB = lambda.CompileFast<VoidReturnByRefTAndOtherIntIn<StructWithIntField>>(true);
            Assert.IsNull(compiledB);
        }
    }
}
