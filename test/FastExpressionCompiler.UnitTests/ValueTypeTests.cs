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

        [Test]
        public void Should_support_struct_methods_requiring_boxing()
        {
            Expression<Func<A, string>> getNExpr = a => a.ToString();

            var getN = getNExpr.CompileFast<Func<A, string>>(true);

            Assert.AreEqual("42", getN(new A { N = 42 }));
        }

        [Test]
        public void Can_create_struct()
        {
            Expression<Func<A>> newAExpr = () => new A();

            var newA = newAExpr.CompileFast<Func<A>>(true);

            Assert.AreEqual(0, newA().N);
        }

        [Test]
        public void Can_init_struct_member()
        {
            Expression<Func<A>> newAExpr = () => new A { N = 43 };

            var newA = newAExpr.CompileFast<Func<A>>(true);

            Assert.AreEqual(43, newA().N);
        }

        struct A
        {
            public int N;
            public override string ToString() => N.ToString();
        }
    }
}
