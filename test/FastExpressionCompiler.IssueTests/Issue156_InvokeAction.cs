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
            InvokeFuncConstantIsSupported();
            InvokeActionConstantIsSupported();
            return 2;
        }

        string _result;
        string Join(object x, object y) => _result = ("" + x) + y;

        [Test]
        public void InvokeActionConstantIsSupported()
        {
            Action<object, object> testAction = (o1, o2) => Join(o1, o2);

            var actionConstant = Constant(testAction, typeof(Action<object, object>));
            var one = Constant(4, typeof(object));
            var two = Constant(2, typeof(object));
            var invoke = Invoke(actionConstant, one, two);
            var expr = Lambda<Action>(invoke);

            expr.PrintCSharp();

            // var x = (Func<string, Issue308_Wrong_delegate_type_returned_with_closure.Command>)((string vm) => //$
            //     (Issue308_Wrong_delegate_type_returned_with_closure.Command)(() => //$
            //         vm));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            ff.Invoke();
            Asserts.AreEqual("42", _result);
        }

        [Test]
        public void InvokeFuncConstantIsSupported()
        {
            Func<int, int, string> testAction = (n1, n2) => "" + n1 + n2;
            var funcConstant = Constant(testAction);
            var one = Constant(4);
            var two = Constant(2);
            var invoke = Invoke(funcConstant, one, two);
            var expr = Lambda<Func<string>>(invoke);

            expr.PrintCSharp();

            // var x = (Func<string, Issue308_Wrong_delegate_type_returned_with_closure.Command>)((string vm) => //$
            //     (Issue308_Wrong_delegate_type_returned_with_closure.Command)(() => //$
            //         vm));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            var result = ff.Invoke();
            Asserts.AreEqual("42", result);
        }
    }
}