using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }

        public delegate void Handler(int? value);

        [Test]
        public void Test()
        {
            var parameterExpr = Parameter(typeof(int?), "param");
            var toStringMethod = typeof(int?).GetMethod(nameof(object.ToString));
            var callExpr = Call(parameterExpr, toStringMethod);
            var callIfNotNull = IfThen(Not(Equal(parameterExpr, Constant(null, typeof(int?)))), callExpr);
            var expr = Lambda<Handler>(callIfNotNull, parameterExpr);
#if LIGHT_EXPRESSION
            System.Console.WriteLine(expr.ToCSharpString());
#endif
            var f = expr.CompileFast(true);
            f(2);
            // Assert.AreEqual("2", s);
        }
    }
}