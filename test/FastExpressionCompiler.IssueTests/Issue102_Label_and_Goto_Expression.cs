using System;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    class Issue102_Label_and_Goto_Expression
    {
        [Test]
        public void BlockWithGotoIsSupported()
        {
            var returnTarget = Label("aaa");

            var writeLineMethod = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
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

            Assert.Throws<InvalidOperationException>(() => lambda.Compile());
            Assert.Throws<InvalidOperationException>(() => lambda.CompileFast(true));
        }
    }
}
