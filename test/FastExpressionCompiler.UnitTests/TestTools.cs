#if DEBUG
#define PRINTIL
#endif
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using FastExpressionCompiler.ILDecoder;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression;
#else
using System.Linq.Expressions;
namespace FastExpressionCompiler;
#endif

public static class TestTools
{
    public static void AssertOpCodes(this Delegate @delegate, params OpCode[] expectedCodes) =>
        AssertOpCodes(@delegate.Method, expectedCodes);

    public static void AssertOpCodes(this MethodInfo method, params OpCode[] expectedCodes)
    {
#if PRINTIL
        var actualCodes = ILReaderFactory.Create(method).Select(x => x.OpCode).ToArray();
        var sb = new StringBuilder();
        var n = 0;
        foreach (var code in actualCodes)
            sb.AppendLine($"{n++, -4}{code}");
        CollectionAssert.AreEqual(expectedCodes, actualCodes, "Unexpected IL OpCodes, actual codes are: " + Environment.NewLine + sb);
#endif
    }

    static private readonly Func<Type, string, string> _stripOuterTypes = (t, s) => s.Substring(s.LastIndexOf('.') + 1);

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintExpression(this Expression expr, bool completeTypeNames = false) =>
        Console.WriteLine(
            expr.ToExpressionString(out var _, out var _, out var _,
            stripNamespace: true,
            printType: completeTypeNames ? null : _stripOuterTypes,
            identSpaces: 4)
        );

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, bool completeTypeNames = false) 
    {
        var sb = new StringBuilder(1024);
        sb.Append("var @cs = ");
        sb = expr.ToCSharpString(sb, lineIdent: 0, stripNamespace: true, printType: completeTypeNames ? null : _stripOuterTypes, identSpaces: 4);
        sb.Append(";");
        Console.WriteLine(sb.ToString());
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, Func<string, string> transform) =>
        Console.WriteLine(transform(expr.ToCSharpString()));

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, CodePrinter.ObjectToCode objectToCode) =>
        Console.WriteLine(expr.ToCSharpString(objectToCode));

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, ref string result) =>
        Console.WriteLine(result = expr.ToCSharpString());

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintIL(this Delegate @delegate, [CallerMemberName] string tag = null)
    {
#if PRINTIL
        @delegate.Method.PrintIL(tag);
#endif
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void PrintIL(this MethodInfo method, string tag = null)
    {
#if PRINTIL
        var s = new StringBuilder();
        s.Append(tag == null ? "<il>" : "<" + tag + ">").AppendLine();
        method.ToILString(s);
        s.AppendLine().Append(tag == null ? "</il>" : "</" + tag + ">");
        Console.WriteLine(s);
#endif
    }

    public static StringBuilder ToILString(this MethodInfo method, StringBuilder s = null)
    {
        if (method is null) throw new ArgumentNullException(nameof(method));

        s = s ?? new StringBuilder();

#if PRINTIL
        var ilReader = ILReaderFactory.Create(method);

        var secondLine = false;
        foreach (var il in ilReader)
        {
            try 
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
                    s.Append(' ').Append(t.Type?.Name);
                else if (il is InlineTokInstruction tok)
                    s.Append(' ').Append(tok.Member.Name);
                else if (il is InlineBrTargetInstruction br)
                    s.Append(' ').Append(br.TargetOffset);
                else if (il is ShortInlineBrTargetInstruction sbr)
                    s.Append(' ').Append(sbr.TargetOffset);
                else if (il is InlineStringInstruction si)
                    s.Append(" \"").Append(si.String).Append('"');
                else if (il is InlineIInstruction ii)
                    s.Append(' ').Append(ii.Int32);
                else if (il is ShortInlineIInstruction sii)
                    s.Append(' ').Append(sii.Byte);
                else if (il is InlineVarInstruction iv)
                    s.Append(' ').Append(iv.Ordinal);
                else if (il is ShortInlineVarInstruction siv)
                    s.Append(' ').Append(siv.Ordinal);
            }
            catch (Exception ex)
            {
                s.AppendLine().AppendLine("EXCEPTION_IN_IL_PRINT: " + ex.Message).AppendLine();
            }
        }
#endif
        return s;
    }
}

public interface ITest
{
    int Run();
}

