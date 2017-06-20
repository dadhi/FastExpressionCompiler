using System;
using NUnit.Compatibility;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class DryExpressionCompilerTests
    {
        [Test]
        public void Can_compile_dry_expression_without_coverting_to_expression()
        {
            var funcExpr =
                DryExpression.Lambda(
                    DryExpression.New(typeof(X).GetConstructors()[0],
                        DryExpression.New(typeof(Y).GetConstructors()[0])));

            var func = DryExpressionCompiler.TryCompile<Func<X>>(funcExpr);
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
