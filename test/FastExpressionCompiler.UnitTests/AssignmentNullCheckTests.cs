using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct AssignmentNullCheckTests : ITestX
{
    public sealed class Receiver
    {
        public static int Calls;
        public int Value { get => 7; set => ++Calls; }
        public int this[int index] { get => 7; set => ++Calls; }
    }
    public void Run(TestRun t) => Reference_setters_check_null_after_rhs(t);
    public void Reference_setters_check_null_after_rhs(TestContext t)
    {
        foreach (var indexer in new[] { false, true })
        foreach (var discard in new[] { false, true })
        {
            var receiver = S.Parameter(typeof(Receiver));
            var rightCalls = 0;
            Func<int> right = () => { ++rightCalls; return 11; };
            var target = indexer ? (System.Linq.Expressions.Expression)S.MakeIndex(receiver,
                typeof(Receiver).GetProperty("Item"), new[] { S.Constant(0) }) : S.Property(receiver, nameof(Receiver.Value));
            var assignment = S.Assign(target, S.Invoke(S.Constant(right)));
            var tree = S.Lambda<Func<Receiver, int>>(discard ? S.Block(assignment, S.Constant(3)) : assignment, receiver);
            var system = tree.Compile();
            var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var compiled in new[] { system, fast })
            {
                Receiver.Calls = rightCalls = 0;
                t.Throws<NullReferenceException>(() => compiled(null));
                t.AreEqual(1, rightCalls);
                t.AreEqual(0, Receiver.Calls);
                t.AreEqual(discard ? 3 : 11, compiled(new Receiver()));
                t.AreEqual(2, rightCalls);
                t.AreEqual(1, Receiver.Calls);
            }
        }
    }
}
