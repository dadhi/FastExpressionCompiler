using System;
using System.Linq.Expressions;
using NUnit.Compatibility;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class DryExpressionTests
    {
        [Test]
        public void Can_use_dry_expression_to_construct_the_normal_expression()
        {
            var expr = 
                Expression.New(typeof(X).GetConstructors()[0],
                DryExpression.New(typeof(Y).GetConstructors()[0]));

            var func = ExpressionCompiler.Compile<Func<X>>(Expression.Lambda(expr));

            Assert.IsInstanceOf<X>(func());
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
