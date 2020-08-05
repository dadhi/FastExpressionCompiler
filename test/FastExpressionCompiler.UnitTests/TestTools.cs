using System;
using System.Linq;
using System.Text;
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
        public static void PrintCSharpString(this Expression expr) =>
            Console.WriteLine(expr.ToCSharpString());

        [System.Diagnostics.Conditional("DEBUG")]
        public static void PrintIL(this Delegate @delegate, string tag = null) => @delegate.Method.PrintIL(tag);

        [System.Diagnostics.Conditional("DEBUG")]
        public static void PrintIL(this MethodInfo method, string tag = null)
        {
            var s = new StringBuilder();
            s.Append(tag == null ? "<il>" : "<" + tag + ">").AppendLine();
            method.ToILString(s);
            s.AppendLine().Append(tag == null ? "</il>" : "</" + tag + ">");
            Console.WriteLine(s);
        }

        public static StringBuilder ToILString(this MethodInfo method, StringBuilder s = null)
        {
            s = s ?? new StringBuilder();

            var ilReader = ILReaderFactory.Create(method);

            var secondLine = false;
            foreach (var il in ilReader)
            {
                if (secondLine) 
                    s.AppendLine();
                else 
                    secondLine = true;
                
                s.Append(il.Offset.ToString().PadRight(4, ' ')).Append(' ').Append(il.OpCode);
                if (il is InlineFieldInstruction f)
                    s.Append(' ').Append(f.Field.DeclaringType.Name).Append('.').Append(f.Field.Name);
                else if (il is InlineMethodInstruction m)
                    s.Append(' ').Append(m.Method.DeclaringType.Name).Append('.').Append(m.Method.Name);
                else if (il is InlineTypeInstruction t)
                    s.Append(' ').Append(t.Type.Name);
                else if (il is InlineTokInstruction tok)
                    s.Append(' ').Append(tok.Member.Name);
                else if (il is InlineBrTargetInstruction br)
                    s.Append(' ').Append(br.TargetOffset);
            }

            return s;
        }
    }

    public interface ITest
    {
        int Run();
    }
}
