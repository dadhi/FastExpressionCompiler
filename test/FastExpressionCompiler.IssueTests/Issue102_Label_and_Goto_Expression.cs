using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    class Issue102_Label_and_Goto_Expression
    {
        [Test]
        public void BlockWithGotoIsSupported()
        {
            var returnTarget = Label("aaa");

            var writeLineMethod = typeof(Console).GetTypeInfo().DeclaredMethods.First(x => x.Name == "WriteLine" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string));
            Assert.IsNotNull(writeLineMethod);

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
            Assert.NotNull(fastCompiled);
        }

        [Test]
        public void UnknownLabelShouldThrow()
        {
            var lambda = Lambda(
                Return(Label(), Constant(1)));

            Assert.Throws<InvalidOperationException>(() => lambda.CompileSys());

            Assert.Throws<InvalidOperationException>(() => lambda.CompileFast(true));
        }
    }
}
