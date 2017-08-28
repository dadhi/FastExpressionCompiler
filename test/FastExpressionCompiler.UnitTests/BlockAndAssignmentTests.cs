using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class BlockAndAssignmentTests
    {
        [Test]
        public void Simple_local_variable_assignment()
        {
            var variable = Expression.Variable(typeof(int));
            var assign = Expression.Assign(variable, Expression.Constant(5));

            var expressions = new List<Expression>
            {
                variable,
                assign,
                variable
            };

            var block = Expression.Block(new[] { variable }, expressions);

            var lambda = Expression.Lambda<Func<int>>(block);

            var stdCompiled = lambda.Compile();

            Assert.NotNull(stdCompiled);
            Assert.AreEqual(5, stdCompiled());

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        [Test]
        public void Simple_local_variable_assignment_with_return()
        {
            var returnTarget = Expression.Label(typeof(bool));

            var variable = Expression.Variable(typeof(int));
            var assign = Expression.Assign(variable, Expression.Constant(5));
            var test = Expression.LessThan(variable, Expression.Constant(6));

            var ifTrue = Expression.Return(returnTarget, Expression.Constant(true));
            var ifFalse = Expression.Return(returnTarget, Expression.Constant(false));

            var expressions = new List<Expression>
            {
                variable,
                assign,
                Expression.IfThenElse(test, ifTrue, ifFalse),
                Expression.Label(returnTarget, Expression.Constant(false))
            };

            var block = Expression.Block(new[] { variable }, expressions);

            var lambda = Expression.Lambda<Func<bool>>(block);

            var stdCompiled = lambda.Compile();

            Assert.NotNull(stdCompiled);
            Assert.IsTrue(stdCompiled());

            var fastCompiled = ExpressionCompiler.TryCompile<Func<bool>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.IsTrue(fastCompiled());
        }
    }
}
