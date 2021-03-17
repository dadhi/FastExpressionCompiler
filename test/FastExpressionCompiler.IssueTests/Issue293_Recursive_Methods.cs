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
    public class Issue293_Recursive_Methods : ITest
    {
        public int Run()
        {
            Test_Recursive_Expression();
            return 1;
        }

        [Test]
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

            Assert.AreEqual(res, res2);
        }

        public Expression<Func<T, T>> MakeFactorialExpressionWithTheTrick<T>()
        {
            var nParam = Expression.Parameter(typeof(T), "n");
            var methodVar  = Expression.Variable(typeof(Func<T, T>),   "fac");
            var methodsVar = Expression.Variable(typeof(Func<T, T>[]), "facs");
            var one = Expression.Constant(1, typeof(T));

            // This does not work:
            // Func<int, int> rec = null;
            // Func<int, int> tmp = n => n <= 1 ? 1 : n * rec(n - 1);
            // rec = tmp; // setting the closure variable! means that this closure variable is not readonly

            // This should work:
            // var recs = new Func<int, int>[1];
            // Func<int, int> tmp = n => n <= 1 ? 1 : n * recs[0](n - 1);
            // recs[0] = tmp; // setting the item inside the closure variable of array type should work because of reference semantic

            return Expression.Lambda<Func<T, T>>(
                Expression.Block(
                    new[] { methodsVar, methodVar },
                    Expression.Assign(methodsVar, Expression.NewArrayBounds(typeof(Func<T, T>), Expression.Constant(1))),
                    Expression.Assign(
                        methodVar,
                        Expression.Lambda<Func<T, T>>(
                            Expression.Condition(
                                // ( n <= 1 )
                                Expression.LessThanOrEqual(nParam, one),
                                // 1
                                one,
                                // n * method( n - 1 )
                                Expression.Multiply(
                                    // n
                                    nParam,
                                    // method( n - 1 )
                                    Expression.Invoke(
                                        Expression.ArrayIndex(methodsVar, Expression.Constant(0)),
                                        Expression.Subtract(nParam, one)))),
                            nParam)),
                    Expression.Assign(Expression.ArrayAccess(methodsVar, Expression.Constant(0)), methodVar),
                    // return method( n );
                    Expression.Invoke(methodVar, nParam)),
                nParam);
        }

        //from https://chriscavanagh.wordpress.com/2012/06/18/recursive-methods-in-expression-trees/
        public Expression<Func<T, T>> MakeFactorialExpression<T>()
        {
            var nParam = Expression.Parameter(typeof(T), "n");
            var methodVar = Expression.Variable(typeof(Func<T, T>), "factorial");
            var one = Expression.Constant(1, typeof(T));

            return Expression.Lambda<Func<T, T>>(
                Expression.Block(
                    // Func<uint, uint> method;
                    new[] { methodVar },
                    // method = n => ( n <= 1 ) ? 1 : n * method( n - 1 );
                    Expression.Assign(
                        methodVar,
                        Expression.Lambda<Func<T, T>>(
                            Expression.Condition(
                                // ( n <= 1 )
                                Expression.LessThanOrEqual(nParam, one),
                                // 1
                                one,
                                // n * method( n - 1 )
                                Expression.Multiply(
                                    // n
                                    nParam,
                                    // method( n - 1 )
                                    Expression.Invoke(
                                        methodVar,
                                        Expression.Subtract(nParam, one)))),
                            nParam)),
                    // return method( n );
                    Expression.Invoke(methodVar, nParam)),
                nParam);
        }
    }
}