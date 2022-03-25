using System;
using NUnit.Framework;
using SysExpr = System.Linq.Expressions;

#pragma warning disable CS0649

#if LIGHT_EXPRESSION
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class UnaryExpressionTests : ITest
    {
        public int Run()
        {
            ArrayLength_compiles();
            Convert_compiles();
            ConvertChecked_compiles();
            Increment_Constant_compiles();
            Decrement_compiles();
            Increment_compiles();
            IsFalse_compiles();
            IsTrue_compiles();
            MakeUnary_compiles();
            Negate_compiles();
            NegateChecked_compiles();
            Binary_Not_compiles();
            OnesComplement_compiles();
            PostDecrementAssign_compiles();
            Parameter_PostIncrementAssign_compiles();

            RefParameter_PostIncrementAssign_works();
            ArrayParameter_PostIncrementAssign_works();
            ArrayParameterByRef_PostIncrementAssign_works();
            ArrayItemRefParameter_PostIncrementAssign_works();

            ArrayOfStructParameter_MemberPostDecrementAssign_works();
            ArrayOfStructParameter_MemberPreDecrementAssign_works();

            PreDecrementAssign_compiles();
            PreIncrementAssign_compiles();
            Throw_compiles();
            TypeAs_compiles();
            UnaryPlus_compiles();
            Unbox_compiles();

            return 27;
        }

        [Test]
        public void ArrayLength_compiles()
        {
            var param = Parameter(typeof(int[]), "i");
            var expression = Lambda<Func<int[], int>>(
                ArrayLength(param),
                param);

            var f = expression.CompileFast(true);
            var result = f(new[] { 1, 2, 3 });

            Assert.AreEqual(3, result);
        }

        [Test]
        public void Convert_compiles()
        {
            var param = Parameter(typeof(double), "d");
            var expression = Lambda<Func<double, int>>(
                Convert(param, typeof(int)),
                param);

            int result = expression.CompileFast(true)(1.5);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void ConvertChecked_compiles()
        {
            var param = Parameter(typeof(double), "d");
            var expression = Lambda<Func<double, int>>(
                ConvertChecked(param, typeof(int)),
                param);

            int result = expression.CompileFast(true)(1.5);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Increment_Constant_compiles()
        {
            var expression = Lambda<Func<double>>(
                Increment(Constant(2.2)));

            var result = expression.CompileFast(true)();

            Assert.AreEqual(3.2, result);
        }

        [Test]
        public void Decrement_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Decrement(param),
                param);

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Increment_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Increment(param),
                param);

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void IsFalse_compiles()
        {
            var param = Parameter(typeof(bool), "b");
            var expression = Lambda<Func<bool, bool>>(
                IsFalse(param),
                param);

            bool result = expression.CompileFast(true)(false);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsTrue_compiles()
        {
            var param = Parameter(typeof(bool), "b");
            var expression = Lambda<Func<bool, bool>>(
                IsTrue(param),
                param);

            bool result = expression.CompileFast(true)(true);

            Assert.IsTrue(result);
        }

        [Test]
        public void MakeUnary_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MakeUnary(ExpressionType.Increment, param, null),
                param);

            var f = expression.CompileFast(true);

            var result = f(2);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void Negate_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Negate(param),
                param);

            var result = expression.CompileFast(true)(1);
            Assert.AreEqual(-1, result);
            
            var result2 = expression.CompileFast(true)(2);
            Assert.AreEqual(-2, result2);
            
            var result3 = expression.CompileFast(true)(-3);
            Assert.AreEqual(3, result3);
        }

        [Test]
        public void NegateChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                NegateChecked(param),
                param);

            var f = expression.CompileFast(true);
            var result = f(1);

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void Binary_Not_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Not(param),
                param);

            int result = expression.CompileFast(true)(1);

            Assert.AreEqual(-2, result);
        }

        [Test]
        public void OnesComplement_compiles()
        {
            var param = Parameter(typeof(uint), "i");
            var expression = Lambda<Func<uint, uint>>(
                OnesComplement(param),
                param);

            var fs = expression.CompileSys();
            Assert.AreEqual(0x0000FFFF, fs(0xFFFF0000));
            Assert.AreEqual(0xF000FFFF, fs(0x0FFF0000));


            var f = expression.CompileFast(true);
            Assert.AreEqual(0x0000FFFF, f(0xFFFF0000));
            Assert.AreEqual(0xF000FFFF, f(0x0FFF0000));
        }

        [Test]
        public void PostDecrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PostDecrementAssign(param),
                param);

            var fs = expression.CompileSys();
            Assert.AreEqual(2, fs(2));

            var f = expression.CompileFast(true);
            Assert.AreEqual(2, f(2));
        }

        [Test]
        public void Parameter_PostIncrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PostIncrementAssign(param),
                param);

            expression.PrintCSharp();

            var fs = expression.CompileSys();
            fs.PrintIL();
            Assert.AreEqual(2, fs(2));

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual(2, ff(2));
        }

        delegate int FuncByRef(ref int n);

        [Test]
        public void RefParameter_PostIncrementAssign_works()
        {
            var param = Parameter(typeof(int).MakeByRefType(), "i");
            var expression = Lambda<FuncByRef>(
                PostIncrementAssign(param),
                param);

            expression.PrintCSharp();

            var fs = expression.CompileSys();
            fs.PrintIL();
            var n = 2;
            Assert.AreEqual(2, fs(ref n));
            Assert.AreEqual(3, n);

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            n = 3;
            Assert.AreEqual(3, ff(ref n));
            Assert.AreEqual(4, n);
        }

        [Test]
        public void ArrayItemRefParameter_PostIncrementAssign_works()
        {
            var param = Parameter(typeof(int).MakeByRefType(), "i");
            var expression = Lambda<FuncByRef>(
                PostIncrementAssign(param),
                param);

            expression.PrintCSharp();

            var fs = expression.CompileSys();
            fs.PrintIL();
            var arr = new[] { 42, 33 };
            Assert.AreEqual(33, fs(ref arr[1]));
            Assert.AreEqual(34, arr[1]);

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual(34, ff(ref arr[1]));
            Assert.AreEqual(35, arr[1]);
        }

        [Test]
        public void ArrayParameter_PostIncrementAssign_works()
        {
            var a = Parameter(typeof(int[]), "a");
            var i = Parameter(typeof(int),   "i");
            var expression = Lambda<Func<int[], int, int>>(
                PostIncrementAssign(ArrayAccess(a, i)),
                a, i);

            expression.PrintCSharp();

            var fs = expression.CompileSys();
            fs.PrintIL();

            var arr = new int[] { 42, 33 };
            Assert.AreEqual(33, fs(arr, 1));
            Assert.AreEqual(34, arr[1]);

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual(34, fs(arr, 1));
            Assert.AreEqual(35, arr[1]);
        }

        struct X { public int N; public string S; }

        [Test]
        public void ArrayOfStructParameter_MemberPostDecrementAssign_works()
        {
            var a = Parameter(typeof(X[]), "a");
            var i = Parameter(typeof(int), "i");
            var expression = Lambda<Func<X[], int, int>>(
                PostDecrementAssign(Field(ArrayAccess(a, i), nameof(X.N))),
                a, i);

            expression.PrintCSharp();

            var f = (Func<X[], int, int>)((
                UnaryExpressionTests.X[] a, int i) => //$
                (a[i].N--));

            var arr = new X[] { new X { N = 42 }, new X { N = 33 } };
            Assert.AreEqual(33, f(arr, 1));
            Assert.AreEqual(32, arr[1].N);

            var fs = expression.CompileSys();
            fs.PrintIL();

            Assert.AreEqual(32, fs(arr, 1));
            Assert.AreEqual(32, arr[1].N); // It should be 31, No? - The System Expression is wrong, what??

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual(32, ff(arr, 1));
            Assert.AreEqual(31, arr[1].N);
        }

        [Test]
        public void ArrayOfStructParameter_MemberPreDecrementAssign_works()
        {
            var a = Parameter(typeof(X[]), "a");
            var i = Parameter(typeof(int), "i");
            var expression = Lambda<Func<X[], int, int>>(
                PreDecrementAssign(Field(ArrayAccess(a, i), nameof(X.N))),
                a, i);

            expression.PrintCSharp();

            var f = (Func<X[], int, int>)((
                UnaryExpressionTests.X[] a, int i) => //$
                (--a[i].N));

            var arr = new X[] { new X { N = 42 }, new X { N = 33 } };
            Assert.AreEqual(32, f(arr, 1));
            Assert.AreEqual(32, arr[1].N);

            var fs = expression.CompileSys();
            fs.PrintIL();

            Assert.AreEqual(31, fs(arr, 1));
            Assert.AreEqual(32, arr[1].N); // It should be 31, No? - The System Expression is wrong, what??

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual(31, ff(arr, 1));
            Assert.AreEqual(31, arr[1].N);
        }

        delegate int FuncArrByRef(ref int[] a, int i);

        [Test]
        public void ArrayParameterByRef_PostIncrementAssign_works()
        {
            var a = Parameter(typeof(int[]).MakeByRefType(), "a");
            var i = Parameter(typeof(int), "i");
            var expression = Lambda<FuncArrByRef>(
                PostIncrementAssign(ArrayAccess(a, i)),
                a, i);

            expression.PrintCSharp();

            var fs = expression.CompileSys();
            fs.PrintIL();

            var arr = new int[] { 42, 33 };
            Assert.AreEqual(33, fs(ref arr, 1));
            Assert.AreEqual(34, arr[1]);

            var ff = expression.CompileFast(true);
            ff.PrintIL();
            Assert.AreEqual(34, fs(ref arr, 1));
            Assert.AreEqual(35, arr[1]);
        }

        [Test]
        public void PreDecrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PreDecrementAssign(param),
                param);

            var f = expression.CompileFast(true);

            Assert.AreEqual(1, f(2));
        }

        [Test]
        public void PreIncrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PreIncrementAssign(param),
                param);

            var f = expression.CompileFast(true);

            Assert.AreEqual(3, f(2));
        }

        //[Test] // todo: Quote is not supported yet
        public void Quote_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, SysExpr.Expression<Func<int>>>>(
                Quote(Lambda(param)),
                param);

            var fs = expression.CompileSys();
            var resultExpression = fs(2);
            var result1 = resultExpression.Compile().Invoke();
            Assert.AreEqual(2, result1);

            var f = expression.CompileFast(true);
            resultExpression = f(2);

            var result2 = resultExpression.Compile().Invoke();
            Assert.AreEqual(2, result2);
        }

        [Test]
        public void Throw_compiles()
        {
            var param = Parameter(typeof(Exception), "e");
            var expression = Lambda<Action<Exception>>(
                Throw(param),
                param);

            Action<Exception> result = expression.CompileFast(true);

            Assert.Throws<DivideByZeroException>(() => result(new DivideByZeroException()));
        }

        [Test]
        public void TypeAs_compiles()
        {
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, string>>(
                TypeAs(param, typeof(string)),
                param);

            string result = expression.CompileFast(true)("123");

            Assert.AreEqual("123", result);
        }

        [Test]
        public void UnaryPlus_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                UnaryPlus(param),
                param);

            int result = expression.CompileFast(true)(1);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Unbox_compiles()
        {
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, int>>(
                Unbox(param, typeof(int)),
                param);

            var fs = expression.CompileSys();
            Assert.AreEqual(1, fs(1));

            var f = expression.CompileFast(true);
            Assert.AreEqual(1, f(1));
        }
    }
}
