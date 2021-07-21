using NUnit.Framework;
using System;
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue307_Switch_with_fall_through_throws_InvalidProgramException : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
            var param = Parameter(typeof(int), "p");
            var label = Label(typeof(string));

            var switchStatement = Switch(
                typeof(void),
                param,
                Return(label, Constant("bar")),
                null,
                SwitchCase(Return(label, Constant("foo")), Constant(1), Constant(2)));

            var lambda = Lambda<Func<int, string>>(
                Block(
                    switchStatement,
                    Label(label, Constant(string.Empty))), 
                param);

            lambda.PrintCSharp();
            // (Func<int, string>)((int p) => //$
            // {
            //     switch (p)
            //     {
            //         case (int)1:
            //         case (int)2:
            //             return "foo";
            //         default:
            //             return "bar";
            //     }
            //     string__58225482:;
            // });

            var compiled = lambda.CompileSys();
            compiled.PrintIL();

            var res = compiled(1);
            Assert.AreEqual("foo", res);

            var compiledFast = lambda.CompileFast();
            compiledFast.PrintIL();

            var resFast = compiledFast(1);
            Assert.AreEqual("foo", resFast);
        }
    }
}