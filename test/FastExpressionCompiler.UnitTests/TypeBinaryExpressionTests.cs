using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class TypeBinaryExpressionTests
    {
        public int Run()
        {
            // TypeEqual_compiles(); // todo: not supported yet
            TypeIs_compiles();

            return 1;
        }

        [Test][Ignore("todo - TypeEqual is not supported yet")]
        public void TypeEqual_compiles()
        {
            var sExpr = Parameter(typeof(object), "o");
            var expr = Lambda<Func<object, bool>>(
                TypeEqual(sExpr, typeof(string)),
                sExpr);

            var f = expr.CompileFast(true);
            bool result = f("123");

            Assert.IsTrue(result);
        }

        [Test]
        public void TypeIs_compiles()
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
