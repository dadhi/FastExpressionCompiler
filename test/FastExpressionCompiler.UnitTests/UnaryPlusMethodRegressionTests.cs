using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct UnaryPlusMethodRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Struct_unary_plus_calls_operator_and_lifts_null(t);
        Reference_unary_plus_calls_operator(t);
        Ignored_unary_plus_keeps_operator_effects(t);
        Primitive_ref_unary_plus_reads_value(t);
#if !LIGHT_EXPRESSION
        Explicit_unary_plus_method_is_called(t);
#endif
    }
    public void Struct_unary_plus_calls_operator_and_lifts_null(TestContext t)
    {
        Check(t, typeof(Counter), new Counter { X = 10, Y = 20 });
        Check(t, typeof(Counter?), new Counter { X = 10, Y = 20 });
        Check(t, typeof(Counter?), null);
    }
    public void Reference_unary_plus_calls_operator(TestContext t) => Check(t, typeof(ReferenceCounter), new ReferenceCounter { Value = 42 });
    public void Primitive_ref_unary_plus_reads_value(TestContext t)
    { Check(t, typeof(int), 42, true); Check(t, typeof(int?), 42, true); Check(t, typeof(int?), null, true); }
    public void Ignored_unary_plus_keeps_operator_effects(TestContext t)
    {
        var expression = Expr.Lambda<Action>(Expr.Block(Expr.UnaryPlus(Expr.Constant(new Counter { X = 10, Y = 20 })), Expr.Empty()));
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Calls = 0; fast(); t.AreEqual(1, Calls);
    }
#if !LIGHT_EXPRESSION
    public void Explicit_unary_plus_method_is_called(TestContext t)
    { Check(t, typeof(int?), 42, false, typeof(UnaryPlusMethodRegressionTests).GetMethod(nameof(Change))); Check(t, typeof(int?), null, false, typeof(UnaryPlusMethodRegressionTests).GetMethod(nameof(Change))); }
#endif
    private static void Check(TestContext t, Type type, object value, bool byRef = false, MethodInfo method = null)
    {
        var p = SysExpr.Parameter(byRef ? type.MakeByRefType() : type);
        var body = SysExpr.Convert(SysExpr.MakeUnary(Node.UnaryPlus, p, null, method), typeof(object));
        var expression = byRef ? SysExpr.Lambda(typeof(Read<>).MakeGenericType(type), body, p) : SysExpr.Lambda(body, p);
        var fp = Expr.Parameter(byRef ? type.MakeByRefType() : type);
        var fb = Expr.Convert(Expr.MakeUnary(Node.UnaryPlus, fp, null, method), typeof(object));
        var fe = byRef ? Expr.Lambda(typeof(Read<>).MakeGenericType(type), fb, fp) : Expr.Lambda(fb, fp);
        var fast = fe.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Calls = 0;
        var expected = expression.Compile().DynamicInvoke(new[] { value });
        var expectedCalls = Calls;
        Calls = 0;
        var actual = fast.DynamicInvoke(new[] { value });
        if (expected is ReferenceCounter e && actual is ReferenceCounter a) t.AreEqual(e.Value, a.Value);
        else t.AreEqual(expected, actual);
        t.AreEqual(expectedCalls, Calls);
    }
    public static int Calls;
    public static long Change(int value) { ++Calls; return value + 10L; }
    public delegate object Read<T>(ref T value);
    public struct Counter
    {
        public int X, Y;
        public static Counter operator +(Counter value) { ++Calls; return new Counter { X = value.X + 1, Y = value.Y + 1 }; }
    }
    public class ReferenceCounter
    {
        public int Value;
        public static ReferenceCounter operator +(ReferenceCounter value) { ++Calls; return new ReferenceCounter { Value = value.Value + 1 }; }
    }
}
