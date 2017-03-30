using System;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class CastTests
    {
        [Test]
        public void Expressions_with_small_int_casts_should_not_crash()
        {
            //currently crashes with NullReferenceException
            var x = 65535;
            Assert.IsTrue(ExpressionCompiler.Compile(() => x == (long)x)());
        }

        [Test]
        public void Expressions_with_larger_int_casts_should_not_crash()
        {
            //currently tears down process: "The process was terminated due to an internal error in the .NET Runtime at IP 00007FFD35C25C22 (00007FFD35B90000) with exit code 80131506."
            var x = 65536;
            Assert.IsTrue(ExpressionCompiler.Compile(() => x == (long)x)());
        }
    }
}
