using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ConditionalOperatorsTests
    {
        [Test]
        public void Logical_and()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<bool>> expr = () => x == 1 && s.Contains("S");

            var dlg = expr.TryCompile<Func<bool>>();

            Assert.IsNotNull(dlg);
            Assert.IsFalse(dlg());
        }

        [Test]
        public void Logical_or()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<bool>> expr = () => x == 0 || s.Contains("S");

            var dele = expr.TryCompile<Func<bool>>();

            Assert.IsNotNull(dele);
        }

        [Test]
        public void Logical_and_with_or()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<bool>> expr = () => x == 1 && (s.Contains("S") || s.Contains("s"));

            var dele = expr.TryCompile<Func<bool>>();

            Assert.IsNotNull(dele);
        }

        [Test]
        public void Logical_and_or_and()
        {
            var f = false;
            var t = true;
            var x = 1;
            var s = "Test";
            Expression<Func<bool>> expr = () => 
                (f || x == 1) && (s.Contains("S") || s.Contains("s")) || t;

            var dlg = expr.TryCompile<Func<bool>>();

            Assert.IsNotNull(dlg);
            Assert.IsTrue(dlg());
        }

        [Test]
        public void Ternarary_operator_with_equality()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<object>> expr = () => x == 1 ? s : string.Empty;

            var dlg = expr.TryCompile<Func<object>>();

            Assert.IsNotNull(dlg);
            Assert.AreEqual(s, dlg());
        }

        [Test]
        public void Ternarary_operator_with_not_equality()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<object>> expr = () => x != 1 ? string.Concat(s, "ccc") : string.Empty;

            var dlg = expr.TryCompile<Func<object>>();

            Assert.IsNotNull(dlg);
            Assert.AreEqual(string.Empty, dlg());
        }

        [Test]
        public void Ternarary_operator_with_less_then()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<object>> expr = () => x < 1 ? string.Concat(s, "ccc") : string.Empty;

            var dlg = expr.TryCompile<Func<object>>();

            Assert.IsNotNull(dlg);
            Assert.AreEqual(string.Empty, dlg());
        }

        [Test]
        public void Ternarary_operator_with_greater_then()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<object>> expr = () => x > 0 ? string.Concat(s, "ccc") : string.Empty;

            var dlg = expr.TryCompile<Func<object>>();

            Assert.IsNotNull(dlg);
            Assert.AreEqual(string.Concat(s, "ccc"), dlg());
        }

        [Test]
        public void Ternarary_operator_with_logical_op()
        {
            var x = 1;
            var s = "Test";
            Expression<Func<object>> expr = () => 
                x > 0 && 
                (s.Contains("e") && s.Contains("X") || 
                 s.StartsWith("T") && s.EndsWith("t")) 
                ? string.Concat(s, "ccc") 
                : string.Empty;

            var dlg = expr.TryCompile<Func<object>>();

            Assert.IsNotNull(dlg);
            Assert.AreEqual(string.Concat(s, "ccc"), dlg());
        }

        [Test]
        public void CompileFast_should_return_null_when_option_is_set_and_expression_type_is_not_supported()
        {
            Assert.IsNull(Expression.Lambda(
                Expression.Coalesce(Expression.Constant("not null"), Expression.Constant("null")))
                .CompileFast(ifFastFailedReturnNull: true));
        }

        [Test]
        public void Test_IfThenElse()
        {
            const bool test = true;

            // This expression represents the conditional block.
            var ifThenElseExpr = Expression.IfThenElse(
                Expression.Constant(test),
                Expression.Call(
                    GetType(),
                    "WriteLine", Type.EmptyTypes,
                    Expression.Constant("The condition is true.")
                ),
                Expression.Call(
                    GetType(),
                    "WriteLine", Type.EmptyTypes,
                    Expression.Constant("The condition is false.")
                )
            );

            // The following statement first creates an expression tree,
            // then compiles it, and then runs it.
            var f = Expression.Lambda<Action>(ifThenElseExpr).CompileFast(true);
            Assert.IsNotNull(f);

            f();
        }

        public static void WriteLine(string s) => Console.WriteLine(s);
    }
}
