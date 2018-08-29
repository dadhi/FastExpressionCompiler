using System;
using System.Linq.Expressions;
using NUnit.Framework;

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

            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = Expression.Lambda(
                Expression.Call(GetType(), nameof(GetS), Type.EmptyTypes,
                    Expression.Lambda(aParam)),
                aParam);

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

            Assert.AreEqual("a", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Func_using_outer_parameter()
        {
            Expression<Func<string, string>> expr = a => GetS(() => a);

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

            Assert.AreEqual("a", f("a"));
        }

        [Test]
        public void Nested_lambda_using_outer_parameter_and_closed_value()
        {
            var b = new S { Value = "b" };

            // The same hoisted expression: 
            //Expression<Func<string, string>> expr = a => GetS(() => b.Append(a));

            var bExpr = Expression.Constant(b);

            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = Expression.Lambda(
                Expression.Call(GetType(), nameof(GetS), Type.EmptyTypes,
                    Expression.Lambda(
                        Expression.Call(bExpr, "Append", Type.EmptyTypes,
                            aParam))),
                aParam);

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

            Assert.AreEqual("ba", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Func_using_outer_parameter_and_closed_value()
        {
            var b = new S { Value = "b" };

            Expression<Func<string, string>> expr = a => GetS(() => b.Append(a));

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

            Assert.AreEqual("ba", f("a"));
        }

        [Test]
        public void Nested_Hoisted_Action_using_outer_parameter_and_closed_value()
        {
            // The same hoisted expression: 
            var s = new S();
            Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var f = ExpressionCompiler.TryCompile<Func<Action<string>>>(expr);

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

            var bExpr = Expression.Constant(b);

            var aParam = Expression.Parameter(typeof(string), "a");
            var bbParam = Expression.Parameter(typeof(string), "bb");

            var expr = Expression.Lambda(
                Expression.Call(GetType(), nameof(GetS), Type.EmptyTypes,
                    Expression.Lambda(
                        Expression.Call(bExpr, "Prepend", Type.EmptyTypes,
                            aParam,
                            Expression.Lambda(Expression.Call(bExpr, "Append", Type.EmptyTypes, bbParam), 
                                bbParam)))),
                aParam);

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

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

            var f = ExpressionCompiler.TryCompile<Func<string, string>>(expr);

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

            var funcFec = ExpressionCompiler.TryCompile<Func<object, object>>(funcExpr);

            Assert.AreSame(arg1, funcFec(arg1));
            Assert.AreSame(arg2, funcFec(arg2));
        }

        [Test]
        public void Given_composed_expr_with_closure_over_parameters_in_nested_lambda_should_work()
        {
            var argExpr = Expression.Parameter(typeof(object));
            var funcExpr = Expression.Lambda(
                Expression.Invoke(Expression.Lambda(
                    Expression.Invoke(Expression.Lambda(argExpr)))),
                argExpr);

            var funcFec = ExpressionCompiler.TryCompile<Func<object, object>>(funcExpr);

            var arg1 = new object();
            Assert.AreSame(arg1, funcFec(arg1));

            var arg2 = new object();
            Assert.AreSame(arg2, funcFec(arg2));

            Assert.AreSame(arg1, funcFec(arg1));
        }

        [Test]
        public void Given_composed_exprInfo_with_closure_over_parameters_in_nested_lambda_should_work()
        {
            var argExpr = Expression.Parameter(typeof(object));
            var funcExpr = ExpressionInfo.Lambda(
                ExpressionInfo.Invoke(ExpressionInfo.Lambda(
                    ExpressionInfo.Invoke(ExpressionInfo.Lambda(argExpr)))),
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

            var aExpr = Expression.Parameter(typeof(A));
            var funcExpr = Expression.Lambda(
                Expression.Call(aExpr, "Increment", new Type[0],
                    Expression.Lambda(
                        Expression.Call(aExpr, "Increment", new Type[0],
                            Expression.Lambda(
                                Expression.Call(aExpr, "Increment", new Type[0],
                                    Expression.Constant(null, typeof(Func<A>))
                                )
                            )
                        )
                    )
                ),
                aExpr);

            var func = ExpressionCompiler.TryCompile<Func<A, A>>(funcExpr);

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
            Func<A, A, A> funcEthalon = (a, b) => a.Increment(b, () => a.Increment(b, () => a.Increment(b, null)));
            var aa = new A();
            var bb = new A();
            funcEthalon(aa, bb);
            Assert.AreEqual(3, aa.X);
            Assert.AreEqual(-3, bb.X);

            var aExpr = Expression.Parameter(typeof(A), "a");
            var bExpr = Expression.Parameter(typeof(A), "b");

            var funcExpr = Expression.Lambda(
                Expression.Call(aExpr, "Increment", new Type[0],
                    bExpr,
                    Expression.Lambda(
                        Expression.Call(aExpr, "Increment", new Type[0],
                            bExpr,
                            Expression.Lambda(
                                Expression.Call(aExpr, "Increment", new Type[0],
                                    bExpr,
                                    Expression.Constant(null, typeof(Func<A>))
                                )
                            )
                        )
                    )
                ),
                aExpr, bExpr);

            var func = ExpressionCompiler.TryCompile<Func<A, A, A>>(funcExpr);

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
            Func<A, A, A> funcEthalon = (a, b) => a.Increment(b, () => a.Increment(b, () => a.Increment(b, null)));
            var aa = new A();
            funcEthalon(aa, aa);
            Assert.AreEqual(0, aa.X);

            var aExpr = Expression.Parameter(typeof(A));
            var bExpr = Expression.Parameter(typeof(A));

            var funcExpr = Expression.Lambda(
                Expression.Call(aExpr, "Increment", new Type[0],
                    aExpr,
                    Expression.Lambda(
                        Expression.Call(aExpr, "Increment", new Type[0],
                            aExpr,
                            Expression.Lambda(
                                Expression.Call(aExpr, "Increment", new Type[0],
                                    aExpr,
                                    Expression.Constant(null, typeof(Func<A>))
                                )
                            )
                        )
                    )
                ),
                aExpr, bExpr);

            var func = ExpressionCompiler.TryCompile<Func<A, A, A>>(funcExpr);

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

            public A Increment(A b, Func<A> then)
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
