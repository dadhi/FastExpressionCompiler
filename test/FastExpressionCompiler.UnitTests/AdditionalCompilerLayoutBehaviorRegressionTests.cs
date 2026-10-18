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

}
