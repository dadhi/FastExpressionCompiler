using System;
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
            
            var block = Block(new[] { variable, variable2 }, 
                Assign(variable, Constant(5)), 
                Assign(variable2, Constant(6)));

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
            
            var block = Block(new[] { variable, variable2 }, 
                Assign(variable, Constant(5)),
                Invoke(Lambda(Assign(variable2, Constant(6)))));

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(6, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_with_param()
        {
            var variable = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var param = Parameter(typeof(int));
            
            var block = Block(new[] { variable }, 
                Assign(variable, Constant(5)),
                Invoke(Lambda(Block(new[] { variable2 }, 
                Assign(variable2, param)))));

            var lambda = Lambda<Func<int, int>>(block, param);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int, int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(8, fastCompiled(8));
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_with_param_override()
        {
            var variable = Variable(typeof(int));
            var param = Parameter(typeof(int));
            
            var block = Block(new[] { variable }, 
                Assign(variable, Constant(5)),
                Invoke(Lambda(Block(Assign(param, variable)))));

            var lambda = Lambda<Func<int, int>>(block, param);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int, int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled(8));
        }

        [Test]
        public void Block_local_variable_assignment_return_with_variable()
        {
            var variable = Variable(typeof(int));
            
            var block = Block(new[] { variable }, 
                Assign(variable, Constant(5)),
                variable);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = ExpressionCompiler.TryCompile<Func<int>>(lambda);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_member_init()
        {
            var variable = Variable(typeof(A));
            
            var block = Block(new[] { variable }, 
                Assign(variable, MemberInit(New(typeof(A).GetConstructor(Type.EmptyTypes)), Bind(typeof(A).GetProperty("K"), Constant(5)))),
                Property(variable, typeof(A).GetProperty("K")));

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
