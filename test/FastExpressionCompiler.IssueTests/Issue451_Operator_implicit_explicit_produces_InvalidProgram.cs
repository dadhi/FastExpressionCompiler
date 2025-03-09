using System;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;




#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue451_Operator_implicit_explicit_produces_InvalidProgram : ITest
{
    public int Run()
    {
        // Convert_nullable_enum_into_the_underlying_nullable_type();
        // Convert_nullable_enum_into_the_compatible_to_underlying_nullable_type();

        Convert_nullable_enum_using_the_conv_op_from_convertible_underlying_type();
        Convert_nullable_enum_using_the_conv_op_from_convertible_nullable_of_underlying_type();

        Original_case();
        The_operator_method_is_provided_in_Convert();

        return 6;
    }

#if FEAT_453 // todo: @wip #453 draft of the implementation
    public record struct TestFailure(string Message, string TestName, int SourceLineNumber, string TestsName, string TestsFile);
    public sealed class Context
    {
        public uint EvaluatedTestCount;
        public uint FailedTestCount;
        public List<TestFailure> Failures = new();

        public string CurrentTestsName;
        public string CurrentTestsFile;

        internal void AssertFails(string message,
            [CallerMemberName] string testName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            ++EvaluatedTestCount;
            ++FailedTestCount;
            Failures.Add(new TestFailure(message, testName, sourceLineNumber, CurrentTestsName, CurrentTestsFile));
        }

        internal void Register(string testsName, string sourceFilePath)
        {
            CurrentTestsName = testsName;
            CurrentTestsFile = sourceFilePath;
        }
    }

    public void Run(Context c,
        [CallerFilePath] string sourceFilePath = "")
    {
        c.Register(GetType().Name, sourceFilePath);
        TestFoo(c);
        // TestBar(c);
    }

    public void TestFoo(Context c)
    {
        c.AssertFails("Not implemented");
    }
#endif

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
        Assert.AreEqual(5, fs());

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Assert.AreEqual(5, ff());
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
        public static explicit operator Foo(byte b) => new Foo { Value = b };
        public static explicit operator Foo(Hey hey) => new Foo { Value = (int)hey };
    }

    public void Convert_nullable_enum_using_the_conv_op_from_convertible_underlying_type()
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

    public class Bar
    {
        public int? Value;
        public static explicit operator Bar(int? n) => new Bar { Value = n };
        public static explicit operator Bar(Hey? hey) => new Bar { Value = hey.HasValue ? (int)hey.Value : null };
    }

    public void Convert_nullable_enum_using_the_conv_op_from_convertible_nullable_of_underlying_type()
    {
        var conversion = Convert(Constant(Hey.Sailor, typeof(Hey?)), typeof(Bar));
        var e = Lambda<Func<Bar>>(conversion);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(5, fs().Value);

        var ff = e.CompileFast(false);
        ff.PrintIL();
        Asserts.AreEqual(5, ff().Value);
    }

    // todo: @wip pass the conversation methods explicitly

    [Test]
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
        Assert.IsNull(sample1());

        var sample2 = lambda2.CompileSys();
        sample2.PrintIL();
        Assert.IsNull(sample2());

        // <- OK
        var sample_fast1 = lambda1.CompileFast(false);
        sample_fast1.PrintIL();
        Assert.IsNull(sample_fast1());

        // <- throws exception
        var sample_fast2 = lambda2.CompileFast(false);
        sample_fast2.PrintIL();
        Assert.IsNull(sample_fast2());
    }

    [Test]
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
        Assert.IsNull(sample());

        var sample_fast = lambda.CompileFast(false);
        sample_fast.PrintIL();
        Assert.IsNull(sample_fast());
    }
}