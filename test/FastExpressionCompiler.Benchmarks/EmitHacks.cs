#if NET7_0 && !LIGHT_EXPRESSION

using System;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.IssueTests;

namespace FastExpressionCompiler.Benchmarks
{

    public class EmitHacks
    {
        /*
        -- Initial result - 4000x slower

        |                    Method |         Mean |        Error |       StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
        |-------------------------- |-------------:|-------------:|-------------:|------:|-------:|-------:|------:|----------:|
        | DynamicMethod_Emit_Newobj | 51,156.76 ns | 1,014.771 ns | 2,095.677 ns | 1.000 | 0.3662 | 0.1831 |     - |    1215 B |
        |  Activator_CreateInstance |     13.20 ns |     0.338 ns |     0.506 ns | 0.000 | 0.0076 |      - |     - |      24 B |
        */

        // [MemoryDiagnoser(displayGenColumns: false)]
        // public class SimpleConstructorEmit
        // {
        //     [Benchmark(Baseline = true)]
        //     public EmitHacksTest.A DynamicMethod_Emit()
        //     {
        //         var f = EmitHacksTest.Get_DynamicMethod_Emit_Newobj();
        //         return f();
        //     }

        //     [Benchmark]
        //     public EmitHacksTest.A Activator_CreateInstance() =>
        //         (EmitHacksTest.A)Activator.CreateInstance(typeof(EmitHacksTest.A));
        // }

        [MemoryDiagnoser(displayGenColumns: false)]
        public class MethodStaticNoArgsEmit
        {
            /*
            -- Baseline

            |                          Method |         Mean |        Error |        StdDev | Ratio | Allocated | Alloc Ratio |
            |-------------------------------- |-------------:|-------------:|--------------:|------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call | 49,512.07 ns | 5,727.220 ns | 16,886.837 ns | 1.000 |    1183 B |        1.00 |
            |               MethodInfo_Invoke |     80.49 ns |     1.145 ns |      1.015 ns | 0.001 |      24 B |        0.02 |

            -- First working hack
            |                          Method |      Mean |     Error |     StdDev |    Median | Ratio | RatioSD | Allocated | Alloc Ratio |
            |-------------------------------- |----------:|----------:|-----------:|----------:|------:|--------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call |  36.03 us |  1.354 us |   3.993 us |  34.30 us |  1.00 |    0.00 |   1.16 KB |        1.00 |
            |         DynamicMethod_Emit_Hack | 607.97 us | 40.797 us | 119.008 us | 560.64 us | 17.03 |    3.79 |    1.4 KB |        1.22 |

            -- Moving fields and methods discovery to statics

            |                          Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
            |-------------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call |  32.74 us |  0.651 us |  1.534 us |  1.00 |    0.00 |   1.16 KB |        1.00 |
            |         DynamicMethod_Emit_Hack | 599.96 us | 28.561 us | 83.313 us | 18.31 |    2.84 |    1.4 KB |        1.21 |

            -- Did some code mangling and now we are talking

            |                          Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD | Allocated | Alloc Ratio |
            |-------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call | 35.79 us | 0.789 us | 2.172 us | 35.07 us |  1.00 |    0.00 |   1.16 KB |        1.00 |
            |         DynamicMethod_Emit_Hack | 37.82 us | 0.749 us | 1.513 us | 37.38 us |  1.05 |    0.07 |   1.31 KB |        1.13 |

            -- Method with no args final results

            |                          Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD | Allocated | Alloc Ratio |
            |-------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call | 35.48 us | 0.720 us | 2.031 us | 34.86 us |  1.00 |    0.00 |   1.16 KB |        1.00 |
            |         DynamicMethod_Emit_Hack | 34.62 us | 0.685 us | 1.253 us | 34.49 us |  0.97 |    0.07 |   1.16 KB |        1.00 |

            -- Method with one argument

            |                          Method |     Mean |    Error |   StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
            |-------------------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call | 42.67 us | 0.840 us | 1.844 us |  1.00 |    0.00 |   1.17 KB |        1.00 |
            |         DynamicMethod_Emit_Hack | 42.13 us | 0.837 us | 1.145 us |  0.99 |    0.06 |   1.17 KB |        1.00 |

            -- All emits replaced by hacks

            |                          Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD | Allocated | Alloc Ratio |
            |-------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
            | DynamicMethod_Emit_OpCodes_Call | 49.92 us | 2.486 us | 7.292 us | 47.76 us |  1.00 |    0.00 |   1.17 KB |        1.00 |
            |         DynamicMethod_Emit_Hack | 46.16 us | 1.322 us | 3.751 us | 45.31 us |  0.94 |    0.15 |   1.12 KB |        0.96 |

            */

            [Benchmark(Baseline = true)]
            public int DynamicMethod_Emit_OpCodes_Call()
            {
                var f = EmitHacksTest.Get_DynamicMethod_Emit_OpCodes_Call();
                return f(41);
            }

            [Benchmark]
            public int DynamicMethod_Emit_Hack()
            {
                var f = EmitHacksTest.Get_DynamicMethod_Emit_Hack();
                return f(41);
            }

            // [Benchmark]
            public int MethodInfo_Invoke() =>
                (int)EmitHacksTest.MethodStaticNoArgs.Invoke(null, null);
        }

    }
}
#endif