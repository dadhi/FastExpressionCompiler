using System;
using SysExpr = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct LiftedUserDefinedLogicalRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Lifted_conditional_operators_match_values_and_effects(t);
        Lifted_explicit_methods_match_values_and_effects(t);
        Ignored_lifted_conditional_operators_preserve_effects(t);
        Lifted_right_operand_may_contain_finally(t);
        Direct_factory_discovers_lifted_operator(t);
    }
    public void Lifted_conditional_operators_match_values_and_effects(TestContext t) => Check(t, false, false, false);
    public void Lifted_explicit_methods_match_values_and_effects(TestContext t) => Check(t, true, false, false);
    public void Ignored_lifted_conditional_operators_preserve_effects(TestContext t) => Check(t, false, true, false);
    public void Lifted_right_operand_may_contain_finally(TestContext t) => Check(t, false, false, true);
    private static void Check(TestContext t, bool explicitMethod, bool ignored, bool finallyEffect)
    {
        foreach (var and in new[] { false, true })
        {
            var left = SysExpr.Parameter(typeof(Func<Logical?>));
            var right = SysExpr.Parameter(typeof(Func<Logical?>));
            var method = explicitMethod ? typeof(Logical).GetMethod(and ? nameof(Logical.And) : nameof(Logical.Or)) : null;
            SysExpr r = SysExpr.Invoke(right);
            if (finallyEffect) r = SysExpr.TryFinally(r, SysExpr.Call(typeof(LiftedUserDefinedLogicalRegressionTests).GetMethod(nameof(Finally))));
            var operation = and ? SysExpr.AndAlso(SysExpr.Invoke(left), r, method) : SysExpr.OrElse(SysExpr.Invoke(left), r, method);
            var expression = ignored ? SysExpr.Lambda(SysExpr.Block(operation, SysExpr.Empty()), left, right) : SysExpr.Lambda(operation, left, right);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var system = expression.Compile();
            foreach (var a in new Logical?[] { null, new Logical { Value = 0 }, new Logical { Value = 3 } })
            foreach (var b in new Logical?[] { null, new Logical { Value = 0 }, new Logical { Value = 3 } })
            {
                Func<Logical?> l = () => { Trace += "L;"; return a; };
                Func<Logical?> rr = () => { Trace += "R;"; return b; };
                Trace = "";
                var expected = system.DynamicInvoke(l, rr);
                var expectedTrace = Trace;
                Trace = "";
                t.AreEqual(expected, fast.DynamicInvoke(l, rr));
                t.AreEqual(expectedTrace, Trace);
            }
        }
    }
    public void Direct_factory_discovers_lifted_operator(TestContext t)
    {
        var l = Expr.Parameter(typeof(Logical?));
        var r = Expr.Parameter(typeof(Logical?));
        foreach (var and in new[] { false, true })
        {
            var body = and ? Expr.AndAlso(l, r) : Expr.OrElse(l, r);
            var fast = Expr.Lambda<Func<Logical?, Logical?, Logical?>>(body, l, r).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual((Logical?)null, fast(null, new Logical { Value = 3 }));
            t.AreEqual((Logical?)null, fast(new Logical { Value = and ? 3 : 0 }, null));
            var expected = new Logical { Value = and ? 0 : 3 };
            t.AreEqual((Logical?)expected, fast(expected, null));
        }
    }
    public static string Trace;
    public static void Finally() => Trace += "F;";
    public struct Logical
    {
        public int Value;
        public string Tag;
        public static bool operator true(Logical value) { Trace += "T;"; return value.Value != 0; }
        public static bool operator false(Logical value) { Trace += "F?;"; return value.Value == 0; }
        public static Logical operator &(Logical left, Logical right) { Trace += "&;"; return new Logical { Value = left.Value + right.Value, Tag = "and" }; }
        public static Logical operator |(Logical left, Logical right) { Trace += "|;"; return new Logical { Value = left.Value + right.Value, Tag = "or" }; }
        public static Logical And(Logical left, Logical right) { Trace += "And;"; return new Logical { Value = left.Value + right.Value, Tag = "explicit and" }; }
        public static Logical Or(Logical left, Logical right) { Trace += "Or;"; return new Logical { Value = left.Value + right.Value, Tag = "explicit or" }; }
    }
}
