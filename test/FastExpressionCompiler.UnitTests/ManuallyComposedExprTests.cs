using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ManuallyComposedExprTests
    {
        [Test]
        public void Should_compile_manually_composed_expr()
        {
            var manualExpr = ComposeManualExpr();

            var lambda = ExpressionCompiler.Compile(manualExpr);

            Assert.IsInstanceOf<X>(lambda());
        }

        private static Expression<Func<object>> ComposeManualExpr()
        {
            var a = new A();
            var b = new B();
            var e = Expression.Lambda<Func<object>>(
                Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
                    Expression.Constant(a, typeof(A)),
                    Expression.Constant(b, typeof(B))));
            return e;
        }

        [Test]
        public void Should_compile_manually_composed_expr_with_parameters()
        {
            var manualExpr = ComposeManualExprWithParams();

            var lambda = ExpressionCompiler.Compile<Func<B, X>>(manualExpr);

            Assert.IsInstanceOf<X>(lambda(new B()));
        }

        private static LambdaExpression ComposeManualExprWithParams()
        {
            var a = new A();
            var bParamExpr = Expression.Parameter(typeof(B), "b");

            var e = Expression.Lambda(
                Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
                    Expression.Constant(a, typeof(A)),
                    bParamExpr),
                bParamExpr);
            return e;
        }

        public class A { }
        public class B { }

        public class X
        {
            public A A { get; }
            public B B { get; }

            public X(A a, B b)
            {
                A = a;
                B = b;
            }
        }
    }
}
