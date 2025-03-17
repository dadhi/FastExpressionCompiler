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
using System.Collections;
using System.Collections.Generic;
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
    public static bool AreSame<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        ReferenceEquals(expected, actual) ? true : throw new AssertionException(
            $"Expected the same `ReferenceEquals({expectedName}, {actualName})`, but found Not the same `{expected.ToCode()}` and `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreNotSame<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        !ReferenceEquals(expected, actual) ? true : throw new AssertionException(
            $"Expected Not same `!ReferenceEquals({expectedName}, {actualName})`, but found the same `{expected.ToCode()}` and `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        Equals(expected, actual) ? true : throw new AssertionException(
            $"Expected `{expectedName} == {actualName}`, but found `{expected.ToCode()} != {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreNotEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !Equals(expected, actual) ? true : throw new AssertionException(
            $"Expected `{expectedName} != {actualName}`, but found `{expected.ToCode()} == {actual.ToCode()}`");

    public static bool AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
    {
        var index = 0;
        var expectedEnumerator = expected.GetEnumerator();
        var actualEnumerator = actual.GetEnumerator();
        var hasExpected = true;
        var hasActual = true;
        var expectedCount = 0;
        var actualCount = 0;

        // Traverse until the end of the largest collection
        while (hasExpected | hasActual)
        {
            hasExpected = hasExpected && expectedEnumerator.MoveNext();
            if (hasExpected) ++expectedCount;

            hasActual = hasActual && actualEnumerator.MoveNext();
            if (hasActual) ++actualCount;

            if (hasExpected & hasActual)
            {
                var exp = expectedEnumerator.Current;
                var act = actualEnumerator.Current;
                if (!Equals(exp, act))
                    // todo: @wip gather the all differences, or better up-to the specified number! 
                    throw new AssertionException(
                        $"Expected the collection `{expectedName} to have the equal items in order with {actualName}`, but found the difference at the index #{index}: `{exp.ToCode()} != {act.ToCode()}`");
            }
            ++index;
        }

        if (hasExpected != hasActual)
            throw new AssertionException(
                $"Expected the collection `{expectedName} to have the same count as {actualName}, but found {expectedCount} != {actualCount}");

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterOrEqual<T>(T expected, T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual")
        where T : IComparable<T> =>
        expected.CompareTo(actual) >= 0 ? true : throw new AssertionException(
            $"Expected `{expectedName} >= {actualName}`, but found `{expected.ToCode()} < {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is null ? true : throw new AssertionException(
            $"Expected null `{actualName}`, but found `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        !actual.HasValue ? true : throw new AssertionException(
            $"Expected this nullable `{actualName}` to be null, but found `{actual.Value}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is not null ? true : throw new AssertionException(
            $"Expected not null `{actualName}`, but found null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        actual.HasValue ? true : throw new AssertionException(
            $"Expected this nullable `{actualName}` to have value, but found null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTrue(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual ? true : throw new AssertionException(
            $"Expected `{actualName}` to be true, but found false");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFalse(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !actual ? true : throw new AssertionException(
            $"Expected `{actualName}` to be false, but found true");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInstanceOf<T>(object actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual is T ? true : throw new AssertionException(
            $"Expected `{actualName}` to be an instance of `{typeof(T).ToCode()}`, but found `{actual.GetType().ToCode()}`");

    public static E Throws<E>(Action action,
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
        catch (E ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"Expected exception of type `{typeof(E).ToCode()}` in `{actionName}`, but found `{ex.GetType().ToCode()}` with message '{ex.Message}'");
        }
        throw new AssertionException($"Expected exception of type `{typeof(E).ToCode()}` in `{actionName}`, but no exception was thrown");
    }
}

public interface ITest
{
    int Run();
}

