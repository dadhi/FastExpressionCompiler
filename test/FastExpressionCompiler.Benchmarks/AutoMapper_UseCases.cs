using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using LE = FastExpressionCompiler.LightExpression.ExpressionCompiler;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class AutoMapper_UseCases
    {
        //private Expression CreateExpression()
        //{

        //}

        [Benchmark]
        public object Compile()
        {
            return null;
        }
    }
}
