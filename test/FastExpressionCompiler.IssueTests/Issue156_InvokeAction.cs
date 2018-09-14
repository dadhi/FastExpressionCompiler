using System;
using System.Linq;
using System.Reflection;
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
    public class Issue156_InvokeAction
    {
        [Test]
        public void InvokeActionConstantIsSupported()
        {
            Action<object, object> testAction = (o1, o2) => Console.WriteLine($"1: {o1}, 2: {o2}");

            var actionConstant = Constant(testAction, typeof(Action<object, object>));
            var one = Constant(1, typeof(object));
            var two = Constant(2, typeof(object));
            var actionInvoke = Invoke(actionConstant, one, two);

            var invokeLambda = Lambda<Action>(actionInvoke);
            var invokeFunc = invokeLambda.CompileFast();

            invokeFunc.Invoke();
        }
    }
}