using System;
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
    public class Issue404_String_plus_parameter_causes_Exception_in_target_invocation : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
            var param = Parameter(typeof(double));
            Expression sumExpr = Call(
                typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) }),
                Constant("The value is "),
                Condition(
                    Equal(ConvertChecked(param, typeof(double?)), Constant(null)),
                    Constant(""),
                    Call(param, typeof(double).GetMethod("ToString", Type.EmptyTypes))
                )
            );

            var e = Lambda<Func<double, string>>(sumExpr, param);
            e.PrintCSharp();

            Delegate dlg2 = e.CompileFast();

            double x = 1.6;
            var res2 = (string)dlg2.DynamicInvoke(x);

            Asserts.AreEqual(res2, "The value is " + x.ToString());
        }
    }
}