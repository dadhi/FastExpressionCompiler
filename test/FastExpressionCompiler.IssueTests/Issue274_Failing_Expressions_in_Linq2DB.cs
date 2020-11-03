using System;
using NUnit.Framework;
#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue274_Failing_Expressions_in_Linq2DB : ITest
    {
        public int Run()
        {
            The_expression_with_anonymous_class_should_output_without_special_symbols();
            return 1;
        }

        [Test]
        public void The_expression_with_anonymous_class_should_output_without_special_symbols() 
        {
            int? fortyTwo = 42;
            var e = Lambda<Func<int?>>(
                PropertyOrField(Constant(new { X = fortyTwo }), "X"));

            var es = e.ToExpressionString();
            StringAssert.DoesNotContain("<>", es);

            var ec = e.ToCSharpString();
            StringAssert.DoesNotContain("<>", ec);

            ExpressionCompiler.EnableDelegateDebugInfo = true;
            var f = e.CompileFast(true);

            var de = (f.Target as ExpressionCompiler.IDelegateDebugInfo)?.Expression;
            Assert.IsNotNull(de);
        }
    }
}