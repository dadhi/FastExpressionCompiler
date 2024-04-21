#if NET7_0_OR_GREATER && !LIGHT_EXPRESSION
using System;
using NUnit.Framework;
using Mapster;

#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue405_NullReferenceException_with_V4X_when_using_nullable_long_array : ITest
    {
        public int Run()
        {
            var sensibleFlags = new[]
            {
                CompilerFlags.Default,
                CompilerFlags.NoInvocationLambdaInlining | CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
            };

            foreach (var compilerFlags in sensibleFlags)
                NullItemArray(compilerFlags);

            return 2;
        }

        private class ReportAdaptRule : TypeAdapterRule
        {
            internal ReportAdaptRule()
            {
                TypeAdapterConfig<ExportInfoModel, ExportInfoDto>.NewConfig();
            }
        }

        private sealed record ExportInfoDto(long[] Locations);
        private sealed record ExportInfoModel(long?[] Locations);

        [TestCase(CompilerFlags.Default)]
        [TestCase(
            CompilerFlags.NoInvocationLambdaInlining |
            CompilerFlags.EnableDelegateDebugInfo |
            CompilerFlags.ThrowOnNotSupportedExpression)]
        public void NullItemArray(CompilerFlags compilerFlags)
        {
            TypeAdapterConfig.RulesTemplate.Add(new ReportAdaptRule());
            TypeAdapterConfig.GlobalSettings.Compiler = exp => 
            {
                exp.PrintCSharp();

                var ff = exp.CompileFast(flags: compilerFlags);
                ff.PrintIL();
                return ff;
            };
            TypeAdapterConfig.GlobalSettings.Compile();

            var model = new ExportInfoModel(new long?[] { 1, null });
            var dto = model.Adapt<ExportInfoDto>();
            CollectionAssert.AreEqual(new long[] { 1, 0 }, dto.Locations);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

#endif