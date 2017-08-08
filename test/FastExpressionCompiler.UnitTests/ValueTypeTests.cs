using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ValueTypeTests
    {
        [Test]
        public void Should_support_struct_params()
        {
            Expression<Func<A, int>> getNExpr = a => a.N;

            var getN = getNExpr.CompileFast<Func<A, int>>(true);

            Assert.AreEqual(42, getN(new A { N = 42 }));
        }

        struct A
        {
            public int N;
        }
    }
}
