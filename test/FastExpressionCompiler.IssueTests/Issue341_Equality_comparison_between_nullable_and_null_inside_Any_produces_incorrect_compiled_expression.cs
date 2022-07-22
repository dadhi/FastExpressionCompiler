using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

#if !LIGHT_EXPRESSION
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests
{
    [TestFixture, Ignore("fixme")]
    public class Issue341_Equality_comparison_between_nullable_and_null_inside_Any_produces_incorrect_compiled_expression : ITest
    {
        public int Run()
        {
            foreach (var (a, op, b, expected) in Data)
            {
                Decimal_nullable_comparison_cases(a, op, b, expected);
            }

            Nullable_decimal_not_equal_to_zero();
            Nullable_decimal_greater_than_zero();
            Nullable_decimal_not_equal_decimal();
            Nullable_decimal_less_then_decimal();
            Nullable_decimal_not_equal_to_null();
            Null_not_equal_to_nullable_decimal();
            Nullable_decimal_equal_to_null();
            Nullable_decimal_member_not_equal_to_null();
            Nullable_decimal_member_not_equal_to_null_inside_predicate();

            return Data.Length + 9;
        }

        public enum Ops { Equal, NotEqual, Greater, Less, GreaterOrEqual, LessOrEqual }

        public static Expression<Func<decimal?, decimal?, bool>>[] opsExpressions = 
        {
            (a, b) => a == b,
            (a, b) => a != b,
            (a, b) => a >  b,
            (a, b) => a <  b,
            (a, b) => a >= b,
            (a, b) => a <= b,
        };

        public static readonly (decimal?, Ops, decimal?, bool)[] Data =
        {
            (0M, Ops.Equal, 0M, true),
            (0M, Ops.NotEqual, 0M, false),
            (1.12M, Ops.Greater, 1.11M, true),
            (1.12M, Ops.GreaterOrEqual, 1.11M, true),
            (1.12M, Ops.LessOrEqual, 1.11M, false),
            (1.101M, Ops.Less, 1.11M, true),
            (1.101M, Ops.LessOrEqual, 1.11M, true),
            (1.101M, Ops.Greater, 1.11M, false),
            (1.142M, Ops.NotEqual, null, true),
            (1.142M, Ops.Equal, null, false),
            (null, Ops.NotEqual, 1.366M, true),
            (null, Ops.Equal, 1.366M, false),
            (null, Ops.Equal, null, true),
            (null, Ops.NotEqual, null, false),
        };

        public static readonly IEnumerable<TestCaseData> TestCases = Data.Select(x => new TestCaseData(x.Item1, x.Item2, x.Item3, x.Item4));
        

        [Test, TestCaseSource(nameof(TestCases))]
        public void Decimal_nullable_comparison_cases(decimal? a, Ops op, decimal? b, bool expected)
        {
            var expression = opsExpressions[(int)op];
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(a, b);
            Assert.AreEqual(expected, result);

            result = compiledFast(a, b);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Nullable_decimal_not_equal_to_zero()
        {
            Expression<Func<decimal?, bool>> expression = n => n != 0M;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = default(decimal);

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsFalse(result);

            result = compiledFast(instance);
            Assert.IsFalse(result);
        }

        [Test]
        public void Nullable_decimal_greater_than_zero()
        {
            Expression<Func<decimal?, bool>> expression = n => n > 0M;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = default(decimal);

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsFalse(result);

            result = compiledFast(instance);
            Assert.IsFalse(result);
        }

        [Test]
        public void Nullable_decimal_not_equal_decimal()
        {
            Expression<Func<decimal?, bool>> expression = n => n != 1.11M;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = 1.111M;

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsTrue(result);

            result = compiledFast(instance);
            Assert.IsTrue(result);
        }

        [Test]
        public void Nullable_decimal_less_then_decimal()
        {
            Expression<Func<decimal?, bool>> expression = n => n < 1.11M;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = 1.101M;

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsTrue(result);

            result = compiledFast(instance);
            Assert.IsTrue(result);
        }

        [Test]
        public void Nullable_decimal_not_equal_to_null()
        {
            Expression<Func<decimal?, bool>> expression = n => n != null;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = default(decimal);

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsTrue(result);

            result = compiledFast(instance);
            Assert.IsTrue(result);
        }

        [Test]
        public void Null_not_equal_to_nullable_decimal()
        {
            Expression<Func<decimal?, bool>> expression = n => null != n;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = default(decimal);

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsTrue(result);

            result = compiledFast(instance);
            Assert.IsTrue(result);
        }

        [Test]
        public void Nullable_decimal_equal_to_null()
        {
            Expression<Func<decimal?, bool>> expression = n => n == null;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = default(decimal);

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsFalse(result);

            result = compiledFast(instance);
            Assert.IsFalse(result);
        }

        [Test]
        public void Nullable_decimal_member_not_equal_to_null()
        {
            // todo: @perf optimize comparison of nullable with null
            Expression<Func<Test2, bool>> expression = t => t.Value != null;
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();
            var compiledFast = expression.CompileFast(true);

            var instance = new Test2() { Value = 0 };

            compiledSys.PrintIL("sys");
            compiledFast.PrintIL("fast");

            var result = compiledSys(instance);
            Assert.IsTrue(result);

            result = compiledFast(instance);
            Assert.IsTrue(result);
        }

        [Test]
        public void Nullable_decimal_member_not_equal_to_null_inside_predicate()
        {
            Expression<Func<Test, bool>> expression = t => t.A.Any(e => e.Value != null);
            expression.PrintCSharp(); // just for debug

            var compiledSys = expression.Compile();

            var compiledFast = expression.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(compiledFast);
            var target = compiledFast.Target;
            var t = target as ExpressionCompiler.DebugArrayClosure;
            var closure = t.ConstantsAndNestedLambdas;

            var instance = new Test()
            {
                A = new[]
                {
                    new Test2() { Value = 0 },
                },
            };

            compiledSys.PrintIL("sys");

            compiledFast.PrintIL("fast");
            ((Delegate)closure[0]).PrintIL("predicate");

            var result = compiledSys(instance);
            Assert.IsTrue(result);

            result = compiledFast(instance);
            Assert.IsTrue(result);
        }

        public class Test
        {
            public Test2[] A { get; set; }
        }

        public class Test2
        {
            public decimal? Value { get; set; }
        }
    }
}
#endif