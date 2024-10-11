using System;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue422_InvalidProgramException_when_having_TryCatch_Default_in_Catch : ITest
{
    public int Run()
    {
        // Original_case();
        return 1;
    }

    [Test]
    public void Original_case()
    {
        var pEntity = Parameter(typeof(Tuple<object>));
        var lastInstanceAccessor = PropertyOrField(pEntity, "Item1");

        var expr = Lambda<Func<Tuple<object>, bool>>(Equal(Constant(null),
            TryCatch(lastInstanceAccessor, new[] { Catch(typeof(NullReferenceException), Default(lastInstanceAccessor.Type)) })), pEntity);

        expr.PrintCSharp();
        // outputs
        T __f<T>(System.Func<T> f) => f();
        var @cs = (Func<Tuple<object>, bool>)((Tuple<object> tuple_object___58225482) => //bool
            null ==
            __f(() =>
            {
                try
                {
                    return tuple_object___58225482.Item1;
                }
                catch (NullReferenceException)
                {
                    return null;
                }
            }));
        Assert.IsTrue(@cs(null));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        // ff.AssertOpCodes(
        //     OpCodes.Ldarg_1,
        //     OpCodes.Conv_R_Un,
        //     OpCodes.Conv_R4,
        //     OpCodes.Ret
        // );

        Assert.IsTrue(fs(null));
        Assert.IsTrue(ff(null));
    }
}