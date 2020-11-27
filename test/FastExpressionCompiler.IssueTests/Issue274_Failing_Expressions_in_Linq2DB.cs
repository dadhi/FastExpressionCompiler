using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
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
            Test_case_4_Full_InvalidCastException();
            Test_case_3_Full_NullReferenceException();
            Test_case_2_Full_ExecutionEngineException();
            Test_case_1_Minimal_compare_nullable_with_null_conditional();
            Test_case_1_Minimal_compare_nullable_returned_by_the_method_with_null_conditional();
            Test_case_1_Minimal_compare_nullable_with_null_conditional_and_nested_conditional();
            Test_case_1_Full_AccessViolationException();
            The_expression_with_anonymous_class_should_output_without_special_symbols();

            return 8;
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
            Assert.AreEqual(10,  fs(Enum15.AA));
            Assert.AreEqual(20,  fs(Enum15.BB));
            Assert.Throws<InvalidOperationException>(() =>
              fs((Enum15)3));

            var f = expr.CompileFast(true);
            f.PrintIL();
            Assert.AreEqual(10,  f(Enum15.AA));
            Assert.AreEqual(20,  f(Enum15.BB));
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
              e[0]=Invoke(
              e[1]=Lambda( // $
                typeof(Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], object[], IGrouping<bool, Customer>>),
                e[2]=Block(
                    typeof(IGrouping<bool, Customer>),
                    new[] {
                    p[0]=Parameter(typeof(SQLiteDataReader), "ldr")
                    },
                  e[3]=MakeBinary(ExpressionType.Assign,
                    p[0 // (SQLiteDataReader ldr)
                    ],
                    e[4]=Convert(
                    p[1]=Parameter(typeof(IDataReader), "dr"),
                    typeof(SQLiteDataReader))),
                  e[5]=Call(
                    null, 
                    typeof(GroupByBuilder.GroupByContext.GroupByHelper<bool, Customer, ExpressionBuilder.GroupSubQuery<bool, Customer>>).GetMethods(BindingFlags.NonPublic|BindingFlags.Static)
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
                    p[2]=Parameter(typeof(IQueryRunner), "qr"),
                    p[3]=Parameter(typeof(IDataContext), "dctx"),
                    p[4]=Parameter(typeof(IDataReader), "rd"),
                    e[6]=Constant(new List<ParameterAccessor>()),
                    p[5]=Parameter(typeof(System.Linq.Expressions.Expression), "expr"),
                    p[6]=Parameter(typeof(object[]), "ps"),
                    e[7]=Lambda( // $
                      typeof(Func<IQueryRunner, IDataContext, IDataReader, System.Linq.Expressions.Expression, object[], bool>),
                      e[8]=Condition(
                        e[9]=Call(
                        p[0 // (SQLiteDataReader ldr)
                          ], 
                        typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "IsDBNull" && 
                          x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                        e[10]=Constant((int)0)),
                        e[11]=Constant(false),
                        e[12]=Convert(
                        e[13]=Call(
                          null, 
                          typeof(ConvertBuilder).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "ConvertDefault" && 
                            x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object), typeof(Type) })),
                          e[14]=Convert(
                          e[15]=Call(
                            p[0 // (SQLiteDataReader ldr)
                            ], 
                            typeof(IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetInt64" && 
                              x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                            e[16]=Constant((int)0)),
                          typeof(object)),
                          e[17]=Constant(typeof(bool))),
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
                    e[18]=Constant(null, typeof(Func<IDataContext, bool, object[], IQueryable<Customer>>)))),
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
                p[7]=Parameter(typeof(object[]), "preamble")),
              p[8]=Parameter(typeof(IQueryRunner), "qr"),
              e[19]=Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("DataContext")),
              p[1 // (IDataReader dr)
                ],
              e[20]=Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Expression")),
              e[21]=Property(
                p[8 // (IQueryRunner qr)
                ],
                typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("Parameters")),
              e[22]=Property(
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
/*
(Func<Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner, IDataReader, IGrouping<bool, Issue274_Failing_Expressions_in_Linq2DB.Customer>>)(
    Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner qr, 
    IDataReader dr) => 
    new Func<Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner, Issue274_Failing_Expressions_in_Linq2DB.IDataContext, IDataReader, Expression, object[], object[], IGrouping<bool, Issue274_Failing_Expressions_in_Linq2DB.Customer>>(
        (Func<Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner, Issue274_Failing_Expressions_in_Linq2DB.IDataContext, IDataReader, Expression, object[], object[], IGrouping<bool, Issue274_Failing_Expressions_in_Linq2DB.Customer>>)(
            Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner qr, 
            Issue274_Failing_Expressions_in_Linq2DB.IDataContext dctx, 
            IDataReader rd, 
            Expression expr, 
            object[] ps, 
            object[] preamble) => 
        {
            Issue274_Failing_Expressions_in_Linq2DB.SQLiteDataReader ldr;
            ldr = ((Issue274_Failing_Expressions_in_Linq2DB.SQLiteDataReader)dr);
            return Issue274_Failing_Expressions_in_Linq2DB.GroupByBuilder.GroupByContext.GroupByHelper<bool, Issue274_Failing_Expressions_in_Linq2DB.Customer, Issue274_Failing_Expressions_in_Linq2DB.ExpressionBuilder.GroupSubQuery<bool, Issue274_Failing_Expressions_in_Linq2DB.Customer>>.GetGrouping(
                qr,
                dctx,
                rd,
                new FastExpressionCompiler.LightExpression.IssueTests.Issue274_Failing_Expressions_in_Linq2DB.ParameterAccessor[]{},
                expr,
                ps,
                (Func<Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner, Issue274_Failing_Expressions_in_Linq2DB.IDataContext, IDataReader, Expression, object[], bool>)(
                    Issue274_Failing_Expressions_in_Linq2DB.IQueryRunner qr, 
                    Issue274_Failing_Expressions_in_Linq2DB.IDataContext dctx, 
                    IDataReader rd, 
                    Expression expr, 
                    object[] ps) => 
                    ldr.IsDBNull((int)0) ?
                        false :
                        ((bool)Issue274_Failing_Expressions_in_Linq2DB.ConvertBuilder.ConvertDefault(
                            ((object)ldr.GetInt64((int)0)),
                            typeof(bool))),
                null);
        })).Invoke(
        qr,
        qr.DataContext,
        dr,
        qr.Expression,
        qr.Parameters,
        qr.Preambles);
*/       
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
            public class GroupSubQuery<TKey,TElement> {}
        }

	      class GroupByBuilder
        {
            internal class GroupByContext 
            {
                internal class GroupByHelper<TKey,TElement,TSource> 
                {
                    internal static IGrouping<TKey,TElement> GetGrouping(
                        IQueryRunner                                                           runner,
                        IDataContext                                                           dataContext,
                        IDataReader                                                            dataReader,
                        List<ParameterAccessor>                                                parameterAccessor,
                        System.Linq.Expressions.Expression                                     expr,
                        object[]                                                               ps,
                        Func<IQueryRunner,IDataContext,IDataReader,System.Linq.Expressions.Expression,object[],TKey>   keyReader,
                        Func<IDataContext,TKey,object[],IQueryable<TElement>>                  itemReader)
                  {
                    var key = keyReader(runner, dataContext, dataReader, expr, ps);
                    return null;
                  }
                }
            }
        }

        [Test]
        public void Test_case_4_Full_InvalidCastException()
        {
          var p = new ParameterExpression[2]; // the parameter expressions 
          var e = new Expression[6]; // the unique expressions 
          var l = new LabelTarget[0]; // the labels 
          var expr = Lambda<Func<object, object>>( // $
              e[0]=Block(
                  typeof(SampleClass),
                  new[] {
                      p[0]=Parameter(typeof(SampleClass))
                  },
                  e[1]=MakeBinary(ExpressionType.Assign,
                      p[0 // (SampleClass SampleClass__14492072)
                  ],
                  e[2]=New(/*2 args*/
                      typeof(SampleClass).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                      p[1]=Parameter(typeof(object)),
                      e[3]=Constant(new Delegate[]
                      {
                          default(Func<SampleClass, int>), 
                          default(Func<SampleClass, int>), 
                          default(Func<SampleClass, int, OtherClass>), 
                          default(Action<SampleClass>), 
                          default(Func<SampleClass, int, RegularEnum1>), 
                          default(Func<SampleClass, int, FlagsEnum>), 
                          default(Func<SampleClass, RegularEnum1, int>), 
                          default(Func<SampleClass, FlagsEnum, int>), 
                          default(Func<SampleClass, RegularEnum1>), 
                          default(Func<SampleClass, FlagsEnum>), 
                          default(Action<SampleClass, bool>), 
                          default(Action<SampleClass, RegularEnum1>), 
                          default(Action<SampleClass, FlagsEnum>), 
                          default(Func<SampleClass, int, RegularEnum2>), 
                          default(Func<SampleClass, RegularEnum2, int>), 
                          default(Func<SampleClass, RegularEnum2>), 
                          default(Action<SampleClass, RegularEnum2>), 
                          default(Func<SampleClass, string, string>), 
                          null, 
                          default(Func<SampleClass, string, int>)})
                      )
                  ),
                  e[4]=Invoke(
                      e[5]=Constant((Action<SampleClass>)((SampleClass s) => {})),
                      p[0 // (SampleClass SampleClass__14492072)
                        ]),
                      p[0 // (SampleClass SampleClass__14492072)
                        ]),
                      p[1 // (object object__42147750)
                        ]);

              expr.PrintCSharp();

              var fs = expr.CompileSys();
              fs.PrintIL();

              var f = expr.CompileFast(true);
              f.PrintIL();
              var obj = new object();
              var s = f(obj);
              Assert.IsInstanceOf<SampleClass>(s);
        }

        class SampleClass 
        {
            public object Instance;
            public SampleClass(object instance, Delegate[] delegates) 
            {
                Instance = instance;
            }
        }
        class OtherClass {}

        enum RegularEnum1 {}
        enum RegularEnum2 {}
        [Flags]
        enum FlagsEnum {}

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