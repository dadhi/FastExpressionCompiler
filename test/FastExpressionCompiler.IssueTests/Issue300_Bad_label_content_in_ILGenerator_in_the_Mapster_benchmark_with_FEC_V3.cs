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
            Test_301_MemberInit_PrivateProperty();
            Test_301_MemberInit_InternalProperty();
            Test_301_Invoke_Lambda_inlining_case_simplified();
            Test_301_Invoke_Lambda_inlining_case();
            Test_301_Goto_to_label_with_default_value_should_not_return_when_followup_expression_is_present();
            Test_301_Goto_to_label_with_default_value_should_not_return_when_followup_expression_is_present_Custom_constant_output();
            Test_301_Goto_to_label_with_default_value_should_return_the_goto_value_when_no_other_expressions_is_present();
            Test_301_Dictionary_case();
            Test_301_TryCatch_case();
            Test_301();
            Test_300();
            return 11;
        }

        public class CustomerDTO2
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class CustomerWithPrivateProperty
        {
            public int Id { get; private set; }
            private string Name { get; set; }

            private CustomerWithPrivateProperty() { }

            public CustomerWithPrivateProperty(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public bool HasName(string name)
            {
                return Name == name;
            }
        }

        [Test]
        public void Test_301_MemberInit_PrivateProperty()
        {
            var p = new ParameterExpression[2]; // the parameter expressions 
            var e = new Expression[11]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<System.Func<object, CustomerDTO2>>( //$
                e[0]=Block(
                    typeof(CustomerDTO2),
                    new[] {
                    p[0]=Parameter(typeof(CustomerWithPrivateProperty))
                    },
                    e[1]=MakeBinary(ExpressionType.Assign,
                    p[0 // (CustomerWithPrivateProperty whenmappingprivatefieldsandproperties_customerwithprivateproperty__61071393)
                        ],
                    e[2]=Convert(
                        p[1]=Parameter(typeof(object)),
                        typeof(CustomerWithPrivateProperty))), 
                    e[3]=Condition(
                    e[4]=MakeBinary(ExpressionType.Equal,
                        p[0 // (CustomerWithPrivateProperty whenmappingprivatefieldsandproperties_customerwithprivateproperty__61071393)
                        ],
                        e[5]=Constant(null, typeof(CustomerWithPrivateProperty))),
                    e[6]=Constant(null, typeof(CustomerDTO2)),
                    e[7]=MemberInit((NewExpression)(
                        e[8]=New( // 0 args
                        typeof(CustomerDTO2).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])), 
                        Bind(
                        typeof(CustomerDTO2).GetTypeInfo().GetDeclaredProperty("Id"), 
                        e[9]=Property(
                            p[0 // (CustomerWithPrivateProperty whenmappingprivatefieldsandproperties_customerwithprivateproperty__61071393)
                            ],
                            typeof(CustomerWithPrivateProperty).GetTypeInfo().GetDeclaredProperty("Id"))), 
                        Bind(
                        typeof(CustomerDTO2).GetTypeInfo().GetDeclaredProperty("Name"), 
                        e[10]=Property(
                            p[0 // (CustomerWithPrivateProperty whenmappingprivatefieldsandproperties_customerwithprivateproperty__61071393)
                            ],
                            typeof(CustomerWithPrivateProperty).GetTypeInfo().GetDeclaredProperty("Name")))),
                    typeof(CustomerDTO2))),
                p[1 // (object object__47154838)
                    ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");

            var customerId = 1;
            var customerName = "Customer 1";
            var customer = new CustomerWithPrivateProperty(customerId, customerName);
            var dto1 = fs(customer);
            Assert.AreEqual(customerName, dto1.Name);
            Assert.AreEqual(customerId,   dto1.Id);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");
            var dto2 = ff(customer);
            Assert.AreEqual(customerName, dto2.Name);
            Assert.AreEqual(customerId,   dto2.Id);
        }

        [Test]
        public void Test_301_MemberInit_InternalProperty()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[8]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels
            var expr = Lambda<System.Func<SimplePoco, SimpleDto>>( //$
                e[0]=Condition(
                    e[1]=MakeBinary(ExpressionType.Equal,
                    p[0]=Parameter(typeof(SimplePoco)),
                    e[2]=Constant(null, typeof(SimplePoco))),
                    e[3]=Constant(null, typeof(SimpleDto)),
                    e[4]=MemberInit((NewExpression)(
                    e[5]=New( // 0 args
                        typeof(SimpleDto).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])), 
                    Bind(
                        typeof(SimpleDto).GetTypeInfo().GetDeclaredProperty("Id"), 
                        e[6]=Property(
                        p[0 // (SimplePoco whenexplicitmappingrequired_simplepoco__63821092)
                            ],
                        typeof(SimplePoco).GetTypeInfo().GetDeclaredProperty("Id"))), 
                    Bind(
                        typeof(SimpleDto).GetTypeInfo().GetDeclaredProperty("Name"), 
                        e[7]=Property(
                        p[0 // (SimplePoco whenexplicitmappingrequired_simplepoco__63821092)
                            ],
                        typeof(SimplePoco).GetTypeInfo().GetDeclaredProperty("Name")))),
                    typeof(SimpleDto)),
                p[0 // (SimplePoco whenexplicitmappingrequired_simplepoco__63821092)
                    ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};
            var dto1 = fs(poco);
            Assert.AreEqual(poco.Name, dto1.Name);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");
            var dto2 = ff(poco);
            Assert.AreEqual(poco.Name, dto2.Name);
        }

        [Test]
        public void Test_301_Goto_to_label_with_default_value_should_not_return_when_followup_expression_is_present_Custom_constant_output()
        {
            var labelTarget = Label();
            var expr = Lambda<Func<Post>>(Block(typeof(Post), new ParameterExpression[0],
                Goto(labelTarget),
                Label(labelTarget, Constant(new Post { Secret = "a" })),
                Constant(new Post { Secret = "b" })
            ));

            expr.PrintCSharp(x => x.Value is Post p ? $@"new Post {{ Secret = ""{p.Secret}"" }}" : null);

            var s = expr.ToExpressionString(x => x.Value is Post p ? $@"new Post {{ Secret = ""{p.Secret}"" }}" : null);

            var fs = expr.CompileSys();
            fs.PrintIL("sys");
            var n1 = fs();
            Assert.AreEqual("b", n1.Secret);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");
            var n2 = ff();
            Assert.AreEqual("b", n2.Secret);
        }

        [Test]
        public void Test_301_Goto_to_label_with_default_value_should_not_return_when_followup_expression_is_present()
        {
            var labelTarget = Label();
            var expr = Lambda<Func<int>>(Block(typeof(int), new ParameterExpression[0],
                Goto(labelTarget),
                Label(labelTarget, Constant(33)),
                Constant(42)
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");
            var n1 = fs();
            Assert.AreEqual(42, n1);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");
            var n2 = ff();
            Assert.AreEqual(42, n2);
        }

        [Test]
        public void Test_301_Goto_to_label_with_default_value_should_return_the_goto_value_when_no_other_expressions_is_present()
        {
            var labelTarget = Label(typeof(int));
            var expr = Lambda<Func<int>>(Block(typeof(int), new ParameterExpression[0],
                Goto(labelTarget, Constant(22)),
                Label(labelTarget, Constant(33))
            ));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");
            var n1 = fs();
            Assert.AreEqual(22, n1);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");
            var n2 = ff();
            Assert.AreEqual(22, n2);
        }

        public class Post
        {
            public IDictionary<string,string> Dic { get; set; }
            public string Secret { get; set; }
        }

        [Test]
        public void Test_301_Invoke_Lambda_inlining_case_simplified()
        {
            var postPar = Parameter(typeof(Post), "post");
            var dictPar = Parameter(typeof(IDictionary<string,string>), "dict");
            var returnTarget = Label(typeof(IDictionary<string,string>));
            var expr = Lambda<Func<Post, Post>>(
                Block(typeof(Post), new ParameterExpression[0],
                    Assign(
                        Property(postPar, nameof(Post.Dic)),
                        Invoke(
                            Lambda<Func<IDictionary<string,string>, IDictionary<string,string>>>(
                                Block(
                                    Return(returnTarget, dictPar),
                                    Label (returnTarget, Constant(null, typeof(IDictionary<string,string>)))
                                ),
                                dictPar
                            ),
                            Property(postPar, nameof(Post.Dic))
                        )
                    ),
                    postPar
                ),
                postPar
            );

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");

            var p1 = new Post
            {
                Dic = new Dictionary<string, string>
                {
                    { "Secret", "test" } 
                }
            };
            var post1 = fs(p1);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");

            var post2 = ff(p1);
        }

        [Test]
        public void Test_301_Invoke_Lambda_inlining_case()
        {
            var f = (Func<Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post, Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post, Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post>)((
                Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post issue300_bad_label_content_in_ilgenerator_in_the_mapster_benchmark_with_fec_v3_post__1707556, 
                Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post issue300_bad_label_content_in_ilgenerator_in_the_mapster_benchmark_with_fec_v3_post__15368010) => //$
            {
                Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post result;
                
                if (issue300_bad_label_content_in_ilgenerator_in_the_mapster_benchmark_with_fec_v3_post__1707556 == null)
                {
                    return null;
                }
                
                result = issue300_bad_label_content_in_ilgenerator_in_the_mapster_benchmark_with_fec_v3_post__15368010 ?? 
                    new Issue300_Bad_label_content_in_ILGenerator_in_the_Mapster_benchmark_with_FEC_V3.Post();
                result.Dic = new Func<IDictionary<string, string>, IDictionary<string, string>, IDictionary<string, string>>(
                    (Func<IDictionary<string, string>, IDictionary<string, string>, IDictionary<string, string>>)((
                        IDictionary<string, string> idictionary_string_string___32347029, 
                        IDictionary<string, string> idictionary_string_string___22687807) => //$
                    {
                        IDictionary<string, string> result;
                        
                        if (idictionary_string_string___32347029 == null)
                        {
                            return null;
                        }

                        result = idictionary_string_string___22687807 ?? 
                            new Dictionary<string, string>();
                        result["Secret"] = null;
                        
                        if (object.ReferenceEquals(
                            idictionary_string_string___32347029,
                            result))
                        {
                            return result;
                        }

                        IEnumerator<KeyValuePair<string, string>> enumerator;
                        enumerator = idictionary_string_string___32347029.GetEnumerator();
                        
                        while (true)
                        {
                            if (enumerator.MoveNext())
                            {
                                KeyValuePair<string, string> kvp;
                                kvp = enumerator.Current;
                                string key;
                                key = kvp.Key;
                                result[key] = kvp.Value;
                            }
                            else
                            {
                                goto LoopBreak;
                            }
                        }
                        LoopBreak: 
                        idictionary_string_string___2863675:
                        return result;
                        idictionary_string_string___25773083:
                        return null;
                    })).Invoke(
                    issue300_bad_label_content_in_ilgenerator_in_the_mapster_benchmark_with_fec_v3_post__1707556.Dic,
                    result.Dic);
                result.Secret = null;
                return result;
                issue300_bad_label_content_in_ilgenerator_in_the_mapster_benchmark_with_fec_v3_post__30631159:
                return null;
            });

            var p = new ParameterExpression[9]; // the parameter expressions 
            var e = new Expression[62]; // the unique expressions 
            var l = new LabelTarget[4]; // the labels 
            var expr = Lambda<System.Func<Post, Post, Post>>( //$
                e[0]=Block(
                    typeof(Post),
                    new[] {
                    p[0]=Parameter(typeof(Post), "result")
                    },
                    e[1]=Condition(
                    e[2]=MakeBinary(ExpressionType.Equal,
                        p[1]=Parameter(typeof(Post)),
                        e[3]=Constant(null, typeof(Post))),
                    e[4]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                        l[0]=Label(typeof(Post)),
                        e[5]=Constant(null, typeof(Post)),
                        typeof(void)),
                    e[6]=Empty(),
                    typeof(void)), 
                    e[7]=MakeBinary(ExpressionType.Assign,
                    p[0 // (Post result)
                        ],
                    e[8]=Coalesce(
                        p[2]=Parameter(typeof(Post)),
                        e[9]=New( // 0 args
                        typeof(Post).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))), 
                    e[10]=Block(
                    typeof(string),
                    new ParameterExpression[0], 
                    e[11]=MakeBinary(ExpressionType.Assign,
                        e[12]=Property(
                        p[0 // (Post result)
                            ],
                        typeof(Post).GetTypeInfo().GetDeclaredProperty("Dic")),
                        e[13]=Invoke(
                        e[14]=Lambda( //$
                            typeof(System.Func<System.Collections.Generic.IDictionary<string, string>, System.Collections.Generic.IDictionary<string, string>, System.Collections.Generic.IDictionary<string, string>>),
                            e[15]=Block(
                            typeof(System.Collections.Generic.IDictionary<string, string>),
                            new[] {
                            p[3]=Parameter(typeof(System.Collections.Generic.IDictionary<string, string>), "result")
                            },
                            e[16]=Condition(
                                e[17]=MakeBinary(ExpressionType.Equal,
                                p[4]=Parameter(typeof(System.Collections.Generic.IDictionary<string, string>)),
                                e[18]=Constant(null, typeof(System.Collections.Generic.IDictionary<string, string>))),
                                e[19]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                                l[1]=Label(typeof(System.Collections.Generic.IDictionary<string, string>)),
                                e[20]=Constant(null, typeof(System.Collections.Generic.IDictionary<string, string>)),
                                typeof(void)),
                                e[6 // Default of void
                                ],
                                typeof(void)), 
                            e[21]=MakeBinary(ExpressionType.Assign,
                                p[3 // (System.Collections.Generic.IDictionary<string, string> result)
                                ],
                                e[22]=Coalesce(
                                p[5]=Parameter(typeof(System.Collections.Generic.IDictionary<string, string>)),
                                e[23]=New( // 0 args
                                    typeof(System.Collections.Generic.Dictionary<string, string>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]))), 
                            e[24]=Block(
                                typeof(System.Collections.Generic.IDictionary<string, string>),
                                new ParameterExpression[0], 
                                e[25]=Block(
                                typeof(string),
                                new ParameterExpression[0], 
                                e[26]=MakeBinary(ExpressionType.Assign,
                                    e[27]=MakeIndex(
                                    p[3 // (System.Collections.Generic.IDictionary<string, string> result)
                                        ], 
                                    typeof(System.Collections.Generic.IDictionary<string, string>).GetTypeInfo().GetDeclaredProperty("Item"), new Expression[] {
                                    e[28]=Constant("Secret")}),
                                    e[29]=Constant(null, typeof(string)))), 
                                e[30]=Condition(
                                e[31]=Call(
                                    null, 
                                    typeof(object).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReferenceEquals" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object), typeof(object) })),
                                    p[4 // (System.Collections.Generic.IDictionary<string, string> idictionary_string, string___28691996)
                                    ], 
                                    p[3 // (System.Collections.Generic.IDictionary<string, string> result)
                                    ]),
                                e[32]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                                    l[2]=Label(typeof(System.Collections.Generic.IDictionary<string, string>)),
                                    p[3 // (System.Collections.Generic.IDictionary<string, string> result)
                                    ],
                                    typeof(void)),
                                e[6 // Default of void
                                    ],
                                typeof(void)), 
                                e[33]=Block(
                                typeof(void),
                                new[] {
                                p[6]=Parameter(typeof(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>>), "enumerator")
                                },
                                e[34]=MakeBinary(ExpressionType.Assign,
                                    p[6 // (System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>> enumerator)
                                    ],
                                    e[35]=Call(
                                    p[4 // (System.Collections.Generic.IDictionary<string, string> idictionary_string, string___28691996)
                                        ], 
                                    typeof(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetEnumerator" && x.GetParameters().Length == 0))), 
                                e[36]=Loop(
                                    e[37]=Condition(
                                    e[38]=Call(
                                        p[6 // (System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>> enumerator)
                                        ], 
                                        typeof(System.Collections.IEnumerator).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "MoveNext" && x.GetParameters().Length == 0)),
                                    e[39]=Block(
                                        typeof(string),
                                        new[] {
                                        p[7]=Parameter(typeof(System.Collections.Generic.KeyValuePair<string, string>), "kvp")
                                        },
                                        e[40]=MakeBinary(ExpressionType.Assign,
                                        p[7 // ([struct] System.Collections.Generic.KeyValuePair<string, string> kvp)
                                            ],
                                        e[41]=Property(
                                            p[6 // (System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>> enumerator)
                                            ],
                                            typeof(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>>).GetTypeInfo().GetDeclaredProperty("Current"))), 
                                        e[42]=Block(
                                        typeof(string),
                                        new[] {
                                        p[8]=Parameter(typeof(string), "key")
                                        },
                                        e[43]=MakeBinary(ExpressionType.Assign,
                                            p[8 // (string key)
                                            ],
                                            e[44]=Property(
                                            p[7 // ([struct] System.Collections.Generic.KeyValuePair<string, string> kvp)
                                                ],
                                            typeof(System.Collections.Generic.KeyValuePair<string, string>).GetTypeInfo().GetDeclaredProperty("Key"))), 
                                        e[45]=MakeBinary(ExpressionType.Assign,
                                            e[46]=MakeIndex(
                                            p[3 // (System.Collections.Generic.IDictionary<string, string> result)
                                                ], 
                                            typeof(System.Collections.Generic.IDictionary<string, string>).GetTypeInfo().GetDeclaredProperty("Item"), new Expression[] {
                                            p[8 // (string key)
                                                ]}),
                                            e[47]=Property(
                                            p[7 // ([struct] System.Collections.Generic.KeyValuePair<string, string> kvp)
                                                ],
                                            typeof(System.Collections.Generic.KeyValuePair<string, string>).GetTypeInfo().GetDeclaredProperty("Value"))))),
                                    e[48]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Break,
                                        l[3]=Label(typeof(void), "LoopBreak"),
                                        null,
                                        typeof(void)),
                                    typeof(void)),
                                    l[3 // (LoopBreak)
                                    ])), 
                                e[49]=Label(l[2 // (idictionary_string, string___64204788)
                                ],
                                e[50]=Constant(null, typeof(System.Collections.Generic.IDictionary<string, string>)))), 
                            e[51]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                                l[1 // (idictionary_string, string___47410583)
                                ],
                                p[3 // (System.Collections.Generic.IDictionary<string, string> result)
                                ],
                                typeof(void)), 
                            e[52]=Label(l[1 // (idictionary_string, string___47410583)
                                ],
                                e[53]=Constant(null, typeof(System.Collections.Generic.IDictionary<string, string>)))),
                            p[4 // (System.Collections.Generic.IDictionary<string, string> idictionary_string, string___28691996)
                            ], 
                            p[5 // (System.Collections.Generic.IDictionary<string, string> idictionary_string, string___44280823)
                            ]),
                        e[54]=Property(
                            p[1 // (Post post__37883691)
                            ],
                            typeof(Post).GetTypeInfo().GetDeclaredProperty("Dic")), 
                        e[55]=Property(
                            p[0 // (Post result)
                            ],
                            typeof(Post).GetTypeInfo().GetDeclaredProperty("Dic")))), 
                    e[56]=MakeBinary(ExpressionType.Assign,
                        e[57]=Property(
                        p[0 // (Post result)
                            ],
                        typeof(Post).GetTypeInfo().GetDeclaredProperty("Secret")),
                        e[58]=Constant(null, typeof(string)))), 
                    e[59]=MakeGoto(System.Linq.Expressions.GotoExpressionKind.Return,
                    l[0 // (post__29732978)
                    ],
                    p[0 // (Post result)
                        ],
                    typeof(void)), 
                    e[60]=Label(l[0 // (post__29732978)
                    ],
                    e[61]=Constant(null, typeof(Post)))),
                p[1 // (Post post__37883691)
                    ], 
                p[2 // (Post post__27610371)
                    ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");

            var p1 = new Post
            {
                Secret = "Test", 
                Dic = new Dictionary<string, string>
                {
                    { "Secret", "test" }, 
                    {"B", "test2" }
                }
            };

            var post1 = fs(p1, p1);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");

            var post2 = ff(p1, p1);
        }

        [Test]
        public void Test_301_Dictionary_case()
        {
            var f = (Func<object, Dictionary<string, object>>)((object object__58225482) => //$
            {
                SimplePoco simplepoco__54267293;
                simplepoco__54267293 = ((SimplePoco)object__58225482);
                return simplepoco__54267293 == null ?
                    null :
                    new Dictionary<string, object>()
                    {
                        {"Id", ((object)simplepoco__54267293.Id)}, 
                        {"Name", simplepoco__54267293.Name == null ?
                            null :
                            ((object)simplepoco__54267293.Name)}
                    };
            });

            var p = new ParameterExpression[2]; // the parameter expressions 
            var e = new Expression[19]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 

            var expr = Lambda<Func<object, Dictionary<string, object>>>( //$
            e[0]=Block(
                typeof(Dictionary<string, object>),
                new[] {
                p[0]=Parameter(typeof(SimplePoco))
                },
                e[1]=MakeBinary(ExpressionType.Assign,
                p[0 // (SimplePoco simplepoco__61450107)
                    ],
                e[2]=Convert(
                    p[1]=Parameter(typeof(object)),
                    typeof(SimplePoco))), 
                e[3]=Condition(
                e[4]=MakeBinary(ExpressionType.Equal,
                    p[0 // (SimplePoco simplepoco__61450107)
                    ],
                    e[5]=Constant(null, typeof(SimplePoco))),
                e[6]=Constant(null, typeof(Dictionary<string, object>)),
                e[7]=ListInit((NewExpression)(
                    e[8]=New( // 0 args
                    typeof(Dictionary<string, object>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])), 
                    ElementInit(
                    typeof(IDictionary<string, object>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(object) })), 
                    e[9]=Constant("Id"), 
                    e[10]=Convert(
                    e[11]=Property(
                        p[0 // (SimplePoco simplepoco__61450107)
                        ],
                        typeof(SimplePoco).GetTypeInfo().GetDeclaredProperty("Id")),
                    typeof(object))), 
                    ElementInit(
                    typeof(IDictionary<string, object>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string), typeof(object) })), 
                    e[12]=Constant("Name"), 
                    e[13]=Condition(
                    e[14]=MakeBinary(ExpressionType.Equal,
                        e[15]=Property(
                        p[0 // (SimplePoco simplepoco__61450107)
                            ],
                        typeof(SimplePoco).GetTypeInfo().GetDeclaredProperty("Name")),
                        e[16]=Constant(null, typeof(string))),
                    e[17]=Constant(null),
                    e[18]=Convert(
                        e[15 // MemberAccess of string
                        ],
                        typeof(object)),
                    typeof(object)))),
                typeof(Dictionary<string, object>))),
            p[1 // (object object__36425974)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL("sys");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var dict = fs(poco);
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(poco.Id, dict["Id"]);
            Assert.AreEqual(poco.Name, dict["Name"]);

            var ff = expr.CompileFast(true);
            ff.PrintIL("fec");

            var dict1 = ff(poco);
            Assert.AreEqual(2, dict1.Count);
            Assert.AreEqual(poco.Id, dict1["Id"]);
            Assert.AreEqual(poco.Name, dict1["Name"]);
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; internal set; }
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