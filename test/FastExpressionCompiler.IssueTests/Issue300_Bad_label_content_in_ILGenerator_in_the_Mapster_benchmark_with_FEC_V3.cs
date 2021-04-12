using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Linq.Expressions;
#if LIGHT_EXPRESSION
using System.Text;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3 : ITest
    {
        public int Run()
        {
            // Test_301();
            // Test_300();
            Test_301_TryCatch_case();
            return 2;
        }

        [Test]
        public void Test_301_TryCatch_case()
        {
            var p = new ParameterExpression[7]; // the parameter expressions 
            var e = new Expression[37]; // the unique expressions 
            var l = new LabelTarget[1]; // the labels 
            var expr = Lambda<Func<object, Test>>( //$
                e[0]=Block(
                    typeof(Test),
                    new[] {
                    p[0]=Parameter(typeof(Test))
                    },
                    e[1]=MakeBinary(ExpressionType.Assign,
                    p[0 // (Test test__42119052)
                        ],
                    e[2]=Convert(
                        p[1]=Parameter(typeof(object)),
                        typeof(Test))), 
                    e[3]=Block(
                    typeof(Test),
                    new[] {
                    p[2]=Parameter(typeof(MapContextScope), "scope")
                    },
                    e[4]=Condition(
                        e[5]=MakeBinary(ExpressionType.Equal,
                        p[0 // (Test test__42119052)
                            ],
                        e[6]=Constant(null, typeof(Test))),
                        e[7]=MakeGoto(GotoExpressionKind.Return,
                        l[0]=Label(typeof(Test)),
                        e[8]=Constant(null, typeof(Test)),
                        typeof(void)),
                        e[9]=Empty(),
                        typeof(void)), 
                    e[10]=MakeBinary(ExpressionType.Assign,
                        p[2 // (MapContextScope scope)
                        ],
                        e[11]=New( // 0 args
                        typeof(MapContextScope).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])), 
                    e[12]=TryCatchFinally(
                        e[13]=Block(
                        typeof(void),
                        new[] {
                        p[3]=Parameter(typeof(object), "cache"),
                        p[4]=Parameter(typeof(Dictionary<ReferenceTuple, object>), "references"),
                        p[5]=Parameter(typeof(ReferenceTuple), "key"),
                        p[6]=Parameter(typeof(Test), "result")
                        },
                        e[14]=MakeBinary(ExpressionType.Assign,
                            p[4 // (Dictionary<ReferenceTuple, object> references)
                            ],
                            e[15]=Property(
                            e[16]=Property(
                                p[2 // (MapContextScope scope)
                                ],
                                typeof(MapContextScope).GetTypeInfo().GetDeclaredProperty("Context")),
                            typeof(MapContext).GetTypeInfo().GetDeclaredProperty("References"))), 
                        e[17]=MakeBinary(ExpressionType.Assign,
                            p[5 // ([struct] ReferenceTuple key)
                            ],
                            e[18]=New( // 2 args
                            typeof(ReferenceTuple).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                            p[0 // (Test test__42119052)
                                ], 
                            e[19]=Constant(typeof(Test)))), 
                        e[20]=Condition(
                            e[21]=Call(
                            p[4 // (Dictionary<ReferenceTuple, object> references)
                                ], 
                            typeof(Dictionary<ReferenceTuple, object>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "TryGetValue" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(ReferenceTuple), typeof(System.Object).MakeByRefType() })),
                            p[5 // ([struct] ReferenceTuple key)
                                ], 
                            p[3 // (object cache)
                                ]),
                            e[22]=MakeGoto(GotoExpressionKind.Return,
                            l[0 // (test__32347029)
                            ],
                            e[23]=Convert(
                                p[3 // (object cache)
                                ],
                                typeof(Test)),
                            typeof(void)),
                            e[9 // Default of void
                            ],
                            typeof(void)), 
                        e[24]=MakeBinary(ExpressionType.Assign,
                            p[6 // (Test result)
                            ],
                            e[25]=New( // 0 args
                            typeof(Test).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])), 
                        e[26]=MakeBinary(ExpressionType.Assign,
                            e[27]=MakeIndex(
                            p[4 // (Dictionary<ReferenceTuple, object> references)
                                ], 
                            typeof(Dictionary<ReferenceTuple, object>).GetTypeInfo().GetDeclaredProperty("Item"), new Expression[] {
                            p[5 // ([struct] ReferenceTuple key)
                                ]}),
                            e[28]=Convert(
                            p[6 // (Test result)
                                ],
                            typeof(object))), 
                        e[29]=Block(
                            typeof(string),
                            new ParameterExpression[0], 
                            e[30]=MakeBinary(ExpressionType.Assign,
                            e[31]=Property(
                                p[6 // (Test result)
                                ],
                                typeof(Test).GetTypeInfo().GetDeclaredProperty("TestString")),
                            e[32]=Property(
                                p[0 // (Test test__42119052)
                                ],
                                typeof(Test).GetTypeInfo().GetDeclaredProperty("TestString")))), 
                        e[33]=MakeGoto(GotoExpressionKind.Return,
                            l[0 // (test__32347029)
                            ],
                            p[6 // (Test result)
                            ],
                            typeof(void))),
                        e[34]=Call(
                        p[2 // (MapContextScope scope)
                            ], 
                        typeof(MapContextScope).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Dispose" && x.GetParameters().Length == 0)),
                        new CatchBlock[0]), 
                    e[35]=Label(l[0 // (test__32347029)
                        ],
                        e[36]=Constant(null, typeof(Test))))),
                p[1 // (object object__56200037)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var test = new Test { TestString = "42" };
            var res = fs(test);

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            var res2 = ff(test);

            Assert.AreEqual(res.TestString, res2.TestString);
        }

        public class Test
        {
            public string TestString { get; set; }
        }

        public class MapContext
        {
#if NETSTANDARD
            private static readonly AsyncLocal<MapContext?> _localContext = new AsyncLocal<MapContext?>();
            public static MapContext? Current
            {
                get => _localContext.Value;
                set => _localContext.Value = value;
            }
#else
            [field: ThreadStatic]
            public static MapContext? Current { get; set; }
#endif

            private Dictionary<ReferenceTuple, object>? _references;
            public Dictionary<ReferenceTuple, object> References => _references ??= new Dictionary<ReferenceTuple, object>();

            private Dictionary<string, object>? _parameters;
            public Dictionary<string, object> Parameters => _parameters ??= new Dictionary<string, object>();
        }

        public class MapContextScope : IDisposable
        {
            public static MapContextScope Required()
            {
                return new MapContextScope();
            }

            public static MapContextScope RequiresNew()
            {
                return new MapContextScope(true);
            }

            public MapContext Context { get; }

            private readonly MapContext? _previousContext;

            public MapContextScope() : this(false) { }
            public MapContextScope(bool ignorePreviousContext)
            {
                _previousContext = MapContext.Current;

                this.Context = ignorePreviousContext
                    ? new MapContext()
                    : _previousContext ?? new MapContext();

                MapContext.Current = this.Context;
            }

            public void Dispose()
            {
                MapContext.Current = _previousContext;
            }

            public static TResult GetOrAddMapReference<TResult>(ReferenceTuple key, Func<ReferenceTuple, TResult> mapFn) where TResult : notnull
            {
                using var context = new MapContextScope();
                var dict = context.Context.References;
                if (!dict.TryGetValue(key, out var reference))
                    dict[key] = reference = mapFn(key);
                return (TResult)reference;
            }
        }

        public readonly struct ReferenceTuple : IEquatable<ReferenceTuple>
        {
            public object Reference { get; }
            public Type DestinationType { get; }
            public ReferenceTuple(object reference, Type destinationType)
            {
                this.Reference = reference;
                this.DestinationType = destinationType;
            }

            public override bool Equals(object obj)
            {
                return obj is ReferenceTuple other && Equals(other);
            }

            public bool Equals(ReferenceTuple other)
            {
                return ReferenceEquals(this.Reference, other.Reference) 
                    && this.DestinationType == other.DestinationType;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(this.Reference) * 397) ^ DestinationType.GetHashCode();
                }
            }

            public static bool operator ==(ReferenceTuple left, ReferenceTuple right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ReferenceTuple left, ReferenceTuple right)
            {
                return !(left == right);
            }
        }

        [Test]
        public void Test_301()
        {
            var p = new ParameterExpression[6]; // the parameter expressions 
            var e = new Expression[41]; // the unique expressions 
            var l = new LabelTarget[2]; // the labels 
            var expr = Lambda<Func<Address[], AddressDTO[]>>( // $
                e[0]=Block(
                    typeof(AddressDTO[]),
                    new[] {
                    p[0]=Parameter(typeof(AddressDTO[]), "result")
                    },
                    e[1]=Condition(
                    e[2]=MakeBinary(ExpressionType.Equal,
                        p[1]=Parameter(typeof(Address[])),
                        e[3]=Constant(null, typeof(Address[]))),
                    e[4]=MakeGoto(GotoExpressionKind.Return,
                        l[0]=Label(typeof(AddressDTO[])),
                        e[5]=Constant(null, typeof(AddressDTO[])),
                        typeof(void)),
                    e[6]=Empty(),
                    typeof(void)), 
                    e[7]=MakeBinary(ExpressionType.Assign,
                    p[0 // (AddressDTO[] result)
                        ],
                    e[8]=NewArrayBounds(
                        typeof(AddressDTO), 
                        e[9]=ArrayLength(
                        p[1 // (Address[] address_arr__14993092)
                            ]//=Parameter(typeof(Address[]))
                            ))), 
                    e[10]=Block(
                    typeof(void),
                    new[] {
                    p[2]=Parameter(typeof(int), "v")
                    },
                    e[11]=MakeBinary(ExpressionType.Assign,
                        p[2 // (int v)
                        ],
                        e[12]=Constant((int)0)), 
                    e[13]=Block(
                        typeof(void),
                        new[] {
                        p[3]=Parameter(typeof(int), "i"),
                        p[4]=Parameter(typeof(int), "len")
                        },
                        e[14]=MakeBinary(ExpressionType.Assign,
                        p[3 // (int i)
                            ],
                        e[15]=Constant((int)0)), 
                        e[16]=MakeBinary(ExpressionType.Assign,
                        p[4 // (int len)
                            ],
                        e[17]=ArrayLength(
                            p[1 // (Address[] address_arr__14993092)
                            ])), 
                        e[18]=Loop(
                        e[19]=Condition(
                            e[20]=MakeBinary(ExpressionType.LessThan,
                            p[3 // (int i)
                                ],
                            p[4 // (int len)
                                ]),
                            e[21]=Block(
                            typeof(int),
                            new[] {
                            p[5]=Parameter(typeof(Address), "item")
                            },
                            e[22]=MakeBinary(ExpressionType.Assign,
                                p[5 // (Address item)
                                ],
                                e[23]=MakeBinary(ExpressionType.ArrayIndex,
                                p[1 // (Address[] address_arr__14993092)
                                    ],
                                p[3 // (int i)
                                    ])), 
                            e[24]=MakeBinary(ExpressionType.Assign,
                                e[25]=ArrayAccess(
                                p[0 // (AddressDTO[] result)
                                    ], new Expression[] {
                                e[26]=PostIncrementAssign(
                                    p[2 // (int v)
                                    ])}),
                                e[27]=Condition(
                                e[28]=MakeBinary(ExpressionType.Equal,
                                    p[5 // (Address item)
                                    ],
                                    e[29]=Constant(null, typeof(Address))),
                                e[30]=Constant(null, typeof(AddressDTO)),
                                e[31]=MemberInit((NewExpression)(
                                    e[32]=New( // 0 args
                                    typeof(AddressDTO).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])), 
                                    Bind(
                                    typeof(AddressDTO).GetTypeInfo().GetDeclaredProperty("Id"), 
                                    e[33]=Property(
                                        p[5 // (Address item)
                                        ],
                                        typeof(Address).GetTypeInfo().GetDeclaredProperty("Id"))), 
                                    Bind(
                                    typeof(AddressDTO).GetTypeInfo().GetDeclaredProperty("City"), 
                                    e[34]=Property(
                                        p[5 // (Address item)
                                        ],
                                        typeof(Address).GetTypeInfo().GetDeclaredProperty("City"))), 
                                    Bind(
                                    typeof(AddressDTO).GetTypeInfo().GetDeclaredProperty("Country"), 
                                    e[35]=Property(
                                        p[5 // (Address item)
                                        ],
                                        typeof(Address).GetTypeInfo().GetDeclaredProperty("Country")))),
                                typeof(AddressDTO))), 
                            e[36]=PostIncrementAssign(
                                p[3 // (int i)
                                ])),
                            e[37]=MakeGoto(GotoExpressionKind.Break,
                            l[1]=Label(typeof(void), "LoopBreak"),
                            null,
                            typeof(void)),
                            typeof(void)),
                        l[1 // (LoopBreak)
                        ]))), 
                    e[38]=MakeGoto(GotoExpressionKind.Return,
                    l[0 // (addressdto_arr__58328727)
                    ]//=Label(typeof(AddressDTO[]))
                    ,
                    p[0 // (AddressDTO[] result)
                        ],
                    typeof(void)), 
                    e[39]=Label(l[0 // (addressdto_arr__58328727)
                    ],
                    e[40]=Constant(null, typeof(AddressDTO[])))),
                p[1 // (Address[] address_arr__14993092)
                    ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var addresses = new Address[] { new Address() };

            var res = fs(addresses);

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            var res2 = ff(addresses);

            CollectionAssert.AreEqual(res, res2);
        }

        [Test]
        public void Test_300()
        {
            var p = new ParameterExpression[3]; // the parameter expressions 
            var e = new Expression[56]; // the unique expressions 
            var l = new LabelTarget[1]; // the labels 
            var expr = Lambda<Func<Customer, CustomerDTO, CustomerDTO>>( // $
            e[0]=Block(
                typeof(CustomerDTO),
                new[] {
                p[0]=Parameter(typeof(CustomerDTO), "result")
                },
                e[1]=Condition(
                e[2]=MakeBinary(ExpressionType.Equal,
                    p[1]=Parameter(typeof(Customer)),
                    e[3]=Constant(null, typeof(Customer))),
                e[4]=MakeGoto(GotoExpressionKind.Return,
                    l[0]=Label(typeof(CustomerDTO)),
                    e[5]=Constant(null, typeof(CustomerDTO)),
                    typeof(void)),
                e[6]=Empty(),
                typeof(void)), 
                e[7]=MakeBinary(ExpressionType.Assign,
                p[0 // (CustomerDTO result)
                    ],
                e[8]=Coalesce(
                    p[2]=Parameter(typeof(CustomerDTO)),
                    e[9]=New( // 0 args
                    typeof(CustomerDTO).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))), 
                e[10]=Block(
                typeof(string),
                new ParameterExpression[0], 
                e[11]=MakeBinary(ExpressionType.Assign,
                    e[12]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Id")),
                    e[13]=Property(
                    p[1 // (Customer customer__62468121)
                        ],
                    typeof(Customer).GetTypeInfo().GetDeclaredProperty("Id"))), 
                e[14]=MakeBinary(ExpressionType.Assign,
                    e[15]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Name")),
                    e[16]=Property(
                    p[1 // (Customer customer__62468121)
                        ],
                    typeof(Customer).GetTypeInfo().GetDeclaredProperty("Name"))), 
                e[17]=MakeBinary(ExpressionType.Assign,
                    e[18]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Address")),
                    e[19]=Call(
                    e[20]=Call(
                        e[21]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Address), typeof(Address)) : x).Single()), 
                    typeof(System.Func<Address, Address, Address>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Address), typeof(Address) })),
                    e[22]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("Address")), 
                    e[23]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Address")))), 
                e[24]=MakeBinary(ExpressionType.Assign,
                    e[25]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("HomeAddress")),
                    e[26]=Call(
                    e[27]=Call(
                        e[28]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Address), typeof(AddressDTO)) : x).Single()), 
                    typeof(System.Func<Address, AddressDTO, AddressDTO>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Address), typeof(AddressDTO) })),
                    e[29]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("HomeAddress")), 
                    e[30]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("HomeAddress")))), 
                e[31]=MakeBinary(ExpressionType.Assign,
                    e[32]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Addresses")),
                    e[33]=Call(
                    e[34]=Call(
                        e[35]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Address[]), typeof(AddressDTO[])) : x).Single()), 
                    typeof(System.Func<Address[], AddressDTO[], AddressDTO[]>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Address[]), typeof(AddressDTO[]) })),
                    e[36]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("Addresses")), 
                    e[37]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("Addresses")))), 
                e[38]=MakeBinary(ExpressionType.Assign,
                    e[39]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("WorkAddresses")),
                    e[40]=Call(
                    e[41]=Call(
                        e[42]=Property(
                        null,
                        typeof(TypeAdapterConfig).GetTypeInfo().GetDeclaredProperty("GlobalSettings")), 
                        typeof(TypeAdapterConfig).GetMethods().Where(x => x.IsGenericMethod && x.Name == "GetMapToTargetFunction" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 2).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(ICollection<Address>), typeof(List<AddressDTO>)) : x).Single()), 
                    typeof(System.Func<ICollection<Address>, List<AddressDTO>, List<AddressDTO>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Invoke" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(ICollection<Address>), typeof(List<AddressDTO>) })),
                    e[43]=Property(
                        p[1 // (Customer customer__62468121)
                        ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("WorkAddresses")), 
                    e[44]=Property(
                        p[0 // (CustomerDTO result)
                        ],
                        typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("WorkAddresses")))), 
                e[45]=MakeBinary(ExpressionType.Assign,
                    e[46]=Property(
                    p[0 // (CustomerDTO result)
                        ],
                    typeof(CustomerDTO).GetTypeInfo().GetDeclaredProperty("AddressCity")),
                    e[47]=Condition(
                    e[48]=MakeBinary(ExpressionType.Equal,
                        e[49]=Property(
                        p[1 // (Customer customer__62468121)
                            ],
                        typeof(Customer).GetTypeInfo().GetDeclaredProperty("Address")),
                        e[50]=Constant(null, typeof(Address))),
                    e[51]=Constant(null, typeof(string)),
                    e[52]=Property(
                        e[49 // MemberAccess of Address
                        ],
                        typeof(Address).GetTypeInfo().GetDeclaredProperty("City")),
                    typeof(string)))), 
                e[53]=MakeGoto(GotoExpressionKind.Return,
                l[0 // (customerdto__39451090)
                ],
                p[0 // (CustomerDTO result)
                    ],
                typeof(void)), 
                e[54]=Label(l[0 // (customerdto__39451090)
                ],
                e[55]=Constant(null, typeof(CustomerDTO)))),
            p[1 // (Customer customer__62468121)
                ], 
            p[2 // (CustomerDTO customerdto__25342185)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var customer = new Customer();
            var customerDto = new CustomerDTO();

            var res = fs(customer, customerDto);

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            var res2 = ff(customer, customerDto);

            Assert.AreEqual(res, res2);
        }

        public class TypeAdapterConfig 
        {
            public static TypeAdapterConfig GlobalSettings { get; } = new TypeAdapterConfig();

            public Func<TSource, TDestination, TDestination> GetMapToTargetFunction<TSource, TDestination>()
            {
                return (TSource s, TDestination d) => d;
            }
        }

        public class Address
        {
            public int Id { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class AddressDTO
        {
            public int Id { get; set; }
            public string City { get; set; }
            public string Country { get; set; }

            public override bool Equals(object obj) => 
                obj is AddressDTO a && a.Id == Id && a.City == City && a.Country == Country;
        }

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal? Credit { get; set; }
            public Address Address { get; set; }
            public Address HomeAddress { get; set; }
            public Address[] Addresses { get; set; }
            public ICollection<Address> WorkAddresses { get; set; }
        }

        public class CustomerDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Address Address { get; set; }
            public AddressDTO HomeAddress { get; set; }
            public AddressDTO[] Addresses { get; set; }
            public List<AddressDTO> WorkAddresses { get; set; }
            public string AddressCity { get; set; }
        }
    }
}