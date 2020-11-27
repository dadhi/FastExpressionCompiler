using System;
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
    [TestFixture]
    public class Issue107_Assign_also_works_for_variables : ITest
    {
        public int Run()
        {
            VariableAssignIsSupported();
            VariableAssignIsSupportedWithUneededConstant();
            VariableAssignIsSupportedWithConstantReturn();
            VariableAddAssignIsSupported();
            return 4;
        }

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
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
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
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
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
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(9, fastCompiled());
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
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(15, fastCompiled());
        }
    }
}
