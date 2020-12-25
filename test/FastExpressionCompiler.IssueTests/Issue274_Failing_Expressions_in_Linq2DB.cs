using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Globalization;
using NUnit.Framework;
#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue274_Failing_Expressions_in_Linq2DB : ITest
    {
        public int Run()
        {
            Test_287_Case2_SimpleDelegate_as_nested_lambda_in_TesCollection_test();

            Test_287_Case1_ConvertTests_NullableParameterInOperatorConvert_VerificationException();

            Test_283_Case6_MappingSchemaTests_CultureInfo_VerificationException();
            Test_283_Case5_ConvertTests_NullableIntToNullableEnum_NullReferenceException();
            Test_283_Case4_SecurityVerificationException();
            Test_283_Case3_SecurityVerificationException();
            Test_283_Case2_NullRefException();
            Test_283_Case2_Minimal_NullRefException();

            Test_case_4_simplified_InvalidCastException();

            Test_case_4_Full_InvalidCastException();
            Test_case_3_Full_NullReferenceException();
            Test_case_2_Full_ExecutionEngineException();
            Test_case_1_Minimal_compare_nullable_with_null_conditional();
            Test_case_1_Minimal_compare_nullable_returned_by_the_method_with_null_conditional();
            Test_case_1_Minimal_compare_nullable_with_null_conditional_and_nested_conditional();
            Test_case_1_Full_AccessViolationException();
            The_expression_with_anonymous_class_should_output_without_special_symbols();

            return 17;
        }

        [Test]
        public void The_expression_with_anonymous_class_should_output_without_special_symbols()
        {
            int? fortyTwo = 42;
            var e = Lambda<Func<int?>>(
                PropertyOrField(Constant(new { X = fortyTwo }), "X"));

            var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);

            var de = f.Target as IDelegateDebugInfo;
            Assert.IsNotNull(de.Expression);

            Assert.IsNotNull(de.ExpressionString);
            StringAssert.DoesNotContain("<>", de.ExpressionString);

            Assert.IsNotNull(de.CSharpString);
            StringAssert.DoesNotContain("<>", de.CSharpString);
        }

        [Test]
        public void Test_case_1_Minimal_compare_nullable_with_null_conditional()
        {
            var p = Parameter(typeof(int?), "i");
            var e = Lambda<Func<int?, int?>>(
                Condition(Equal(p, Constant(null, typeof(int?))), Constant(null, typeof(int?)), Convert(Constant(100), typeof(int?))), p);

            var fs = e.CompileSys();
            fs.PrintIL();

            var f = e.CompileFast(true);
            f.PrintIL();

            Assert.AreEqual(100, f(42));
        }

        [Test]
        public void Test_case_1_Minimal_compare_nullable_returned_by_the_method_with_null_conditional()
        {
            var p = Parameter(typeof(int?), "i");
            var e = Lambda<Func<int?, int?>>(
                Condition(
                  Equal(Call(GetType().GetMethod(nameof(CheckNullable)), p),
                  Constant(null, typeof(int?))),
                  Constant(null, typeof(int?)),
                  Convert(Constant(100), typeof(int?))), p);

            var fs = e.CompileSys();
            fs.PrintIL();

            var f = e.CompileFast(true);
            f.PrintIL();

            Assert.AreEqual(100, f(42));
        }

        [Test]
        public void Test_case_1_Minimal_compare_nullable_with_null_conditional_and_nested_conditional()
        {
            var i = Parameter(typeof(int?), "i");
            var e = Lambda<Func<int?, int?>>(
                Condition(
                    Equal(i, Constant(null, typeof(int?))),
                    Constant(null, typeof(int?)),
                    Condition(
                        Equal(Call(GetType().GetMethod(nameof(CheckNullable)), i), Constant(null, typeof(int?))),
                        Constant(null, typeof(int?)),
                        Convert(Constant(100), typeof(int?)))
                ), i);

            var fs = e.CompileSys();
            fs.PrintIL();
            Assert.AreEqual(100, fs(42));

            var f = e.CompileFast(true);
            f.PrintIL();
            Assert.AreEqual(100, f(42));
        }

        public static int? CheckNullable(int? i) => 5;

        [Test]
        public void Test_case_1_Full_AccessViolationException()
        {
            var p = new ParameterExpression[10]; // the paramiter expressions i
            var e = new Expression[30]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda( // $
              typeof(Func<IQueryRunner, IDataReader, f__AnonymousType142<Nullable<int>>>),
              e[0] = Invoke(
                e[1] = Lambda( // $
                  typeof(Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], object[], f__AnonymousType142<Nullable<int>>>),
                  e[2] = Block(
                    typeof(f__AnonymousType142<Nullable<int>>),
                    new[] {
                        p[0]=Parameter(typeof(SQLiteDataReader), "ldr"),
                        p[1]=Parameter(typeof(f__AnonymousType142<Nullable<int>>))
                    },
                    e[3] = MakeBinary(ExpressionType.Assign,
                      p[0 // (SQLiteDataReader ldr)
                        ],
                      e[4] = Convert(
                        p[2] = Parameter(typeof(IDataReader), "dr"),
                        typeof(SQLiteDataReader))),
                    e[5] = MakeBinary(ExpressionType.Assign,
                      p[1 // (f__AnonymousType142<Nullable<int>> __f__AnonymousType142_Nullable_int____12662012)
                        ],
                      e[6] = New(/*1 args*/
                        typeof(f__AnonymousType142<Nullable<int>>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                        e[7] = Condition(
                          e[8] = MakeBinary(ExpressionType.NotEqual,
                            e[9] = Condition(
                              e[10] = Call(
                                p[0 // (SQLiteDataReader ldr)
                                  ],
                                typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "IsDBNull" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                e[11] = Constant((int)0)),
                              e[12] = Constant(null, typeof(Nullable<int>)),
                              e[13] = New(/*1 args*/
                                typeof(Nullable<int>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                                e[14] = Call(
                                  p[0 // (SQLiteDataReader ldr)
                                    ],
                                  typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetInt32" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                  e[15] = Constant((int)0))),
                              typeof(Nullable<int>)),
                            e[16] = Constant(null, typeof(Nullable<int>))),
                          e[17] = Condition(
                            e[18] = Call(
                              p[0 // (SQLiteDataReader ldr)
                                ],
                              typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "IsDBNull" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                              e[19] = Constant((int)0)),
                            e[20] = Constant(null, typeof(Nullable<int>)),
                            e[21] = New(/*1 args*/
                              typeof(Nullable<int>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                              e[22] = Call(
                                p[0 // (SQLiteDataReader ldr)
                                  ],
                                typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetInt32" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                e[23] = Constant((int)0))),
                            typeof(Nullable<int>)),
                          e[24] = Convert(
                            e[25] = Constant((int)100),
                            typeof(Nullable<int>)),
                          typeof(Nullable<int>)))),
                    p[1 // (f__AnonymousType142<Nullable<int>> __f__AnonymousType142_Nullable_int____12662012)
                      ]),
                  p[3] = Parameter(typeof(IQueryRunner), "qr"),
                  p[4] = Parameter(typeof(IDataContext), "dctx"),
                  p[5] = Parameter(typeof(IDataReader), "rd"),
                  p[6] = Parameter(typeof(System.Linq.Expressions.Expression), "expr"),
                  p[7] = Parameter(typeof(object[]), "ps"),
                  p[8] = Parameter(typeof(object[]), "preamble")),
                p[9] = Parameter(typeof(IQueryRunner), "qr"),
                e[26] = Property(
                  p[9 // (IQueryRunner qr)
                    ],
                  typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("DataContext")),
                p[2 // (IDataReader dr)
                  ],
                e[27] = Property(
                  p[9 // (IQueryRunner qr)
                    ],
                  typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Expression")),
                e[28] = Property(
                  p[9 // (IQueryRunner qr)
                    ],
                  typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Parameters")),
                e[29] = Property(
                  p[9 // (IQueryRunner qr)
                    ],
                  typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Preambles"))),
              p[9 // (IQueryRunner qr)
                ],
              p[2 // (IDataReader dr)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true, CompilerFlags.Default);
            f.PrintIL();
        }

        [Test]
        public void Test_case_2_Full_ExecutionEngineException()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[13]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<Nullable<Enum15>, Nullable<int>>>( // $
              e[0] = Condition(
                e[1] = Property(
                  p[0] = Parameter(typeof(Nullable<Enum15>), "p"),
                  typeof(Nullable<Enum15>).GetTypeInfo().GetDeclaredProperty("HasValue")),
                e[2] = Switch(
                  e[3] = Convert(
                    p[0 // (Nullable<Enum15> p)
                      ],
                    typeof(Enum15)),
                  e[4] = Convert(
                    e[5] = Call(
                      null,
                      typeof(ConvertBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "ConvertDefault" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object), typeof(Type) })),
                      e[6] = Convert(
                        e[3 // Convert of Enum15
                          ],
                        typeof(object)),
                      e[7] = Constant(typeof(Nullable<int>))),
                    typeof(Nullable<int>)),
                  SwitchCase(
                  e[8] = Constant((int)10, typeof(Nullable<int>)),
                  e[9] = Constant(Enum15.AA)),
                  SwitchCase(
                  e[10] = Constant((int)20, typeof(Nullable<int>)),
                  e[11] = Constant(Enum15.BB))),
                e[12] = Constant(null, typeof(Nullable<int>)),
                typeof(Nullable<int>)),
              p[0 // (Nullable<Enum15> p)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual(10, fs(Enum15.AA));
            Assert.AreEqual(20, fs(Enum15.BB));
            Assert.Throws<InvalidOperationException>(() =>
              fs((Enum15)3));

            var f = expr.CompileFast(true);
            f.PrintIL();
            Assert.AreEqual(10, f(Enum15.AA));
            Assert.AreEqual(20, f(Enum15.BB));
            Assert.Throws<InvalidOperationException>(() =>
              f((Enum15)3));
        }

        [Test]
        public void Test_case_3_Full_NullReferenceException()
        {
            var p = new ParameterExpression[9]; // the parameter expressions 
            var e = new Expression[23]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<IQueryRunner, IDataReader, IGrouping<bool, Customer>>>(
              e[0] = Invoke(
              e[1] = Lambda( // $
                typeof(Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], object[], IGrouping<bool, Customer>>),
                e[2] = Block(
                    typeof(IGrouping<bool, Customer>),
                    new[] {
                    p[0]=Parameter(typeof(SQLiteDataReader), "ldr")
                    },
                  e[3] = MakeBinary(ExpressionType.Assign,
                    p[0 // (SQLiteDataReader ldr)
                    ],
                    e[4] = Convert(
                    p[1] = Parameter(typeof(IDataReader), "dr"),
                    typeof(SQLiteDataReader))),
                  e[5] = Call(
                    null,
                    typeof(GroupByBuilder.GroupByContext.GroupByHelper<bool, Customer, ExpressionBuilder.GroupSubQuery<bool, Customer>>).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                      .Single(x => !x.IsGenericMethod && x.Name == "GetGrouping" &&
                        x.GetParameters().Select(y => y.ParameterType)
                        .SequenceEqual(new[] {
                            typeof(IQueryRunner),
                            typeof(IDataContext),
                            typeof(IDataReader),
                            typeof(List<ParameterAccessor>),
                            typeof(System.Linq.Expressions.Expression),
                            typeof(object[]),
                            typeof(Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], bool>),
                            typeof(Func<IDataContext, bool, object[], IQueryable<Customer>>) })),
                    p[2] = Parameter(typeof(IQueryRunner), "qr"),
                    p[3] = Parameter(typeof(IDataContext), "dctx"),
                    p[4] = Parameter(typeof(IDataReader), "rd"),
                    e[6] = Constant(new List<ParameterAccessor>()),
                    p[5] = Parameter(typeof(System.Linq.Expressions.Expression), "expr"),
                    p[6] = Parameter(typeof(object[]), "ps"),
                    e[7] = Lambda( // $
                      typeof(Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], bool>),
                      e[8] = Condition(
                        e[9] = Call(
                        p[0 // (SQLiteDataReader ldr)
                          ],
                        typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "IsDBNull" &&
                          x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                        e[10] = Constant((int)0)),
                        e[11] = Constant(false),
                        e[12] = Convert(
                        e[13] = Call(
                          null,
                          typeof(ConvertBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "ConvertDefault" &&
                              x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object), typeof(Type) })),
                          e[14] = Convert(
                          e[15] = Call(
                            p[0 // (SQLiteDataReader ldr)
                            ],
                            typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetInt64" &&
                              x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                            e[16] = Constant((int)0)),
                          typeof(object)),
                          e[17] = Constant(typeof(bool))),
                        typeof(bool)),
                      typeof(bool)),
                      p[2 // (IQueryRunner qr)
                        ],
                      p[3 // (IDataContext dctx)
                        ],
                      p[4 // (IDataReader rd)
                        ],
                      p[5 // (System.Linq.Expressions.Expression expr)
                        ],
                      p[6 // (object[] ps)
                        ]),
                    e[18] = Constant(null, typeof(Func<IDataContext, bool, object[], IQueryable<Customer>>)))),
                p[2 // (IQueryRunner qr)
                ],
                p[3 // (IDataContext dctx)
                ],
                p[4 // (IDataReader rd)
                ],
                p[5 // (System.Linq.Expressions.Expression expr)
                ],
                p[6 // (object[] ps)
                ],
                p[7] = Parameter(typeof(object[]), "preamble")),
              p[8] = Parameter(typeof(IQueryRunner), "qr"),
              e[19] = Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("DataContext")),
              p[1 // (IDataReader dr)
                ],
              e[20] = Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Expression")),
              e[21] = Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Parameters")),
              e[22] = Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Preambles"))),
              p[8 // (IQueryRunner qr)
              ],
              p[1 // (IDataReader dr)
              ]);

            expr.PrintCSharp();
            var reExpr = expr.ToExpressionString();
            StringAssert.Contains("!!!", reExpr);

            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true);
            f.PrintIL();

            Assert.DoesNotThrow(() =>
              f(new NullQueryRunner(), new SQLiteDataReader()));
        }

        public class Customer
        {
        }

        class ParameterAccessor
        {
        }

        class ExpressionBuilder
        {
            public class GroupSubQuery<TKey, TElement> { }
        }

        class GroupByBuilder
        {
            internal class GroupByContext
            {
                internal class GroupByHelper<TKey, TElement, TSource>
                {
                    internal static IGrouping<TKey, TElement> GetGrouping(
                        IQueryRunner runner,
                        IDataContext dataContext,
                        IDataReader dataReader,
                        List<ParameterAccessor> parameterAccessor,
                        System.Linq.Expressions.Expression expr,
                        object[] ps,
                        Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], TKey> keyReader,
                        Func<IDataContext, TKey, object[], IQueryable<TElement>> itemReader)
                    {
                        var key = keyReader(runner, dataContext, dataReader, expr, ps);
                        return null;
                    }
                }
            }
        }

        [Test]
        public void Test_case_4_simplified_InvalidCastException()
        {
            var hs = Constant(new Delegate[] { (Action<string>)null });
            var expr = Lambda<Func<SimpleDelegate>>(
                Convert(
                  ArrayIndex(hs, Constant(0)),
                  typeof(SimpleDelegate))
            );

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.IsNull(fs());

            var f = expr.CompileFast(true);
            f.PrintIL();
            Assert.IsNull(f());
        }

        public static void SimpleStringHandler(string s) { }

        [Test]
        public void Test_case_4_Full_InvalidCastException()
        {
            var p = new ParameterExpression[2]; // the parameter expressions 
            var e = new Expression[6]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<object, object>>( // $
                e[0] = Block(
                    typeof(SampleClass),
                    new[] {
                      p[0]=Parameter(typeof(SampleClass))
                    },
                    e[1] = MakeBinary(ExpressionType.Assign,
                        p[0 // (SampleClass sampleclass__14492072)
                    ],
                    e[2] = New(/*2 args*/
                        typeof(SampleClass).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                        p[1] = Parameter(typeof(object)),
                        e[3] = Constant(new Delegate[]
                        {
                          (Func<SampleClass, int>)(x => 42),
                          (Func<SampleClass, int, OtherClass>)((x, i) => new OtherClass()),
                          (Action<SampleClass>)(x => {}), 
                          // default(Func<SampleClass, int, RegularEnum1>), 
                          // default(Func<SampleClass, int, FlagsEnum>), 
                          // default(Func<SampleClass, RegularEnum1, int>), 
                          // default(Func<SampleClass, FlagsEnum, int>), 
                          // default(Func<SampleClass, RegularEnum1>), 
                          // default(Func<SampleClass, FlagsEnum>), 
                          // default(Action<SampleClass, bool>), 
                          // default(Action<SampleClass, RegularEnum1>), 
                          // default(Action<SampleClass, FlagsEnum>), 
                          // default(Func<SampleClass, int, RegularEnum2>), 
                          // default(Func<SampleClass, RegularEnum2, int>), 
                          // default(Func<SampleClass, RegularEnum2>), 
                          // default(Action<SampleClass, RegularEnum2>), 
                          // default(Func<SampleClass, string, string>), 
                          (SimpleDelegate)HandleString,
                          (Func<SampleClass, string, int>)((x, s) => 43)
                          })
                        )
                    ),
                    e[4] = Invoke(
                        e[5] = Constant((Action<SampleClass>)((SampleClass s) =>
                        {
                            ((SimpleDelegate)s.Delegates[3]).Invoke("Hey!");
                        })),
                        p[0 // (SampleClass SampleClass__14492072)
                          ]),
                        p[0 // (SampleClass SampleClass__14492072)
                          ]),
                        p[1 // (object object__42147750)
                          ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            var obj1 = new object();
            var s1 = fs(obj1);
            Assert.IsInstanceOf<SampleClass>(s1);

            var f = expr.CompileFast(true);
            f.PrintIL();
            var obj2 = new object();
            var s2 = f(obj2);
            Assert.IsInstanceOf<SampleClass>(s2);
        }

        [Test]
        public void Test_287_Case2_SimpleDelegate_as_nested_lambda_in_TesCollection_test()
        {
            string gotS = null;
            SimpleDelegate sd = s => gotS = s;

            var p = new ParameterExpression[9]; // the parameter expressions 
            var e = new Expression[63]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Action<Dynamic.SampleClass>>( // $
              e[0] = Block(
                typeof(void),
                new ParameterExpression[0],
                e[1] = Call(
                  p[0] = Parameter(typeof(Dynamic.SampleClass)),
                  typeof(Dynamic.SampleClass).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "add_SimpleDelegateEvent" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Dynamic.SimpleDelegate) })),
                  e[4] = Lambda<Dynamic.SimpleDelegate>(Invoke(
                      Constant(sd, typeof(SimpleDelegate)),
                      p[1] = Parameter(typeof(string), "_")), 
                    p[1]))
                ),
              p[0 // (SampleClass sampleclass__26320983)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            var s = new Dynamic.SampleClass();
            fs(s);
            s.InvokeSimpleDelegateEvent("1");
            Assert.AreEqual("1", gotS);

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            s = new Dynamic.SampleClass();
            fx(s);
            s.InvokeSimpleDelegateEvent("2");
            Assert.AreEqual("2", gotS);
        }

        [Test]
        public void Test_287_Case1_ConvertTests_NullableParameterInOperatorConvert_VerificationException()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[2]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<System.Decimal, CustomMoneyType>>(
              e[0] = Convert(
                e[1] = New(/*1 args*/
                  typeof(System.Nullable<System.Decimal>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                  p[0] = Parameter(typeof(System.Decimal), "p")),
                typeof(CustomMoneyType),
                typeof(CustomMoneyType).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Explicit" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(System.Nullable<System.Decimal>) }))),
              p[0 // ([struct] System.Decimal p)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual(new CustomMoneyType { Amount = 1.11m }, fs(1.11m));

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            Assert.AreEqual(new CustomMoneyType { Amount = 1.11m }, fx(1.11m));
        }

        private struct CustomMoneyType
        {
            public decimal? Amount;
            public static explicit operator CustomMoneyType(decimal? amount) =>
              new CustomMoneyType() { Amount = amount };
        }

        [Test]
        public void Test_283_Case6_MappingSchemaTests_CultureInfo_VerificationException()
        {
            var ci = (CultureInfo)new CultureInfo("ru-RU", false).Clone();

            ci.DateTimeFormat.FullDateTimePattern = "dd.MM.yyyy HH:mm:ss";
            ci.DateTimeFormat.LongDatePattern = "dd.MM.yyyy";
            ci.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            ci.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            ci.DateTimeFormat.ShortTimePattern = "HH:mm:ss";

            var x = new c__DisplayClass41_0();
            x.info = ci;

            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[4]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<DateTime, string>>( // $
              e[0] = Call(
                p[0] = Parameter(typeof(System.DateTime), "v"),
                typeof(System.DateTime).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ToString" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(System.IFormatProvider) })),
                e[1] = Property(
                  e[2] = Field(
                    e[3] = Constant(x, typeof(c__DisplayClass41_0)),
                    typeof(c__DisplayClass41_0).GetTypeInfo().GetDeclaredField("info")),
                  typeof(CultureInfo).GetTypeInfo().GetDeclaredProperty("DateTimeFormat"))),
              p[0 // ([struct] System.DateTime v)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.AreEqual("20.01.2012 16:30:40", fs(new DateTime(2012, 1, 20, 16, 30, 40)));

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            Assert.AreEqual("20.01.2012 16:30:40", fx(new DateTime(2012, 1, 20, 16, 30, 40)));
        }

        class c__DisplayClass41_0
        {
            public System.Globalization.CultureInfo info;
        }

        [Test]
        public void Test_283_Case5_ConvertTests_NullableIntToNullableEnum_NullReferenceException()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[5]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<System.Nullable<int>, System.Nullable<Enum1>>>( // $
              e[0] = Condition(
                e[1] = Property(
                  p[0] = Parameter(typeof(System.Nullable<int>), "p"),
                  typeof(System.Nullable<int>).GetTypeInfo().GetDeclaredProperty("HasValue")),
                e[2] = Convert(
                  e[3] = Property(
                    p[0 // ([struct] System.Nullable<int> p)
                      ],
                    typeof(System.Nullable<int>).GetTypeInfo().GetDeclaredProperty("Value")),
                  typeof(System.Nullable<Enum1>)),
                e[4] = Constant(null, typeof(System.Nullable<Enum1>)),
                typeof(System.Nullable<Enum1>)),
              p[0 // ([struct] System.Nullable<int> p)
                ]);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.That(fs(null), Is.EqualTo(null));

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            Assert.That(fx(null), Is.EqualTo(null));
        }

        public enum Enum1 { X }

        [Test]
        public void Test_283_Case4_SecurityVerificationException()
        {
            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[1]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<System.Func<int, string>>( // $
              e[0] = Call(
                p[0] = Parameter(typeof(int), "p"),
                typeof(int).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ToString" && x.GetParameters().Length == 0)),
              p[0 // (int p)
                ]);

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.That(fs(1), Is.EqualTo("1"));

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            Assert.That(fx(1), Is.EqualTo("1"));
        }

        [Test]
        public void Test_283_Case3_SecurityVerificationException()
        {
            var p = new ParameterExpression[2]; // the parameter expressions 
            var e = new Expression[2]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<SampleClass, int, string>>(
              e[0] = Property(
                e[1] = Call(
                  p[0] = Parameter(typeof(SampleClass), "s"),
                  typeof(SampleClass).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetOther" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  p[1] = Parameter(typeof(int), "i")),
                typeof(OtherClass).GetTypeInfo().GetDeclaredProperty("OtherStrProp")),
              p[0 // (SampleClass s)
                ],
              p[1 // (int i)
                ]);

            expr.PrintCSharp();

            var concrete = new SampleClass { Id = 1, Value = 33 };

            var fs = expr.CompileSys();
            fs.PrintIL();
            Assert.That(fs(concrete, 22), Is.EqualTo("OtherStrValue22"));

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            Assert.That(fx(concrete, 22), Is.EqualTo("OtherStrValue22"));
        }

        [Test]
        public void Test_283_Case2_NullRefException()
        {
            var p = new ParameterExpression[3]; // the parameter expressions 
            var e = new Expression[23]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<System.Linq.Expressions.Expression, IDataContext, object[], object>>( // $
              e[0] = Convert(
                e[1] = Call(
                  null,
                  typeof(ConvertTo<int>).GetMethods().Where(x => x.IsGenericMethod && x.Name == "From" && x.GetGenericArguments().Length == 1)
                    .Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(System.Enum)) : x)
                    .Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(System.Enum) })),
                  e[2] = Convert(
                    e[3] = Field(
                      e[4] = Convert(
                        e[5] = Property(
                          e[6] = Convert(
                            e[7] = Property(
                              e[8] = Convert(
                                e[9] = Property(
                                  e[10] = Convert(
                                    e[11] = Call(
                                      e[12] = Property(
                                        e[13] = Convert(
                                          e[14] = Property(
                                            e[15] = Convert(
                                              e[16] = Property(
                                                e[17] = Convert(
                                                  e[18] = Call(
                                                    e[19] = Property(
                                                      e[20] = Convert(
                                                        p[0] = Parameter(typeof(System.Linq.Expressions.Expression), "expr"),
                                                        typeof(System.Linq.Expressions.MethodCallExpression)),
                                                      typeof(System.Linq.Expressions.MethodCallExpression).GetTypeInfo().GetDeclaredProperty("Arguments")),
                                                    typeof(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "get_Item" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                                    e[21] = Constant((int)1)),
                                                  typeof(System.Linq.Expressions.UnaryExpression)),
                                                typeof(System.Linq.Expressions.UnaryExpression).GetTypeInfo().GetDeclaredProperty("Operand")),
                                              typeof(System.Linq.Expressions.LambdaExpression)),
                                            typeof(System.Linq.Expressions.LambdaExpression).GetTypeInfo().GetDeclaredProperty("Body")),
                                          typeof(System.Linq.Expressions.MethodCallExpression)),
                                        typeof(System.Linq.Expressions.MethodCallExpression).GetTypeInfo().GetDeclaredProperty("Arguments")),
                                      typeof(System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.Expression>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "get_Item" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                      e[22] = Constant((int)0)),
                                    typeof(System.Linq.Expressions.UnaryExpression)),
                                  typeof(System.Linq.Expressions.UnaryExpression).GetTypeInfo().GetDeclaredProperty("Operand")),
                                typeof(System.Linq.Expressions.MemberExpression)),
                              typeof(System.Linq.Expressions.MemberExpression).GetTypeInfo().GetDeclaredProperty("Expression")),
                            typeof(System.Linq.Expressions.ConstantExpression)),
                          typeof(System.Linq.Expressions.ConstantExpression).GetTypeInfo().GetDeclaredProperty("Value")),
                        typeof(c__DisplayClass6_0)),
                      typeof(c__DisplayClass6_0).GetTypeInfo().GetDeclaredField("flag")),
                    typeof(System.Enum))),
                typeof(object)),
              p[0 // (System.Linq.Expressions.Expression expr)
                ],
              p[1] = Parameter(typeof(IDataContext), "dctx"),
              p[2] = Parameter(typeof(object[]), "ps"));

            expr.PrintCSharp();
            /*
            (Func<Expression, Issue274_Failing_Expressions_in_Linq2DB.IDataContext, object[], object>)(
                Expression expr, 
                Issue274_Failing_Expressions_in_Linq2DB.IDataContext dctx, 
                object[] ps) => 
                ((object)Issue274_Failing_Expressions_in_Linq2DB.ConvertTo<int>.From<Enum>(((Enum)((Issue274_Failing_Expressions_in_Linq2DB.c__DisplayClass6_0)((ConstantExpression)((MemberExpression)((UnaryExpression)((MethodCallExpression)((LambdaExpression)((UnaryExpression)((MethodCallExpression)expr).Arguments.Item).Operand).Body).Arguments.Item).Operand).Expression).Value).flag)));
            */

            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true);
            f.PrintIL();
        }

        [Test]
        public void Test_283_Case2_Minimal_NullRefException()
        {
            var p = Parameter(typeof(object), "o");
            var expr = Lambda<Func<object, object>>(
                Convert(
                  Convert(
                    Field(
                      Convert(p, typeof(c__DisplayClass6_0)),
                      typeof(c__DisplayClass6_0).GetField(nameof(c__DisplayClass6_0.flag))),
                      typeof(System.Enum)),
                    typeof(object)),
                p);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true);
            f.PrintIL();
            var flags = f(new c__DisplayClass6_0());
            Assert.AreEqual(FlagsEnum.All, (FlagsEnum)flags);
        }

        [Flags]
        public enum FlagsEnum
        {
            None = 0,

            Flag1 = 0x1,
            Flag2 = 0x2,
            Flag3 = 0x4,

            All = Flag1 | Flag2 | Flag3
        }

        class c__DisplayClass6_0
        {
            public FlagsEnum flag = FlagsEnum.All;
        }

        public delegate void SimpleDelegate(string input);

        public class Dynamic
        {
            public delegate void SimpleDelegate(string input);

            public class SampleClass 
            {
                public event SimpleDelegate SimpleDelegateEvent;

                public void InvokeSimpleDelegateEvent(string s) => SimpleDelegateEvent?.Invoke(s);
            }
        }

        public static void HandleString(string s)
        {

        }

        class TypeWrapper
        {
            public object instance_ { get; }
        }


        class SampleClass : TypeWrapper
        {
            public int Id { get; set; }
            public int Value { get; set; }
            public object Instance;
            public Delegate[] Delegates;

            public OtherClass GetOther(int idx) => new OtherClass { OtherStrProp = "OtherStrValue" + idx };

            public OtherClass GetOtherAnother(int idx) => new OtherClass { OtherStrProp = "OtherAnotherStrValue" + idx };

            private SimpleDelegate _SimpleDelegateEvent;
            public event SimpleDelegate SimpleDelegateEvent
            {
                add => _SimpleDelegateEvent = (SimpleDelegate)Delegate.Combine(_SimpleDelegateEvent, value);
                remove => _SimpleDelegateEvent = (SimpleDelegate)Delegate.Remove(_SimpleDelegateEvent, value);
            }


            public SampleClass(object instance = null, Delegate[] delegates = null)
            {
                Instance = instance;
                Delegates = delegates;
            }
        }
        class OtherClass
        {
            public string OtherStrProp { get; set; }
        }

        enum RegularEnum1 { }
        enum RegularEnum2 { }

        enum Enum15
        {
            AA,
            BB,
        }

        static class ConvertBuilder
        {
            internal static object ConvertDefault(object value, Type conversionType)
            {
                try
                {
                    return System.Convert.ChangeType(value, conversionType, System.Threading.Thread.CurrentThread.CurrentCulture);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot convert value '{value}' to type '{conversionType.FullName}'", ex);
                }
            }
        }

        public static class ConvertTo<TTo>
        {
            public static TTo From<TFrom>(TFrom obj)
            {
                return (TTo)System.Convert.ChangeType(obj, typeof(TTo), System.Threading.Thread.CurrentThread.CurrentCulture);
            }
        }

        public static class Convert<TFrom, TTo>
        {

        }

        interface IDataContext { }

        interface IQueryRunner
        {
            IDataContext DataContext { get; set; }
            System.Linq.Expressions.Expression Expression { get; set; }
            object[] Parameters { get; set; }
            object[] Preambles { get; set; }
        }

        class NullQueryRunner : IQueryRunner
        {
            public IDataContext DataContext { get; set; }
            public System.Linq.Expressions.Expression Expression { get; set; }
            public object[] Parameters { get; set; }
            public object[] Preambles { get; set; }
        }

        class f__AnonymousType142<T>
        {
            public T Value;
            public f__AnonymousType142(T value) => Value = value;
        }

        class SQLiteDataReader : IDataReader
        {
            public object this[int i] => throw new NotImplementedException();

            public object this[string name] => throw new NotImplementedException();

            public int Depth => throw new NotImplementedException();

            public bool IsClosed => throw new NotImplementedException();

            public int RecordsAffected => throw new NotImplementedException();

            public int FieldCount => throw new NotImplementedException();

            public void Close()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean(int i)
            {
                throw new NotImplementedException();
            }

            public byte GetByte(int i)
            {
                throw new NotImplementedException();
            }

            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public char GetChar(int i)
            {
                throw new NotImplementedException();
            }

            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public IDataReader GetData(int i)
            {
                throw new NotImplementedException();
            }

            public string GetDataTypeName(int i)
            {
                throw new NotImplementedException();
            }

            public DateTime GetDateTime(int i)
            {
                throw new NotImplementedException();
            }

            public decimal GetDecimal(int i)
            {
                throw new NotImplementedException();
            }

            public double GetDouble(int i)
            {
                throw new NotImplementedException();
            }

            public Type GetFieldType(int i)
            {
                throw new NotImplementedException();
            }

            public float GetFloat(int i)
            {
                throw new NotImplementedException();
            }

            public Guid GetGuid(int i)
            {
                throw new NotImplementedException();
            }

            public short GetInt16(int i)
            {
                throw new NotImplementedException();
            }

            public int GetInt32(int i)
            {
                throw new NotImplementedException();
            }

            public long GetInt64(int i)
            {
                return (long)i;
            }

            public string GetName(int i)
            {
                throw new NotImplementedException();
            }

            public int GetOrdinal(string name)
            {
                throw new NotImplementedException();
            }

            public DataTable GetSchemaTable()
            {
                throw new NotImplementedException();
            }

            public string GetString(int i)
            {
                throw new NotImplementedException();
            }

            public object GetValue(int i)
            {
                throw new NotImplementedException();
            }

            public int GetValues(object[] values)
            {
                throw new NotImplementedException();
            }

            public bool IsDBNull(int i)
            {
                return false;
            }

            public bool NextResult()
            {
                throw new NotImplementedException();
            }

            public bool Read()
            {
                throw new NotImplementedException();
            }
        }
    }
}