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
    public class LightExprVsExpr_CreateAndCompile_NestedLambdaExpr
    {
        /*
        ## 25.01.2019

                                       Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ------------------------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 CreateExpression_and_Compile | 275.27 us | 1.8691 us | 1.7484 us | 13.31 |    0.10 |      1.4648 |      0.4883 |           - |             8.42 KB |
             CreateExpression_and_FastCompile |  25.29 us | 0.2089 us | 0.1852 us |  1.22 |    0.01 |      1.3428 |      0.6714 |      0.0610 |             6.24 KB |
         CreateLightExpression_and_FastCompile |  20.68 us | 0.1641 us | 0.1535 us |  1.00 |    0.00 |      1.2512 |      0.6104 |      0.0610 |             5.85 KB |

        ## V3

                                       Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        ------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                 CreateExpression_and_Compile | 271.41 us | 2.0075 us | 1.8778 us | 26.19 |    0.20 | 1.4648 | 0.4883 |      - |   8.42 KB |
             CreateExpression_and_FastCompile |  12.67 us | 0.0627 us | 0.0556 us |  1.22 |    0.01 | 0.7782 | 0.3815 | 0.0610 |   3.55 KB |
         CreateLightExpression_and_FastCompile |  10.36 us | 0.0551 us | 0.0488 us |  1.00 |    0.00 | 0.7629 | 0.3815 | 0.0610 |   3.49 KB |

         */

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
        public object CreateLightExpression_and_FastCompile()
        {
            //Expression<Func<Action<string>>> expr = () => a => s.SetValue(a);

            var aParam = LE.Parameter(typeof(string), "a");
            var expr = LE.Lambda(
                LE.Lambda(LE.Call(LE.Constant(_s), _setValueMethod, aParam), 
                    aParam));

            return LEC.TryCompile<Func<Action<string>>>(expr);
        }
    }
}
