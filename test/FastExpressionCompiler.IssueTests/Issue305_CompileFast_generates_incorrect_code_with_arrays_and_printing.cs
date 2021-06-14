using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
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

        private List<double> _items = new List<double>();

        public void WriteLine(double n) => _items.Add(n);

        [Test]
        public void Test1()
        {
            var arr = Variable(typeof(double[]), "arr");

            var print = this.GetType().GetMethod(nameof(WriteLine), new[] { typeof(double) });

            var expr = Lambda<Func<double>>(
                Block(new[] { arr },
                    Assign(arr, NewArrayBounds(typeof(double), Constant(1))),
                    Assign(ArrayAccess(arr, Constant(0)), Constant(123.456)),

                    Call(Constant(this), print, ArrayAccess(arr, Constant(0))),
                    Call(Constant(this), print, ArrayAccess(arr, Constant(0))),

                    ArrayAccess(arr, Constant(0))
            ));

            expr.PrintCSharp();

            _items.Clear();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var res = fs();
            Assert.AreEqual(123.456, res);

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            var res2 = ff();
            Assert.AreEqual(123.456, res2);
            Assert.AreEqual(4, _items.Count(x => x == 123.456));
        }
    }
}