using System;
using System.Reflection;
using System.Linq;
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
            Test_case_1_Minimal_compare_nullable_with_null_conditional();
            Test_case_1_Minimal_compare_nullable_returned_by_the_method_with_null_conditional();
            Test_case_1_Minimal_compare_nullable_with_null_conditional_and_nested_conditional();

            Test_case_1_OriginalLinq2DB_Access_ViolationException();

            The_expression_with_anonymous_class_should_output_without_special_symbols();

            return 5;
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

        [Test] // todo: @bug 
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

        [Test]//, Ignore("fixme")] // todo: @bug 
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
        public void Test_case_1_OriginalLinq2DB_Access_ViolationException()
        {
            var p = new ParameterExpression[10]; // the paramiter expressions i
            var e = new Expression[30]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda( // $
              typeof(System.Func<IQueryRunner, System.Data.IDataReader, f__AnonymousType142<System.Nullable<int>>>),
              e[0] = Invoke(
                e[1] = Lambda( // $
                  typeof(System.Func<IQueryRunner, IDataContext, System.Data.IDataReader, System.Linq.Expressions.Expression, object[], object[], f__AnonymousType142<System.Nullable<int>>>),
                  e[2] = Block(
                    typeof(f__AnonymousType142<System.Nullable<int>>),
                    new[] {
                        p[0]=Parameter(typeof(SQLiteDataReader), "ldr"),
                        p[1]=Parameter(typeof(f__AnonymousType142<System.Nullable<int>>))
                    },
                    e[3] = MakeBinary(ExpressionType.Assign,
                      p[0 // (SQLiteDataReader ldr)
                        ],
                      e[4] = Convert(
                        p[2] = Parameter(typeof(System.Data.IDataReader), "dr"),
                        typeof(SQLiteDataReader))),
                    e[5] = MakeBinary(ExpressionType.Assign,
                      p[1 // (f__AnonymousType142<System.Nullable<int>> __f__AnonymousType142_Nullable_int____12662012)
                        ],
                      e[6] = New(/*1 args*/
                        typeof(f__AnonymousType142<System.Nullable<int>>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                        e[7] = Condition(
                          e[8] = MakeBinary(ExpressionType.NotEqual,
                            e[9] = Condition(
                              e[10] = Call(
                                p[0 // (SQLiteDataReader ldr)
                                  ],
                                typeof(System.Data.IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "IsDBNull" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                e[11] = Constant((int)0)),
                              e[12] = Constant(null, typeof(System.Nullable<int>)),
                              e[13] = New(/*1 args*/
                                typeof(System.Nullable<int>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                                e[14] = Call(
                                  p[0 // (SQLiteDataReader ldr)
                                    ],
                                  typeof(System.Data.IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetInt32" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                  e[15] = Constant((int)0))),
                              typeof(System.Nullable<int>)),
                            e[16] = Constant(null, typeof(System.Nullable<int>))),
                          e[17] = Condition(
                            e[18] = Call(
                              p[0 // (SQLiteDataReader ldr)
                                ],
                              typeof(System.Data.IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "IsDBNull" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                              e[19] = Constant((int)0)),
                            e[20] = Constant(null, typeof(System.Nullable<int>)),
                            e[21] = New(/*1 args*/
                              typeof(System.Nullable<int>).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                              e[22] = Call(
                                p[0 // (SQLiteDataReader ldr)
                                  ],
                                typeof(System.Data.IDataRecord).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetInt32" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                e[23] = Constant((int)0))),
                            typeof(System.Nullable<int>)),
                          e[24] = Convert(
                            e[25] = Constant((int)100),
                            typeof(System.Nullable<int>)),
                          typeof(System.Nullable<int>)))),
                    p[1 // (f__AnonymousType142<System.Nullable<int>> __f__AnonymousType142_Nullable_int____12662012)
                      ]),
                  p[3] = Parameter(typeof(IQueryRunner), "qr"),
                  p[4] = Parameter(typeof(IDataContext), "dctx"),
                  p[5] = Parameter(typeof(System.Data.IDataReader), "rd"),
                  p[6] = Parameter(typeof(System.Linq.Expressions.Expression), "expr"),
                  p[7] = Parameter(typeof(object[]), "ps"),
                  p[8] = Parameter(typeof(object[]), "preamble")),
                p[9] = Parameter(typeof(IQueryRunner), "qr"),
                e[26] = Property(
                  p[9 // (IQueryRunner qr)
                    ],
                  typeof(IQueryRunner).GetTypeInfo().GetDeclaredProperty("DataContext")),
                p[2 // (System.Data.IDataReader dr)
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
              p[2 // (System.Data.IDataReader dr)
                ]);

              expr.PrintCSharpString();

              var fs = expr.CompileSys();
              fs.PrintIL();

              var f = expr.CompileFast(true, CompilerFlags.Default);
              f.PrintIL();
        }

        interface IDataContext { }

        interface IQueryRunner 
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
                throw new NotImplementedException();
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
                throw new NotImplementedException();
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