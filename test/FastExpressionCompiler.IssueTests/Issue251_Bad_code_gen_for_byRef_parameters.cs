using System;
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue251_Bad_code_gen_for_byRef_parameters : ITest
    {
        public int Run()
        {
            Test_1();
            return 1;
        }

        public void Test_1()
        {
            var leftParameterExpr = Parameter(typeof(double).MakeByRefType(), "leftValue");
            var rightParameterExpr = Parameter(typeof(double).MakeByRefType(), "rightValue");
            var equalsMethod = typeof(double).GetMethod(nameof(IEquatable<object>.Equals), new[] { typeof(double) });
            var callExpr = Call(leftParameterExpr, equalsMethod, rightParameterExpr);

            var expr = Lambda<EqualsHandler>(callExpr, leftParameterExpr, rightParameterExpr);

            expr.PrintCSharp();

            var f = expr.CompileFast(true);
            Asserts.IsNotNull(f);
            f.PrintIL();

            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldind_R8,
                OpCodes.Call,
                OpCodes.Ret);

            var a = 1d;
            var b = 1d;
            var c = 2d;
            Asserts.IsTrue(f(in a, in b));
            Asserts.IsFalse(f(in c, in b));
        }

        public delegate bool EqualsHandler(in double a, in double b);
    }
}