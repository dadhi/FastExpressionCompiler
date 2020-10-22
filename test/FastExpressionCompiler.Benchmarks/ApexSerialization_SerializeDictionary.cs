using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    public class ApexSerialization_SerializeDictionary
    {
        /*
        ## V3 Benchmarks

        */
        [MemoryDiagnoser]
        public class Compile
        {
            private static readonly FastExpressionCompiler.LightExpression.LambdaExpression _lightExpr = 
                FastExpressionCompiler.LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails.CreateSerializeDictionaryExpression();
            private static readonly System.Linq.Expressions.LambdaExpression _sysExpr = _lightExpr.ToLambdaExpression();

            [Benchmark]
            public object CompileSys() => _sysExpr.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => LightExpression.ExpressionCompiler.CompileFast(_lightExpr);
        }
    }
}
