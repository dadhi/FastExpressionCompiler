using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

using LEC = FastExpressionCompiler.LightExpression.ExpressionCompiler;
using LE = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class ExprInfoVsExpr_CreateAndCompile_NestedLambdaExpr
    {
        public class S
        {
            public string Value;

            public void SetValue(string s)
            {
                Value = s;
            }
        }

        private readonly S _s = new S();

        private static readonly MethodInfo _setValueMethod = 
            typeof(S).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(S.SetValue));

        [Benchmark]
        public object CreateExpression_and_Compile()
        {
            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = Expression.Lambda(
                Expression.Lambda(
                    Expression.Call(Expression.Constant(_s), _setValueMethod, aParam),
                    aParam
                )
            );
            return expr.Compile();
        }

        [Benchmark]
        public object CreateExpression_and_FastCompile()
        {
            var aParam = Expression.Parameter(typeof(string), "a");
            var expr = Expression.Lambda(
                Expression.Lambda(
                    Expression.Call(Expression.Constant(_s), _setValueMethod, aParam),
                    aParam
                )
            );

            return expr.TryCompile<Func<Action<string>>>();
        }

        [Benchmark(Baseline = true)]
        public object CreateExpressionInfo_and_FastCompile()
        {
            //Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var aParam = LE.Parameter(typeof(string), "a");
            var expr = LE.Lambda(LE.Lambda(LE.Call(LE.Constant(_s), _setValueMethod, aParam), aParam));

            return LEC.TryCompile<Func<Action<string>>>(expr);
        }
    }
}
