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
using FastExpressionCompiler.ILDecoder;
using System.IO;
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
        var index = 0;
        foreach (var code in actualCodes)
            sb.AppendLine($"{index++,-4}{code}");

        // todo: @wip
        // Asserts.AreEqual(expectedCodes, actualCodes, "Unexpected IL OpCodes, actual codes are: " + Environment.NewLine + sb);
        Asserts.AreEqual<OpCode>(expectedCodes, actualCodes);
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
            $"Expected `AreSame({expectedName}, {actualName})`, but found `{expected.ToCode()}` is Not the same `{actual.ToCode()}`");

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
            $"Expected `AreNotSame({expectedName}, {actualName})`, but found `{expected.ToCode()}` is same as `{actual.ToCode()}`");

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
            $"Expected `AreEqual({expectedName}, {actualName})`, but found `{expected.ToCode()}` is Not equal to `{actual.ToCode()}`");

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
            $"Expected `AreNotEqual({expectedName}, {actualName})`, but found `{expected.ToCode()}` is equal to `{actual.ToCode()}`");

    /// <summary>Should cover the case with the `expected` to be an array as well.</summary>
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
        var expectedEnumerator = expected.GetEnumerator();
        var actualEnumerator = actual.GetEnumerator();
        var expectedCount = 0;
        var actualCount = 0;

        // Traverse until the end of the largest collection
        var hasExpected = true;
        var hasActual = true;
        for (var index = 0; hasExpected | hasActual; ++index)
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
        }

        if (hasExpected != hasActual)
            throw new AssertionException(
                $"Expected `AreEqual({expectedName}, {actualName})`, but found the expected count {expectedCount} is not equal to actual count {actualCount}");

        return true;
    }

    public static bool AreEqual<T>(T[] expected, T[] actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(expected))] 
#endif
        string expectedName = "expected",
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        AreEqual((IEnumerable<T>)expected, actual, expectedName, actualName);

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
            $"Expected `GreaterOrEqual({expectedName}, {actualName})`, but found `{expected.ToCode()} < {actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is null ? true : throw new AssertionException(
            $"Expected `IsNull({actualName})`, but found not null `{actual.ToCode()}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        !actual.HasValue ? true : throw new AssertionException(
            $"Expected the nullable `IsNull({actualName})`, but found it has a value `{actual.Value}`");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(T actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : class =>
        actual is not null ? true : throw new AssertionException(
            $"Expected `IsNotNull({actualName})`, but found null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(T? actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") where T : struct =>
        actual.HasValue ? true : throw new AssertionException(
            $"Expected the nullable `IsNotNull({actualName})`, but found null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTrue(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual ? true : throw new AssertionException(
            $"Expected `IsTrue({actualName})`, but found false");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFalse(bool actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        !actual ? true : throw new AssertionException(
            $"Expected `IsFalse({actualName})`, but found true");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInstanceOf<T>(object actual,
#if NETCOREAPP3_0_OR_GREATER
        [CallerArgumentExpression(nameof(actual))]
#endif
        string actualName = "actual") =>
        actual is T ? true : throw new AssertionException(
            $"Expected `IsInstanceOf<{typeof(T).ToCode()}>({actualName})`, but found `IsInstanceOf<{actual?.GetType().ToCode() ?? "_"}>({actual.ToCode()})`");

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
                $"Expected `Throws<{typeof(E).ToCode()}>({actionName})`, but found it throws `{ex.GetType().ToCode()}` with message '{ex.Message}'");
        }
        throw new AssertionException($"Expected `Throws<{typeof(E).ToCode()}>({actionName})`, but no exception was thrown");
    }
}

public interface ITest
{
    int Run();
}
