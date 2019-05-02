﻿using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    public class Issue190_Inc_Dec_Assign_Parent_Block_Var
    {
        [Test]
        public void PreIncOfParentBlockVarIsSupported()
        {
            var eVar1 = Variable(typeof(int));
            var eVar2 = Variable(typeof(int));
            
            var blockExpr =
                Block(new[] { eVar1 },
                    Block(new[] { eVar2 }, PreIncrementAssign(eVar1))
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(1, fastCompiled());
        }
    }
}
