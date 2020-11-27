using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue106_Power_support : ITest
    {
        public int Run()
        {
            PowerIsSupported();
            TestPowerAssign();
            return 2;
        }

        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void PowerIsSupported()
        {
            var lambda = Lambda<Func<double>>(Power(Constant(5.0), Constant(2.0)));
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(25, fastCompiled());
        }

        [Test]
        public void TestPowerAssign()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(PowerAssign(objRef, Constant((double)2.0)), objRef);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(25.0, exampleB);
        }
    }
}
