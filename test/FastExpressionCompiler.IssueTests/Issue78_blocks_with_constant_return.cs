using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
            Block2();
            return 6;
        }

        [Test]
        public void BlockWithConstantReturnIsSupported()
        {
            var ret = Block(Label(Label(typeof(int)), Constant(7)));
            var lambda = Lambda<Func<int>>(ret);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual(7, fastCompiled());
        }

        [Test]
        public void MultipleConstantReturnsAreRemoved()
        {
            var ret = Block(Constant(7), Constant(7), Constant(7));
            var lambda = Lambda<Func<int>>(ret);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual(7, fastCompiled());

            fastCompiled.AssertOpCodes(
                OpCodes.Ldc_I4_7,
                OpCodes.Ret
            );
        }

        [Test]
        public void ConstantReturnIsSupported()
        {
            var lambda = Lambda<Func<int>>(Label(Label(typeof(int)), Constant(7)));
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual(7, fastCompiled());
        }

        [Test]
        public void ConstantReturnIsSupported2()
        {
            var varr = Variable(typeof(int), "xxx");
            var assign = Assign(varr, Constant(7));
            var lambda = Lambda<Func<int>>(Block(new List<ParameterExpression> { varr }, assign, Label(Label(typeof(int)), varr)));
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual(7, fastCompiled());
        }

        [Test]
        public void Block1()
        {
            var ret = Block(Constant(7));
            var lambda = Lambda<Action>(ret);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            fastCompiled();
        }

        [Test]
        public void Block2()
        {
            var p = Parameter(typeof(object));
            var ret = Block(Convert(p, typeof(string)));
            var lambda = Lambda<Action<object>>(ret, p);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            fastCompiled("a");
            Asserts.Throws<InvalidCastException>(() => fastCompiled(1));
        }
    }
}
