using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    class Issue100_ExpressionInfo_wrong_return_type
    {
        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void RefAssignExpression()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(Assign(objRef, Add(objRef, Constant((double)3.0))), objRef);

            var compiledA = lambda.Compile();
            var compiledB = lambda.CompileFast(true);
            var exampleA = 5.0;
            var exampleB = 5.0;
            compiledA(ref exampleA);
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleA);
            Assert.AreEqual(8.0, exampleB);
        }
    }
}
