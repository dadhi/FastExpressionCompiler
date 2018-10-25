using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ILDebugging.Decoder;
using NUnit.Framework;

namespace FastExpressionCompiler
{
    public static class TestTools
    {
        public static void AssertOpCodes(this MethodInfo method, params OpCode[] expectedCodes) =>
            CollectionAssert.AreEqual(expectedCodes, ILReaderFactory.Create(method).Select(x => x.OpCode).ToArray());
    }
}