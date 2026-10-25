#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression;
using E = FastExpressionCompiler.LightExpression.Expression;
using L = FastExpressionCompiler.LightExpression.LambdaExpression;
namespace Regression.Light;
#else
using FastExpressionCompiler;
using E = System.Linq.Expressions.Expression;
using L = System.Linq.Expressions.LambdaExpression;
namespace Regression.Standard;
#endif

// Compiled twice against the respective compiler binaries: identical workload definitions.
public static class Workloads
{
    public sealed class Model { public int Value { get; set; } = 7; public int Add(int value) => Value + value; }

    private static E Arithmetic(E x, int depth) => depth == 0 ? x : E.Add(Arithmetic(x, depth - 1), Arithmetic(x, depth - 1));

    private static L Build(string shape)
    {
        var x = E.Parameter(typeof(int), "x");
        E body = shape switch
        {
            "Scalar" => E.Add(x, E.Constant(1)),
            "Arithmetic" => Arithmetic(x, 5),
            "CallConditional" => E.Condition(E.LessThan(x, E.Constant(0)),
                E.Call(E.Constant(new Model()), typeof(Model).GetMethod(nameof(Model.Add))!, x),
                E.Property(E.Constant(new Model()), nameof(Model.Value))),
            "Closure" => E.Lambda<Func<int>>(E.Add(x, E.Constant(1))),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        return shape == "Closure" ? E.Lambda<Func<int, Func<int>>>(body, x) : E.Lambda<Func<int, int>>(body, x);
    }

    public static object Construct(string shape) => Build(shape);

    public static Func<Delegate> Prepare(string shape)
    {
        var tree = Build(shape);
        return () => tree.CompileFast(ifFastFailedReturnNull: true)
            ?? throw new InvalidOperationException("CompileFast returned null: " + shape);
    }

    public static int Invoke(Delegate compiled, int input) => compiled switch
    {
        Func<int, int> simple => simple(input),
        Func<int, Func<int>> closure => closure(input)(),
        _ => throw new InvalidOperationException("Unexpected delegate type")
    };

    public static void Verify(string shape, Func<Delegate> compile)
    {
        var compiled = compile();
        foreach (var input in new[] { -2, 0, 3 })
        {
            var expected = shape switch { "Scalar" or "Closure" => input + 1, "Arithmetic" => input * 32,
                "CallConditional" => input < 0 ? 7 + input : 7, _ => throw new ArgumentException(shape) };
            if (Invoke(compiled, input) != expected) throw new InvalidOperationException("Incorrect result: " + shape);
        }
    }
}
