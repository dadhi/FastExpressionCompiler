using System;
using System.Reflection;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ExpressionInfoTests
    {
        [Test]
        public void Can_compile_lambda_without_coverting_to_expression()
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

        [Test]
        public void Can_compile_lambda_with_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr =
                ExpressionInfo.Lambda(
                        ExpressionInfo.Property(thisType.GetProperty(nameof(PropX))));

            var func = ExpressionCompiler.TryCompile<Func<X>>(funcExpr);
            Assert.IsNotNull(func);

            var x = func();
            Assert.IsInstanceOf<X>(x);
        }

        [Test]
        public void Can_compile_lambda_with_call_and_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr =
                ExpressionInfo.Lambda(
                    ExpressionInfo.Call(thisType.GetMethod(nameof(GetX)),
                        ExpressionInfo.Property(thisType.GetProperty(nameof(PropX)))));

            var func = ExpressionCompiler.TryCompile<Func<X>>(funcExpr);
            Assert.IsNotNull(func);

            var x = func();
            Assert.IsInstanceOf<X>(x);
        }

        public static X PropX => new X(new Y());
        public static X GetX(X x) => x;
    }
}
