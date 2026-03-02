#if NET8_0_OR_GREATER
using System;
using System.Reflection.Emit;
using System.Text.Json;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue495_Incomplete_pattern_detection_for_NotSupported_1007_Return_goto_from_TryCatch_with_Assign_generates_invalid_IL : ITestX
{
    public void Run(TestRun t)
    {
        ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007(t);
    }

    public void ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007(TestContext t)
    {
        // Arrange: Build expression with Return(label, Assign(...)) inside TryCatch
        var variable = Variable(typeof(object), "var");
        var finalResult = Variable(typeof(object), "finalResult");
        var returnLabel = Label(typeof(object), "return");
        var exceptionParam = Parameter(typeof(Exception), "ex");

        var block = Block(
            new[] { variable, finalResult },
            TryCatch(
                Block(
                    typeof(void),
                    Assign(variable, Constant("hello", typeof(object))),
                    IfThen(
                        NotEqual(variable, Constant(null, typeof(object))),
                        // FEC should detect this as error 1007 and reject it
                        Return(returnLabel, Assign(finalResult, variable), typeof(object))
                        // @wip other patters:
                        // - Return(label, Block(Assign(var, value), value))
                        // - Return(label, Call(MethodThatAssigns, ref var, value))
                        // - Return(label, Coalesce(value, Assign(var, default)))
                    ),
                    Assign(finalResult, Constant("default", typeof(object))),
                    Label(returnLabel, Constant("fallback", typeof(object)))
                ),
                Catch(exceptionParam, Empty())
            ),
            finalResult
        );

        var expr = Lambda<Func<object>>(block);

        expr.PrintCSharp();
        var @cs = (Func<object>)(() => //object
        {
            object var = null;
            object finalResult = null;
            try
            {
                var = (object)"hello";
                if (var != null)
                {
                    return finalResult = var;
                }; // todo: @wip remove ;
                finalResult = (object)"default";
                // return:; // todo: @wip remove or comment or rename but make it a valid c#
            }
            catch (Exception ex) // no need for ex
            {
                ; // todo: @wip remove ; 
            }
            return finalResult;
        });


        var fs = expr.CompileSys();
        fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        fs();

        // Act: CompileFast should throw NotSupportedExpressionException or return null
        // var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        // ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);

        // // Expected: compiled should be null (pattern detected as unsupported)
        // t.IsNull(ff);
    }
}
#endif