using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    class Issue100_ExpressionInfo_wrong_return_type
    {
        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void RefAssignExpInfo()
        {
            var objRef = FastExpressionCompiler.ExpressionInfo.Parameter(typeof(double).MakeByRefType());
            var lambda = FastExpressionCompiler.ExpressionInfo.Lambda<ActionRef<double>>(FastExpressionCompiler.ExpressionInfo.Assign(objRef, FastExpressionCompiler.ExpressionInfo.Add(objRef, FastExpressionCompiler.ExpressionInfo.Constant((double)3.0))), objRef);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }

        [Test]
        public void RefAssignExpression()
        {
            var objRef = Expression.Parameter(typeof(double).MakeByRefType());
            var lambda = Expression.Lambda<ActionRef<double>>(Expression.Assign(objRef, Expression.Add(objRef, Expression.Constant((double)3.0))), objRef);

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
