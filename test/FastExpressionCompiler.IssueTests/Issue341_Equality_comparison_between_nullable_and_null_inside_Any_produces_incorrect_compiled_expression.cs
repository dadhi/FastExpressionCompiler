using System;
using System.Linq;
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
            Compare_nullable_decimal_with_null_not_equal_should_work();
            Compare_nullable_decimal_with_null_should_work();
            Compare_nullable_decimal_member_with_null_should_work();
            Works_with_Compile_but_not_with_CompileFast();
            return 4;
        }

        [Test]
        public void Compare_nullable_decimal_with_null_should_work()
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
        public void Compare_nullable_decimal_with_null_not_equal_should_work()
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
        public void Compare_nullable_decimal_member_with_null_should_work()
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
        public void Works_with_Compile_but_not_with_CompileFast()
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