using System;
using System.Linq;
using System.Reflection;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue102_Label_and_Goto_Expression : ITest
    {
        public int Run()
        {
            BlockWithGotoIsSupported();
            UnknownLabelShouldThrow();
            return 2;
        }


        public void BlockWithGotoIsSupported()
        {
            var returnTarget = Label("aaa");

            var writeLineMethod = typeof(Console).GetTypeInfo().DeclaredMethods.First(x => x.Name == "WriteLine" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string));
            Asserts.IsNotNull(writeLineMethod);

            var blockExpr =
                Block(
                    Call(writeLineMethod,
                        Constant("GoTo")),
                    Goto(returnTarget),
                    Call(writeLineMethod,
                        Constant("Other Work")),
                    Label(returnTarget),
                    Constant(7)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Asserts.IsNotNull(fastCompiled);
        }


        public void UnknownLabelShouldThrow()
        {
            var lambda = Lambda(
                Return(Label(), Constant(1)));

            Asserts.Throws<InvalidOperationException>(() => lambda.CompileSys());

            Asserts.Throws<InvalidOperationException>(() => lambda.CompileFast(true));
        }
    }
}
