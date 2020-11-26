using NUnit.Framework;
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
    public class Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null : ITest
    {
        public int Run()
        {
            Not_equal_in_void_Handler_should_work();
            Equal_in_void_Handler_should_work();
            return 2;
        }

        public delegate void Handler(int? value);

        [Test]
        public void Not_equal_in_void_Handler_should_work()
        {
            var parameterExpr = Parameter(typeof(int?), "param");
            var toStringMethod = parameterExpr.Type.GetMethod(nameof(object.ToString));
            var callExpr = Call(parameterExpr, toStringMethod);
            var callIfNotNull = IfThen(
                Not(Equal(parameterExpr, Constant(null, typeof(int?)))), 
                callExpr);
                
            var expr = Lambda<Handler>(callIfNotNull, parameterExpr);

            expr.PrintCSharp();
            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true);
            f.PrintIL();
            f(2);
            f(null);
        }

        [Test]
        public void Equal_in_void_Handler_should_work()
        {
            var parameterExpr = Parameter(typeof(int?), "param");
            var toStringMethod = parameterExpr.Type.GetMethod(nameof(object.ToString));

            var callIfNotNull = IfThen(
                Equal(parameterExpr, Constant(null, typeof(int?))),
                Call(Default(parameterExpr.Type), toStringMethod));
            
            var expr = Lambda<Handler>(callIfNotNull, parameterExpr);

            expr.PrintCSharp();
            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true);
            f.PrintIL();
            f(2);
            f(null);
        }
    }
}