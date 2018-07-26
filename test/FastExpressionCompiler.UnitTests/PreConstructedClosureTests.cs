using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture()]
    public class PreConstructedClosureTests
    {
        [Test]
        public void Can_pass_closure_with_constant_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Expression.Constant(x);
            var expr = Expression.Lambda<Func<X>>(xConstExpr);

            var c = ExpressionCompiler.Closure.Create(x);
            var f = expr.TryCompile<Func<X>>(c, xConstExpr);

            Assert.IsInstanceOf<X>(f());
        }

        public class X { }

        [Test]
        public void Can_pass_ANY_class_closure_with_constant_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Expression.Constant(x);
            var expr = Expression.Lambda<Func<X>>(xConstExpr);

            var cx = new ClosureX(x);
            var f = expr.TryCompile<Func<X>>(cx, xConstExpr);

            Assert.IsInstanceOf<X>(f());
        }

        public class ClosureX
        {
            public readonly X X;
            public ClosureX(X x) { X = x; }
        }

        [Test]
        public void Can_prevent_closure_creation_when_compiling_a_static_delegate()
        {
            Expression<Func<X>> expr = () => new X();

            var f = expr.TryCompile<Func<X>>(closure: null);

            Assert.IsInstanceOf<X>(f());
        }
    }
}
