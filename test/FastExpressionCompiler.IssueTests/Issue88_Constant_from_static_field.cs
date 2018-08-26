using System;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    class Issue88_Constant_from_static_field
    {
        [Test]
        public void ConstantFromStaticField()
        {
            var lambda = Lambda<Func<IntPtr>>(Block(Constant(IntPtr.Zero)));
            var compiledA = lambda.Compile();
            Assert.AreEqual(IntPtr.Zero, compiledA());
            var compiledB = lambda.CompileFast<Func<IntPtr>>(true);
            Assert.IsNotNull(compiledB);
            Assert.AreEqual(IntPtr.Zero, compiledB());
        }

        [Test]
        public void ConstantFromStaticField2()
        {
            var lambda = Lambda<Func<UIntPtr>>(Block(Constant(UIntPtr.Zero)));
            var compiledA = lambda.Compile();
            Assert.AreEqual(UIntPtr.Zero, compiledA());
            var compiledB = lambda.CompileFast<Func<UIntPtr>>(true);
            Assert.IsNotNull(compiledB);
            Assert.AreEqual(UIntPtr.Zero, compiledB());
        }
    }
}
