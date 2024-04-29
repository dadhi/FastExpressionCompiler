using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

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
    public class Issue408_Dictionary_mapping_failing_when_the_InvocationExpression_inlining_is_involved : ITest
    {
        public int Run()
        {
            AutoMapper_UnitTests_When_mapping_to_a_generic_dictionary_with_mapped_value_pairs();
            return 1;
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

        [Test]
        public void AutoMapper_UnitTests_When_mapping_to_a_generic_dictionary_with_mapped_value_pairs()
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

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            ff.PrintIL();

            var source = new Source
            {
                Values = new Dictionary<string, SourceValue>
                {
                    {"Key1", new SourceValue {Value = 5}},
                    {"Key2", new SourceValue {Value = 10}},
                }
            };

            var destination = fs(source, null, new ResolutionContext());
            Assert.AreEqual(2,  destination.Values.Count);
            Assert.AreEqual(5,  destination.Values["Key1"].Value);
            Assert.AreEqual(10, destination.Values["Key2"].Value);

            destination = ff(source, null, new ResolutionContext());
            Assert.AreEqual(2,  destination.Values.Count);
            Assert.AreEqual(5,  destination.Values["Key1"].Value);
            Assert.AreEqual(10, destination.Values["Key2"].Value);

            destination = ff(source, destination, new ResolutionContext());
            Assert.AreEqual(2,  destination.Values.Count);
            Assert.AreEqual(5,  destination.Values["Key1"].Value);
            Assert.AreEqual(10, destination.Values["Key2"].Value);
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
    }
}