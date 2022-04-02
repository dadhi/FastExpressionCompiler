using System;
using NUnit.Framework;
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class NestedLambdaTests : ITest
    {
        public int Run()
        {
            Nested_lambda_using_outer_parameter();
            Nested_lambda_using_outer_parameter_and_closed_value();
            Nested_lambda_using_outer_parameter_and_closed_value_deeply_nested_lambda();
            Given_composed_expr_with_closure_over_parameters_in_nested_lambda_should_work();
            Given_composed_expr_with_closure_over_parameters_used_in_2_levels_of_nested_lambda();
            Given_composed_expr_with_closure_over_2_parameters_used_in_2_levels_of_nested_lambda();
            Given_composed_expr_with_closure_over_2_same_parameters_used_in_2_levels_of_nested_lambda();
            Two_same_nested_lambdas_should_compile_once();
            Hmm_I_can_use_the_same_parameter_for_outer_and_nested_lambda();

#if !LIGHT_EXPRESSION
            Nested_Hoisted_Func_using_outer_parameter();
            Nested_Hoisted_Func_using_outer_parameter_and_closed_value();
            Nested_Hoisted_Action_using_outer_parameter_and_closed_value();
            Nested_Hoisted_lambda_using_outer_parameter_and_closed_value_deeply_nested_lambda();
            Given_hoisted_expr_with_closure_over_parameters_in_nested_lambda_should_work();
            return 14;
#else
            return 9;
#endif
        }

#if !LIGHT_EXPRESSION
        [Test]
        public void Nested_Hoisted_Func_using_outer_parameter()
        {
            System.Linq.Expressions.Expression<Func<string, string>> expr = a => GetS(() => a);

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("a", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Func_using_outer_parameter_and_closed_value()
        {
            var b = new S { Value = "b" };

            System.Linq.Expressions.Expression<Func<string, string>> expr = a => GetS(() => b.Append(a));

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("ba", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Action_using_outer_parameter_and_closed_value()
        {
            // The same hoisted expression: 
            var s = new S();
            System.Linq.Expressions.Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var f = expr.TryCompile<Func<Action<string>>>();

            f()("a");
            Assert.AreEqual("a", s.Value);
        }
        [Test]
        public void Nested_Hoisted_lambda_using_outer_parameter_and_closed_value_deeply_nested_lambda()
        {
            var b = new S { Value = "b" };

            System.Linq.Expressions.Expression<Func<string, string>> expr =
                a => GetS(
                    () => b.Prepend(a,
                        rest => b.Append(rest)));

            var f = expr.TryCompile<Func<string, string>>();

            Assert.AreEqual("abb", f("a"));
        }

        [Test]
        public void Given_hoisted_expr_with_closure_over_parameters_in_nested_lambda_should_work()
        {
            System.Linq.Expressions.Expression<Func<object, object>> funcExpr = a =>
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

#endif

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

        [Test]
        public void Two_same_nested_lambdas_should_compile_once()
        {
            var n = Parameter(typeof(int), "n");
            var add = Lambda<Func<int, int>>(Add(n, Constant(1)), n);

            var m = Parameter(typeof(int), "m");
            var sub = Lambda<Func<int, int>>(Subtract(
                m, Invoke(add, Constant(5))), 
                m);

            var e = Lambda<Func<int>>(
                Add(Invoke(sub, Constant(42)), 
                    Invoke(add, Constant(13))));

            var f = e.CompileFast(true);
            Assert.IsNotNull(f);
            Assert.AreEqual(50, f());
        }

        [Test]
        public void Hmm_I_can_use_the_same_parameter_for_outer_and_nested_lambda()
        {
            var nParam = Parameter(typeof(int), "n");

            var add = Lambda<Func<int, int>>(Add(nParam, Constant(1)), nParam);

            var e = Lambda<Func<int>>(
                Add(
                    Invoke(Lambda<Func<int, int>>(Subtract(nParam, Invoke(add, nParam)), nParam), Constant(42)),
                    Invoke(add, Constant(13))
                ));

            var fs =  e.CompileSys();
            Assert.AreEqual(13, fs());

            var fi = e.CompileFast(true);
            Assert.AreEqual(13, fi());

            var f = e.CompileFast(true, CompilerFlags.NoInvocationLambdaInlining);
            Assert.AreEqual(13, f());
        }
    }
}
