using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue451_Operator_implicit_explicit_produces_InvalidProgram : ITest
{
    public int Run()
    {
        TestCollectionAssertAreEqual();

        ConvertChecked_int_to_byte_enum();
        Convert_int_to_byte_enum();
        Convert_byte_to_enum();
        Convert_byte_to_nullable_enum();

        Convert_nullable_to_nullable_given_the_conv_op_of_underlying_to_underlying();

        Convert_nullable_enum_into_the_underlying_nullable_type();
        Convert_nullable_enum_into_the_compatible_to_underlying_nullable_type();

        Convert_nullable_enum_using_the_conv_op();
        Convert_nullable_enum_using_the_Passed_conv_method();

        Convert_nullable_enum_using_the_conv_op_with_nullable_param();

        Original_case();
        The_operator_method_is_provided_in_Convert();

        return 12;
    }


#if TRUE // todo: @wip #453 draft of the implementation

    public struct FooBarTests : ITest
    {
        public void Run(TestRunContext t)
        {
            TestFoo(t);
            TestBar(t);
        }

        public void TestFoo(TestMethodContext t)
        {
            t.IsTrue(false);
            t.Fails("Not implemented");
        }

        public void TestBar(TestMethodContext t)
        {
            t.Fails("Not implemented");
        }
    }

    public interface ITest
    {
        void Run(TestRunContext t);
    }

    public static class TestRunner
    {
        // returns number of the tests
        public static int Run()
        {
            var ctx = new TestRunContext();

            ctx.Run(new FooBarTests());

            return ctx.TotalTestCount;
        }
    }

    public record struct TestFailure(
        string TestMethodName,
        int SourceLineNumber,
        AssertKind Kind,
        object actual, string actualName,
        object optionalExpected, string optionalExpectedName);

    public record struct TestStats(
        string TestsName,
        string TestsFile,
        Exception TestStopException,
        int TestCount,
        int FirstFailureIndex,
        int FailureCount);

    public enum TestRunTracking
    {
        TrackFailedTestsOnly = 0,
        TrackAllTests,
    }

    /// <summary>Per-thread context, accumulating the stats and failures in its Run method.</summary>
    public sealed class TestRunContext
    {
        public int TotalTestCount;
        // todo: @perf it may use ImTools.SmallList for the stats and failures to more local access to the Count
        public List<TestStats> Stats = new();
        public List<TestFailure> Failures = new();

        public void Run(ITest test, TestRunTracking tracking = TestRunTracking.TrackFailedTestsOnly)
        {
            var totalTestCount = TotalTestCount;
            var failureCount = Failures.Count;
            Exception testStopException = null;
            try
            {
                test.Run(this);
            }
            catch (Exception ex)
            {
                testStopException = ex;
            }

            var testFailureCount = Failures.Count - failureCount;
            if (testStopException != null ||
                tracking == TestRunTracking.TrackAllTests ||
                tracking == TestRunTracking.TrackFailedTestsOnly & testFailureCount > 0)
            {
                // todo: @wip is there a more performant way to get the test name and file?
                var testsType = test.GetType();
                var testsName = testsType.Name;
                var testsFile = new Uri(testsType.Assembly.Location).LocalPath;

                var testCount = TotalTestCount - totalTestCount;

                var stats = new TestStats(testsName, testsFile, testStopException, testCount, failureCount, testFailureCount);
                Stats.Add(stats);
            }
        }
    }

    public enum AssertKind
    {
        CommandedToFail,
        IsTrue,
        IsFalse,
        AreEqual,
        AreNotEqual,
        Throws,
    }

    // Wrapper for the context per test method
    public struct TestMethodContext
    {
        public readonly TestRunContext TestRunContext;
        public TestMethodContext(TestRunContext testRunContext) => TestRunContext = testRunContext;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TestMethodContext(TestRunContext t)
        {
            // A trick to automatically increment the test count when passing context to the test method
            t.TotalTestCount += 1;
            return new TestMethodContext(t);
        }

        /// <summary>Always failes with the provided message</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fails(string message,
#if NETCOREAPP3_0_OR_GREATER
            [CallerMemberName]
#endif
            string testName = "<test>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerLineNumber]
#endif
            int sourceLineNumber = -1)
        {
            var failure = new TestFailure(testName, sourceLineNumber, AssertKind.CommandedToFail, null, message, null, null);
            TestRunContext.Failures.Add(failure);
        }

        /// <summary>Method returns the Assert result to ptentially be used by the User for the latter test logic, e.g. returning early</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTrue(bool actual,
#if NETCOREAPP3_0_OR_GREATER
            [CallerArgumentExpression(nameof(actual))]
#endif
            string actualName = "<actual>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerMemberName]
#endif
            string testName = "<test>",
#if NETCOREAPP3_0_OR_GREATER
            [CallerLineNumber]
#endif
            int sourceLineNumber = -1)
        {
            if (actual)
                return true;

            var failure = new TestFailure(testName, sourceLineNumber, AssertKind.IsTrue, actual, actualName, null, null);
            TestRunContext.Failures.Add(failure);
            return false;
        }
    }
