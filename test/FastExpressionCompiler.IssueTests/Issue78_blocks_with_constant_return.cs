using System;
using System.Collections.Generic;
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
    class Issue78_blocks_with_constant_return
    {
        [Test]
        public void BlockWithConstanReturnIsSupported()
        {
            var ret = Block(Label(Label(typeof(int)), Constant(7)));
            var lambda = Lambda<Func<int>>(ret);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }

        [Test]
        public void ConstantReturnIsSupported()
        {
            var lambda = Lambda<Func<int>>(Label(Label(typeof(int)), Constant(7)));
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }

        [Test]
        public void ConstantReturnIsSupported2()
        {
            var varr = Variable(typeof(int), "xxx");
            var assign = Assign(varr, Constant(7));
            var lambda = Lambda<Func<int>>(Block(new List<ParameterExpression> { varr }, assign, Label(Label(typeof(int)), varr)));
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }
    }
}
