using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct UnboxStorageRegressionTests : ITestX
{
    public struct Counter { public int Value; public int Increment() => ++Value; public int Add(int amount) => Value += amount; }
    public static void Bump(ref Counter value) => ++value.Value;
    public void Run(TestRun t) { Unbox_mutates_storage_while_convert_copies(t); Fields_and_ref_arguments_use_box_storage(t); Nullable_unbox_has_value_semantics(t); Address_receivers_survive_exception_arguments(t); }
    public void Unbox_mutates_storage_while_convert_copies(TestContext t)
    {
        var box = S.Parameter(typeof(object)); var increment = typeof(Counter).GetMethod(nameof(Counter.Increment));
        var unbox = S.Lambda<Func<object, int>>(S.Call(S.Unbox(box, typeof(Counter)), increment), box).FromSysExpression().CompileFast(true);
        var convert = S.Lambda<Func<object, int>>(S.Call(S.Convert(box, typeof(Counter)), increment), box).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(unbox) || !t.IsNotNull(convert)) return;
        object value = new Counter();
        t.AreEqual(1, unbox(value)); t.AreEqual(2, unbox(value)); t.AreEqual(2, ((Counter)value).Value);
        t.AreEqual(3, convert(value)); t.AreEqual(3, convert(value)); t.AreEqual(2, ((Counter)value).Value);
        t.Throws<NullReferenceException>(() => unbox(null)); t.Throws<InvalidCastException>(() => unbox(42));
    }
    public void Fields_and_ref_arguments_use_box_storage(TestContext t)
    {
        var box = S.Parameter(typeof(object));
        var field = S.Lambda<Action<object>>(S.Assign(S.Field(S.Unbox(box, typeof(Counter)), nameof(Counter.Value)), S.Constant(7)), box).FromSysExpression().CompileFast(true);
        var bump = S.Lambda<Action<object>>(S.Call(typeof(UnboxStorageRegressionTests).GetMethod(nameof(Bump)), S.Unbox(box, typeof(Counter))), box).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(field) || !t.IsNotNull(bump)) return;
        object value = new Counter(); field(value); bump(value); t.AreEqual(8, ((Counter)value).Value);
    }
    public void Nullable_unbox_has_value_semantics(TestContext t)
    {
        var box = S.Parameter(typeof(object));
        var tree = S.Lambda<Func<object, bool>>(S.Property(S.Unbox(box, typeof(int?)), nameof(Nullable<int>.HasValue)), box);
        var fast = tree.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        t.IsFalse(fast(null)); t.IsTrue(fast(7));
    }
    public void Address_receivers_survive_exception_arguments(TestContext t)
    {
        var box = S.Parameter(typeof(object)); var rhs = S.TryFinally(S.Constant(3), S.Empty());
        var tree = S.Lambda<Func<object, int>>(S.Call(S.Unbox(box, typeof(Counter)), typeof(Counter).GetMethod(nameof(Counter.Add)), rhs), box);
        var fast = tree.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        object value = new Counter(); t.AreEqual(3, fast(value)); t.AreEqual(6, fast(value)); t.AreEqual(6, ((Counter)value).Value);
    }
}
