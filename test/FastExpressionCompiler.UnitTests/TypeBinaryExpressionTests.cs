using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class TypeBinaryExpressionTests
    {
        public int Run()
        {
            TypeEqual_should_work();
            TypeIs_should_work();

            return 2;
        }

        [Test]
        public void TypeEqual_should_work()
        {
            var sExpr = Parameter(typeof(object), "o");
            var expr = Lambda<Func<object, bool>>(
                TypeEqual(sExpr, typeof(string)),
                sExpr);

            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            bool result = f("123");

            Assert.IsTrue(result);
        }

        [Test]
        public void TypeIs_should_work()
        {
            var sExpr = Parameter(typeof(object), "o");
            var expr = Lambda<Func<object, bool>>(
                TypeIs(sExpr, typeof(string)),
                sExpr);

            var f = expr.CompileFast(true);
            bool result = f("123");

            Assert.IsTrue(result);
        }
    }
}
