using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class OperatorsTests
    {
        [Test]
        public void AndAndOperator()
        {
            var expr = ExpressionCompiler.Compile(GetAndAlso());
        }

        private static Expression<Func<bool>> GetAndAlso()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }
    }
}
