using System;

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
        ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007_null_path(t);
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
        var _ = (Func<object>)(() => //object
        {
            object @var = null;
            object finalResult = null;
            try
            {
                @var = "hello";
                if (@var != null)
                {
                    return finalResult = @var;
                }
                finalResult = "default";
            @return:;
            }
            catch (Exception
                #pragma warning disable CS0168 // unused var
                ex
                #pragma warning restore CS0168
            )
            {
                ;
            }
            return finalResult;
        });

        var fs = expr.CompileSys();
        fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        var a = fs();
        t.AreEqual("hello", a);

        // Act: CompileFast should throw NotSupportedExpressionException or return null
        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        var b = ff();
        t.AreEqual("hello", b);
    }

    public void ReturnGotoFromTryCatchWithAssign_ShouldBeDetectedAsError1007_null_path(TestContext t)
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
                    Assign(variable, Constant(null, typeof(object))),
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
        var _ = (Func<object>)(() => //object
        {
            object @var = null;
            object finalResult = null;
            try
            {
                @var = null;
                if (@var != null)
                {
                    return finalResult = @var;
                }
                finalResult = "default";
            @return:;
            }
            catch (Exception)
            {
                ;
            }
            return finalResult;
        });

        var fs = expr.CompileSys();
        fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        var a = fs();
        t.AreEqual("default", a);

        // Act: CompileFast should throw NotSupportedExpressionException or return null
        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        var b = ff();
        t.AreEqual("default", b);
    }
}