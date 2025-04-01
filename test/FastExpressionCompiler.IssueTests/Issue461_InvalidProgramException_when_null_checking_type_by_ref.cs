#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

using System.Linq.Expressions;
using System.Reflection.Emit;

public struct Issue461_InvalidProgramException_when_null_checking_type_by_ref : ITest
{
    public int Run()
    {
        Original_case1();
        return 1;
    }

    private class Target
    {
        public int X { get; set; }
    }

    private delegate R InFunc<T, out R>(in T t);

    public void Original_case1()
    {
        var p = Parameter(typeof(Target).MakeByRefType(), "p");

        var expr = Lambda<InFunc<Target, bool>>(
            MakeBinary(ExpressionType.Equal, p, Constant(null)),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.IsTrue(fs(null));
        Asserts.IsFalse(fs(new Target()));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        Asserts.IsTrue(ff(null));
        Asserts.IsFalse(ff(new Target()));

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldind_Ref,
            OpCodes.Ldnull,
            OpCodes.Ceq,
            OpCodes.Ret
        );
    }
}
