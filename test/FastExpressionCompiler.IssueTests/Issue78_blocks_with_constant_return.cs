using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    class Issue78_blocks_with_constant_return
    {
        [Test]
        [Ignore("needs fix")]
        public void BlockWithConstanReturnIsSupported()
        {
            var ret = Block(Label(Label(typeof(int)), Constant(7)));
            var lambda = Lambda<Func<int>>(ret);
            var compiled = lambda.Compile();
            var value1 = compiled();
            Assert.AreEqual(7, value1);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }

        [Test]
        [Ignore("needs fix")]
        public void ConstantReturnIsSupported()
        {
            var lambda = Lambda<Func<int>>(Label(Label(typeof(int)), Constant(7)));
            var compiled = lambda.Compile();
            var value1 = compiled();
            Assert.AreEqual(7, value1);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }

        [Test]
        [Ignore("needs fix")]
        public void ConstantReturnIsSupported2()
        {
            var varr = Variable(typeof(int), "xxx");
            var assign = Assign(varr, Constant(7));
            var lambda = Lambda<Func<int>>(Block(new List<ParameterExpression> { varr }, assign, Label(Label(typeof(int)), varr)));
            var compiled = lambda.Compile();
            var value1 = compiled();
            Assert.AreEqual(7, value1);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }
    }
}
