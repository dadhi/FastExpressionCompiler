using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue88_Constant_from_static_field : ITest
    {
        public int Run()
        {
            ConstantFromStaticField();
            ConstantFromStaticField2();
            return 2;
        }

        [Test]
        public void ConstantFromStaticField()
        {
            var lambda = Lambda<Func<IntPtr>>(Block(Constant(IntPtr.Zero)));
            var compiledB = lambda.CompileFast<Func<IntPtr>>(true);
            Assert.IsNotNull(compiledB);
            Assert.AreEqual(IntPtr.Zero, compiledB());
        }

        [Test]
        public void ConstantFromStaticField2()
        {
            var lambda = Lambda<Func<UIntPtr>>(Block(Constant(UIntPtr.Zero)));
            var compiledB = lambda.CompileFast<Func<UIntPtr>>(true);
            Assert.IsNotNull(compiledB);
            Assert.AreEqual(UIntPtr.Zero, compiledB());
        }
    }
}
