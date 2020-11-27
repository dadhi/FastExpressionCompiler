using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue281_Index_Out_of_Range : ITest
    {
        public int Run()
        {
            Index_Out_of_Range();

            return 1;
        }

        [Test]
        public void Index_Out_of_Range()
        {
            var input = Parameter(typeof(List<string>));
            var idx   = Parameter(typeof(int));

            var listIdxProp = typeof(List<string>).GetProperties().FirstOrDefault(x => x.GetIndexParameters()?.Count() > 0);
            var printMethod = typeof(object).GetMethod(nameof(ToString));

            var lambda = Lambda<Func<List<string>,int,string>>(
                Call(MakeIndex(input, listIdxProp, new[] { idx }), printMethod),
                input, idx);

            lambda.PrintCSharp();
            var s = lambda.ToExpressionString();

            var fs = lambda.CompileSys();
            fs.PrintIL();

            var f = lambda.CompileFast(true);
            f.PrintIL();

            var m = new List<string> { "a" };
            Assert.AreEqual("a", fs(m, 0));
            Assert.AreEqual("a", f(m, 0));
        }
   }
}