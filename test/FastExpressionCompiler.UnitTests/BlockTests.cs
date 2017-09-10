using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class BlockTests
    {
        [Test]
        public void Block_local_variable_assignment()
        {
            var variable = Variable(typeof(int));
            var variable2 = Variable(typeof(int));

            var expressions = new List<Expression>
            {
                Assign(variable, Constant(5)),
                Assign(variable2, Constant(6))
            };

            var block = Block(new[] { variable, variable2 }, expressions);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(6, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda()
        {
            var variable = Variable(typeof(int));
            var variable2 = Variable(typeof(int));

            var expressions = new List<Expression>
            {
                Assign(variable, Constant(5)),
                Invoke(Lambda(Assign(variable2, Constant(6))))
            };

            var block = Block(new[] { variable, variable2 }, expressions);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(6, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_return_with_variable()
        {
            var variable = Variable(typeof(int));

            var expressions = new List<Expression>
            {
                Assign(variable, Constant(5)),
                variable
            };

            var block = Block(new[] { variable }, expressions);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_member_init()
        {
            var variable = Variable(typeof(A));

            var expressions = new List<Expression>
            {
                Assign(variable, MemberInit(New(typeof(A).GetConstructor(Type.EmptyTypes)), Bind(typeof(A).GetProperty("K"), Constant(5)))),
                Property(variable, typeof(A).GetProperty("K"))
            };

            var block = Block(new[] { variable }, expressions);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        private class A
        {
            public int K { get; set; }
        }
    }
}
