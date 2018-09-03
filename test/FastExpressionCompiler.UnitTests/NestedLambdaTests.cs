using System;
using System.Linq.Expressions;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class NestedLambdaTests
    {
        [Test]
        public void Nested_lambda_using_outer_parameter()
        {
            // The same hoisted expression: 
            //Expression<Func<string, string>> expr = a => GetS(() => a);

            var aParam = Parameter(typeof(string), "a");
            var expr = Lambda(
                Call(GetType(), nameof(GetS), Type.EmptyTypes,
                    Lambda(aParam)),
                aParam);

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("a", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Func_using_outer_parameter()
        {
            Expression<Func<string, string>> expr = a => GetS(() => a);

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("a", f("a"));
        }

        [Test]
        public void Nested_lambda_using_outer_parameter_and_closed_value()
        {
            var b = new S { Value = "b" };

            // The same hoisted expression: 
            //Expression<Func<string, string>> expr = a => GetS(() => b.Append(a));

            var bExpr = Constant(b);

            var aParam = Parameter(typeof(string), "a");
            var expr = Lambda(
                Call(GetType(), nameof(GetS), Type.EmptyTypes,
                    Lambda(
                        Call(bExpr, "Append", Type.EmptyTypes,
                            aParam))),
                aParam);

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("ba", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Func_using_outer_parameter_and_closed_value()
        {
            var b = new S { Value = "b" };

            Expression<Func<string, string>> expr = a => GetS(() => b.Append(a));

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("ba", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Action_using_outer_parameter_and_closed_value()
        {
            // The same hoisted expression: 
            var s = new S();
            Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var f = expr.TryCompile<Func<Action<string>>>();

            f()("a");
            Assert.AreEqual("a", s.Value);
        }

        [Test]
        public void Nested_lambda_using_outer_parameter_and_closed_value_deeply_nested_lambda()
        {
            var b = new S { Value = "b" };

            // The same hoisted expression: 
            //Expression<Func<string, string>> expr =
            //    a => GetS(
            //        () => b.Prepend(a,
            //            rest => b.Append(rest)));

            var bExpr = Constant(b);

            var aParam = Parameter(typeof(string), "a");
            var bbParam = Parameter(typeof(string), "bb");

            var expr = Lambda(
                Call(GetType(), nameof(GetS), Type.EmptyTypes,
                    Lambda(
                        Call(bExpr, "Prepend", Type.EmptyTypes,
                            aParam,
                            Lambda(Call(bExpr, "Append", Type.EmptyTypes, bbParam), 
                                bbParam)))),
                aParam);

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("abb", f("a"));
        }

        [Test]
        public void Nested_Hoisted_lambda_using_outer_parameter_and_closed_value_deeply_nested_lambda()
        {
            var b = new S { Value = "b" };

            Expression<Func<string, string>> expr =
                a => GetS(
                    () => b.Prepend(a,
                        rest => b.Append(rest)));

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("abb", f("a"));
        }

        public static string GetS(Func<string> getS)
        {
            return getS();
        }

        public class S
        {
            public string Value;

            public string Append(string s)
            {
                return Value + s;
            }

            public string Prepend(string s, Func<string, string> restTransform)
            {
                return s + restTransform(Value);
            }

            public void SetValue(string s)
            {
                Value = s;
            }
        }

        [Test]
        public void Given_hoisted_expr_with_closure_over_parameters_in_nested_lambda_should_work()
        {
            Expression<Func<object, object>> funcExpr = a =>
                new Func<object>(() =>
                    new Func<object>(() => a)())();

            var func = funcExpr.Compile();

            var arg1 = new object();
            Assert.AreSame(arg1, func(arg1));

            var arg2 = new object();
            Assert.AreSame(arg2, func(arg2));

            var funcFec = funcExpr.TryCompile<Func<object, object>>();

            Assert.AreSame(arg1, funcFec(arg1));
            Assert.AreSame(arg2, funcFec(arg2));
        }

        [Test]
        public void Given_composed_expr_with_closure_over_parameters_in_nested_lambda_should_work()
        {
            var argExpr = Parameter(typeof(object));
            var funcExpr = Lambda(
                Invoke(Lambda(
                    Invoke(Lambda(argExpr)))),
                argExpr);

            var funcFec = funcExpr.TryCompile<Func<object, object>>();

            var arg1 = new object();
            Assert.AreSame(arg1, funcFec(arg1));

            var arg2 = new object();
            Assert.AreSame(arg2, funcFec(arg2));

            Assert.AreSame(arg1, funcFec(arg1));
        }

        [Test]
        public void Given_composed_expr_with_closure_over_parameters_used_in_2_levels_of_nested_lambda()
        {
            //Func<A, A> funcEthalon = a => a.Increment(() => a.Increment(() => a.Increment(null)));

            var aExpr = Parameter(typeof(A));
            var funcExpr = Lambda(
                Call(aExpr, "Increment", new Type[0],
                    Lambda(
                        Call(aExpr, "Increment", new Type[0],
                            Lambda(
                                Call(aExpr, "Increment", new Type[0],
                                    Constant(null, typeof(Func<A>))
                                )
                            )
                        )
                    )
                ),
                aExpr);

            var func = funcExpr.TryCompile<Func<A, A>>();

            var a1 = new A();
            var result1 = func(a1);
            Assert.AreEqual(3, result1.X);
            Assert.AreSame(a1, result1);

            var a2 = new A();
            Assert.AreSame(a2, func(a2));
        }

        [Test]
        public void Given_composed_expr_with_closure_over_2_parameters_used_in_2_levels_of_nested_lambda()
        {
            Func<A, A, A> funcEthalon = (a, b) => a.Increment(b, () => a.Increment(b, () => a.Increment(b, null, null), () => a), () => a);
            var aa = new A();
            var bb = new A();
            funcEthalon(aa, bb);
            Assert.AreEqual(3, aa.X);
            Assert.AreEqual(-3, bb.X);

            var aExpr = Parameter(typeof(A), "a");
            var bExpr = Parameter(typeof(A), "b");

            var aLambdaExpr = Lambda(aExpr);
            var aNullLambdaExpr = Constant(null, typeof(Func<A>));
            var funcExpr = Lambda(
                Call(aExpr, "Increment", new Type[0],
                    bExpr,
                    Lambda(
                        Call(aExpr, "Increment", new Type[0],
                            bExpr,
                            Lambda(
                                Call(aExpr, "Increment", new Type[0],
                                    bExpr,
                                    aNullLambdaExpr,
                                    aNullLambdaExpr
                                )
                            ),
                            aLambdaExpr
                        )
                    ),
                    aLambdaExpr
                ),
                aExpr, bExpr);

            var func = funcExpr.TryCompile<Func<A, A, A>>();

            var a1 = new A();
            var b1 = new A();
            var result1 = func(a1, b1);
            Assert.AreEqual(3, result1.X);
            Assert.AreSame(a1, result1);

            var a2 = new A();
            var b2 = new A();
            Assert.AreSame(a2, func(a2, b2));
        }

        [Test]
        public void Given_composed_expr_with_closure_over_2_same_parameters_used_in_2_levels_of_nested_lambda()
        {
            Func<A, A, A> funcEthalon = (a, b) => a.Increment(b, () => a.Increment(b, () => a.Increment(b, null, null), () => a), () => a);
            var aa = new A();
            funcEthalon(aa, aa);
            Assert.AreEqual(0, aa.X);

            var aExpr = Parameter(typeof(A));
            var bExpr = Parameter(typeof(A));

            var funcExpr = Lambda(
                Call(aExpr, "Increment", new Type[0],
                    aExpr,
                    Lambda(
                        Call(aExpr, "Increment", new Type[0],
                            aExpr,
                            Lambda(
                                Call(aExpr, "Increment", new Type[0],
                                    aExpr,
                                    Constant(null, typeof(Func<A>)),
                                    Constant(null, typeof(Func<A>))
                                )
                            ),
                            Lambda(aExpr)
                        )
                    ),
                    Lambda(aExpr)
                ),
                aExpr, bExpr);

            var func = funcExpr.TryCompile<Func<A, A, A>>();

            var a1 = new A();
            var result1 = func(a1, a1);
            Assert.AreEqual(0, result1.X);
            Assert.AreSame(a1, result1);

            var a2 = new A();
            Assert.AreSame(a2, func(a2, a2));
        }

        public class A
        {
            public int X { get; private set; }

            public A Increment(Func<A> then)
            {
                X += 1;
                if (then == null)
                    return this;
                return then();
            }

            public A Increment(A b, Func<A> then, Func<A> then2)
            {
                X += 1;
                b.X -= 1;
                if (then == null)
                    return this;
                return then();
            }
        }
    }
}
