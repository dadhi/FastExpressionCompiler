using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    public class AutoMapper_Map_Dictionary_of_Obj_with_One_prop
    {
        [MemoryDiagnoser]
        public class Compile_only
        {
            /*
            ## V4.2

            BenchmarkDotNet v0.13.10, Windows 11 (10.0.22631.3447/23H2/2023Update/SunValley3)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 8.0.104
            [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
            DefaultJob : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2

            | Method                       | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |----------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile                      | 627.90 us | 11.774 us | 29.754 us | 616.91 us | 30.05 |    2.30 | 4.8828 | 3.9063 |  35.16 KB |        3.50 |
            | CompileFast                  |  22.85 us |  0.400 us |  0.520 us |  22.93 us |  1.09 |    0.03 | 1.7090 | 1.5869 |  10.66 KB |        1.06 |
            | CompileFast_NoInvokeInlining |  23.28 us |  0.409 us |  0.437 us |  23.16 us |  1.11 |    0.04 | 1.4648 | 1.3428 |   9.64 KB |        0.96 |
            | CompileFast_LightExpression  |  20.90 us |  0.413 us |  0.631 us |  20.78 us |  1.00 |    0.00 | 1.5869 | 1.4648 |  10.05 KB |        1.00 |
            
            */
            
            private static readonly Expression<Func<Source, Destination, ResolutionContext, Destination>> _expression = CreateExpression();
            private static readonly LightExpression.Expression<Func<Source, Destination, ResolutionContext, Destination>> _lightExpression = CreateLightExpression();

            [Benchmark]
            public object Compile() => _expression.Compile();

            [Benchmark]
            public object CompileFast() => _expression.CompileFast();

            [Benchmark]
            public object CompileFast_NoInvokeInlining() => _expression.CompileFast(flags: CompilerFlags.NoInvocationLambdaInlining);

            [Benchmark(Baseline = true)]
            public object CompileFast_LightExpression() =>
                LightExpression.ExpressionCompiler.CompileFast(_lightExpression);
        }

        /*

        */
        [MemoryDiagnoser]
        public class Create_and_Compile
        {
            [Benchmark]
            public object Create_n_Compile() => CreateExpression().Compile();

            [Benchmark]
            public object Create_n_CompileFast() => CreateExpression().CompileFast();

            [Benchmark(Baseline = true)]
            public object Create_n_CompileFast_LightExpression() => LightExpression.ExpressionCompiler.CompileFast(CreateLightExpression());
        }


        public class Invoke_compiled_delegate
        {
            /*
            | Method                               | Mean     | Error    | StdDev    | Median   | Ratio | RatioSD |
            |------------------------------------- |---------:|---------:|----------:|---------:|------:|--------:|
            | Invoke_Compiled                      | 90.22 ns | 3.131 ns |  9.134 ns | 86.50 ns |  1.00 |    0.00 |
            | Invoke_CompiledFast                  | 86.14 ns | 1.954 ns |  5.510 ns | 84.94 ns |  0.97 |    0.11 |
            | Invoke_CompiledFast_NoInvokeInlining | 93.63 ns | 3.405 ns |  9.931 ns | 90.49 ns |  1.05 |    0.14 |
            | Invoke_CompiledFast_LightExpression  | 93.56 ns | 3.857 ns | 11.312 ns | 87.86 ns |  1.05 |    0.15 |
            */
            private static readonly Func<Source, Destination, ResolutionContext, Destination> _compiled = CreateExpression().Compile();
            private static readonly Func<Source, Destination, ResolutionContext, Destination> _compiledFast = CreateExpression().CompileFast(true);
            private static readonly Func<Source, Destination, ResolutionContext, Destination> _compiledFastNoInvokeInlining = CreateExpression().CompileFast(true, CompilerFlags.NoInvocationLambdaInlining);
            private static readonly Func<Source, Destination, ResolutionContext, Destination> _compiledFastLE = LightExpression.ExpressionCompiler.CompileFast(CreateLightExpression(), true);

            private static readonly Source _source = new Source
            {
                Values = new Dictionary<string, SourceValue>
                {
                    {"Key1", new SourceValue {Value = 5}},
                    {"Key2", new SourceValue {Value = 10}},
                }
            };

            [Benchmark(Baseline = true)]
            public Destination Invoke_Compiled() => _compiled(_source, null, new ResolutionContext());

            [Benchmark]
            public Destination Invoke_CompiledFast() => _compiledFast(_source, null, new ResolutionContext());

            [Benchmark]
            public Destination Invoke_CompiledFast_NoInvokeInlining() => _compiledFastNoInvokeInlining(_source, null, new ResolutionContext());

            [Benchmark]
            public Destination Invoke_CompiledFast_LightExpression() => _compiledFastLE(_source, null, new ResolutionContext());
        }

        public class Source
        {
            public Dictionary<string, SourceValue> Values { get; set; }
        }

        public class SourceValue
        {
            public int Value { get; set; }
        }

        public class Destination
        {
            public Dictionary<string, DestinationValue> Values { get; set; }
        }

        public class DestinationValue
        {
            public int Value { get; set; }
        }

        #region AutoMapper stuff
        class ResolutionContext
        {
            internal static void CheckContext(ref ResolutionContext resolutionContext) { }
            internal object GetDestination(object source, Type destinationType) => source;
            internal void CacheDestination(object source, Type destinationType, object destination) { }
            internal TDestination MapInternal<TSource, TDestination>(TSource source, TDestination destination, MemberMap memberMap) => destination;
        }

        class TypeMapPlanBuilder
        {
            private static AutoMapperMappingException MemberMappingError(Exception innerException, MemberMap memberMap) =>
                new AutoMapperMappingException("Error mapping types.", innerException, memberMap);
        }

        public class MemberMap { }
        public class PropertyMap : MemberMap { }

        public readonly struct MapRequest { }

        public class MapperConfiguration
        {
            public static AutoMapperMappingException GetMappingError(Exception innerException, in MapRequest mapRequest) =>
                new AutoMapperMappingException("Error mapping types.", innerException, null);
        }
        public class AutoMapperMappingException : Exception
        {
            public AutoMapperMappingException(string message, Exception innerException, MemberMap memberMap) { }
        }

        #endregion

        private static Expression<Func<Source, Destination, ResolutionContext, Destination>> CreateExpression()
        {
            var propertyMap = new PropertyMap();

            var p = new ParameterExpression[15]; // the parameter expressions
            var e = new Expression[70]; // the unique expressions
            var l = new LabelTarget[1]; // the labels
            var expr = Lambda<Func<Source, Destination, ResolutionContext, Destination>>(
              e[0] = Condition(
                e[1] = MakeBinary(ExpressionType.Equal,
                  p[0] = Parameter(typeof(Source), "source"),
                  e[2] = Default(typeof(object))),
                e[3] = Condition(
                  e[4] = MakeBinary(ExpressionType.Equal,
                    p[1] = Parameter(typeof(Destination), "destination"),
                    e[2 // Default of object
                      ]),
                  e[5] = Default(typeof(Destination)),
                  p[1 // (Destination destination)
                    ],
                  typeof(Destination)),
                e[6] = Block(
                  typeof(Destination),
                  new[] {
                    p[2]=Parameter(typeof(Destination), "typeMapDestination")
                  },
                  e[7] = Block(
                    typeof(Destination),
                    new ParameterExpression[0],
                    e[8] = MakeBinary(ExpressionType.Assign,
                      p[2 // (Destination typeMapDestination)
                        ],
                      e[9] = Coalesce(
                        p[1 // (Destination destination)
                          ],
                        e[10] = New( // 0 args
                          typeof(Destination).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0]))),
                    e[11] = TryCatch(
                      e[12] = Block(
                        typeof(Dictionary<string, DestinationValue>),
                        new[] {
                            p[3]=Parameter(typeof(Dictionary<string, SourceValue>), "resolvedValue"),
                            p[4]=Parameter(typeof(Dictionary<string, DestinationValue>), "mappedValue")
                        },
                        e[13] = MakeBinary(ExpressionType.Assign,
                          p[3 // (System.Collections.Generic.Dictionary<string, SourceValue> resolvedValue)
                            ],
                          e[14] = Property(
                            p[0 // (Source source)
                              ],
                            typeof(Source).GetTypeInfo().GetDeclaredProperty("Values"))),
                        e[15] = MakeBinary(ExpressionType.Assign,
                          p[4 // (System.Collections.Generic.Dictionary<string, DestinationValue> mappedValue)
                            ],
                          e[16] = Condition(
                            e[17] = MakeBinary(ExpressionType.Equal,
                              p[3 // (System.Collections.Generic.Dictionary<string, SourceValue> resolvedValue)
                                ],
                              e[2 // Default of object
                                ]),
                            e[18] = New( // 0 args
                              typeof(Dictionary<string, DestinationValue>).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0]),
                            e[19] = Block(
                              typeof(Dictionary<string, DestinationValue>),
                              new[] {
                    p[5]=Parameter(typeof(Dictionary<string, DestinationValue>), "collectionDestination"),
                    p[6]=Parameter(typeof(Dictionary<string, DestinationValue>), "passedDestination")
                              },
                              e[20] = MakeBinary(ExpressionType.Assign,
                                p[6 // (System.Collections.Generic.Dictionary<string, DestinationValue> passedDestination)
                                  ],
                                e[21] = Condition(
                                  e[22] = MakeBinary(ExpressionType.Equal,
                                    p[1 // (Destination destination)
                                      ],
                                    e[2 // Default of object
                                      ]),
                                  e[23] = Default(typeof(Dictionary<string, DestinationValue>)),
                                  e[24] = Property(
                                    p[2 // (Destination typeMapDestination)
                                      ],
                                    typeof(Destination).GetTypeInfo().GetDeclaredProperty("Values")),
                                  typeof(Dictionary<string, DestinationValue>))),
                              e[25] = MakeBinary(ExpressionType.Assign,
                                p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                  ],
                                e[26] = Coalesce(
                                  p[6 // (System.Collections.Generic.Dictionary<string, DestinationValue> passedDestination)
                                    ],
                                  e[27] = New( // 0 args
                                    typeof(Dictionary<string, DestinationValue>).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0]))),
                              e[28] = Call(
                                p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                  ],
                                typeof(ICollection<KeyValuePair<string, DestinationValue>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Clear" && x.GetParameters().Length == 0)),
                              e[29] = Block(
                                typeof(void),
                                new[] {
                      p[7]=Parameter(typeof(Dictionary<string, SourceValue>.Enumerator), "enumerator"),
                      p[8]=Parameter(typeof(KeyValuePair<string, SourceValue>), "item")
                                },
                                e[30] = MakeBinary(ExpressionType.Assign,
                                  p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                    ],
                                  e[31] = Call(
                                    p[3 // (System.Collections.Generic.Dictionary<string, SourceValue> resolvedValue)
                                      ],
                                    typeof(Dictionary<string, SourceValue>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetEnumerator" && x.GetParameters().Length == 0))),
                                e[32] = TryCatchFinally(
                                  e[33] = Loop(
                                    e[34] = Condition(
                                      e[35] = Call(
                                        p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                          ],
                                        typeof(Dictionary<string, SourceValue>.Enumerator).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "MoveNext" && x.GetParameters().Length == 0)),
                                      e[36] = Block(
                                        typeof(void),
                                        new ParameterExpression[0],
                                        e[37] = MakeBinary(ExpressionType.Assign,
                                          p[8 // ([struct] System.Collections.Generic.KeyValuePair<string, SourceValue> item)
                                            ],
                                          e[38] = Property(
                                            p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                              ],
                                            typeof(Dictionary<string, SourceValue>.Enumerator).GetTypeInfo().GetDeclaredProperty("Current"))),
                                        e[39] = Call(
                                          p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                            ],
                                          typeof(ICollection<KeyValuePair<string, DestinationValue>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(KeyValuePair<string, DestinationValue>) })),
                                          e[40] = New( // 2 args
                                            typeof(KeyValuePair<string, DestinationValue>).GetTypeInfo().DeclaredConstructors.AsArray()[0],
                                            e[41] = Property(
                                              p[8 // ([struct] System.Collections.Generic.KeyValuePair<string, SourceValue> item)
                                                ],
                                              typeof(KeyValuePair<string, SourceValue>).GetTypeInfo().GetDeclaredProperty("Key")),
                                            e[42] = Invoke(
                                              e[43] = Lambda<Func<SourceValue, DestinationValue, ResolutionContext, DestinationValue>>(
                                                e[44] = Condition(
                                                  e[45] = MakeBinary(ExpressionType.Equal,
                                                    p[9] = Parameter(typeof(SourceValue), "source"),
                                                    e[2 // Default of object
                                                      ]),
                                                  e[46] = Condition(
                                                    e[47] = MakeBinary(ExpressionType.Equal,
                                                      p[10] = Parameter(typeof(DestinationValue), "destination"),
                                                      e[2 // Default of object
                                                        ]),
                                                    e[48] = Default(typeof(DestinationValue)),
                                                    p[10 // (DestinationValue destination)
                                                      ],
                                                    typeof(DestinationValue)),
                                                  e[49] = Block(
                                                    typeof(DestinationValue),
                                                    new[] {
                                                      p[11]=Parameter(typeof(DestinationValue), "typeMapDestination")
                                                    },
                                                    e[50] = Block(
                                                      typeof(DestinationValue),
                                                      new ParameterExpression[0],
                                                      e[51] = MakeBinary(ExpressionType.Assign,
                                                        p[11 // (DestinationValue typeMapDestination)
                                                          ],
                                                        e[52] = Coalesce(
                                                          p[10 // (DestinationValue destination)
                                                            ],
                                                          e[53] = New( // 0 args
                                                            typeof(DestinationValue).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0]))),
                                                      e[54] = TryCatch(
                                                        e[55] = Block(
                                                          typeof(int),
                                                          new[] {
                                                            p[12]=Parameter(typeof(int), "resolvedValue")
                                                          },
                                                          e[56] = MakeBinary(ExpressionType.Assign,
                                                            p[12 // (int resolvedValue)
                                                              ],
                                                            e[57] = Property(
                                                              p[9 // (SourceValue source)
                                                                ],
                                                              typeof(SourceValue).GetTypeInfo().GetDeclaredProperty("Value"))),
                                                          e[58] = MakeBinary(ExpressionType.Assign,
                                                            e[59] = Property(
                                                              p[11 // (DestinationValue typeMapDestination)
                                                                ],
                                                              typeof(DestinationValue).GetTypeInfo().GetDeclaredProperty("Value")),
                                                            p[12 // (int resolvedValue)
                                                              ])),
                                                        MakeCatchBlock(
                                                          typeof(Exception),
                                                          p[13] = Parameter(typeof(Exception), "ex"),
                                                          e[60] = Throw(
                                                            e[61] = Call(
                                                              null,
                                                              typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MemberMap) })),
                                                              p[13 // (System.Exception ex)
                                                                ],
                                                              e[62] = Constant(propertyMap)),
                                                            typeof(int)),
                                                          null)),
                                                      p[11 // (DestinationValue typeMapDestination)
                                                        ])),
                                                  typeof(DestinationValue)),
                                                p[9 // (SourceValue source)
                                                  ],
                                                p[10 // (DestinationValue destination)
                                                  ],
                                                p[14] = Parameter(typeof(ResolutionContext), "context")),
                                              e[63] = Property(
                                                p[8 // ([struct] System.Collections.Generic.KeyValuePair<string, SourceValue> item)
                                                  ],
                                                typeof(KeyValuePair<string, SourceValue>).GetTypeInfo().GetDeclaredProperty("Value")),
                                              e[48 // Default of DestinationValue
                                                ],
                                              p[14 // (ResolutionContext context)
                                                ])))),
                                      e[64] = MakeGoto(GotoExpressionKind.Break,
                                        l[0] = Label(typeof(void), "LoopBreak"),
                                        null,
                                        typeof(void)),
                                      typeof(void)),
                                    l[0 // (LoopBreak)
                                    ]),
                                  e[65] = Call(
                                    p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                      ],
                                    typeof(IDisposable).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Dispose" && x.GetParameters().Length == 0)), new CatchBlock[0])),
                              p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                ]),
                            typeof(Dictionary<string, DestinationValue>))),
                        e[66] = MakeBinary(ExpressionType.Assign,
                          e[24 // MemberAccess of System.Collections.Generic.Dictionary<string, DestinationValue>
                            ],
                          p[4 // (System.Collections.Generic.Dictionary<string, DestinationValue> mappedValue)
                            ])),
                      MakeCatchBlock(
                        typeof(Exception),
                        p[13 // (System.Exception ex)
                          ],
                        e[67] = Throw(
                          e[68] = Call(
                            null,
                            typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MemberMap) })),
                            p[13 // (System.Exception ex)
                              ],
                            e[69] = Constant(propertyMap)),
                          typeof(Dictionary<string, DestinationValue>)),
                        null)),
                    p[2 // (Destination typeMapDestination)
                      ])),
                typeof(Destination)),
              p[0 // (Source source)
                ],
              p[1 // (Destination destination)
                ],
              p[14 // (ResolutionContext context)
                ]);
            return expr;
        }

        private static LightExpression.Expression<Func<Source, Destination, ResolutionContext, Destination>> CreateLightExpression()
        {
            var propertyMap = new PropertyMap();

            var p = new LightExpression.ParameterExpression[15]; // the parameter expressions
            var e = new LightExpression.Expression[70]; // the unique expressions
            var l = new LightExpression.LabelTarget[1]; // the labels
            var expr = L.Lambda<Func<Source, Destination, ResolutionContext, Destination>>(
              e[0] = L.Condition(
                e[1] = L.MakeBinary(ExpressionType.Equal,
                  p[0] = L.Parameter(typeof(Source), "source"),
                  e[2] = L.Default(typeof(object))),
                e[3] = L.Condition(
                  e[4] = L.MakeBinary(ExpressionType.Equal,
                    p[1] = L.Parameter(typeof(Destination), "destination"),
                    e[2 // Default of object
                      ]),
                  e[5] = L.Default(typeof(Destination)),
                  p[1 // (Destination destination)
                    ],
                  typeof(Destination)),
                e[6] = L.Block(
                  typeof(Destination),
                  new[] {
                    p[2]=L.Parameter(typeof(Destination), "typeMapDestination")
                  },
                  e[7] = L.Block(
                    typeof(Destination),
                    new LightExpression.ParameterExpression[0],
                    e[8] = L.MakeBinary(ExpressionType.Assign,
                      p[2 // (Destination typeMapDestination)
                        ],
                      e[9] = L.Coalesce(
                        p[1 // (Destination destination)
                          ],
                        e[10] = L.New( // 0 args
                          typeof(Destination).GetTypeInfo().DeclaredConstructors.AsArray()[0], new LightExpression.Expression[0]))),
                    e[11] = L.TryCatch(
                      e[12] = L.Block(
                        typeof(Dictionary<string, DestinationValue>),
                        new[] {
                            p[3]=L.Parameter(typeof(Dictionary<string, SourceValue>), "resolvedValue"),
                            p[4]=L.Parameter(typeof(Dictionary<string, DestinationValue>), "mappedValue")
                        },
                        e[13] = L.MakeBinary(ExpressionType.Assign,
                          p[3 // (System.Collections.Generic.Dictionary<string, SourceValue> resolvedValue)
                            ],
                          e[14] = L.Property(
                            p[0 // (Source source)
                              ],
                            typeof(Source).GetTypeInfo().GetDeclaredProperty("Values"))),
                        e[15] = L.MakeBinary(ExpressionType.Assign,
                          p[4 // (System.Collections.Generic.Dictionary<string, DestinationValue> mappedValue)
                            ],
                          e[16] = L.Condition(
                            e[17] = L.MakeBinary(ExpressionType.Equal,
                              p[3 // (System.Collections.Generic.Dictionary<string, SourceValue> resolvedValue)
                                ],
                              e[2 // Default of object
                                ]),
                            e[18] = L.New( // 0 args
                              typeof(Dictionary<string, DestinationValue>).GetTypeInfo().DeclaredConstructors.AsArray()[0], new LightExpression.Expression[0]),
                            e[19] = L.Block(
                              typeof(Dictionary<string, DestinationValue>),
                              new[] {
                    p[5]=L.Parameter(typeof(Dictionary<string, DestinationValue>), "collectionDestination"),
                    p[6]=L.Parameter(typeof(Dictionary<string, DestinationValue>), "passedDestination")
                              },
                              e[20] = L.MakeBinary(ExpressionType.Assign,
                                p[6 // (System.Collections.Generic.Dictionary<string, DestinationValue> passedDestination)
                                  ],
                                e[21] = L.Condition(
                                  e[22] = L.MakeBinary(ExpressionType.Equal,
                                    p[1 // (Destination destination)
                                      ],
                                    e[2 // Default of object
                                      ]),
                                  e[23] = L.Default(typeof(Dictionary<string, DestinationValue>)),
                                  e[24] = L.Property(
                                    p[2 // (Destination typeMapDestination)
                                      ],
                                    typeof(Destination).GetTypeInfo().GetDeclaredProperty("Values")),
                                  typeof(Dictionary<string, DestinationValue>))),
                              e[25] = L.MakeBinary(ExpressionType.Assign,
                                p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                  ],
                                e[26] = L.Coalesce(
                                  p[6 // (System.Collections.Generic.Dictionary<string, DestinationValue> passedDestination)
                                    ],
                                  e[27] = L.New( // 0 args
                                    typeof(Dictionary<string, DestinationValue>).GetTypeInfo().DeclaredConstructors.AsArray()[0], new LightExpression.Expression[0]))),
                              e[28] = L.Call(
                                p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                  ],
                                typeof(ICollection<KeyValuePair<string, DestinationValue>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Clear" && x.GetParameters().Length == 0)),
                              e[29] = L.Block(
                                typeof(void),
                                new[] {
                      p[7]=L.Parameter(typeof(Dictionary<string, SourceValue>.Enumerator), "enumerator"),
                      p[8]=L.Parameter(typeof(KeyValuePair<string, SourceValue>), "item")
                                },
                                e[30] = L.MakeBinary(ExpressionType.Assign,
                                  p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                    ],
                                  e[31] = L.Call(
                                    p[3 // (System.Collections.Generic.Dictionary<string, SourceValue> resolvedValue)
                                      ],
                                    typeof(Dictionary<string, SourceValue>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetEnumerator" && x.GetParameters().Length == 0))),
                                e[32] = L.TryCatchFinally(
                                  e[33] = L.Loop(
                                    e[34] = L.Condition(
                                      e[35] = L.Call(
                                        p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                          ],
                                        typeof(Dictionary<string, SourceValue>.Enumerator).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "MoveNext" && x.GetParameters().Length == 0)),
                                      e[36] = L.Block(
                                        typeof(void),
                                        new LightExpression.ParameterExpression[0],
                                        e[37] = L.MakeBinary(ExpressionType.Assign,
                                          p[8 // ([struct] System.Collections.Generic.KeyValuePair<string, SourceValue> item)
                                            ],
                                          e[38] = L.Property(
                                            p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                              ],
                                            typeof(Dictionary<string, SourceValue>.Enumerator).GetTypeInfo().GetDeclaredProperty("Current"))),
                                        e[39] = L.Call(
                                          p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                            ],
                                          typeof(ICollection<KeyValuePair<string, DestinationValue>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(KeyValuePair<string, DestinationValue>) })),
                                          e[40] = L.New( // 2 args
                                            typeof(KeyValuePair<string, DestinationValue>).GetTypeInfo().DeclaredConstructors.AsArray()[0],
                                            e[41] = L.Property(
                                              p[8 // ([struct] System.Collections.Generic.KeyValuePair<string, SourceValue> item)
                                                ],
                                              typeof(KeyValuePair<string, SourceValue>).GetTypeInfo().GetDeclaredProperty("Key")),
                                            e[42] = L.Invoke(
                                              e[43] = L.Lambda<Func<SourceValue, DestinationValue, ResolutionContext, DestinationValue>>(
                                                e[44] = L.Condition(
                                                  e[45] = L.MakeBinary(ExpressionType.Equal,
                                                    p[9] = L.Parameter(typeof(SourceValue), "source"),
                                                    e[2 // Default of object
                                                      ]),
                                                  e[46] = L.Condition(
                                                    e[47] = L.MakeBinary(ExpressionType.Equal,
                                                      p[10] = L.Parameter(typeof(DestinationValue), "destination"),
                                                      e[2 // Default of object
                                                        ]),
                                                    e[48] = L.Default(typeof(DestinationValue)),
                                                    p[10 // (DestinationValue destination)
                                                      ],
                                                    typeof(DestinationValue)),
                                                  e[49] = L.Block(
                                                    typeof(DestinationValue),
                                                    new[] {
                                                      p[11]=L.Parameter(typeof(DestinationValue), "typeMapDestination")
                                                    },
                                                    e[50] = L.Block(
                                                      typeof(DestinationValue),
                                                      new LightExpression.ParameterExpression[0],
                                                      e[51] = L.MakeBinary(ExpressionType.Assign,
                                                        p[11 // (DestinationValue typeMapDestination)
                                                          ],
                                                        e[52] = L.Coalesce(
                                                          p[10 // (DestinationValue destination)
                                                            ],
                                                          e[53] = L.New( // 0 args
                                                            typeof(DestinationValue).GetTypeInfo().DeclaredConstructors.AsArray()[0], new LightExpression.Expression[0]))),
                                                      e[54] = L.TryCatch(
                                                        e[55] = L.Block(
                                                          typeof(int),
                                                          new[] {
                                                            p[12]=L.Parameter(typeof(int), "resolvedValue")
                                                          },
                                                          e[56] = L.MakeBinary(ExpressionType.Assign,
                                                            p[12 // (int resolvedValue)
                                                              ],
                                                            e[57] = L.Property(
                                                              p[9 // (SourceValue source)
                                                                ],
                                                              typeof(SourceValue).GetTypeInfo().GetDeclaredProperty("Value"))),
                                                          e[58] = L.MakeBinary(ExpressionType.Assign,
                                                            e[59] = L.Property(
                                                              p[11 // (DestinationValue typeMapDestination)
                                                                ],
                                                              typeof(DestinationValue).GetTypeInfo().GetDeclaredProperty("Value")),
                                                            p[12 // (int resolvedValue)
                                                              ])),
                                                        L.MakeCatchBlock(
                                                          typeof(Exception),
                                                          p[13] = L.Parameter(typeof(Exception), "ex"),
                                                          e[60] = L.Throw(
                                                            e[61] = L.Call(
                                                              null,
                                                              typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MemberMap) })),
                                                              p[13 // (System.Exception ex)
                                                                ],
                                                              e[62] = L.Constant(propertyMap)),
                                                            typeof(int)),
                                                          null)),
                                                      p[11 // (DestinationValue typeMapDestination)
                                                        ])),
                                                  typeof(DestinationValue)),
                                                p[9 // (SourceValue source)
                                                  ],
                                                p[10 // (DestinationValue destination)
                                                  ],
                                                p[14] = L.Parameter(typeof(ResolutionContext), "context")),
                                              e[63] = L.Property(
                                                p[8 // ([struct] System.Collections.Generic.KeyValuePair<string, SourceValue> item)
                                                  ],
                                                typeof(KeyValuePair<string, SourceValue>).GetTypeInfo().GetDeclaredProperty("Value")),
                                              e[48 // Default of DestinationValue
                                                ],
                                              p[14 // (ResolutionContext context)
                                                ])))),
                                      e[64] = L.MakeGoto(GotoExpressionKind.Break,
                                        l[0] = L.Label(typeof(void), "LoopBreak"),
                                        null,
                                        typeof(void)),
                                      typeof(void)),
                                    l[0 // (LoopBreak)
                                    ]),
                                  e[65] = L.Call(
                                    p[7 // ([struct] System.Collections.Generic.Dictionary<string, SourceValue>.Enumerator enumerator)
                                      ],
                                    typeof(IDisposable).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Dispose" && x.GetParameters().Length == 0)), new LightExpression.CatchBlock[0])),
                              p[5 // (System.Collections.Generic.Dictionary<string, DestinationValue> collectionDestination)
                                ]),
                            typeof(Dictionary<string, DestinationValue>))),
                        e[66] = L.MakeBinary(ExpressionType.Assign,
                          e[24 // MemberAccess of System.Collections.Generic.Dictionary<string, DestinationValue>
                            ],
                          p[4 // (System.Collections.Generic.Dictionary<string, DestinationValue> mappedValue)
                            ])),
                      L.MakeCatchBlock(
                        typeof(Exception),
                        p[13 // (System.Exception ex)
                          ],
                        e[67] = L.Throw(
                          e[68] = L.Call(
                            null,
                            typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MemberMap) })),
                            p[13 // (System.Exception ex)
                              ],
                            e[69] = L.Constant(propertyMap)),
                          typeof(Dictionary<string, DestinationValue>)),
                        null)),
                    p[2 // (Destination typeMapDestination)
                      ])),
                typeof(Destination)),
              p[0 // (Source source)
                ],
              p[1 // (Destination destination)
                ],
              p[14 // (ResolutionContext context)
                ]);
            return expr;
        }
    }
}
