using System;
using System.Runtime.CompilerServices;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NestedStructFieldAddressRegressionTests : ITestX
{
    public struct Counter { public int Value; }
    private static int OwnerReads;
    private static int RightReads;
    public static StrongBox<Counter> Owner(StrongBox<Counter> box) { ++OwnerReads; return box; }
    public static int Right(int value) { ++RightReads; return value; }
    public static void Increment(ref int value) => ++value;

    private static readonly CompilerFlags[] Flags =
    {
        CompilerFlags.Default,
        CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
    };

    public void Run(TestRun t)
    {
        Nested_struct_field_assignment_uses_one_owner_address(t);
        Compound_and_increment_results_preserve_the_final_address(t);
        Nested_struct_field_ref_argument_mutates_original_storage(t);
    }

    private static System.Linq.Expressions.MemberExpression Field(System.Linq.Expressions.Expression box) =>
        S.Field(S.Field(box, nameof(StrongBox<Counter>.Value)), nameof(Counter.Value));

    private static System.Linq.Expressions.MethodCallExpression OwnerCall(System.Linq.Expressions.Expression box) =>
        S.Call(typeof(NestedStructFieldAddressRegressionTests).GetMethod(nameof(Owner)), box);

    public void Nested_struct_field_assignment_uses_one_owner_address(TestContext t)
    {
        var box = S.Parameter(typeof(StrongBox<Counter>));
        var value = S.Parameter(typeof(int));
        var source = S.Lambda<Func<StrongBox<Counter>, int, int>>(S.Assign(Field(OwnerCall(box)),
            S.Call(typeof(NestedStructFieldAddressRegressionTests).GetMethod(nameof(Right)), value)), box, value);
        Verify(t, source, 5, 12, 12, 12, 1);

        // A void delegate cannot leave an extra intermediate owner address at ret.
        var action = S.Lambda<Action<StrongBox<Counter>, int>>(source.Body, box, value);
        var expected = new StrongBox<Counter>(new Counter { Value = 5 });
        OwnerReads = RightReads = 0; action.Compile()(expected, 17);
        t.AreEqual(17, expected.Value.Value); t.AreEqual(1, OwnerReads); t.AreEqual(1, RightReads);
        foreach (var flags in Flags)
        {
            var fast = action.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) continue;
            var actual = new StrongBox<Counter>(new Counter { Value = 5 });
            OwnerReads = RightReads = 0; fast(actual, 17);
            t.AreEqual(17, actual.Value.Value); t.AreEqual(1, OwnerReads); t.AreEqual(1, RightReads);
        }
    }

    public void Compound_and_increment_results_preserve_the_final_address(TestContext t)
    {
        var box = S.Parameter(typeof(StrongBox<Counter>));
        var value = S.Parameter(typeof(int));
        var field = Field(OwnerCall(box));
        var right = S.Call(typeof(NestedStructFieldAddressRegressionTests).GetMethod(nameof(Right)), value);
        // System's compound-expression reduction mutates a copy of this nested struct.
        // Keep that measured control, and prove writable storage with an explicit
        // once-only owner/value expansion using ordinary field assignment.
        Verify(t, S.Lambda<Func<StrongBox<Counter>, int, int>>(S.AddAssign(field, right), box, value),
            5, 7, 12, 12, 1, CompoundControl(box, value, add: true, post: false));
        Verify(t, S.Lambda<Func<StrongBox<Counter>, int, int>>(S.PostIncrementAssign(field), box, value),
            5, 0, 5, 6, 0, CompoundControl(box, value, add: false, post: true));
        Verify(t, S.Lambda<Func<StrongBox<Counter>, int, int>>(S.PreIncrementAssign(field), box, value),
            5, 0, 6, 6, 0, CompoundControl(box, value, add: false, post: false));
    }

    private static System.Linq.Expressions.Expression<Func<StrongBox<Counter>, int, int>> CompoundControl(
        System.Linq.Expressions.ParameterExpression box, System.Linq.Expressions.ParameterExpression value, bool add, bool post)
    {
        var owner = S.Variable(typeof(StrongBox<Counter>));
        var previous = S.Variable(typeof(int));
        System.Linq.Expressions.Expression increment = add
            ? S.Call(typeof(NestedStructFieldAddressRegressionTests).GetMethod(nameof(Right)), value)
            : S.Constant(1);
        return S.Lambda<Func<StrongBox<Counter>, int, int>>(S.Block(new[] { owner, previous },
            S.Assign(owner, OwnerCall(box)),
            S.Assign(previous, Field(owner)),
            S.Assign(Field(owner), S.Add(previous, increment)),
            post ? (System.Linq.Expressions.Expression)previous : Field(owner)), box, value);
    }

    public void Nested_struct_field_ref_argument_mutates_original_storage(TestContext t)
    {
        var box = S.Parameter(typeof(StrongBox<Counter>));
        var value = S.Parameter(typeof(int));
        var source = S.Lambda<Func<StrongBox<Counter>, int, int>>(S.Block(
            S.Call(typeof(NestedStructFieldAddressRegressionTests).GetMethod(nameof(Increment)), Field(OwnerCall(box))),
            Field(box)), box, value);
        Verify(t, source, 5, 0, 6, 6, 0);
    }

    private static void Verify(TestContext t,
        System.Linq.Expressions.Expression<Func<StrongBox<Counter>, int, int>> source,
        int initial, int argument, int result, int stored, int rightReads,
        System.Linq.Expressions.Expression<Func<StrongBox<Counter>, int, int>> systemBehaviorControl = null)
    {
        var expected = new StrongBox<Counter>(new Counter { Value = initial });
        OwnerReads = RightReads = 0;
        t.AreEqual(result, source.Compile()(expected, argument));
        t.AreEqual(systemBehaviorControl == null ? stored : initial, expected.Value.Value);
        t.AreEqual(1, OwnerReads); t.AreEqual(rightReads, RightReads);
        if (systemBehaviorControl != null)
        {
            expected.Value = new Counter { Value = initial };
            OwnerReads = RightReads = 0;
            t.AreEqual(result, systemBehaviorControl.Compile()(expected, argument));
            t.AreEqual(stored, expected.Value.Value); t.AreEqual(1, OwnerReads); t.AreEqual(rightReads, RightReads);
        }
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) continue;
            var actual = new StrongBox<Counter>(new Counter { Value = initial });
            OwnerReads = RightReads = 0;
            t.AreEqual(result, fast(actual, argument));
            t.AreEqual(stored, actual.Value.Value); t.AreEqual(1, OwnerReads); t.AreEqual(rightReads, RightReads);
        }
    }
}
