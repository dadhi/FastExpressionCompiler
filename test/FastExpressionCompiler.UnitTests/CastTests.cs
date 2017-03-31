using System;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class CastTests
    {
        [Test]
        public void Expressions_with_small_long_casts_should_not_crash()
        {
            var x = 65535;
            Assert.IsTrue(ExpressionCompiler.Compile(() => x == (long)x)());
        }

        [Test]
        public void Expressions_with_larger_long_casts_should_not_crash()
        {
            var y = 65536;
            var yn1 = y + 1;
            Assert.IsTrue(ExpressionCompiler.Compile(() => yn1 != (long)y)());
        }

        [Test]
        public void Expressions_with_ulong_constants_and_casts()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0UL == (ulong)"x".Length)());
        }

        [Test]
        public void Expressions_with_DateTime()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0UL == (ulong)DateTime.Now.Day)());
        }
    }
}
