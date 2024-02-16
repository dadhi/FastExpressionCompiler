#if NET7_0_OR_GREATER && !LIGHT_EXPRESSION
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using NUnit.Framework;
using Mapster;
using Mapster.Utils;
using MapsterMapper;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests;

[TestFixture]
public class Issue390_System_AccessViolationException_when_mapping_using_Mapster : ITest
{
    public int Run()
    {
        Test_extracted_mapping_code();
        Test_mapping();

        return 2;
    }

    [Test]
    public void Test_extracted_mapping_code()
    {
        var p = new ParameterExpression[7]; // the parameter expressions
        var e = new Expression[48]; // the unique expressions
        var l = new LabelTarget[1]; // the labels
        var expr = Lambda<Func<AuthResultDto, Token>>(
        e[0]=Block(
            typeof(Token),
            new[] {
            p[0]=Parameter(typeof(MapContextScope), "scope")
            },
            e[1]=Condition(
                e[2]=MakeBinary(ExpressionType.Equal,
                    p[1]=Parameter(typeof(AuthResultDto)),
                    e[3]=Constant(null, typeof(AuthResultDto))),
                e[4]=MakeGoto(GotoExpressionKind.Return,
                    l[0]=Label(typeof(Token)),
                    e[5]=Constant(null, typeof(Token)),
                    typeof(void)),
                e[6]=Empty(),
                typeof(void)), 
            e[7]=MakeBinary(ExpressionType.Assign,
                p[0 // (MapContextScope scope)
                    ],
                e[8]=New( // 0 args
                    typeof(MapContextScope).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0])), 
            e[9]=TryCatchFinally(
                e[10]=Block(
                    typeof(void),
                    new[] {
                    p[2]=Parameter(typeof(object), "cache"),
                    p[3]=Parameter(typeof(Dictionary<ReferenceTuple, object>), "references"),
                    p[4]=Parameter(typeof(ReferenceTuple), "key"),
                    p[5]=Parameter(typeof(Token), "result")
                    },
                    e[11]=MakeBinary(ExpressionType.Assign,
                        p[3 // (Dictionary<ReferenceTuple, object> references)
                            ],
                        e[12]=Property(
                            e[13]=Property(
                                p[0 // (MapContextScope scope)
                                    ],
                                typeof(MapContextScope).GetTypeInfo().GetDeclaredProperty("Context")),
                            typeof(MapContext).GetTypeInfo().GetDeclaredProperty("References"))), 
                    e[14]=MakeBinary(ExpressionType.Assign,
                        p[4 // ([struct] ReferenceTuple key)
                            ],
                        e[15]=New( // 2 args
                            typeof(ReferenceTuple).GetTypeInfo().DeclaredConstructors.AsArray()[0],
                            p[1 // (AuthResultDto issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015)
                                ], 
                            e[16]=Constant(typeof(Token)))), 
                    e[17]=Condition(
                        e[18]=Call(
                            p[3 // (Dictionary<ReferenceTuple, object> references)
                                ], 
                        typeof(Dictionary<ReferenceTuple, object>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "TryGetValue" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(ReferenceTuple), typeof(Object).MakeByRefType() })),
                            p[4 // ([struct] ReferenceTuple key)
                                ], 
                            p[2 // (object cache)
                                ]),
                        e[19]=MakeGoto(GotoExpressionKind.Return,
                            l[0 // (issue390_system_accessviolationexception_when_mapping_using_mapster_token__41962596)
                            ],
                            e[20]=Convert(
                                p[2 // (object cache)
                                    ],
                                typeof(Token)),
                            typeof(void)),
                        e[6 // Default of void
                            ],
                        typeof(void)), 
                    e[21]=MakeBinary(ExpressionType.Assign,
                        p[5 // (Token result)
                            ],
                        e[22]=New( // 0 args
                            typeof(Token).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0])), 
                    e[23]=MakeBinary(ExpressionType.Assign,
                        e[24]=MakeIndex(
                            p[3 // (Dictionary<ReferenceTuple, object> references)
                                ], 
                            typeof(Dictionary<ReferenceTuple, object>).GetTypeInfo().GetDeclaredProperty("Item"), new Expression[] {
                            p[4 // ([struct] ReferenceTuple key)
                                ]}),
                        e[25]=Convert(
                            p[5 // (Token result)
                                ],
                            typeof(object))), 
                    e[26]=Block(
                        typeof(DateTime),
                        new ParameterExpression[0], 
                        e[27]=MakeBinary(ExpressionType.Assign,
                            e[28]=Property(
                                p[5 // (Token result)
                                    ],
                                typeof(Token).GetTypeInfo().GetDeclaredProperty("RefreshTokenExpirationDate")),
                            e[29]=Invoke(
                                e[30]=Lambda<Func<DateTime?, DateTime>>(
                                    e[31]=Condition(
                                        e[32]=MakeBinary(ExpressionType.Equal,
                                            p[6]=Parameter(typeof(DateTime?)),
                                            e[33]=Constant(null, typeof(DateTime?)),
                                            liftToNull: false,
                                            typeof(DateTime).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Equality" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(DateTime), typeof(DateTime) }))),
                                        e[34]=Constant(DateTime.Parse("1/1/0001 12:00:00 AM")),
                                        e[35]=Convert(
                                            p[6 // ([struct] DateTime? datetime__9799115)
                                                ],
                                            typeof(DateTime)),
                                        typeof(DateTime)),
                                    p[6 // ([struct] DateTime? datetime__9799115)
                                        ]),
                                e[36]=Condition(
                                    e[37]=MakeBinary(ExpressionType.Equal,
                                        e[38]=Property(
                                            p[1 // (AuthResultDto issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015)
                                                ],
                                            typeof(AuthResultDto).GetTypeInfo().GetDeclaredProperty("RefreshToken")),
                                        e[39]=Constant(null, typeof(RefreshTokenDto))),
                                    e[40]=Constant(null, typeof(DateTime?)),
                                    e[41]=Convert(
                                        e[42]=Property(
                                            e[43]=Property(
                                                e[38 // MemberAccess of RefreshTokenDto
                                                    ],
                                                typeof(RefreshTokenDto).GetTypeInfo().GetDeclaredProperty("ExpirationDate")),
                                            typeof(DateTimeOffset).GetTypeInfo().GetDeclaredProperty("LocalDateTime")),
                                        typeof(DateTime?)),
                                    typeof(DateTime?))))), 
                    e[44]=MakeGoto(GotoExpressionKind.Return,
                        l[0 // (issue390_system_accessviolationexception_when_mapping_using_mapster_token__41962596)
                        ],
                        p[5 // (Token result)
                            ],
                        typeof(void))),
                e[45]=Call(
                    p[0 // (MapContextScope scope)
                        ], 
                    typeof(MapContextScope).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Dispose" && x.GetParameters().Length == 0)),new CatchBlock[0]), 
            e[46]=Label(l[0 // (issue390_system_accessviolationexception_when_mapping_using_mapster_token__41962596)
                ],
                e[47]=Constant(null, typeof(Token)))),
        p[1 // (AuthResultDto issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015)
            ]);

