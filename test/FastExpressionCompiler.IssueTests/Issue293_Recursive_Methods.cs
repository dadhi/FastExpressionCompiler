
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

    public class Issue293_Recursive_Methods : ITest
    {
        public int Run()
        {
            Test_Recursive_Expression();
            return 1;
        }


        public void Test_Recursive_Expression()
        {
            var expr = MakeFactorialExpressionWithTheTrick<int>();
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            var res = fs(4);

            var f = expr.CompileFast(true);
            f.PrintIL();
            var res2 = f(4);

            Asserts.AreEqual(res, res2);
        }

        public Expression<Func<T, T>> MakeFactorialExpressionWithTheTrick<T>()
        {
            var nParam = Parameter(typeof(T), "n");
            var methodVar = Variable(typeof(Func<T, T>), "fac");
            var methodsVar = Variable(typeof(Func<T, T>[]), "facs");
            var one = Constant(1, typeof(T));

            // This does not work:
            // Func<int, int> rec = null;
            // Func<int, int> tmp = n => n <= 1 ? 1 : n * rec(n - 1);
            // rec = tmp; // setting the closure variable! means that this closure variable is not readonly

            // This should work:
            // var recs = new Func<int, int>[1];
            // Func<int, int> tmp = n => n <= 1 ? 1 : n * recs[0](n - 1);
            // recs[0] = tmp; // setting the item inside the closure variable of array type should work because of reference semantic

            return Lambda<Func<T, T>>(
                Block(
                    new[] { methodsVar, methodVar },
                    Assign(
                        methodsVar, NewArrayBounds(typeof(Func<T, T>), Constant(1))),
                    Assign(
                        methodVar,
                        Lambda<Func<T, T>>(
                            Condition(
                                // ( n <= 1 )
                                LessThanOrEqual(nParam, one),
                                // 1
                                one,
                                // n * method( n - 1 )
                                Multiply(
                                    // n
                                    nParam,
                                    // method( n - 1 )
                                    Invoke(
                                        ArrayIndex(methodsVar, Constant(0)),
                                        Subtract(nParam, one)))),
                            nParam)),
                    Assign(ArrayAccess(methodsVar, Constant(0)), methodVar),
                    // return method( n );
                    Invoke(methodVar, nParam)),
                nParam);
        }

        //from https://chriscavanagh.wordpress.com/2012/06/18/recursive-methods-in-expression-trees/
        public Expression<Func<T, T>> MakeFactorialExpression<T>()
        {
            var nParam = Parameter(typeof(T), "n");
            var methodVar = Variable(typeof(Func<T, T>), "factorial");
            var one = Constant(1, typeof(T));

            return Lambda<Func<T, T>>(
                Block(
                    // Func<uint, uint> method;
                    new[] { methodVar },
                    // method = n => ( n <= 1 ) ? 1 : n * method( n - 1 );
                    Assign(
                        methodVar,
                        Lambda<Func<T, T>>(
                            Condition(
                                // ( n <= 1 )
                                LessThanOrEqual(nParam, one),
                                // 1
                                one,
                                // n * method( n - 1 )
                                Multiply(
                                    // n
                                    nParam,
                                    // method( n - 1 )
                                    Invoke(
                                        methodVar,
                                        Subtract(nParam, one)))),
                            nParam)),
                    // return method( n );
                    Invoke(methodVar, nParam)),
                nParam);
        }
    }
}