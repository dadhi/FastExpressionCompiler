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
            Test1();
            // Test2();
            return 2;
        }

        [Test]
        public void Test1()
        {
            var sumFunc = Parameter(typeof(Func<int, int>), "sumFunc");
            var i = Parameter(typeof(int), "i");
            var n = Parameter(typeof(int), "n");
            var m = Parameter(typeof(int), "m");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { sumFunc, m },
                    Assign(m, Constant(45)),  // let's assign before and see if the variable value is correctly used in the nested lambda
                    Assign(sumFunc, Lambda(MakeBinary(ExpressionType.Add, i, m), i)),
                    Assign(m, Constant(999)), // todo: @fixme assign the variable later when the lambda is already created above
                    Invoke(sumFunc, n)
                ),
                n);

            expr.PrintCSharp();
            // print outputs valid csharp code:
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<int, int> sumFunc = null;
                int m;
                m = 45;
                sumFunc = (Func<int, int>)((int i) =>
                    (i + m));
                m = 999;
                return new Func<int, int>(
                    sumFunc).Invoke(
                    n);
            });
            Assert.AreEqual(1009, @cs(10));

            // how it is done right now
            var @cs2 = (Func<int, int>)((int n) =>
            {
                Func<object[], int, int> sumFunc = null;
                int m;
                m = 45;
                var closure = new object[] { m };
                sumFunc = (Func<object[], int, int>)(
                    (object[] cl, int i) =>
                    {
                        var m1 = (int)cl[0];
                        return i + m1;
                    });
                closure[0] = 999;
                return sumFunc(closure, n);
            });
            Assert.AreEqual(1009, @cs2(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(1009, x);

            var f = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            f.PrintIL();

            var y = f(10);
            Assert.AreEqual(55, y);
        }

        [Test]
        public void Test2()
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