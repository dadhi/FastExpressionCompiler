using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ILDebugging.Decoder;
using NUnit.Framework;

namespace FastExpressionCompiler
{
    public static class TestTools
    {
        public static OpCode[] GetOpCodes(this MethodInfo method) =>
            ILReaderFactory.Create(method).Select(x => x.OpCode).ToArray();

        public static void PrintIL(this MethodInfo method, Action<string> printer = null)
        {
            if (printer == null)
                printer = x => System.Diagnostics.Debug.WriteLine(x);

            var ilReader = ILReaderFactory.Create(method);
            foreach (var il in ilReader)
            {
                if (il is InlineFieldInstruction f)
                    Console.WriteLine(il.OpCode + " " + f.Field.Name);
                else if (il is InlineMethodInstruction m)
                    Console.WriteLine(il.OpCode + " " + m.Method.Name);
                else if (il is InlineTypeInstruction t)
                    Console.WriteLine(il.OpCode + " " + t.Type.Name);
                else if (il is InlineTokInstruction tok)
                    Console.WriteLine(il.OpCode + " " + tok.Member.Name);
                else 
                    Console.WriteLine(il.OpCode);
            }
        }

        public static void AssertOpCodes(this MethodInfo method, params OpCode[] expectedCodes) =>
            CollectionAssert.AreEqual(expectedCodes, method.GetOpCodes());
    }

    public interface ITest 
    {
        int Run();
    }
}
