using System;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class AssignTests
    {
        [Test]
        public void Can_assign_to_parameter()
        {
            var sParamExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Func<string, string>>(
                Assign(sParamExpr, Constant("aaa")),
                sParamExpr);

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("aaa", f("ignored"));
        }

        [Test]
        public void Can_assign_to_parameter_in_nested_lambda()
        {
            // s => () => s = "aaa" 
            var sParamExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Func<string, Func<string>>>(
                Lambda<Func<string>>(
                    Assign(sParamExpr, Constant("aaa"))),
                sParamExpr);

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("aaa", f("ignored")());
        }
    }
}
