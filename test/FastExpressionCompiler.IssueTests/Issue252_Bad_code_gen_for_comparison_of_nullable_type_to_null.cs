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
            Test_returning_void_Handler();
            // Test_returning_string();
            return 1;
        }

        public delegate void Handler(int? value);

        [Test]
        public void Test_returning_void_Handler()
        {
            var parameterExpr = Parameter(typeof(int?), "param");
            var toStringMethod = typeof(int?).GetMethod(nameof(object.ToString));
            var callExpr = Call(parameterExpr, toStringMethod);
            var callIfNotNull = IfThen(
                Not(Equal(parameterExpr, Constant(null, typeof(int?)))), 
                callExpr);
            var expr = Lambda<Handler>(callIfNotNull, parameterExpr);
#if LIGHT_EXPRESSION
            System.Console.WriteLine(expr.ToCSharpString(new StringBuilder(), 4, stripNamespace: true));
#endif
            var f = expr.CompileFast(true);
            f(2);
            f(null);
        }

        public delegate string Handler2(int? value);

        [Test]
        public void Test_returning_void_Handler_with_IfThenElse()
        {
            var parameterExpr = Parameter(typeof(int?), "param");

            var callExpr = Call(parameterExpr, typeof(int?).GetMethod(nameof(object.ToString)));
            var callIfNotNull = IfThenElse(
                Not(Equal(parameterExpr, Constant(null, typeof(int?)))), 
                callExpr,
                Constant(null));

            var expr = Lambda<Handler>(callIfNotNull, parameterExpr);
#if LIGHT_EXPRESSION
            System.Console.WriteLine(expr.ToCSharpString(new StringBuilder(), 4, stripNamespace: true));
#endif
            var f = expr.CompileFast(true);

            f(2);
            f(null);
        }


//         [Test]
//         public void Test_returning_string_Handler()
//         {
//             var parameterExpr = Parameter(typeof(int?), "param");

//             var callExpr = Call(parameterExpr, typeof(int?).GetMethod(nameof(object.ToString)));
//             var callIfNotNull = IfThen(
//                 Not(Equal(parameterExpr, Constant(null, typeof(int?)))), 
//                 callExpr,
//                 Constant(null),
//                 typeof(string));

//             var expr = Lambda<Handler2>(callIfNotNull, parameterExpr);
// #if LIGHT_EXPRESSION
//             System.Console.WriteLine(expr.ToCSharpString(new StringBuilder(), 4, stripNamespace: true));
// #endif
//             var f = expr.CompileFast(true);

//             Assert.AreEqual("2", f(2));
//             Assert.AreEqual(null, f(null));
//         }
    }
}