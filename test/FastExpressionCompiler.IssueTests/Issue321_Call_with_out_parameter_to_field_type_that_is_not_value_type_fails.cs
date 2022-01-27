using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue321_Call_with_out_parameter_to_field_type_that_is_not_value_type_fails : ITest
    {
        public int Run()
        {
            Test_outparameter();
            return 1;
        }

        class TestPOD
        {
            public string stringvalue;
            public int intvalue;
        }

        static private void TestStringOutMethod(string input, out string output) => output = input;
        static private void TestIntOutMethod(int input, out int output) => output = input;

        [Test]
        public void Test_outparameter()
        {
            var stringMethod = this.GetType().GetMethod("TestStringOutMethod", bindingAttr: BindingFlags.NonPublic | BindingFlags.Static)!;
            var intMethod = this.GetType().GetMethod("TestIntOutMethod", bindingAttr: BindingFlags.NonPublic | BindingFlags.Static)!;

            var pod = new TestPOD();

            var program = Expression.Block(
                Expression.Call(null, stringMethod,
                    Expression.Constant("hello world"), Expression.Field(Expression.Constant(pod), pod.GetType().GetField("stringvalue")!)),
                Expression.Call(null, intMethod,
                    Expression.Constant(4), Expression.Field(Expression.Constant(pod), pod.GetType().GetField("intvalue")!))
            );

            // Make a lambda and compile it
            var expr = Expression.Lambda<Action>(program);
            expr.PrintCSharp(s => s.Replace(GetType().Name + ".", ""));
            // the output:
            var a = (Action)(() => //$
            {
                TestStringOutMethod(
                    "hello world",
                    out default(TestPOD)/*(!) Please provide the non-default value for the constant*/.stringvalue);
                TestIntOutMethod(
                    4,
                    out default(TestPOD) /* (!) Please provide the non-default value for the constant*/.intvalue);
            });

            var fSys = expr.CompileSys();
            fSys.PrintIL("sys");
            fSys();

            var fFast = expr.CompileFast();
            fFast.PrintIL("fast");
            fFast();
        }
    }
}