using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    class Issue91_Issue95_Tests
    {
        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void RefAssign()
        {
            var objRef = Expression.Parameter(typeof(double).MakeByRefType());
            var lambda = Expression.Lambda<ActionRef<double>>(Expression.Assign(objRef, Expression.Add(objRef, Expression.Constant((double)3.0))), objRef);

            var compiledA = lambda.Compile();
            var exampleA = 5.0;
            compiledA(ref exampleA);
            Assert.AreEqual(8.0, exampleA);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }

        [Test]
        public void NullComparisonTest()
        {
            var pParam = Expression.Parameter(typeof(string), "p");

            var condition = Expression.Condition(Expression.NotEqual(pParam, Expression.Constant(null)),
                Expression.Constant(1),
                Expression.Constant(0));
            var lambda = Expression.Lambda<Func<string, int>>(condition, pParam);
            var convert0 = lambda.Compile();
            Assert.NotNull(convert0);
            var convert1 = FastExpressionCompiler.ExpressionCompiler.CompileFast(lambda, true);
            Assert.NotNull(convert1);
        }

        [Test]
        public void TestAddAssign()
        {
            var objRef = Expression.Parameter(typeof(double).MakeByRefType());
            var lambda = Expression.Lambda<ActionRef<double>>(Expression.AddAssign(objRef, Expression.Constant((double)3.0)), objRef);

            var compiledA = lambda.Compile();
            var exampleA = 5.0;
            compiledA(ref exampleA);
            Assert.AreEqual(8.0, exampleA);

            var compiledB = lambda.CompileFast<ActionRef<double>>(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }
    }
}
