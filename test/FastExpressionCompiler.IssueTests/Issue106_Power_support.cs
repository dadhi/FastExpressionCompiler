using System;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    class Issue106_Power_support
    {
        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void PowerIsSupported()
        {
            var lambda = Lambda<Func<double>>(Power(Constant(5.0), Constant(2.0)));
            var compiled = lambda.Compile();
            var fastCompiled = lambda.CompileFast(true);
            Assert.AreEqual(25.0, compiled());
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(25, fastCompiled());
        }

        [Test]
        public void TestPowerAssign()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(PowerAssign(objRef, Constant((double)2.0)), objRef);

            var compiledA = lambda.Compile();
            var exampleA = 5.0;
            compiledA(ref exampleA);
            Assert.AreEqual(25.0, exampleA);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(25.0, exampleB);
        }
    }
}
