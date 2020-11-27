using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ILDebugging.Decoder;
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
    public class Issue78_blocks_with_constant_return : ITest
    {
        public int Run()
        {
            BlockWithConstantReturnIsSupported();
            MultipleConstantReturnsAreRemoved();
            ConstantReturnIsSupported();
            ConstantReturnIsSupported2();
            Block1();
#if !LIGHT_EXPRESSION
            Block2();
            return 6;
#else
            return 5;
#endif
        }

        [Test]
        public void BlockWithConstantReturnIsSupported()
        {
            var ret = Block(Label(Label(typeof(int)), Constant(7)));
            var lambda = Lambda<Func<int>>(ret);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());
        }

        [Test]
        public void MultipleConstantReturnsAreRemoved()
        {
            var ret = Block(Constant(7), Constant(7), Constant(7));
            var lambda = Lambda<Func<int>>(ret);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.IsNotNull(fastCompiled);
            Assert.AreEqual(7, fastCompiled());

            var il = ILReaderFactory.Create(fastCompiled.Method);
            CollectionAssert.AreEqual(il.Select(x => x.OpCode),
                new[] {OpCodes.Ldc_I4_7,  OpCodes.Ret});
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

        [Test]
        public void Block1()
        {
            var ret = Block(Constant(7));
            var lambda = Lambda<Action>(ret);
            var fastCompiled = lambda.CompileFast(true);
            Assert.IsNotNull(fastCompiled);
            fastCompiled();
        }

#if !LIGHT_EXPRESSION

        [Test]
        public void Block2()
        {
            var p = Parameter(typeof(object));
            var ret = Block(Convert(p, typeof(string)));
            var lambda = Lambda<Action<object>>(ret, p);
            var fastCompiled = lambda.CompileFast(true);
            Assert.IsNotNull(fastCompiled);
            Assert.DoesNotThrow(() => fastCompiled("a"));
            Assert.Throws<InvalidCastException>(() => fastCompiled(1));
        }
#endif
    }
}
