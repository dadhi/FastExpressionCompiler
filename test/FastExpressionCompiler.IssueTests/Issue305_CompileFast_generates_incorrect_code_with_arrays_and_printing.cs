using NUnit.Framework;
using System;
using System.Linq.Expressions;
#if LIGHT_EXPRESSION
using System.Text;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue305_CompileFast_generates_incorrect_code_with_arrays_and_printing : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        public static void WriteLine(double d) {}

        [Test]
        public void Test1()
        {
            var arr = Variable(typeof(double[]), "arr");

            var printDouble = this.GetType().GetMethod(nameof(WriteLine), new[] { typeof(double) });

            var expr = Lambda<Func<double>>(
                Block(new[] { arr },
                    Assign(arr, NewArrayBounds(typeof(double), Constant(1))),
                    Assign(ArrayAccess(arr, Constant(0)), Constant(123.456)),

                    Call(printDouble, ArrayAccess(arr, Constant(0))),
                    Call(printDouble, ArrayAccess(arr, Constant(0))),
                    Call(printDouble, ArrayAccess(arr, Constant(0))),
                    Call(printDouble, ArrayAccess(arr, Constant(0))),
                    Call(printDouble, ArrayAccess(arr, Constant(0))),
                    Call(printDouble, ArrayAccess(arr, Constant(0))),
                    Call(printDouble, ArrayAccess(arr, Constant(0))),

                    ArrayAccess(arr, Constant(0))
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var res = fs();
            Assert.AreEqual(123.456, res);

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            var res2 = ff();
            Assert.AreEqual(123.456, res2);
        }
    }
}