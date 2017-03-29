using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class LogicalOperatorsTests
    {
        private static Expression<Func<bool>> And()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && s.Contains("S");
        }

        [Test]
        public void Logical_and()
        {
            var expr = And();

            var dlg = ExpressionCompiler.TryCompile<Func<bool>>(expr);

            Assert.IsNotNull(dlg);
            Assert.IsFalse(dlg());
        }

        private static Expression<Func<bool>> Or()
        {
            var x = 1;
            var s = "Test";
            return () => x == 0 || s.Contains("S");
        }

        [Test]
        public void Logical_or()
        {
            var expr = Or();

            var dele = ExpressionCompiler.TryCompile<Func<bool>>(expr);

            Assert.IsNotNull(dele);
        }

        private static Expression<Func<bool>> And_with_or()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }

        [Test]
        public void Logical_and_with_or()
        {
            var expr = And_with_or();

            var dele = ExpressionCompiler.TryCompile<Func<bool>>(expr);

            Assert.IsNotNull(dele);
        }
    }
}
