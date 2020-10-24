using System;
using NUnit.Framework;

namespace FastExpressionCompiler.LightExpression.IssueTests
{
    [TestFixture]
    public class Issue231_ExpressionNew_does_not_work_with_struct : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }

        [Test]
        public void Test()
        {
            var newExample = Expression.New(typeof(Example));

            var e = Expression.Lambda<Func<Example>>(newExample);

            var f = e.CompileFast(true);

            Assert.IsInstanceOf<Example>(f());
        }

        struct Example { }
    }
}
