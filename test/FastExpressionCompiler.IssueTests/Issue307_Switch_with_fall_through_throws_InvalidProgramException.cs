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
            // (Func<int, string>)((int p) => //string
            // {
            //     switch (p)
            //     {
            //         case 1:
            //         case 2:
            //             return "foo";
            //         default:
            //             return "bar";
            //     }
            //     string__58225482:;
            // });

            var fs = lambda.CompileSys();
            fs.PrintIL();

            Asserts.AreEqual("foo", fs(1));
            Asserts.AreEqual("foo", fs(2));
            Asserts.AreEqual("bar", fs(42));

            var ff = lambda.CompileFast();
            ff.PrintIL();

            Asserts.AreEqual("foo", ff(1));
            Asserts.AreEqual("foo", ff(2));
            Asserts.AreEqual("bar", ff(42));
        }
    }
}