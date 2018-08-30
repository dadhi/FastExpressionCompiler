using System;
using System.Linq.Expressions;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue107_Assign_also_works_for_variables
    {
        [Test]
        public void VariableAssignIsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    Assign(eVar,Constant(7)),
                    eVar
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            var compiled = lambda.Compile();
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(compiled(), fastCompiled());
        }

        [Test]
        public void VariableAssignIsSupportedWithUneededConstant()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    Assign(eVar, Constant(7)),
                    Constant(9),
                    eVar
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            var compiled = lambda.Compile();
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(compiled(), fastCompiled());
        }

        [Test]
        public void VariableAssignIsSupportedWithConstantReturn()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    Assign(eVar, Constant(7)),
                    eVar,
                    Constant(9)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            var compiled = lambda.Compile();
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(compiled(), fastCompiled());
        }

        [Test]
        public void VariableAddAssignIsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    Assign(eVar, Constant(7)),
                    AddAssign(eVar, Constant(8)),
                    eVar
                );
            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            var compiled = lambda.Compile();
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(compiled(), fastCompiled());
        }
    }
}