#endif

    public void TestCollectionAssertAreEqual()
    {
        var expected = new[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        var actual =   new[] { 1, 3, 6, 7, 9, 12, 14, 15 };

        Asserts.AreEqual(expected, actual);
    }

    public void Convert_byte_to_nullable_enum()
    {
        var conversion = Convert(Constant((byte)5, typeof(byte)), typeof(Hey?));
        var e = Lambda<Func<Hey?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(Hey.Sailor, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(Hey.Sailor, ff());
    }

    public void Convert_byte_to_enum()
    {
        var conversion = Convert(Constant((byte)5, typeof(byte)), typeof(Hey));
        var e = Lambda<Func<Hey>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(Hey.Sailor, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(Hey.Sailor, ff());
    }

    public void Convert_int_to_byte_enum()
    {
        var n = Parameter(typeof(int), "n");
        var conversion = Convert(n, typeof(Hey));
        var e = Lambda<Func<int, Hey>>(conversion, n);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual((Hey)Byte.MaxValue, fs(Byte.MaxValue));
        Asserts.AreEqual(default(Hey), fs(Byte.MaxValue + 1));

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual((Hey)Byte.MaxValue, ff(Byte.MaxValue));
        Asserts.AreEqual(default(Hey), ff(Byte.MaxValue + 1));
    }

    public void ConvertChecked_int_to_byte_enum()
    {
        var n = Parameter(typeof(int), "n");
        var conversion = ConvertChecked(n, typeof(Hey));
        var e = Lambda<Func<int, Hey>>(conversion, n);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        var x = fs(Byte.MaxValue);
        Asserts.AreEqual((Hey)Byte.MaxValue, fs(Byte.MaxValue));
        Asserts.Throws<OverflowException>(() => fs(Byte.MaxValue + 1));

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual((Hey)Byte.MaxValue, ff(Byte.MaxValue));
        Asserts.Throws<OverflowException>(() => ff(Byte.MaxValue + 1));
    }

    public struct SampleType
    {
        public bool? Value { get; set; }

        public SampleType(bool? value) { Value = value; }

        public static implicit operator bool?(SampleType left) =>
            (left.Value is not null && left.Value is bool b) ? b : null;

        public static explicit operator bool?(SampleType? left) =>
            left == null ? null : (left.Value.Value is bool b && b) ? b : null;

        public static explicit operator bool(SampleType left) =>
            left.Value is not null && left.Value is bool b && b;
    }

    public enum Hey : byte { Sailor = 5 }

    public void Convert_nullable_enum_into_the_underlying_nullable_type()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(byte?));
        var e = Lambda<Func<byte?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    public void Convert_nullable_enum_into_the_compatible_to_underlying_nullable_type()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(int?));
        var e = Lambda<Func<int?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff());
    }

    public class Foo
    {
        public int Value;
        public static explicit operator Foo(byte b) => new Foo { Value = b }; // unused
        public static explicit operator Foo(Hey hey) => new Foo { Value = (int)hey };
    }

    public void Convert_nullable_enum_using_the_conv_op()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(Foo));
        var e = Lambda<Func<Foo>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    public void Convert_nullable_enum_using_the_Passed_conv_method()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(Foo),
            typeof(Foo).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "op_Explicit" &&
                    m.GetParameters()[0].ParameterType == typeof(Hey)));

        var e = Lambda<Func<Foo>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    public struct Bar
    {
        public int? Value;
        public static explicit operator Bar(int? n) => new Bar { Value = n };
        public static explicit operator Bar(Hey? hey) => new Bar { Value = hey.HasValue ? (int)hey.Value : null };
        public static explicit operator Bar?(Hey? hey) => !hey.HasValue ? null : new Bar { Value = (int)hey.Value };
    }

    public void Convert_nullable_enum_using_the_conv_op_with_nullable_param()
    {
        var conversion = Convert(Constant(null, typeof(Hey?)), typeof(Bar));
        var e = Lambda<Func<Bar>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.IsNull(fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.IsNull(ff().Value);
    }

    public struct Jazz
    {
        public int Value;
        public static explicit operator Jazz(int n) => new Jazz { Value = n };
    }

    public void Convert_nullable_to_nullable_given_the_conv_op_of_underlying_to_underlying()
    {
        var conversion = Convert(Constant(42, typeof(int?)), typeof(Jazz?));
        var e = Lambda<Func<Jazz?>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(42, fs().Value.Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(42, fs().Value.Value);
    }

    public void Original_case()
    {
        var ctorMethodInfo = typeof(SampleType).GetConstructors()[0];

        var newExpression = New(ctorMethodInfo, Constant(null, typeof(bool?)));

        var conversion1 = Convert(newExpression, typeof(bool));
        var lambda1 = Lambda<Func<bool>>(conversion1);
        lambda1.PrintCSharp();

        var conversion2 = Convert(newExpression, typeof(bool?));
        var lambda2 = Lambda<Func<bool?>>(conversion2);
        lambda2.PrintCSharp();

        var sample1 = lambda1.CompileSys();
        sample1.PrintIL();
        Asserts.AreEqual(false, sample1());

        var sample2 = lambda2.CompileSys();
        sample2.PrintIL();
        Asserts.IsNull(sample2());

        // <- OK
        var sample_fast1 = lambda1.CompileFast(false);
        sample_fast1.PrintIL();
        Asserts.AreEqual(false, sample_fast1());

        // <- throws exception
        var sample_fast2 = lambda2.CompileFast(false);
        sample_fast2.PrintIL();
        Asserts.IsNull(sample_fast2());
    }

    public void The_operator_method_is_provided_in_Convert()
    {
        var ctorMethodInfo = typeof(SampleType).GetConstructors()[0];

        var newExpression = New(ctorMethodInfo, Constant(null, typeof(bool?)));

        // let's use the explicit operator method which converts to bool
        var convertToNullableBoolMethod = typeof(SampleType).FindConvertOperator(typeof(SampleType), typeof(bool));
        var conversion = Convert(newExpression, typeof(bool?), convertToNullableBoolMethod);

        var lambda = Lambda<Func<bool?>>(conversion);
        lambda.PrintCSharp();

        var sample = lambda.CompileSys();
        sample.PrintIL();
        Asserts.AreEqual(false, sample());

        var sample_fast = lambda.CompileFast(false);
        sample_fast.PrintIL();
        Asserts.AreEqual(false, sample_fast());
    }
}