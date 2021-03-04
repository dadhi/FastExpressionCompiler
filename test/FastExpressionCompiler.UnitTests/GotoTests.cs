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
    public class GotoTests : ITest
    {
        public int Run()
        {
            Can_goto_with_value();
            return 1;
        }

        [Test]
        public void Can_goto_with_value()
        {
            var intParameter = Parameter(typeof(int));
            var returnLabel = Label(typeof(string));

            var expr = Lambda<Func<int, string>>(
                Block(
                    IfThen(Equal(intParameter, Constant(10)), Goto(returnLabel, Constant("TEN"))),
                    IfThen(Equal(intParameter, Constant(5)), Goto(returnLabel, Constant("FIVE"))),
                    IfThen(Equal(intParameter, Constant(3)), Goto(returnLabel, Constant("THREE"))),
                    Label(returnLabel, Constant("ZERO"))),
                intParameter);

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("TEN", f(10));
            Assert.AreEqual("FIVE", f(5));
            Assert.AreEqual("THREE", f(3));
            Assert.AreEqual("ZERO", f(1));
        }
    }
}
