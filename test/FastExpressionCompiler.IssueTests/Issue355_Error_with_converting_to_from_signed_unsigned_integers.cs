using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue355_Error_with_converting_to_from_signed_unsigned_integers : ITest
    {
        public int Run()
        {
            Test0_1();
            // Test1();
            return 2;
        }

        [Test]
        public void Test0_1()
        {
            var x = -1;
            var y = 0x7fffffff;
            var param = Parameter(typeof(int));
            var lambda = Lambda<Func<int,uint>>(
                RightShift(Convert(param, typeof(uint)), Constant(1)),
                param
            );
            Assert.AreEqual(y,lambda.CompileSys()(x));
            Assert.AreEqual(y,lambda.CompileFast(true)(x));
        }
    }
}