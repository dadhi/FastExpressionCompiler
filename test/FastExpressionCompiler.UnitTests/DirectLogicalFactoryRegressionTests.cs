using System;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct DirectLogicalFactoryRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Direct_factory_preserves_custom_operand_type(t);
        Direct_factory_discovers_custom_operator(t);
        Direct_factory_results_are_values(t);
    }

    public void Direct_factory_preserves_custom_operand_type(TestContext t)
    {
        var left = Expr.Parameter(typeof(UserDefinedLogicalRegressionTests.LogicalStruct));
        var right = Expr.Parameter(left.Type);
        foreach (var body in new[] { Expr.AndAlso(left, right), Expr.OrElse(left, right) })
            t.AreEqual(left.Type, body.Type);
    }

    public void Direct_factory_discovers_custom_operator(TestContext t)
    {
        var left = Expr.Parameter(typeof(UserDefinedLogicalRegressionTests.LogicalStruct));
        var right = Expr.Parameter(left.Type);
        foreach (var and in new[] { false, true })
        {
            var body = and ? Expr.AndAlso(left, right) : Expr.OrElse(left, right);
            var fast = Expr.Lambda<Func<UserDefinedLogicalRegressionTests.LogicalStruct,
                UserDefinedLogicalRegressionTests.LogicalStruct, UserDefinedLogicalRegressionTests.LogicalStruct>>(body, left, right).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in new[] { 0, 5 })
            {
                var expected = and ? value & 3 : value != 0 ? value : 3;
                t.AreEqual(expected, fast(new UserDefinedLogicalRegressionTests.LogicalStruct(value),
                    new UserDefinedLogicalRegressionTests.LogicalStruct(3)).Value);
            }
        }
    }
    public void Direct_factory_results_are_values(TestContext t)
    {
        var left = Expr.Parameter(typeof(UserDefinedLogicalRegressionTests.LogicalStruct));
        var right = Expr.Parameter(left.Type);
        foreach (var body in new[] { Expr.AndAlso(left, right), Expr.OrElse(left, right) })
        {
            var field = Expr.Field(body, nameof(UserDefinedLogicalRegressionTests.LogicalStruct.Value));
            var fast = Expr.Lambda<Func<UserDefinedLogicalRegressionTests.LogicalStruct, UserDefinedLogicalRegressionTests.LogicalStruct, int>>(field, left, right).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(body.NodeType == System.Linq.Expressions.ExpressionType.AndAlso ? 5 & 3 : 5, fast(new UserDefinedLogicalRegressionTests.LogicalStruct(5), new UserDefinedLogicalRegressionTests.LogicalStruct(3)));
        }
    }
}
