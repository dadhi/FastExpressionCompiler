#if DEBUG
#define PRINTIL
#define PRINTCS
#endif
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Diagnostics;
using NUnit.Framework;
using FastExpressionCompiler.ILDecoder;
using System.IO;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression;
#else
namespace FastExpressionCompiler;
#endif

public static class TestTools
{
    public static void AssertOpCodes(this Delegate @delegate, params OpCode[] expectedCodes) =>
        AssertOpCodes(@delegate.Method, expectedCodes);

    public static void AssertOpCodes(this MethodInfo method, params OpCode[] expectedCodes)
    {
        var ilReader = ILReaderFactory.Create(method);
        if (ilReader is null)
        {
            Debug.WriteLine($"Reading IL is currently not supported");
            return;
        }
        var actualCodes = ilReader.Select(x => x.OpCode).ToArray();
        var sb = new StringBuilder();
        var n = 0;
        foreach (var code in actualCodes)
            sb.AppendLine($"{n++,-4}{code}");
        CollectionAssert.AreEqual(expectedCodes, actualCodes, "Unexpected IL OpCodes, actual codes are: " + Environment.NewLine + sb);
    }

    [Conditional("DEBUG")]
    public static void PrintExpression(this Expression expr, bool completeTypeNames = false) =>
        Console.WriteLine(
            expr.ToExpressionString(out var _, out var _, out var _,
            stripNamespace: true,
            printType: completeTypeNames ? null : CodePrinter.PrintTypeStripOuterClasses,
            indentSpaces: 4)
        );

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, bool completeTypeNames = false,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
#if PRINTCS
        Console.WriteLine();
        Console.WriteLine($"//{Path.GetFileNameWithoutExtension(filePath)}.{caller}");

        var sb = new StringBuilder(1024);
        sb.Append("var @cs = ");
        sb = expr.ToCSharpString(sb,
            lineIndent: 0,
            stripNamespace: true,
            printType: completeTypeNames ? null : CodePrinter.PrintTypeStripOuterClasses,
            indentSpaces: 4);
        sb.Append(';');
        Console.WriteLine(sb.ToString());
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, Func<string, string> transform,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
#if PRINTCS
        Console.WriteLine();
        Console.WriteLine($"//{Path.GetFileNameWithoutExtension(filePath)}.{caller}");
        Console.WriteLine(transform(expr.ToCSharpString()));
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, CodePrinter.ObjectToCode objectToCode,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
#if PRINTCS
        Console.WriteLine();
        Console.WriteLine($"//{Path.GetFileNameWithoutExtension(filePath)}.{caller}");
        Console.WriteLine(expr.ToCSharpString(objectToCode));
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintCSharp(this Expression expr, ref string result)
    {
#if PRINTCS
        Console.WriteLine(result = expr.ToCSharpString());
#endif
    }

    [Conditional("DEBUG")]
    public static void PrintIL(this Delegate @delegate, [CallerMemberName] string tag = null)
    {
#if PRINTIL
        @delegate.Method.PrintIL(tag);
#endif
    }

    [Conditional("DEBUG")]
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
}

// todo: @wip @feat #453 replacing the last NUnit bone of Assert
public static class Asserts
{
    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssertionException AreEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        Equals(expected, actual) ? null : throw new AssertionException(
            $"Expected `{expectedName} == {actualName}`, but found `{expected?.ToString() ?? "null"} == {actual}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssertionException IsNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is null ? null : throw new AssertionException(
            $"Expected null `{actualName}`, but found `{actual}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssertionException IsNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        !actual.HasValue ? null : throw new AssertionException(
            $"Expected this nullable `{actualName}` to be null, but found `{actual.Value}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssertionException IsNotNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is not null ? null : throw new AssertionException(
            $"Expected not null `{actualName}`, but was null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssertionException IsTrue(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual ? null : throw new AssertionException(
            $"Expected `{actualName} == true`, but was `false`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssertionException IsFalse(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !actual ? null : throw new AssertionException(
            $"Expected `{actualName} == false`, but was `true`");

    public static AssertionException Throws<E>(Action action,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(action))]
#endif
        string actionName = "<action to throw>")
        where E : Exception
    {
        try
        {
            action();
        }
        catch (E)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected exception of type `{typeof(E).Name}` in `{actionName}`, but found `{ex.GetType().Name}`: {ex.Message}");
        }
        throw new AssertionException($"Expected exception of type `{typeof(E).Name}` in `{actionName}`, but no exception was thrown");
    }
}

public interface ITest
{
    int Run();
}