        expr.PrintCSharp();

        var auth = new AuthResultDto() { RefreshToken = new() };

        var fs = expr.CompileSys();
        fs.PrintIL();
        var token = fs(auth);
        Assert.AreEqual(auth.RefreshToken.ExpirationDate.LocalDateTime, token.RefreshTokenExpirationDate);

        // var ff = expr.CompileFast(true); // todo: @fixme
        var ff = expr.CompileFast(true, flags: CompilerFlags.NoInvocationLambdaInlining);
        ff.PrintIL();
        token = ff(auth);
        Assert.AreEqual(auth.RefreshToken.ExpirationDate.LocalDateTime, token.RefreshTokenExpirationDate);
    }

    [Test]
    public void Test_mapping()
    {
        var auth = new AuthResultDto() { RefreshToken = new() };

        var token = DataMapper.Current.Map<Token>(auth);

        Assert.AreEqual(auth.RefreshToken.ExpirationDate.LocalDateTime, token.RefreshTokenExpirationDate);
    }

    public class DataMapper
    {
        private readonly Lazy<Mapper> _lazyMapper;
        public Mapper Mapper => _lazyMapper.Value;
        private static DataMapper _instance;
        private static TypeAdapterConfig _cfg;

        public static DataMapper Current
        {
            get
            {
                _instance ??= new DataMapper();
                return _instance;
            }
        }

        public DataMapper() => _lazyMapper = new Lazy<Mapper>(() => new Mapper(_cfg ??= Config()));

        public TSource Clone<TSource>(TSource source) => Mapper.Map<TSource, TSource>(source);

        public TDes Map<TSource, TDes>(TSource source) => Mapper.Map<TSource, TDes>(source);

        public TDes Map<TDes>(object source) => Mapper.Map<TDes>(source);

        private static TypeAdapterConfig Config()
        {
            var cfg = TypeAdapterConfig.GlobalSettings;
            cfg.Compiler = static e =>
            {
                try
                {
                    e.PrintCSharp();
                    e.PrintExpression();

                    var @cs = (Func<AuthResultDto, Token>)((AuthResultDto issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015) =>
                    {
                        MapContextScope scope = null;
                        if (issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015 == (AuthResultDto)null)
                        {
                            return (Token)null;
                        }
                        
                        scope = new MapContextScope();
                        try
                        {
                            object cache = null;
                            Dictionary<ReferenceTuple, object> references = null;
                            ReferenceTuple key = default;
                            Token result = null;
                            references = scope.Context.References;
                            key = new ReferenceTuple(
                                issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015,
                                typeof(Token));
                            if (references.TryGetValue(
                                key,
                                out cache))
                            {
                                return ((Token)cache);
                            }
                            
                            result = new Token();
                            references[key] = ((object)result);
                            result.RefreshTokenExpirationDate = ((Func<DateTime?, DateTime>)((DateTime? datetime__9799115) =>
                                    (datetime__9799115 == (DateTime?)null) ?
                                        DateTime.Parse("1/1/0001 12:00:00 AM") :
                                        ((DateTime)datetime__9799115))).Invoke(
                                (issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015.RefreshToken == (RefreshTokenDto)null) ?
                                    (DateTime?)null :
                                    ((DateTime?)issue390_system_accessviolationexception_when_mapping_using_mapster_authresultdto__63208015.RefreshToken.ExpirationDate.LocalDateTime));
                            return result;
                        }
                        finally
                        {
                            scope.Dispose();
                        }
                        
                        issue390_system_accessviolationexception_when_mapping_using_mapster_token__41962596:;
                    });

                    var fs = e.CompileSys();
                    fs.PrintIL();

                    // var ff = e.CompileFast();
                    var ff = e.CompileFast(true, flags: CompilerFlags.NoInvocationLambdaInlining);
                    Assert.IsNotNull(ff);
                    ff.PrintIL();

                    return ff;
                }
                catch (Exception)
                {
                    throw;
                }
            };

            cfg.RequireDestinationMemberSource = true;
            cfg.Default.PreserveReference(true);
            RegisterMappings(cfg);

            try
            {
                cfg.Compile();
            }
            catch (Exception)
            {
            }

            return cfg;
        }

        private static void RegisterMappings(TypeAdapterConfig cfg)
        {
            cfg.NewConfig<AuthResultDto, Token>().Map(
                static dst => dst.RefreshTokenExpirationDate,
                static src => src.RefreshToken.ExpirationDate.LocalDateTime);
        }
    }

    public class AuthResultDto
    {
        public RefreshTokenDto RefreshToken { get; set; }
    }

    public class RefreshTokenDto
    {
        public DateTimeOffset ExpirationDate { get; set; }
    }

    public class Token
    {
        public DateTime RefreshTokenExpirationDate { get; set; }
    }

}
#endif
