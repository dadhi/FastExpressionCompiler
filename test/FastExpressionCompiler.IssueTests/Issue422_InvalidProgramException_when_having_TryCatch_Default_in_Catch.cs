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
        Original_case();
        Original_case_but_comparing_with_non_null_left_operand();
        Original_case_but_comparing_with_nullable_left_operand();
        Change_comparison_operators_order_as_expected();
        return 4;
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
        Asserts.IsTrue(@cs(null));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call,
            OpCodes.Stloc_0,
            OpCodes.Leave,
            OpCodes.Ldnull,
            OpCodes.Stloc_0,
            OpCodes.Leave,
            OpCodes.Ldloc_0,
            OpCodes.Ldnull,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsTrue(fs(null));
        Asserts.IsTrue(ff(null));
    }

    [Test]
    public void Original_case_but_comparing_with_non_null_left_operand()
    {
        var pEntity = Parameter(typeof(Tuple<object>));
        var lastInstanceAccessor = PropertyOrField(pEntity, "Item1");

        var left = new object();
        var expr = Lambda<Func<Tuple<object>, bool>>(Equal(Constant(left),
            TryCatch(lastInstanceAccessor, new[] { Catch(typeof(NullReferenceException), Default(lastInstanceAccessor.Type)) })), pEntity);

        expr.PrintCSharp((x, stripNamespace, printType) => "left");

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        Asserts.IsFalse(fs(null));
        Asserts.IsFalse(ff(null));
    }

    [Test]
    public void Original_case_but_comparing_with_nullable_left_operand()
    {
        var pEntity = Parameter(typeof(Tuple<DateTime?>));
        var lastInstanceAccessor = PropertyOrField(pEntity, "Item1");

        DateTime? left = DateTime.MaxValue;
        var expr = Lambda<Func<Tuple<DateTime?>, bool>>(Equal(Constant(left, typeof(DateTime?)),
            TryCatch(lastInstanceAccessor, new[] { Catch(typeof(NullReferenceException), Constant(DateTime.MaxValue, typeof(DateTime?))) })), pEntity);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        Asserts.IsTrue(fs(null));
        Asserts.IsTrue(ff(null));
    }

    [Test]
    public void Change_comparison_operators_order_as_expected()
    {
        var pEntity = Parameter(typeof(Tuple<object>));
        var lastInstanceAccessor = PropertyOrField(pEntity, "Item1");

        var expr = Lambda<Func<Tuple<object>, bool>>(
            Equal(
                TryCatch(lastInstanceAccessor, new[] { Catch(typeof(NullReferenceException), Default(lastInstanceAccessor.Type)) }),
                Constant(null)
            ),
            pEntity);

        expr.PrintCSharp();
        // outputs
        T __f<T>(System.Func<T> f) => f();
        var @cs = (Func<Tuple<object>, bool>)((Tuple<object> tuple_object___58225482) => //bool
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
            }) == null);
        Asserts.IsTrue(@cs(null));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        Asserts.IsTrue(fs(null));
        Asserts.IsTrue(ff(null));
    }
}