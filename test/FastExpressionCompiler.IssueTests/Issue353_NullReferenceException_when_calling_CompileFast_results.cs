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
            Test1_simplified();
            Test1_isolated_assign_int_to_the_array_of_objects_and_use_for_addition();
            Test1_closure_over_array_and_changing_its_element();
            Test1_manual_closure();
            Test2_original_issue_case();
            return 6;
        }

        [Test]
        public void Test1_isolated_assign_int_to_the_array_of_objects_and_use_for_addition()
        {
            var n = Parameter(typeof(int), "n");
            var a = Parameter(typeof(object[]), "a");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { a },
                    Assign(a, NewArrayInit(typeof(object), Constant(0, typeof(object)))),
                    Assign(ArrayAccess(a, Constant(0)), Constant(999, typeof(object))),
                    Add(n, Convert(ArrayAccess(a, Constant(0)), typeof(int)))
                ),
                n);

            expr.PrintCSharp();
            // c# output
            var @cs = (Func<int, int>)((int n) =>
            {
                object[] a = default;
                a = new object[]{(object)0};
                a[0] = (object)999;
                return (n + ((int)a[0]));
            });
            Assert.AreEqual(1009, @cs(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(1009, x);

            var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(ff);
            ff.PrintIL();

            var y = ff(10);
            Assert.AreEqual(1009, y);
        }

        [Test]
        public void Test1_closure_over_array_and_changing_its_element()
        {
            var n = Parameter(typeof(int), "n");
            var i = Parameter(typeof(int), "i");

            var f = Parameter(typeof(Func<int, int>), "f");
            var a = Parameter(typeof(object[]), "a");
            var b = Parameter(typeof(object[]), "b");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { f, b },
                    Assign(f, Lambda<Func<int, int>>(
                        MakeBinary(ExpressionType.Add, i, Convert(ArrayAccess(b, Constant(0)), typeof(int))),
                        i)),
                    Assign(b, NewArrayInit(typeof(object), Constant(999, typeof(object)))),
                    Invoke(f, n)
                ),
                n);

            expr.PrintCSharp();
            // c# output
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<int, int> f = null;
                object[] b = null;
                f = (Func<int, int>)((int i) =>
                    i + ((int)b[0]));
                b = new object[]{(object)999};
                return f(
                    n);
            });
            Assert.AreEqual(1009, @cs(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(1009, x);

            var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(ff);
            ff.PrintIL();

            var y = ff(10);
            Assert.AreEqual(1009, y);
        }

        [Test]
        public void Test1_manual_closure()
        {
            var n = Parameter(typeof(int), "n");
            var i = Parameter(typeof(int), "i");

            var f = Parameter(typeof(Func<object[], int, int>), "f");
            var a = Parameter(typeof(object[]), "a");
            var b = Parameter(typeof(object[]), "b");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { f, b },
                    Assign(f, Lambda<Func<object[], int, int>>(
                        MakeBinary(ExpressionType.Add, i, Convert(ArrayAccess(a, Constant(0)), typeof(int))),
                        a, i)),
                    Assign(b, NewArrayInit(typeof(object), Constant(999, typeof(object)))),
                    Invoke(f, b, n)
                ),
                n);

            expr.PrintCSharp();
            // c# output
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<object[], int, int> f = null;
                object[] b;
                f = (Func<object[], int, int>)((
                    object[] a, 
                    int i) =>
                    i + ((int)a[0]));
                b = new object[] {(object)999};
                return f(
                    b,
                    n);
            });
            Assert.AreEqual(1009, @cs(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(1009, x);

            var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(ff);
            ff.PrintIL();

            var y = ff(10);
            Assert.AreEqual(1009, y);
        }

        [Test]
        public void Test1_simplified()
        {
            var sumFunc = Parameter(typeof(Func<int, int>), "sumFunc");
            var i = Parameter(typeof(int), "i");
            var n = Parameter(typeof(int), "n");
            var m = Parameter(typeof(int), "m");

            var expr = Lambda<Func<int, int>>(
                Block(new[] { sumFunc, m },
                    Assign(m, Constant(45)),  // let's assign before and see if the variable value is correctly used in the nested lambda
                    Assign(sumFunc, Lambda(MakeBinary(ExpressionType.Add, i, m), i)),
                    Assign(m, Constant(999)),
                    Invoke(sumFunc, n)
                ),
                n);

            expr.PrintCSharp();
            // print outputs valid csharp code:
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<int, int> sumFunc = null;
                int m = default;
                m = 45;
                sumFunc = (Func<int, int>)((int i) =>
                        i + m);
                m = 999;
                return sumFunc(
                    n);
            });
            Assert.AreEqual(1009, @cs(10));

            var fs = expr.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(1009, x);

            var f = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            f.PrintIL();

            if (f.TryGetDebugClosureNestedLambdaOrConstant(out var item) && item is Delegate d)
                d.PrintIL("sumFunc");

            var y = f(10);
            Assert.AreEqual(1009, y);
        }

        [Test]
        public void Test1()
        {
            var sumFunc = Parameter(typeof(Func<int, int>), "sumFunc");
            var i = Parameter(typeof(int), "i");
            var n = Parameter(typeof(int), "n");
            var m = Parameter(typeof(int), "m");

            var e = Lambda<Func<int, int>>(
                Block(new[] { sumFunc, m },
                    Assign(m, Constant(45)),  // let's assign before and see if the variable value is correctly used in the nested lambda
                    Assign(sumFunc, Lambda(MakeBinary(ExpressionType.Add, i, m), i)),
                    Assign(m, Constant(999)), // todo: @fixme assign the variable later when the lambda is already created above
                    Invoke(sumFunc, n)
                ),
                n);

            e.PrintCSharp();
            // print outputs valid csharp code:
            var @cs = (Func<int, int>)((int n) =>
            {
                Func<int, int> sumFunc = null;
                int m = default;
                m = 45;
                sumFunc = (Func<int, int>)((int i) =>
                        i + m);
                m = 999;
                return sumFunc.Invoke(
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

            var fs = e.CompileSys();
            fs.PrintIL();

            var x = fs(10);
            Assert.AreEqual(1009, x);

            var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Assert.IsNotNull(f);
            f.PrintIL();

            var y = f(10);
            Assert.AreEqual(1009, y);
        }

        [Test]
        public void Test2_original_issue_case()
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
                Func<int, int> sumFunc = default;
                sumFunc = (Func<int, int>)((int i) =>
                    (i == 0) ?
                        0 :
                        (i + sumFunc(
                            (i - 1))));
                return sumFunc(
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