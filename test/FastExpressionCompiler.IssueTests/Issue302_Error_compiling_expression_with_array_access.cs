using NUnit.Framework;
using System;

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
    public class Issue302_Error_compiling_expression_with_array_access : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
            var counter = 0;
            System.Linq.Expressions.Expression<Func<LinqTestModel, int>> se = m => m.Array[counter].ValorInt;
            var expr = se.FromSysExpression();

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var model = new LinqTestModel
            {
                Array = new[] { new LinqTestModel { ValorInt = 1 } }
            };

            var res = fs(model);
            Asserts.AreEqual(1, res);

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            var res2 = ff(model);
            Asserts.AreEqual(1, res2);
        }

        public class LinqTestModel
        {
            public LinqTestModel[] Array { get; set; }
            public int ValorInt { get; set; }
        }
    }
}