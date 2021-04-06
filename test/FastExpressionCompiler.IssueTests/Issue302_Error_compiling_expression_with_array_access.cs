#if !LIGHT_EXPRESSION
using NUnit.Framework;
using System;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests
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
            Expression<Func<LinqTestModel, int>> expr = m => m.Array[counter].ValorInt;

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var model = new LinqTestModel
            {
                Array = new[] { new LinqTestModel { ValorInt = 1 } }
            };

            var res = fs(model);
            Assert.AreEqual(1, res);

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            var res2 = ff(model);
            Assert.AreEqual(1, res2);
        }

        public class LinqTestModel
        {
            public LinqTestModel[] Array { get; set; }
            public int ValorInt { get; set; }
        }
    }
}
#endif