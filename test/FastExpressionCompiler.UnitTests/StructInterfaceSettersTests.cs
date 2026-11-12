using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct StructInterfaceSettersTests : ITestX
{
    public void Run(TestRun t)
    {
        Interface_setters_mutate_struct_receivers(t);
    }

    public void Interface_setters_mutate_struct_receivers(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(Indexed));
        var property = typeof(IIndexed).GetProperty("Value");
        var indexer = typeof(IIndexed).GetProperty("Item");
        var body = Expr.Block(
            Expr.Assign(Expr.Property(parameter, property), Expr.Constant(50)),
            Expr.Assign(Expr.MakeIndex(parameter, indexer, new[] { Expr.Constant(2) }), Expr.Constant(62)),
            Expr.Property(parameter, property));
        var fast = Expr.Lambda<Func<Indexed, int>>(body, parameter).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(60, fast(new Indexed { Seed = 40 }));
        var constant = Expr.Constant(new Indexed { Seed = 40 });
        var ignored = Expr.Lambda<Action>(Expr.Block(
            Expr.Assign(Expr.MakeIndex(constant, indexer, new[] { Expr.Constant(2) }), Expr.Constant(62)), Expr.Empty())).CompileFast(true);
        if (!t.IsNotNull(ignored)) return;
        Calls = 0;
        ignored();
        t.AreEqual(1, Calls);
    }

    public delegate object Read<T>(ref T value);
    public delegate bool HasValue(ref int? value);
    public interface IIndexed { int this[int index] { get; set; } int Value { get; set; } int Read(); }
    public static int Calls;
    public struct Indexed : IIndexed
    {
        public int Seed;
        public int this[int index] { get { ++Calls; return Seed + index; } set { ++Calls; Seed = value - index; } }
        public int Value { get { ++Calls; return Seed; } set { ++Calls; Seed = value; } }
        public int Read() => Seed + 1;
    }
}
