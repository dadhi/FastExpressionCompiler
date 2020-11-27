using NUnit.Framework;
using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue44_Conversion_To_Nullable_Throws_Exception : ITest
    {
        public int Run()
        {
            Conversion_to_nullable_should_work_with_null_constructed_with_expressions();

#if !LIGHT_EXPRESSION
            Conversion_to_nullable_should_work();
            Conversion_to_nullable_should_work_with_null();
            return 3;
#else            
            return 1;
#endif
        }


#if !LIGHT_EXPRESSION
        [Test]
        public void Conversion_to_nullable_should_work()
        {
            Expression<Func<int?>> expression = () => 42;
            int? answer = expression.CompileFast(true).Invoke();

            Assert.IsTrue(answer.HasValue);
            Assert.AreEqual(42, answer.Value);
        }

        [Test]
        public void Conversion_to_nullable_should_work_with_null()
        {
            Expression<Func<int?>> expression = () => null;
            int? answer = expression.CompileFast(true).Invoke();

            Assert.IsFalse(answer.HasValue);
        }
#endif

        [Test]
        public void Conversion_to_nullable_should_work_with_null_constructed_with_expressions()
        {
            var expr = Lambda<Func<int?>>(Convert(Constant(null), typeof(int?)));

            expr.PrintCSharp();

            int? answer = expr.CompileFast(true).Invoke();

            Assert.IsFalse(answer.HasValue);
        }
    }
}
