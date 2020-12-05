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
    public class LoopTests : ITest
    {
        public int Run()
        {
            Loop_with_return();
            Loop_with_break();
            Loop_with_unused_break();
            Loop_with_break_and_continue();
            Loop_with_unused_continue();
            Loop_with_unused_break_and_continue();
            Loop_with_return_value();
            return 7;
        }

        [Test]
        public void Loop_with_return()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var returnLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeReturn = IfThen(variableMoreThanThree, Return(returnLabel));

            var loop = Loop(Block(ifMoreThanThreeReturn, incrementVariable));
            var lambdaBody = Block(new[] { intVariable }, loop, Label(returnLabel));

            var loopLambda = Lambda<Action>(lambdaBody);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            loopFunc.Invoke();
        }

        [Test]
        public void Loop_with_break()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var breakLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeBreak = IfThen(variableMoreThanThree, Break(breakLabel));

            var loopBody = Block(new[] { intVariable }, ifMoreThanThreeBreak, incrementVariable);
            var loopWithBreak = Loop(loopBody, breakLabel);

            var loopLambda = Lambda<Action>(loopWithBreak);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            loopFunc.Invoke();
        }

        [Test]
        public void Loop_with_unused_break()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var breakLabel = Label();
            var returnLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeReturn = IfThen(variableMoreThanThree, Return(returnLabel));

            var loop = Loop(Block(ifMoreThanThreeReturn, incrementVariable), breakLabel);
            var lambdaBody = Block(new[] { intVariable }, loop, Label(returnLabel));

            var loopLambda = Lambda<Action>(lambdaBody);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            loopFunc.Invoke();
        }

        [Test]
        public void Loop_with_break_and_continue()
        {
            var intVariable1 = Variable(typeof(int), "i");
            var incrementVariable1 = PreIncrementAssign(intVariable1);

            var intVariable2 = Variable(typeof(int), "j");
            var incrementVariable2 = PreIncrementAssign(intVariable2);

            var breakLabel = Label();
            var continueLabel = Label();

            var variable2EqualsZero = Equal(intVariable2, Constant(0));
            var incrementVariable2AndContinue = Block(incrementVariable2, Continue(continueLabel));
            var ifVariable2IsZeroIncrementAndContinue = IfThen(variable2EqualsZero, incrementVariable2AndContinue);

            var variable1MoreThanThree = GreaterThan(intVariable1, Constant(3));
            var ifVariable1MoreThanThreeBreak = IfThen(variable1MoreThanThree, Break(breakLabel));

            var loopBody = Block(ifVariable2IsZeroIncrementAndContinue, ifVariable1MoreThanThreeBreak, incrementVariable1);
            var loopWithBreakAndContinue = Loop(loopBody, breakLabel, continueLabel);
            var lambdaBody = Block(new[] { intVariable1, intVariable2 }, loopWithBreakAndContinue);

            var loopLambda = Lambda<Action>(lambdaBody);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            loopFunc.Invoke();
        }

        [Test]
        public void Loop_with_unused_continue()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var breakLabel = Label();
            var continueLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeReturn = IfThen(variableMoreThanThree, Break(breakLabel));

            var loop = Loop(Block(ifMoreThanThreeReturn, incrementVariable), breakLabel, continueLabel);
            var lambdaBody = Block(new[] { intVariable }, loop);

            var loopLambda = Lambda<Action>(lambdaBody);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            loopFunc.Invoke();
        }

        [Test]
        public void Loop_with_unused_break_and_continue()
        {
            var intVariable = Variable(typeof(int), "i");
            var incrementVariable = PreIncrementAssign(intVariable);

            var breakLabel = Label();
            var continueLabel = Label();
            var returnLabel = Label();

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeReturn = IfThen(variableMoreThanThree, Return(returnLabel));

            var loop = Loop(Block(ifMoreThanThreeReturn, incrementVariable), breakLabel, continueLabel);
            var lambdaBody = Block(new[] { intVariable }, loop, Label(returnLabel));

            var loopLambda = Lambda<Action>(lambdaBody);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            loopFunc.Invoke();
        }

        [Test]
        public void Loop_with_return_value()
        {
            var intVariable = Variable(typeof(int), "i");
            var assignVariable = Assign(intVariable, Constant(4));

            var returnLabel = Label(typeof(int));

            var variableMoreThanThree = GreaterThan(intVariable, Constant(3));
            var ifMoreThanThreeReturnFive = IfThen(variableMoreThanThree, Return(returnLabel, Constant(5)));

            var loop = Loop(Block(assignVariable, ifMoreThanThreeReturnFive));
            var lambdaBody = Block(new[] { intVariable }, loop, Label(returnLabel, Constant(3)));

            var loopLambda = Lambda<Func<int>>(lambdaBody);
            var loopFunc = loopLambda.CompileFast(true);

            Assert.IsNotNull(loopFunc);

            var result = loopFunc.Invoke();

            Assert.AreEqual(5, result);
        }
    }
}
