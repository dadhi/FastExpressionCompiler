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
            
#if LIGHT_EXPRESSION && DEBUG
            Console.WriteLine(expr.ToCSharpString(new StringBuilder(), 4, stripNamespace: true));
#endif
            var f = expr.CompileFast(true);
            // f.Method.PrintIL();
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

        [Test]
        public void Equal_in_void_Handler_should_work()
        {
            var parameterExpr = Parameter(typeof(int?), "param");
            var toStringMethod = parameterExpr.Type.GetMethod(nameof(object.ToString));

            var callIfNotNull = IfThen(
                Equal(parameterExpr, Constant(null, typeof(int?))),
                Call(Default(parameterExpr.Type), toStringMethod));
            
            var expr = Lambda<Handler>(callIfNotNull, parameterExpr);

#if LIGHT_EXPRESSION && DEBUG
            Console.WriteLine(expr.ToCSharpString(new StringBuilder(), 4, stripNamespace: true));
#endif

            var f = expr.CompileFast(true);
            // f.Method.PrintIL();
            f(2);
            f(null);
            /*
                Expected IL - sharplab.io:

                IL_0000: ldarga.s param
                IL_0002: call instance bool valuetype [System.Private.CoreLib]System.Nullable`1<int32>::get_HasValue()
                IL_0007: brtrue.s IL_001e

                IL_0009: ldloca.s 0
                IL_000b: dup
                IL_000c: initobj valuetype [System.Private.CoreLib]System.Nullable`1<int32>
                IL_0012: constrained. valuetype [System.Private.CoreLib]System.Nullable`1<int32>
                IL_0018: callvirt instance string [System.Private.CoreLib]System.Object::ToString()
                IL_001d: pop
            */
        }
    }
}