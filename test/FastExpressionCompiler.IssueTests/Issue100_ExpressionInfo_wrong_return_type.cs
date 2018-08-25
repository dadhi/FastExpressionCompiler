using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    class Issue100_ExpressionInfo_wrong_return_type
    {
        delegate void ActionRef<T>(ref T a1);

        [Test, Ignore("needs fix")]
        public void RefAssignExpInfo()
        {
            var objRef = FastExpressionCompiler.ExpressionInfo.Parameter(typeof(double).MakeByRefType());
            var lambda = FastExpressionCompiler.ExpressionInfo.Lambda<ActionRef<double>>(FastExpressionCompiler.ExpressionInfo.Assign(objRef, FastExpressionCompiler.ExpressionInfo.Add(objRef, FastExpressionCompiler.ExpressionInfo.Constant((double)3.0))), objRef);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }
    }
}
