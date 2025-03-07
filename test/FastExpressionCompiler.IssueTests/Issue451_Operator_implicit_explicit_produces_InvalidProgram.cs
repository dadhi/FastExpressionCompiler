using System;
using NUnit.Framework;
using System.Reflection;


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
        Convert_nullable_enum_into_the_underlying_nullable_type();

        Original_case();
        The_operator_method_is_provided_in_Convert();
        return 3;
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

    enum Hey : byte { Sailor = 5 }

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