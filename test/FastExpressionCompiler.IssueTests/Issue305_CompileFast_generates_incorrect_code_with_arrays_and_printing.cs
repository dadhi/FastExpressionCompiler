using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
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
            Test_double_array_index_access();
            Test_double_array_index_access_and_instance_call();
            return 2;
        }

        private List<double> _items = new List<double>();

        public void WriteLine(double n) => _items.Add(n);
        public void WriteLine(int n) => _items.Add(n);

        [Test]
        public void Test_double_array_index_access()
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

        [Test]
        public void Test_double_array_index_access_and_instance_call()
        {
            var arr = Variable(typeof(double[]), "arr");

            var print = this.GetType().GetMethod(nameof(WriteLine), new[] { typeof(int) });
            var compareTo = typeof(double).GetMethod(nameof(double.CompareTo), new[] { typeof(double) });

            var expr = Lambda<Func<double>>(
                Block(new[] { arr },
                    Assign(arr, NewArrayBounds(typeof(double), Constant(1))),
                    Assign(ArrayAccess(arr, Constant(0)), Constant(123.456)),

                    Call(Constant(this), print, Call(ArrayAccess(arr, Constant(0)), compareTo, Constant(123.456))),
                    Call(Constant(this), print, Call(ArrayAccess(arr, Constant(0)), compareTo, Constant(123.456))),

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
            Assert.AreEqual(4, _items.Count(x => x == 0));
        }
    }
}