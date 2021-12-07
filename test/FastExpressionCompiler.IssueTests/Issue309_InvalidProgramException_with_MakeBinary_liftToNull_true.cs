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
    public class Issue309_InvalidProgramException_with_MakeBinary_liftToNull_true : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
            var expr = Lambda<Func<object>>(
                Convert(
                  MakeBinary(ExpressionType.GreaterThan, Default(typeof(int?)), Constant(10, typeof(int?)), true, null),
                  typeof(object)));

            expr.PrintCSharp();

            // var f = (Func<object>)(() => //$
            //     ((object)(default(Nullable<int>) > (Nullable<int>)(int)10)));

            var fSys = expr.CompileSys();
            fSys.PrintIL();

            Assert.IsNull(fSys());

            var fFast = expr.CompileFast();
            fFast.PrintIL();

            Assert.IsNull(fFast());
        }
    }
}