using NUnit.Framework;
using System;
using System.Linq.Expressions;
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue310_InvalidProgramException_ignored_nullable
    {
        public int Run()
        {
            Test1();
            Test2();
            return 2;
        }

        [Test]
        public void Test1()
        {
            var p = Parameter(typeof(int), "tmp0");
            var expr = 
                Lambda<Action<int>>(Block(
                Convert(p, typeof(int?)),
                Default(typeof(void))), p);

            var f = expr.CompileFast();
            f(2);
        }

        [Test]
        public void Test2()
        {
            var p = Parameter(typeof(int), "tmp0");
            var expr = 
                Lambda<Action<int>>(Convert(p, typeof(int?)), new[] { p });
            var f = expr.CompileFast();
            f(2);
        }
    }
}
