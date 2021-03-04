using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class HoistedLambdaExprTests : ITest
    {
        public int Run()
        {
            Should_compile_nested_lambda();
            return 1;
        }

        [Test]
        public void Should_compile_nested_lambda()
        {
            var a = new A();
            Expression<Func<X>> getXExpr = () => X.Get(it => new X(it), new Lazy<A>(() => a));

            var getX = getXExpr.CompileFast(true);

            var x = getX();
            Assert.AreSame(a, x.A);
        }

        public class A {}

        public class X
        {
            public static X Get(Func<A, X> factory, Lazy<A> a)
            {
                return factory(a.Value);
            }

            public A A { get; }

            public X(A a)
            {
                A = a;
            }
        }
    }
}
