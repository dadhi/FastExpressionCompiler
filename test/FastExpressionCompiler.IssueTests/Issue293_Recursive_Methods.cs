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
            var expr = MakeFactorialExpression<int>();

            expr.PrintCSharp();
            var fs = expr.CompileSys();
            fs.PrintIL();
            var res = fs(4);

            var f = expr.CompileFast(true);
            f.PrintIL();
            var res2 = f(4);

            Assert.AreEqual(res, res2);
        }

        //from https://chriscavanagh.wordpress.com/2012/06/18/recursive-methods-in-expression-trees/
        public Expression<Func<T, T>> MakeFactorialExpression<T>()
        {
            var nParam = Expression.Parameter(typeof(T), "n");
            var methodVar = Expression.Variable(typeof(Func<T, T>), "factorial");
            var one = Expression.Convert(Expression.Constant(1), typeof(T));

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