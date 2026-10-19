using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CapturedValueRefRegressionTests : ITestX
{
    public void Run(TestRun t)
    {

        Nullable_and_reference_captures_expose_writable_cells(t);

        Readonly_captures_keep_the_original_value_storage(t);

        Delegate_ref_arguments_keep_captures_live(t);
    }
    public static int Add(ref int value, int increment) => value += increment;
    public static int? AddNullable(ref int? value, int increment) => value = value + increment;
    public static string Replace(ref string value) => value = "changed";
    public static object Identity(object value) => value;
    public sealed class ReferenceHolder
    {
        public readonly object Value;
        public ReferenceHolder(object value) => Value = value;
    }
    public delegate object ObjectRefUpdater(ref object value);
    private static readonly object ChangedReference = new object();
    public static object ReplaceObject(ref object value) => value = ChangedReference;

    public void Nullable_and_reference_captures_expose_writable_cells(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int?, Func<int?>>> nullable = x => () => AddNullable(ref x, 1);
        System.Linq.Expressions.Expression<Func<string, Func<string>>> reference = x => () => Replace(ref x);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var numbers = nullable.FromSysExpression().CompileFast(true, flags);
            var strings = reference.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(numbers) || !t.IsNotNull(strings)) return;
            var expected = nullable.Compile()(3);
            var first = numbers(3);
            var second = numbers(30);
            t.AreEqual(expected(), first()); t.AreEqual(expected(), first());
            t.AreEqual((int?)31, second()); t.AreEqual((int?)6, first());
            t.AreEqual(null, (object)numbers(null)());
            var text = strings("original");
            t.AreEqual(reference.Compile()("original")(), text());
            t.AreEqual("changed", text());
        }
    }

    public void Readonly_captures_keep_the_original_value_storage(TestContext t)
    {
        var value = new object();
        System.Linq.Expressions.Expression<Func<object, Func<object>>> parameter = x => () => x;
        var local = SysExpr.Variable(typeof(object));
        var localExpression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { local },
            SysExpr.Assign(local, SysExpr.Constant(value)), SysExpr.Lambda<Func<object>>(local)));
        var capturedParameter = parameter.FromSysExpression().CompileFast(true)(value);
        var capturedLocal = localExpression.FromSysExpression().CompileFast(true)();
        t.IsTrue(ReferenceEquals(value, capturedParameter()));
        t.IsTrue(ReferenceEquals(value, capturedLocal()));
        System.Linq.Expressions.Expression<Func<object, Func<object>>> call = x => () => Identity(x);
        System.Linq.Expressions.Expression<Func<object, Func<object>>> constructor = x => () => new ReferenceHolder(x).Value;
        var argument = SysExpr.Parameter(typeof(object));
        var invoke = SysExpr.Lambda<Func<object, Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Invoke(SysExpr.Constant((Func<object, object>)Identity), argument)), argument);
        foreach (var captured in new Delegate[] { capturedParameter, capturedLocal,
            call.FromSysExpression().CompileFast(true)(value), constructor.FromSysExpression().CompileFast(true)(value),
            invoke.FromSysExpression().CompileFast(true)(value) })
        {
            var foundClosure = false;
            foreach (var field in captured.Target.GetType().GetFields(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                if (field.GetValue(captured.Target) is ExpressionCompiler.ArrayClosureWithNonPassedParams closure)
                {
                    foundClosure = true;
                    t.IsTrue(ReferenceEquals(value, closure.NonPassedParams[0]));
                }
            }
            t.IsTrue(foundClosure);
        }
    }

    public void Delegate_ref_arguments_keep_captures_live(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(object));
        var expression = SysExpr.Lambda<Func<object, Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Block(SysExpr.Invoke(SysExpr.Constant((ObjectRefUpdater)ReplaceObject), value), value)), value);
        var actual = expression.FromSysExpression().CompileFast(true)(new object());
        t.IsTrue(ReferenceEquals(ChangedReference, actual()));
        t.IsTrue(ReferenceEquals(ChangedReference, actual()));
    }
}
