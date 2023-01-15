using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public class Issue353_NullReferenceException_when_calling_CompileFast_results : ITest
    {
        public int Run()
        {
            Test0_1();
            // Test1();
            return 2;
        }

        [Test]
        public void Test0_1()
        {
            var sumFunc = Parameter(typeof(Func<int, int>), "sumFunc");
            var i = Parameter(typeof(int), "i");
            var n = Parameter(typeof(int), "n");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { sumFunc },
                    Assign(
                        sumFunc,
                        Lambda(
                            MakeBinary(ExpressionType.Add, i, Constant(45)),
                            i)),
                    Invoke(sumFunc, n)
                ),
                n);

            expr.PrintCSharp();
            // print outputs valid csharp code:
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<int, int> sumFunc = null;
                sumFunc = (Func<int, int>)((int i) =>
                    (i + 45));
                return new Func<int, int>(
                    sumFunc).Invoke(
                    n);
            });
            Assert.AreEqual(55, @cs(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(55, x);

            var f = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            f.PrintIL();

            var y = f(10);
            Assert.AreEqual(55, y);
        }

        [Test]
        public void Test1()
        {
            var sumFunc = Parameter(typeof(Func<int, int>), "sumFunc");
            var i = Parameter(typeof(int), "i");
            var n = Parameter(typeof(int), "n");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { sumFunc },
                    Assign(
                        sumFunc,
                        Lambda(
                            Condition(
                                MakeBinary(ExpressionType.Equal, i, Constant(0)),
                                Constant(0),
                                MakeBinary(
                                    ExpressionType.Add,
                                    i,
                                    Invoke(
                                        sumFunc,
                                        MakeBinary(ExpressionType.Subtract, i, Constant(1))))),
                            i)),
                    Invoke(sumFunc, n)),
                n);

            expr.PrintCSharp();
            // print outputs valid csharp code:
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<int, int> func_int_int___58225482 = null;
                func_int_int___58225482 = (Func<int, int>)((int i) =>
                    (i == 0) ?
                        0 :
                        (i + new Func<int, int>(
                            func_int_int___58225482).Invoke(
                            (i - 1))));
                return new Func<int, int>(
                    func_int_int___58225482).Invoke(
                    n);
            });
            Assert.AreEqual(55, @cs(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(55, x);

            var f = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            f.PrintIL();

            var y = f(10);
            Assert.AreEqual(55, y);
        }
    }
}