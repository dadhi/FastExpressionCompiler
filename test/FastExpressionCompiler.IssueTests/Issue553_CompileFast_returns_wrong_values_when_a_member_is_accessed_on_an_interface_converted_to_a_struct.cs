using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue553_CompileFast_returns_wrong_values_when_a_member_is_accessed_on_an_interface_converted_to_a_struct : ITest
{
    public int Run()
    {
        Interface_to_struct_members();
        return 1;
    }

    public interface IHasId { }

    public struct Start : IHasId
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string Field;
        public string GetId() => Id;
    }

    private static Start NewStart() => new Start { Id = "s", Number = 42, Field = "f" };

    public void Interface_to_struct_members()
    {
        var p = Parameter(typeof(IHasId), "x");
        var conv = Convert(p, typeof(Start));
        var arg = NewStart();

        Check(Lambda<Func<IHasId, object>>(Convert(Property(conv, nameof(Start.Id)), typeof(object)), p), arg, "s");
        Check(Lambda<Func<IHasId, object>>(Convert(Property(conv, nameof(Start.Number)), typeof(object)), p), arg, 42);
        Check(Lambda<Func<IHasId, object>>(Convert(Field(conv, nameof(Start.Field)), typeof(object)), p), arg, "f");
        Check(Lambda<Func<IHasId, object>>(Convert(Call(conv, typeof(Start).GetMethod(nameof(Start.GetId))), typeof(object)), p), arg, "s");
    }

    private static void Check(Expression<Func<IHasId, object>> e, IHasId arg, object expected)
    {
        var f = e.CompileFast(true);
        f.PrintIL();
        var result = f(arg);
        if (!Equals(expected, result))
            throw new Exception($"Expected {expected} but got {result}");
    }
}
