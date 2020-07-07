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
    public class Issue252_Bad_code_gen_for_comparison_of_nullable_type_to_null : ITest
    {
        public int Run()
        {
            Test_returning_void_Handler();
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
            Console.WriteLine(expr.ToCSharpString(new StringBuilder(), 4, stripNamespace: true));
#endif
            var f = expr.CompileFast(true);
            f.Method.PrintIL();

            f(2);
            f(null);

/*
Expected IL - sharplab.io:

        IL_0000: ldarga.s param
        IL_0002: call instance bool valuetype [System.Private.CoreLib]System.Nullable`1<int32>::get_HasValue()
        IL_0007: brfalse.s IL_0017

        IL_0009: ldarga.s param
        IL_000b: constrained. valuetype [System.Private.CoreLib]System.Nullable`1<int32>
        IL_0011: callvirt instance string [System.Private.CoreLib]System.Object::ToString()
        IL_0016: pop

        IL_0017: ret
*/
        }
    }
}