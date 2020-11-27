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
    public class Issue156_InvokeAction : ITest
    {
        public int Run() 
        {
            InvokeActionConstantIsSupported();
            return 1;
        }

        static string Join(object x, object y) => "" + x + y;

        [Test]
        public void InvokeActionConstantIsSupported()
        {
            Action<object, object> testAction = (o1, o2) => Join(o1, o2);

            var actionConstant = Constant(testAction, typeof(Action<object, object>));
            var one = Constant(1, typeof(object));
            var two = Constant(2, typeof(object));
            var actionInvoke = Invoke(actionConstant, one, two);

            var invokeLambda = Lambda<Action>(actionInvoke);
            var invokeFunc = invokeLambda.CompileFast(true);

            invokeFunc.Invoke();
        }
    }
}