using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue100_LightExpression_wrong_return_type : ITest
    {
        public int Run()
        {
            RefAssignExpression();
            return 1;
        }

        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void RefAssignExpression()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(Assign(objRef, Add(objRef, Constant((double)3.0))), objRef);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }
    }
}
