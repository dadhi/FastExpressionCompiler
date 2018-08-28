using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using static FastExpressionCompiler.ExpressionInfo;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ExpressionInfoTests
    {
        [Test]
        public void Can_compile_lambda_without_converting_to_expression()
        {
            var funcExpr = Lambda(
                    New(typeof(X).GetTypeInfo().GetConstructors()[0],
                        New(typeof(Y).GetTypeInfo().GetConstructors()[0])));

            var func = funcExpr.TryCompile<Func<X>>();
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
            var funcExpr = Lambda(Property(thisType.GetProperty(nameof(PropX))));

            var func = funcExpr.TryCompile<Func<X>>();
            Assert.IsNotNull(func);

            var x = func();
            Assert.IsInstanceOf<X>(x);
        }

        [Test]
        public void Can_compile_lambda_with_call_and_property()
        {
            var thisType = GetType().GetTypeInfo();
            var funcExpr =
                Lambda(Call(thisType.GetMethod(nameof(GetX)),
                    Property(thisType.GetProperty(nameof(PropX)))));

            var func = funcExpr.TryCompile<Func<X>>();
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
            var expr = Lambda(
                Call(GetType().GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(GetS)),
                    Lambda(aParam)),
                aParam);

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("a", f("a"));
        }

        public static string GetS(Func<string> getS)
        {
            return getS();
        }

        [Test]
        public void Nested_Action_using_outer_parameter_and_closed_value()
        {
            //Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var s = new S();
            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = Lambda(
                Lambda(
                    Call(
                        Constant(s),
                        typeof(S).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(S.SetValue)),
                        aParam),
                    aParam)
                );

            var f = expr.TryCompile<Func<Action<string>>>();

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

        // Result delegate to be created by CreateComplexExpression
        public object CreateA(object[] state)
        {
            return new A(
                new B(), 
                (string)state[11], 
                new ID[] { new D1(), new D2() }
            )
            {
                Prop = new P(new B()),
                Bop = new B()
            };
        }

        private static ConstructorInfo _ctorOfA = typeof(A).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfB = typeof(B).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfP = typeof(P).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfD1 = typeof(D1).GetTypeInfo().DeclaredConstructors.First();
        private static ConstructorInfo _ctorOfD2 = typeof(D2).GetTypeInfo().DeclaredConstructors.First();
        private static PropertyInfo _propAProp = typeof(A).GetTypeInfo().DeclaredProperties.First(p => p.Name == "Prop");
        private static FieldInfo _fieldABop = typeof(A).GetTypeInfo().DeclaredFields.First(p => p.Name == "Bop");

        public static Expression<Func<object[], object>> CreateComplexExpression()
        {
            var stateParamExpr = Expression.Parameter(typeof(object[]));

            var funcExpr = Expression.Lambda<Func<object[], object>>(
                Expression.MemberInit(
                    Expression.New(_ctorOfA,
                        Expression.New(_ctorOfB),
                        Expression.Convert(Expression.ArrayIndex(stateParamExpr, Expression.Constant(11)), typeof(string)),
                        Expression.NewArrayInit(typeof(ID),
                            Expression.New(_ctorOfD1),
                            Expression.New(_ctorOfD2))),
                    Expression.Bind(_propAProp,
                        Expression.New(_ctorOfP,
                            Expression.New(_ctorOfB))),
                    Expression.Bind(_fieldABop,
                        Expression.New(_ctorOfB))),
                stateParamExpr);

            return funcExpr;
        }

        public static ExpressionInfo<Func<object[], object>> CreateComplexExpressionInfo()
        {
            var stateParamExpr = Expression.Parameter(typeof(object[]));

            var expr = Lambda<Func<object[], object>>(
                MemberInit(
                    New(_ctorOfA,
                        New(_ctorOfB),
                        Convert(
                            ArrayIndex(stateParamExpr, Constant(11)), 
                            typeof(string)),
                        NewArrayInit(typeof(ID),
                            New(_ctorOfD1),
                            New(_ctorOfD2))),
                    Bind(_propAProp,
                        New(_ctorOfP,
                            New(_ctorOfB))),
                    Bind(_fieldABop,
                        New(_ctorOfB))),
                stateParamExpr);

            return expr;
        }

        [Test]
        public void Can_compile_complex_expr_with_Array_Properties_and_Casts()
        {
            var expr = CreateComplexExpressionInfo();
            var func = expr.TryCompile();
            func(new object[12]);
        }

        public class A
        {
            public P Prop { get; set; }
            public B Bop;

            public A(B b, string s, IEnumerable<ID> ds) { }
        }

        public class B { }

        public class P { public P(B b) { } }

        public interface ID { }
        public class D1 : ID { }
        public class D2 : ID { }

        [Test]
        public void Can_embed_normal_Expression_into_ExpressionInfo_eg_as_Constructor_argument()
        {
            var func = Lambda(
                New(_ctorOfP,
                    Expression.New(_ctorOfB))).TryCompile<Func<P>>();

            Assert.IsInstanceOf<P>(func());
        }
    }
}
