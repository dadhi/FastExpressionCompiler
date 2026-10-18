using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FastExpressionCompiler;

namespace System.Linq.Expressions
{
    // Every original Compile call goes here. Both upstream modes exercise FEC;
    // the boolean remains in the test data to preserve all upstream branches.
    internal static class FecCompilation
    {
        private static bool UseSystem => Environment.GetEnvironmentVariable("FEC_TEST_COMPILER") == "system";
        public static TDelegate CompileForFec<TDelegate>(this Expression<TDelegate> expression, bool preferInterpretation = false)
            where TDelegate : Delegate
        {
            CompilationObservationAttribute.Record();
            if (UseSystem) return expression.Compile(preferInterpretation);
            return expression.CompileFast(ifFastFailedReturnNull: true)
                ?? throw new NotSupportedException("FEC strict compilation returned null (fallback disabled). Node: " + expression.Body.NodeType);
        }
        public static Delegate CompileForFec(this LambdaExpression expression, bool preferInterpretation = false)
        {
            CompilationObservationAttribute.Record();
            if (UseSystem) return expression.Compile(preferInterpretation);
            return expression.CompileFast(ifFastFailedReturnNull: true)
                ?? throw new NotSupportedException("FEC strict compilation returned null (fallback disabled). Node: " + expression.Body.NodeType);
        }
        public static TDelegate CompileForFec<TDelegate>(this Expression<TDelegate> expression, DebugInfoGenerator debugInfoGenerator)
            where TDelegate : Delegate
        {
            CompilationObservationAttribute.Record();
            if (UseSystem) return expression.Compile(debugInfoGenerator);
            throw new NotSupportedException("FEC does not expose DebugInfoGenerator compilation.");
        }
        public static Delegate CompileForFec(this LambdaExpression expression, DebugInfoGenerator debugInfoGenerator)
        {
            CompilationObservationAttribute.Record();
            if (UseSystem) return expression.Compile(debugInfoGenerator);
            throw new NotSupportedException("FEC does not expose DebugInfoGenerator compilation.");
        }
    }
}

