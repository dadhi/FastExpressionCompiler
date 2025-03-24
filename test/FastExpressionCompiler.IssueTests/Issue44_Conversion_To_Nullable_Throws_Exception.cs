
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

    public class Issue44_Conversion_To_Nullable_Throws_Exception : ITest
    {
        public int Run()
        {
            Conversion_to_nullable_should_work_with_null_constructed_with_expressions();
            Conversion_to_nullable_should_work();
            Conversion_to_nullable_should_work_with_null();
            return 3;
        }


        public void Conversion_to_nullable_should_work()
        {
            System.Linq.Expressions.Expression<Func<int?>> sExpression = () => 42;
            var expression = sExpression.FromSysExpression();
            int? answer = expression.CompileFast(true).Invoke();

            Asserts.IsTrue(answer.HasValue);
            Asserts.AreEqual(42, answer.Value);
        }


        public void Conversion_to_nullable_should_work_with_null()
        {
            System.Linq.Expressions.Expression<Func<int?>> sExpression = () => null;
            var expression = sExpression.FromSysExpression();
            int? answer = expression.CompileFast(true).Invoke();

            Asserts.IsFalse(answer.HasValue);
        }


        public void Conversion_to_nullable_should_work_with_null_constructed_with_expressions()
        {
            var expr = Lambda<Func<int?>>(Convert(Constant(null), typeof(int?)));

            expr.PrintCSharp();

            int? answer = expr.CompileFast(true).Invoke();

            Asserts.IsFalse(answer.HasValue);
        }
    }
}
