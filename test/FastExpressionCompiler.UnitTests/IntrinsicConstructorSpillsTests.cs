using System;
using System.Collections;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct IntrinsicConstructorSpillsTests : ITestX
{
    public void Run(TestRun t)
    {
        Intrinsic_constructor_arguments_keep_exception_stack_empty(t);
        Intrinsic_list_constructor_preserves_effects_when_ignored(t);
    }
    public class Items : IEnumerable
    {
        public int Total;
        public Items(int left, int right) { Total = left + right; Trace.Add(10); }
        public void Add(int value) { Total += value; Trace.Add(20); }
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
    public class RefItems : IEnumerable
    {
        public int Total;
        public RefItems(ref int value, int increment) { value += increment; Total = value; }
        public void Add(int value) => Total += value;
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
    public static List<int> Trace = new List<int>();
    public static int Mark(int value) { Trace.Add(value); return value; }
    private static E Argument(int value) => E.Call(typeof(IntrinsicConstructorSpillsTests).GetMethod(nameof(Mark)), E.Constant(value));

    public void Intrinsic_constructor_arguments_keep_exception_stack_empty(TestContext t) => Check(t, true, false);
    public void Intrinsic_list_constructor_preserves_effects_when_ignored(TestContext t) => Check(t, true, true);
    private static void Check(TestContext t, bool intrinsic, bool list)
    {
        foreach (var ignored in new[] { false, true })
        {
            var constructor = typeof(Items).GetConstructor(new[] { typeof(int), typeof(int) });
            var first = Argument(1);
            var second = E.TryFinally(Argument(2), E.Block(Argument(3), E.Empty()));
#if LIGHT_EXPRESSION
            var created = intrinsic ? E.NewNoByRefArgs(constructor, first, second) : E.New(constructor, first, second);
#else
            var created = E.New(constructor, first, second);
#endif
            E constructed = list ? E.ListInit(created, E.ElementInit(typeof(Items).GetMethod("Add"), Argument(4))) : created;
            E body = ignored ? E.Block(constructed, E.Constant(99)) : E.Field(constructed, nameof(Items.Total));
            var fast = E.Lambda<Func<int>>(body).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Trace.Clear();
            t.AreEqual(ignored ? 99 : list ? 7 : 3, fast());
            t.AreEqual(list ? "1,2,3,10,4,20" : "1,2,3,10", string.Join(",", Trace));
        }
    }
    public delegate int RefFactory(ref int value);

}
