using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue222_Make_AutoMapper_FEC_branch_to_pass_all_the_tests : ITest
    {
        public int Run()
        {
            Should_map_the_existing_array_elements_over();
            return 1;
        }

        class ResolutionContext 
        {
            internal static void CheckContext(ref ResolutionContext resolutionContext) {}
            internal object GetDestination(object source, Type destinationType) => source;
            internal void CacheDestination(object source, Type destinationType, object destination) {}
            internal TDestination MapInternal<TSource, TDestination>(TSource source, TDestination destination, MemberMap memberMap) => destination;
        }

        class TypeMapPlanBuilder 
        {
            private static AutoMapperMappingException MemberMappingError(Exception innerException, MemberMap memberMap) => 
                new AutoMapperMappingException("Error mapping types.", innerException, memberMap);
        }

        public class MemberMap {}
        public class PropertyMap : MemberMap {}

        public readonly struct MapRequest {}

        public class MapperConfiguration 
        {
            public static AutoMapperMappingException GetMappingError(Exception innerException, in MapRequest mapRequest) =>
                new AutoMapperMappingException("Error mapping types.", innerException, null);
        }
        public class AutoMapperMappingException : Exception
        {
            public AutoMapperMappingException(string message, Exception innerException, MemberMap memberMap) {}
        }

        // [Test]
        public void Should_map_the_existing_array_elements_over()
        {
            var p = new ParameterExpression[17]; // the parameter expressions 
            var e = new Expression[116]; // the unique expressions 
            var l = new LabelTarget[2]; // the labels 
            var expr = Lambda<Func<List<SourceObject>, List<DestObject>, ResolutionContext, List<DestObject>>>( //$
            e[0]=Condition(
                e[1]=MakeBinary(ExpressionType.Equal,
                p[0]=Parameter(typeof(List<SourceObject>), "source"),
                e[2]=Constant(null)),
                e[3]=Condition(
                e[4]=MakeBinary(ExpressionType.Equal,
                    p[1]=Parameter(typeof(List<DestObject>), "mapperDestination"),
                    e[2 // Constant of object
                    ]),
                e[5]=New( // 0 args
                    typeof(List<DestObject>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                e[6]=Block(
                    typeof(List<DestObject>),
                    new[] {
                    p[2]=Parameter(typeof(List<DestObject>), "collectionDestination")
                    },
                    e[7]=MakeBinary(ExpressionType.Assign,
                    p[2 // (List<DestObject> collectionDestination)
                        ],
                    p[1 // (List<DestObject> mapperDestination)
                        ]), 
                    e[8]=Condition(
                    e[9]=MakeBinary(ExpressionType.Equal,
                        p[2 // (List<DestObject> collectionDestination)
                        ],
                        e[2 // Constant of object
                        ]),
                    e[10]=Empty(),
                    e[11]=Call(
                        p[2 // (List<DestObject> collectionDestination)
                        ], 
                        typeof(IList).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Clear" && x.GetParameters().Length == 0)),
                    typeof(void)), 
                    p[2 // (List<DestObject> collectionDestination)
                    ]),
                typeof(List<DestObject>)),
                e[12]=TryCatch(
                e[13]=Block(
                    typeof(List<DestObject>),
                    new ParameterExpression[0], 
                    e[14]=Call(
                    null, 
                    typeof(ResolutionContext).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "CheckContext" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(ResolutionContext).MakeByRefType() })),
                    p[3]=Parameter(typeof(ResolutionContext), "context")), 
                    e[15]=Block(
                    typeof(List<DestObject>),
                    new[] {
                    p[4]=Parameter(typeof(List<DestObject>), "collectionDestination"),
                    p[5]=Parameter(typeof(List<DestObject>), "passedDestination")
                    },
                    e[16]=MakeBinary(ExpressionType.Assign,
                        p[5 // (List<DestObject> passedDestination)
                        ],
                        p[1 // (List<DestObject> mapperDestination)
                        ]), 
                    e[17]=MakeBinary(ExpressionType.Assign,
                        p[4 // (List<DestObject> collectionDestination)
                        ],
                        e[18]=Coalesce(
                        p[5 // (List<DestObject> passedDestination)
                            ],
                        e[19]=New( // 0 args
                            typeof(List<DestObject>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))), 
                    e[20]=Call(
                        p[4 // (List<DestObject> collectionDestination)
                        ], 
                        typeof(IList).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Clear" && x.GetParameters().Length == 0)), 
                    e[21]=Block(
                        typeof(void),
                        new[] {
                        p[6]=Parameter(typeof(List<SourceObject>.Enumerator), "enumerator"),
                        p[7]=Parameter(typeof(SourceObject), "item")
                        },
                        e[22]=MakeBinary(ExpressionType.Assign,
                        p[6 // ([struct] List<SourceObject>.Enumerator enumerator)
                            ],
                        e[23]=Call(
                            p[0 // (List<SourceObject> source)
                            ], 
                            typeof(List<SourceObject>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetEnumerator" && x.GetParameters().Length == 0))), 
                        e[24]=TryCatchFinally(
                        e[25]=Loop(
                            e[26]=Condition(
                            e[27]=Call(
                                p[6 // ([struct] List<SourceObject>.Enumerator enumerator)
                                ], 
                                typeof(List<SourceObject>.Enumerator).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "MoveNext" && x.GetParameters().Length == 0)),
                            e[28]=Block(
                                typeof(void),
                                new ParameterExpression[0], 
                                e[29]=MakeBinary(ExpressionType.Assign,
                                p[7 // (SourceObject item)
                                    ],
                                e[30]=Property(
                                    p[6 // ([struct] List<SourceObject>.Enumerator enumerator)
                                    ],
                                    typeof(List<SourceObject>.Enumerator).GetTypeInfo().GetDeclaredProperty("Current"))), 
                                e[31]=Call(
                                p[4 // (List<DestObject> collectionDestination)
                                    ], 
                                typeof(ICollection<DestObject>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(DestObject) })),
                                e[32]=Condition(
                                    e[33]=MakeBinary(ExpressionType.Equal,
                                    p[7 // (SourceObject item)
                                        ],
                                    e[2 // Constant of object
                                        ]),
                                    e[34]=Condition(
                                    e[35]=MakeBinary(ExpressionType.Equal,
                                        e[36]=Default(typeof(DestObject)),
                                        e[2 // Constant of object
                                        ]),
                                    e[36 // Default of DestObject
                                        ],
                                    e[36 // Default of DestObject
                                        ],
                                    typeof(DestObject)),
                                    e[37]=Block(
                                    typeof(DestObject),
                                    new[] {
                                    p[8]=Parameter(typeof(DestObject), "typeMapDestination")
                                    },
                                    e[38]=Call(
                                        null, 
                                        typeof(ResolutionContext).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "CheckContext" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(ResolutionContext).MakeByRefType() })),
                                        p[3 // (ResolutionContext context)
                                        ]), 
                                    e[39]=Coalesce(
                                        e[40]=Convert(
                                        e[41]=Call(
                                            p[3 // (ResolutionContext context)
                                            ], 
                                            typeof(ResolutionContext).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Single(x => !x.IsGenericMethod && x.Name == "GetDestination" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object), typeof(Type) })),
                                            p[7 // (SourceObject item)
                                            ], 
                                            e[42]=Constant(typeof(DestObject))),
                                        typeof(DestObject)),
                                        e[43]=Condition(
                                        e[44]=MakeBinary(ExpressionType.Equal,
                                            p[7 // (SourceObject item)
                                            ],
                                            e[2 // Constant of object
                                            ]),
                                        e[45]=Default(typeof(DestObject)),
                                        e[46]=Block(
                                            typeof(DestObject),
                                            new ParameterExpression[0], 
                                            e[47]=Block(
                                            typeof(DestObject),
                                            new ParameterExpression[0], 
                                            e[48]=MakeBinary(ExpressionType.Assign,
                                                p[8 // (DestObject typeMapDestination)
                                                ],
                                                e[49]=Coalesce(
                                                e[36 // Default of DestObject
                                                    ],
                                                e[50]=New( // 0 args
                                                    typeof(DestObject).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))), 
                                            e[51]=Call(
                                                p[3 // (ResolutionContext context)
                                                ], 
                                                typeof(ResolutionContext).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Single(x => !x.IsGenericMethod && x.Name == "CacheDestination" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object), typeof(Type), typeof(object) })),
                                                p[7 // (SourceObject item)
                                                ], 
                                                e[52]=Constant(typeof(DestObject)), 
                                                p[8 // (DestObject typeMapDestination)
                                                ]), 
                                            p[8 // (DestObject typeMapDestination)
                                                ]), 
                                            e[53]=TryCatch(
                                            e[54]=Block(
                                                typeof(int),
                                                new[] {
                                                p[9]=Parameter(typeof(int), "resolvedValue")
                                                },
                                                e[55]=MakeBinary(ExpressionType.Assign,
                                                p[9 // (int resolvedValue)
                                                    ],
                                                e[56]=Condition(
                                                    e[57]=MakeBinary(ExpressionType.Equal,
                                                    p[7 // (SourceObject item)
                                                        ],
                                                    e[2 // Constant of object
                                                        ]),
                                                    e[58]=Field(
                                                    p[8 // (DestObject typeMapDestination)
                                                        ],
                                                    typeof(DestObject).GetTypeInfo().GetDeclaredField("Id")),
                                                    e[59]=Field(
                                                    p[7 // (SourceObject item)
                                                        ],
                                                    typeof(SourceObject).GetTypeInfo().GetDeclaredField("Id")),
                                                    typeof(int))), 
                                                e[60]=MakeBinary(ExpressionType.Assign,
                                                e[58 // MemberAccess of int
                                                    ],
                                                p[9 // (int resolvedValue)
                                                    ])),
                                            MakeCatchBlock(
                                                typeof(Exception),
                                                p[10]=Parameter(typeof(Exception), "ex"),
                                                e[61]=Throw(
                                                e[62]=Call(
                                                    null, 
                                                    typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MemberMap) })),
                                                    p[10 // (Exception ex)
                                                    ], 
                                                    e[63]=Constant(null, typeof(PropertyMap)
                                                    // !!! Please provide the non-default value
                                                    )),
                                                typeof(int)),
                                                null)), 
                                            e[64]=TryCatch(
                                            e[65]=Block(
                                                typeof(IList<DestObject>),
                                                new[] {
                                                p[11]=Parameter(typeof(IList<SourceObject>), "resolvedValue"),
                                                p[12]=Parameter(typeof(IList<DestObject>), "mappedValue")
                                                },
                                                e[66]=MakeBinary(ExpressionType.Assign,
                                                p[11 // (IList<SourceObject> resolvedValue)
                                                    ],
                                                e[67]=Condition(
                                                    e[68]=MakeBinary(ExpressionType.Equal,
                                                    p[7 // (SourceObject item)
                                                        ],
                                                    e[2 // Constant of object
                                                        ]),
                                                    e[69]=Default(typeof(IList<SourceObject>)),
                                                    e[70]=Field(
                                                    p[7 // (SourceObject item)
                                                        ],
                                                    typeof(SourceObject).GetTypeInfo().GetDeclaredField("Children")),
                                                    typeof(IList<SourceObject>))), 
                                                e[71]=MakeBinary(ExpressionType.Assign,
                                                p[12 // (IList<DestObject> mappedValue)
                                                    ],
                                                e[72]=Condition(
                                                    e[73]=MakeBinary(ExpressionType.Equal,
                                                    p[11 // (IList<SourceObject> resolvedValue)
                                                        ],
                                                    e[2 // Constant of object
                                                        ]),
                                                    e[74]=Convert(
                                                    e[75]=New( // 0 args
                                                        typeof(List<DestObject>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                                                    typeof(IList<DestObject>)),
                                                    e[76]=Block(
                                                    typeof(IList<DestObject>),
                                                    new[] {
                                                    p[13]=Parameter(typeof(IList<DestObject>), "collectionDestination"),
                                                    p[14]=Parameter(typeof(IList<DestObject>), "passedDestination")
                                                    },
                                                    e[77]=MakeBinary(ExpressionType.Assign,
                                                        p[14 // (IList<DestObject> passedDestination)
                                                        ],
                                                        e[78]=Condition(
                                                        e[79]=MakeBinary(ExpressionType.Equal,
                                                            e[36 // Default of DestObject
                                                            ],
                                                            e[2 // Constant of object
                                                            ]),
                                                        e[80]=Default(typeof(IList<DestObject>)),
                                                        e[81]=Field(
                                                            p[8 // (DestObject typeMapDestination)
                                                            ],
                                                            typeof(DestObject).GetTypeInfo().GetDeclaredField("Children")),
                                                        typeof(IList<DestObject>))), 
                                                    e[82]=MakeBinary(ExpressionType.Assign,
                                                        p[13 // (IList<DestObject> collectionDestination)
                                                        ],
                                                        e[83]=Coalesce(
                                                        p[14 // (IList<DestObject> passedDestination)
                                                            ],
                                                        e[84]=Convert(
                                                            e[85]=New( // 0 args
                                                            typeof(List<DestObject>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                                                            typeof(IList<DestObject>)))), 
                                                    e[86]=Call(
                                                        p[13 // (IList<DestObject> collectionDestination)
                                                        ], 
                                                        typeof(ICollection<DestObject>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Clear" && x.GetParameters().Length == 0)), 
                                                    e[87]=Block(
                                                        typeof(void),
                                                        new[] {
                                                        p[15]=Parameter(typeof(IEnumerator<SourceObject>), "enumerator"),
                                                        p[16]=Parameter(typeof(SourceObject), "item")
                                                        },
                                                        e[88]=MakeBinary(ExpressionType.Assign,
                                                        p[15 // (IEnumerator<SourceObject> enumerator)
                                                            ],
                                                        e[89]=Call(
                                                            p[11 // (IList<SourceObject> resolvedValue)
                                                            ], 
                                                            typeof(IEnumerable<SourceObject>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetEnumerator" && x.GetParameters().Length == 0))), 
                                                        e[90]=TryCatchFinally(
                                                        e[91]=Loop(
                                                            e[92]=Condition(
                                                            e[93]=Call(
                                                                p[15 // (IEnumerator<SourceObject> enumerator)
                                                                ], 
                                                                typeof(IEnumerator).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "MoveNext" && x.GetParameters().Length == 0)),
                                                            e[94]=Block(
                                                                typeof(void),
                                                                new ParameterExpression[0], 
                                                                e[95]=MakeBinary(ExpressionType.Assign,
                                                                p[16 // (SourceObject item)
                                                                    ],
                                                                e[96]=Property(
                                                                    p[15 // (IEnumerator<SourceObject> enumerator)
                                                                    ],
                                                                    typeof(IEnumerator<SourceObject>).GetTypeInfo().GetDeclaredProperty("Current"))), 
                                                                e[97]=Call(
                                                                p[13 // (IList<DestObject> collectionDestination)
                                                                    ], 
                                                                typeof(ICollection<DestObject>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(DestObject) })),
                                                                e[98]=Condition(
                                                                    e[99]=MakeBinary(ExpressionType.Equal,
                                                                    p[16 // (SourceObject item)
                                                                        ],
                                                                    e[2 // Constant of object
                                                                        ]),
                                                                    e[100]=Condition(
                                                                    e[101]=MakeBinary(ExpressionType.Equal,
                                                                        e[102]=Default(typeof(DestObject)),
                                                                        e[2 // Constant of object
                                                                        ]),
                                                                    e[102 // Default of DestObject
                                                                        ],
                                                                    e[102 // Default of DestObject
                                                                        ],
                                                                    typeof(DestObject)),
                                                                    e[103]=Call(
                                                                    p[3 // (ResolutionContext context)
                                                                        ], 
                                                                    typeof(ResolutionContext).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Where(x => x.IsGenericMethod && x.Name == "MapInternal" && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(SourceObject), typeof(DestObject)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(SourceObject), typeof(DestObject), typeof(MemberMap) })),
                                                                    p[16 // (SourceObject item)
                                                                        ], 
                                                                    e[102 // Default of DestObject
                                                                        ], 
                                                                    e[104]=Constant(null, typeof(MemberMap))),
                                                                    typeof(DestObject)))),
                                                            e[105]=MakeGoto(GotoExpressionKind.Break,
                                                                l[0]=Label(typeof(void), "LoopBreak"),
                                                                null,
                                                                typeof(void)),
                                                            typeof(void)),
                                                            l[0 // (LoopBreak)
                                                            ]),
                                                        e[106]=Call(
                                                            p[15 // (IEnumerator<SourceObject> enumerator)
                                                            ], 
                                                            typeof(IDisposable).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Dispose" && x.GetParameters().Length == 0)),new CatchBlock[0])), 
                                                    p[13 // (IList<DestObject> collectionDestination)
                                                        ]),
                                                    typeof(IList<DestObject>))), 
                                                e[107]=MakeBinary(ExpressionType.Assign,
                                                e[81 // MemberAccess of IList<DestObject>
                                                    ],
                                                p[12 // (IList<DestObject> mappedValue)
                                                    ])),
                                            MakeCatchBlock(
                                                typeof(Exception),
                                                p[10 // (Exception ex)
                                                ],
                                                e[108]=Throw(
                                                e[109]=Call(
                                                    null, 
                                                    typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MemberMap) })),
                                                    p[10 // (Exception ex)
                                                    ], 
                                                    e[110]=Constant(null, typeof(PropertyMap)
                                                    // !!! Please provide the non-default value
                                                    )),
                                                typeof(IList<DestObject>)),
                                                null)), 
                                            p[8 // (DestObject typeMapDestination)
                                            ]),
                                        typeof(DestObject)))),
                                    typeof(DestObject)))),
                            e[111]=MakeGoto(GotoExpressionKind.Break,
                                l[1]=Label(typeof(void), "LoopBreak"),
                                null,
                                typeof(void)),
                            typeof(void)),
                            l[1 // (LoopBreak)
                            ]),
                        e[112]=Call(
                            p[6 // ([struct] List<SourceObject>.Enumerator enumerator)
                            ], 
                            typeof(IDisposable).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Dispose" && x.GetParameters().Length == 0)),new CatchBlock[0])), 
                    p[4 // (List<DestObject> collectionDestination)
                        ])),
                MakeCatchBlock(
                    typeof(Exception),
                    p[10 // (Exception ex)
                    ],
                    e[113]=Throw(
                    e[114]=Call(
                        null, 
                        typeof(MapperConfiguration).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(MapRequest).MakeByRefType() })),
                        p[10 // (Exception ex)
                        ], 
                        e[115]=Constant(default(MapRequest)
                        // !!! Please provide the non-default value
                        )),
                    typeof(List<DestObject>)),
                    null)),
                typeof(List<DestObject>)),
            p[0 // (List<SourceObject> source)
                ], 
            p[1 // (List<DestObject> mapperDestination)
                ], 
            p[3 // (ResolutionContext context)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var sourceList = new List<SourceObject>();
            var destList = new List<DestObject>();

            // var config = new MapperConfiguration(cfg => cfg.CreateMap<SourceObject, DestObject>().PreserveReferences());
            // config.AssertConfigurationIsValid();

            var source1 = new SourceObject
            {
                Id = 1,
            };
            sourceList.Add(source1);

            var source2 = new SourceObject
            {
                Id = 2,
            };
            sourceList.Add(source2);

            source1.AddChild(source2); // This causes the problem

            // config.CreateMapper().Map(sourceList, destList);
            // destList.Count.ShouldBe(2);
            // destList[0].Children.Count.ShouldBe(1);
            // destList[0].Children[0].ShouldBeSameAs(destList[1]);

            Assert.Throws<AutoMapperMappingException>(() =>
            fs(sourceList, destList, new ResolutionContext()));

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            Assert.Throws<AutoMapperMappingException>(() =>
            ff(sourceList, destList, new ResolutionContext()));
        }
    }

    public class SourceObject
    {
        public int Id;
        public IList<SourceObject> Children;

        public void AddChild(SourceObject childObject)
        {
            if (this.Children == null)
                this.Children = new List<SourceObject>();
            Children.Add(childObject);
        }
    }

    public class DestObject
    {
        public int Id;
        public IList<DestObject> Children;
        public DestObject() {}

        public void AddChild(DestObject childObject)
        {
            if (this.Children == null)
                this.Children = new List<DestObject>();
            Children.Add(childObject);
        }
    }
}