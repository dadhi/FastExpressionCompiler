using System;
using System.Reflection;
using NUnit.Framework;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
{
    [TestFixture]
    public class Issue65_Add_LightExpression_Elvis_operator_support : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }

        [Test]
        public void Test()
        {
            Assert.AreEqual("42", GetAnA(42)?.GetTheAnswer());

            var n = Parameter(typeof(int), "n");
            var block = CallIfNotNull(
                Call(GetType().GetTypeInfo().GetDeclaredMethod(nameof(GetAnA)), n),
                typeof(A).GetTypeInfo().GetDeclaredMethod(nameof(A.GetTheAnswer)));

            var getTheAnswer = Lambda<Func<int, string>>(block, n);

            getTheAnswer.PrintCSharp();

            var f = getTheAnswer.CompileFast(ifFastFailedReturnNull: true);
            Assert.IsNull(f(43));
            Assert.AreEqual("42", f(42));
        }

        public static A GetAnA(int n) => n == 42 ? new A() : null;

        public class A
        {
            public string GetTheAnswer() => "42";
        }
    }
}
