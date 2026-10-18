using System;
using SysExpr = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct AdditionalCompilerLayoutBehaviorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Insignificant_blocks_preserve_action_behavior(t);
        Tail_call_then_return_skips_following_throw(t);
    }
    public void Insignificant_blocks_preserve_action_behavior(TestContext t)
    {
        var expression = SysExpr.Lambda<Action>(SysExpr.Block(
            SysExpr.Block(SysExpr.Empty(), SysExpr.Default(typeof(void))),
            SysExpr.Block(SysExpr.Empty(), SysExpr.Default(typeof(void))),
            SysExpr.Block(SysExpr.Empty(), SysExpr.Default(typeof(void))),
            SysExpr.Block(SysExpr.Empty(), SysExpr.Default(typeof(void)))));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        fast();
        expression.Compile()();
    }
    public static int Calls;
    public static void DoNothing() => ++Calls;
    public void Tail_call_then_return_skips_following_throw(TestContext t)
    {
        var target = SysExpr.Label();
        var expression = SysExpr.Lambda<Action>(SysExpr.Block(
            SysExpr.Call(typeof(AdditionalCompilerLayoutBehaviorRegressionTests).GetMethod(nameof(DoNothing))),
            SysExpr.Return(target), SysExpr.Throw(SysExpr.Constant(new Exception())), SysExpr.Label(target)), true);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Calls = 0;
        fast();
        t.AreEqual(1, Calls);
        expression.Compile()();
        t.AreEqual(2, Calls);
    }
}
