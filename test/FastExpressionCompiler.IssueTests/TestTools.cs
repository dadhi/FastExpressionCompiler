using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ILDebugging.Decoder;
using NUnit.Framework;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression
#else
using System.Linq.Expressions;
namespace FastExpressionCompiler
#endif
{
    public static class TestTools
    {
        public static OpCode[] GetOpCodes(this MethodInfo method) =>
            ILReaderFactory.Create(method).Select(x => x.OpCode).ToArray();

        public static void AssertOpCodes(this Delegate @delegate, params OpCode[] expectedCodes) =>
            AssertOpCodes(@delegate.Method, expectedCodes);

        public static void AssertOpCodes(this MethodInfo method, params OpCode[] expectedCodes) =>
            CollectionAssert.AreEqual(expectedCodes, method.GetOpCodes(), "Unexpected IL OpCodes...");

        [System.Diagnostics.Conditional("DEBUG")]
        public static void PrintIL(this Delegate @delegate) => @delegate.Method.PrintIL();

        [System.Diagnostics.Conditional("DEBUG")]
        public static void PrintCSharpString(this Expression expr) =>Console.WriteLine(expr.ToCSharpString());

        [System.Diagnostics.Conditional("DEBUG")]
        public static void PrintIL(this MethodInfo method, Action<string> print = null)
        {
            if (print == null)
                print = x => System.Console.WriteLine(x);

            print(">>> IL starts");

            var ilReader = ILReaderFactory.Create(method);
            foreach (var il in ilReader)
            {
                if (il is InlineFieldInstruction f)
                    print(il.OpCode + " " + f.Field.Name);
                else if (il is InlineMethodInstruction m)
                    print(il.OpCode + " " + m.Method.Name);
                else if (il is InlineTypeInstruction t)
                    print(il.OpCode + " " + t.Type.Name);
                else if (il is InlineTokInstruction tok)
                    print(il.OpCode + " " + tok.Member.Name);
                else
                    print(il.OpCode.ToString());
            }
            print("<<< IL ends");
        }
    }

    public interface ITest
    {
        int Run();
    }
}
