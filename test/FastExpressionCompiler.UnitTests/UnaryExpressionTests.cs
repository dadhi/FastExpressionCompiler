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
        public void PostIncrementAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                PostIncrementAssign(param),
                param);

            var f = expression.CompileFast(true);

            Assert.AreEqual(2, f(2));
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

        [Test]
        [Ignore("Not supported yet")]
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
