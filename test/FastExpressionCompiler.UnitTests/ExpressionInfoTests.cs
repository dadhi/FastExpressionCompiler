using System;
using System.Linq;
using System.Linq.Expressions;
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

        [Test]
        public void Nested_Func_using_outer_parameter()
        {
            // The same hoisted expression: 
            //Expression<Func<string, string>> expr = a => GetS(() => a);

            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = ExpressionInfo.Lambda(
                ExpressionInfo.Call(GetType().GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(GetS)),
                    ExpressionInfo.Lambda(aParam)),
                aParam);

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

            Assert.AreEqual("a", f("a"));
        }

        public static string GetS(Func<string> getS)
        {
            return getS();
        }

        [Test]
        public void Nested_Action_using_outer_parameter_and_closed_value()
        {
            var s = new S();
            //Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = ExpressionInfo.Lambda(
                ExpressionInfo.Lambda(
                    ExpressionInfo.Call(
                        ExpressionInfo.Constant(s),
                        typeof(S).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(S.SetValue)),
                        aParam),
                    aParam)
                );

            var f = ExpressionCompiler.TryCompile<Func<Action<string>>>(expr);

            f()("a");
            Assert.AreEqual("a", s.Value);
        }

        public class S
        {
            public string Value;

            public void SetValue(string s)
            {
                Value = s;
            }
        }
    }
}
