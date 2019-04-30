using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class Issue180_emit_loop
    {
        [Test]
        public void LoopIsSupported()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var returnLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeReturn = IfThen(variableMoreThanThree, Return(returnLabel));

            var loop = Loop(Block(ifMoreThanThreeReturn, incrementVariable));
            var lambdaBody = Block(new[] { intVariable }, loop, Label(returnLabel));

            var loopLambda = Lambda<Action>(lambdaBody);
            var loopFunc = loopLambda.CompileFast();

            loopFunc.Invoke();
        }

        [Test]
        public void LoopWithBreakIsSupported()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var breakLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeBreak = IfThen(variableMoreThanThree, Break(breakLabel));

            var loopBody = Block(new[] { intVariable }, ifMoreThanThreeBreak, incrementVariable);
            var loopWithBreak = Loop(loopBody, breakLabel);

            var loopLambda = Lambda<Action>(loopWithBreak);
            var loopFunc = loopLambda.CompileFast();

            loopFunc.Invoke();
        }
    }
}
