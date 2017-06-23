using System;
using System.Reflection;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ExpressionInfoTests
    {
        [Test]
        public void Can_compile_dry_expression_without_coverting_to_expression()
        {
            var funcExpr =
                ExpressionInfo.Lambda(
                    ExpressionInfo.New(typeof(X).GetTypeInfo().GetConstructors()[0],
                        ExpressionInfo.New(typeof(Y).GetTypeInfo().GetConstructors()[0])));

            var func = ExpressionCompiler.TryCompile<Func<X>>(funcExpr);
            Assert.IsNotNull(func);

            var x = func();
            Assert.IsInstanceOf<X>(x);
        }

        public class Y { }
        public class X
        {
            public Y Y { get; }
            public X(Y y)
            {
                Y = y;
            }
        }
    }
}
