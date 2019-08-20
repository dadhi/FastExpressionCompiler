using System;
using NUnit.Framework;
using SysExpr = System.Linq.Expressions;

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
    public class UnaryExpressionTests
    {
        [Test]
        [Ignore("Does not work yet")]
        public void ArrayLength_compiles()
        {
            var param = Parameter(typeof(int[]), "i");
            var expression = Lambda<Func<int[], int>>(
                ArrayLength(param),
                param);

            int result = expression.CompileFast(true)(new[] { 1, 2, 3 });

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

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void Negate_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Negate(param),
                param);

            int result = expression.CompileFast(true)(1);
            Assert.AreEqual(-1, result);
            int result2 = expression.CompileFast(true)(2);
            Assert.AreEqual(-2, result2);
            int result3 = expression.CompileFast(true)(-3);
            Assert.AreEqual(3, result3);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void NegateChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                NegateChecked(param),
                param);

            int result = expression.CompileFast(true)(1);

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void Not_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Not(param),
                param);

            int result = expression.CompileFast(true)(1);

            Assert.AreEqual(-2, result);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void OnesComplement_compiles()
        {
            var param = Parameter(typeof(uint), "i");
            var expression = Lambda<Func<uint, uint>>(
                OnesComplement(param),
                param);

            uint result = expression.CompileFast(true)(0xFFFF0000);

            Assert.AreEqual(0x0000FFFF, result);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void PostDecrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PostDecrementAssign(param),
                param);

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(2, result);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void PostIncrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PostIncrementAssign(param),
                param);

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(2, result);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void PreDecrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PreDecrementAssign(param),
                param);

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(1, result);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void PreIncrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PreIncrementAssign(param),
                param);

            int result = expression.CompileFast(true)(2);

            Assert.AreEqual(3, result);
        }

        [Test]
        [Ignore("Does not work yet")]
        public void Quote_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, SysExpr.Expression<Func<int>>>>(
                Quote(Lambda(param)),
                param);

            var resultExpression = expression.CompileFast(true)(2);
            int result = resultExpression.Compile()();

            Assert.AreEqual(2, result);
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
        [Ignore("Maybe works, but maybe value it's not yet boxed")]
        public void Unbox_compiles()
        {
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, int>>(
                Unbox(param, typeof(int)),
                param);

            int result = expression.CompileFast(true)(1);

            Assert.AreEqual(1, result);
        }
    }
}
