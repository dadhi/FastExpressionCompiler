﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;


#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unassigned fields

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public sealed class Issue83_linq2db : ITest
    {
        public int Run()
        {
            linq2db_NullReferenceException();
            linq2db_InvalidProgramException2_reuse_variable_for_upper_and_nested_lambda();
            String_to_number_conversion_using_convert_with_method();
            String_to_number_conversion_using_convert_with_method_with_DefaultExpression();
            Jit_compiler_internal_limitation();
            Struct_test();
            Struct_test2();
            NullableEnum();
            NullableEnum2();
            NewNullableTest();
            TestToString();
            Test2ToString();
            TestDecimal();
            TestDecimal1();
            Test3Bool();
            Test4Bool();
            ConvertNullableTest();
            ConvertNullable2Test();
            ConvertTest();
            ConvertTest2();
            AddNullTest();
            AddNullTest2();
            Triple_convert_with_decimal_nullables();
            Unbox_the_decimal();
            Type_as_nullable_decimal();
            Type_as_nullable_decimal_passing_the_null();
            Negate_decimal();
            Increment_decimal();
            Decrement_decimal();
            linq2db_Expression();
            Equal1_Test();
            Equal2_Test();
            Equal3_Test();
            TypeAs_Test();
            TypeIs_Test();
            Enum_to_enum_conversion();
            Enum_to_enumNull_conversion();
            EnumNull_to_enum_conversion();
            AccessViolationException_on_nullable_char_convert_to_object();
            linq2db_InvalidProgramException();
            linq2db_InvalidProgramException2();
            linq2db_InvalidProgramException3();
            linq2db_InvalidProgramException4();
            TestDoubleConvertSupported();
            TestLambdaInvokeSupported();
            TestLambdaInvokeSupported2();
            TestLambdaInvokeSupported3();
            TestFirstLambda();
            TestConverterFailure();
            TestConverterNullable();
            TestLdArg();
            return 51;
        }


        public void String_to_number_conversion_using_convert_with_method()
        {
            var from = typeof(string);
            var to = typeof(int);

            var p = Parameter(from, "p");

            var body = Condition(
                NotEqual(p, Constant(null, from)),
                Convert(p, to, to.GetTypeInfo().DeclaredMethods.First(x => x.Name == "Parse" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == from)),
                Constant(0));

            var expr = Lambda<Func<string, int>>(body, p);

            var compiled = expr.CompileFast(true);

            Asserts.AreEqual(10, compiled("10"));
        }


        public void String_to_number_conversion_using_convert_with_method_with_DefaultExpression()
        {
            var from = typeof(string);
            var to = typeof(int);

            var p = Parameter(from, "p");

            var body = Condition(
                NotEqual(p, Default(from)),
                Convert(p, to, to.GetTypeInfo().DeclaredMethods.First(x => x.Name == "Parse" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == from)),
                Default(typeof(int)));

            var expr = Lambda<Func<string, int>>(body, p);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual(10, fs("10"));

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual(10, ff("10"));
        }

        interface IQueryRunner
        {
            IDataContext DataContext { get; }
            Expression Expression { get; }
            object[] Parameters { get; }
        }

        interface IDataContext
        {
        }

        public interface IDataRecord
        {
            Guid GetGuid(int i);
            int GetInt32(int i);
            object GetValue(int i);
            bool IsDBNull(int i);
        }

        interface IDataReader : IDataRecord
        {
        }

        class DataContext : IDataContext
        {
        }

        class QueryRunner : IQueryRunner
        {
            IDataContext IQueryRunner.DataContext => new DataContext();

            Expression IQueryRunner.Expression => Constant(null);

            object[] IQueryRunner.Parameters => Array.Empty<object>();
        }

        class SQLiteDataReader : IDataReader
        {
            private readonly bool _dbNull;

            public SQLiteDataReader(bool dbNull)
            {
                _dbNull = dbNull;
            }

            public bool IsDBNull(int idx)
            {
                return _dbNull;
            }

            public int GetInt32(int idx)
            {
                return 1;
            }

            public Guid GetGuid(int idx)
            {
                return new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883");
            }

            public object GetValue(int idx)
            {
                return MyDbNull.Value;
            }
        }

        class MyDbNull
        {
            public static MyDbNull Value => new MyDbNull();

        }

        public enum TypeCodeEnum
        {
            Base,
            A,
            A1,
            A2,
        }

        class InheritanceTests
        {


            public abstract class InheritanceBase
            {
                public Guid GuidValue { get; set; }

                public virtual TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.Base; }
                }
            }

            public abstract class InheritanceA : InheritanceBase
            {
                public List<InheritanceB> Bs { get; set; }

                public override TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.A; }
                }
            }

            public class InheritanceB : InheritanceBase
            {
            }

            public class InheritanceA2 : InheritanceA
            {
                public override TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.A2; }
                }
            }

            public class InheritanceA1 : InheritanceA
            {
                public override TypeCodeEnum TypeCode
                {
                    get { return TypeCodeEnum.A1; }
                }
            }
        }

        class TableBuilder
        {
            public class TableContext
            {
                public static object OnEntityCreated(IDataContext context, object entity)
                {
                    return entity;
                }
            }
        }

        public enum Test
        {
            One,
            Two
        }


        public void linq2db_NullReferenceException()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Condition(
                    Equal(
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(TypeCodeEnum.Base),
                            Convert(
                                Call(ldr, nameof(SQLiteDataReader.GetInt32), null, Constant(0)),
                                typeof(TypeCodeEnum))),
                        Constant(TypeCodeEnum.A1)),
                    Convert(
                        Convert(
                            Call(
                                typeof(TableBuilder.TableContext).GetMethod(nameof(TableBuilder.TableContext.OnEntityCreated)),
                                a2,
                                MemberInit(
                                    New(typeof(InheritanceTests.InheritanceA1)),
                                    Bind(
                                        typeof(InheritanceTests.InheritanceA1).GetProperty("GuidValue"),
                                        Condition(
                                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(1)),
                                            Constant(Guid.Empty),
                                            Call(ldr, nameof(SQLiteDataReader.GetGuid), null, Constant(1))))
                                    )
                                ),
                            typeof(InheritanceTests.InheritanceA1)),
                        typeof(InheritanceTests.InheritanceA)),
                    Convert(
                        Convert(
                            Call(
                                typeof(TableBuilder.TableContext).GetMethod(nameof(TableBuilder.TableContext.OnEntityCreated)),
                                a2,
                                MemberInit(
                                    New(typeof(InheritanceTests.InheritanceA2)),
                                    Bind(
                                        typeof(InheritanceTests.InheritanceA2).GetProperty("GuidValue"),
                                        Condition(
                                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(1)),
                                            Constant(Guid.Empty),
                                            Call(ldr, nameof(SQLiteDataReader.GetGuid), null, Constant(1))))
                                    )
                                ),
                            typeof(InheritanceTests.InheritanceA2)),
                        typeof(InheritanceTests.InheritanceA))));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], InheritanceTests.InheritanceA>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Parameter(typeof(IQueryRunner), "qr");
            var p2 = Parameter(typeof(IDataReader), "dr");

            var body = Invoke(
                mapper,
                p1,
                Property(p1, nameof(IQueryRunner.DataContext)),
                p2,
                Property(p1, nameof(IQueryRunner.Expression)),
                Property(p1, nameof(IQueryRunner.Parameters)));

            var expr = Lambda<Func<IQueryRunner, IDataReader, InheritanceTests.InheritanceA>>(body, p1, p2);
            expr.PrintCSharp();
            // var @cs = (Func<IQueryRunner, IDataReader, InheritanceA>)((
            //     IQueryRunner qr, 
            //     IDataReader dr) => //InheritanceA
            //     ((Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], InheritanceA>)((
            //             IQueryRunner qr, 
            //             IDataContext dctx, 
            //             IDataReader rd, 
            //             Expression expr, 
            //             object[] ps) => //InheritanceA
            //     {
            //             SQLiteDataReader ldr = null;
            //             ldr = (SQLiteDataReader)rd;
            //             return (ldr.IsDBNull(0) ? TypeCodeEnum.Base : 
            //                 (TypeCodeEnum)ldr.GetInt32(0) == TypeCodeEnum.A1) ? 
            //                 (InheritanceA)(InheritanceA1)TableContext.OnEntityCreated(
            //                     dctx,
            //                     new InheritanceA1()
            //                     {
            //                         GuidValue = ldr.IsDBNull(1) ? Guid.Parse("00000000-0000-0000-0000-000000000000") : 
            //                         ldr.GetGuid(1),
            //                     }) : 
            //                 (InheritanceA)(InheritanceA2)TableContext.OnEntityCreated(
            //                     dctx,
            //                     new InheritanceA2()
            //                     {
            //                         GuidValue = ldr.IsDBNull(1) ? Guid.Parse("00000000-0000-0000-0000-000000000000") : 
            //                         ldr.GetGuid(1),
            //                     });
            //     }))
            //     .Invoke(
            //         qr,
            //         qr.DataContext,
            //         dr,
            //         qr.Expression,
            //         qr.Parameters));

            var fs = expr.CompileSys();
            fs.PrintIL();
            var res = fs(new QueryRunner(), new SQLiteDataReader(false));
            Asserts.IsNotNull(res);
            Asserts.AreEqual(TypeCodeEnum.A2, res.TypeCode);
            Asserts.AreEqual(new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), res.GuidValue);

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            res = ff(new QueryRunner(), new SQLiteDataReader(false));
            Asserts.IsNotNull(res);
            Asserts.AreEqual(TypeCodeEnum.A2, res.TypeCode);
            Asserts.AreEqual(new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), res.GuidValue);
        }

        enum TestEnum1
        {
            Value1 = 3,
            Value2,
        }

        class NullableTestTable1
        {
            public int? Id;
            public TestEnum1? TestField;
        }

        const int RID = 101;


        public void linq2db_Expression()
        {
            var param = TestEnum1.Value1;

            var l = new List<NullableTestTable1>();
            System.Linq.Expressions.Expression<Func<IEnumerable<NullableTestTable1>>> se = () => l
                .Where(r => r.Id == RID && r.TestField == TestEnum1.Value2)
                .Select(r => new NullableTestTable1
                {
                    Id = r.Id,
                    TestField = param
                });
            var e = se.FromSysExpression();
            var compiled = e.CompileFast(true);
            compiled();
        }

        enum Enum2
        {
            Value1 = 1,
            Value2 = 2,
        }

        enum Enum3
        {
            Value1 = 1,
            Value2 = 2,
        }


        public void Equal1_Test()
        {
            var p = Parameter(typeof(object));
            var pp = new Patient();
            var body = Equal(Constant(pp), p);
            var expr = Lambda<Func<object, bool>>(body, p);

            var compiled = expr.CompileFast(true);
            var c = expr.CompileSys();

            Asserts.AreEqual(c(pp), compiled(pp));
            Asserts.AreEqual(c(new Patient()), compiled(new Patient()));
        }


        public void Equal2_Test()
        {
            var p = Parameter(typeof(Patient));
            var pp = new Patient();
            var body = Equal(Constant(pp), p);
            var expr = Lambda<Func<Patient, bool>>(body, p);

            var compiled = expr.CompileFast(true);
            var c = expr.CompileSys();

            Asserts.AreEqual(c(pp), compiled(pp));
            Asserts.AreEqual(c(new Patient()), compiled(new Patient()));
        }


        public void Equal3_Test()
        {
            var p = Parameter(typeof(Patient));
            var pp = new Patient2();
            var body = Equal(Constant(pp), p);
            var expr = Lambda<Func<Patient, bool>>(body, p);

            var compiled = expr.CompileFast(true);
            var c = expr.CompileSys();

            Asserts.AreEqual(c(pp), compiled(pp));
            Asserts.AreEqual(c(new Patient()), compiled(new Patient()));
        }


        public void TypeAs_Test()
        {
            var p = Parameter(typeof(object));
            var body = TypeAs(p, typeof(Patient));
            var expr = Lambda<Func<object, Patient>>(body, p);

            var compiled = expr.CompileFast(true);
            var c = expr.CompileSys();

            var pp = new Patient();
            var s = "a";
            Asserts.AreEqual(c(pp), compiled(pp));
            Asserts.AreEqual(c(s), compiled(s));
        }


        public void TypeIs_Test()
        {
            var p = Parameter(typeof(object));
            var body = TypeIs(p, typeof(Patient));
            var expr = Lambda<Func<object, bool>>(body, p);

            var compiled = expr.CompileFast(true);
            var c = expr.CompileSys();

            var pp = new Patient();
            var s = "a";
            Asserts.AreEqual(c(pp), compiled(pp));
            Asserts.AreEqual(c(s), compiled(s));
        }


        public void Enum_to_enum_conversion()
        {
            var from = typeof(Enum3);
            var to = typeof(Enum2);

            var p = Parameter(from, "p");

            var body = Convert(
                Convert(p, typeof(int)),
                to);

            var expr = Lambda<Func<Enum3, Enum2>>(body, p);

            var compiled = expr.CompileFast(true);

            Asserts.AreEqual(Enum2.Value2, compiled(Enum3.Value2));
        }


        public void Enum_to_enumNull_conversion()
        {
            var from = typeof(Enum3);
            var to = typeof(Enum3?);

            var p = Parameter(from, "p");

            var body = Convert(
                Convert(p, typeof(Enum3?)),
                to);

            var expr = Lambda<Func<Enum3, Enum3?>>(body, p);

            var compiled = expr.CompileFast(true);

            Asserts.AreEqual(Enum3.Value2, compiled(Enum3.Value2));
        }


        public void EnumNull_to_enum_conversion()
        {
            var from = typeof(Enum3?);
            var to = typeof(Enum3);

            var p = Parameter(from, "p");

            var body = Convert(
                Convert(p, typeof(Enum3)),
                to);

            var e = Lambda<Func<Enum3?, Enum3>>(body, p);

            var fs = e.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual(Enum3.Value2, fs(Enum3.Value2));
            Asserts.Throws<InvalidOperationException>(() => fs(null));

            var ff = e.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual(Enum3.Value2, ff(Enum3.Value2));
            Asserts.Throws<InvalidOperationException>(() => ff(null));
        }


        public void AccessViolationException_on_nullable_char_convert_to_object()
        {
            var body = Convert(
                Constant(' ', typeof(char?)),
                typeof(object));

            var expr = Lambda<Func<object>>(body);

            var compiled = expr.CompileFast(true);

            Asserts.AreEqual(' ', compiled());
        }

        public static int CheckNullValue(IDataRecord reader, object context)
        {
            if (reader.IsDBNull(0))
                throw new InvalidOperationException(
                    $"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
            return 0;
        }

        public static object ConvertDefault(object value, Type conversionType)
        {
            try
            {
                return System.Convert.ChangeType(value, conversionType
#if !NETSTANDARD1_6
                    , Thread.CurrentThread.CurrentCulture
#endif
                    );
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot convert value '{value}' to type '{conversionType.FullName}'", ex);
            }
        }


        public void linq2db_InvalidProgramException()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Convert(
                    Block(
                        Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Constant("Average")),
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(0d),
                            Convert(
                                Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Convert(
                                        Convert(
                                            Call(ldr, nameof(SQLiteDataReader.GetValue), null, Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Parameter(typeof(IQueryRunner), "qr");
            var p2 = Parameter(typeof(IDataReader), "dr");


            var body = Block(
                    Invoke(
                        mapper,
                        p1,
                        Property(p1, nameof(IQueryRunner.DataContext)),
                        p2,
                        Property(p1, nameof(IQueryRunner.Expression)),
                        Property(p1, nameof(IQueryRunner.Parameters))));

            var lambda = Lambda<Func<IQueryRunner, IDataReader, object>>(body, p1, p2);


            var compiled = lambda.CompileFast(true);
            var c = lambda.CompileSys();

            Asserts.Throws<InvalidOperationException>(() => compiled(new QueryRunner(), new SQLiteDataReader(true)));
        }


        public void linq2db_InvalidProgramException2()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Convert(
                    Block(
                        Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Constant("Average")),
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(0d),
                            Convert(
                                Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Convert(
                                        Convert(
                                            Call(ldr, nameof(SQLiteDataReader.GetValue), null, Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Parameter(typeof(IQueryRunner), "qr");
            var p2 = Parameter(typeof(IDataReader), "dr");


            var body = Block(
                    Invoke(
                        mapper,
                        p1,
                        Property(p1, nameof(IQueryRunner.DataContext)),
                        p2,
                        Property(p1, nameof(IQueryRunner.Expression)),
                        Property(p1, nameof(IQueryRunner.Parameters)))
                        ,
                    Invoke(
                        mapper,
                        p1,
                        Property(p1, nameof(IQueryRunner.DataContext)),
                        p2,
                        Property(p1, nameof(IQueryRunner.Expression)),
                        Property(p1, nameof(IQueryRunner.Parameters)))
                        )
                    ;

            var lambda = Lambda<Func<IQueryRunner, IDataReader, object>>(body, p1, p2);


            var compiled = lambda.CompileFast(true);
            var c = lambda.CompileSys();

            Asserts.Throws<InvalidOperationException>(() => compiled(new QueryRunner(), new SQLiteDataReader(true)));
        }


        public void linq2db_InvalidProgramException2_reuse_variable_for_upper_and_nested_lambda()
        {
            var qr = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");

            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Convert(
                    Block(
                        Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Constant("Average")),
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(0d),
                            Convert(
                                Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Convert(
                                        Convert(
                                            Call(ldr, nameof(SQLiteDataReader.GetValue), null, Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, qr, a2, a3, a4, a5);

            var p2 = Parameter(typeof(IDataReader), "dr");

            var body = Block(
                    Invoke(
                        mapper,
                        qr,
                        Property(qr, nameof(IQueryRunner.DataContext)),
                        p2,
                        Property(qr, nameof(IQueryRunner.Expression)),
                        Property(qr, nameof(IQueryRunner.Parameters)))
                        ,
                    Invoke(
                        mapper,
                        qr,
                        Property(qr, nameof(IQueryRunner.DataContext)),
                        p2,
                        Property(qr, nameof(IQueryRunner.Expression)),
                        Property(qr, nameof(IQueryRunner.Parameters))
                    )
                );

            var lambda = Lambda<Func<IQueryRunner, IDataReader, object>>(body, qr, p2);
            lambda.PrintCSharp();

            var fs = lambda.CompileSys();
            fs.PrintIL();
            Asserts.Throws<InvalidOperationException>(() => fs(new QueryRunner(), new SQLiteDataReader(true)));

            var ff = lambda.CompileFast(true);
            ff.PrintIL();
            Asserts.Throws<InvalidOperationException>(() => ff(new QueryRunner(), new SQLiteDataReader(true)));
        }

        public static int GetDefault2(int n)
        {
            return n;
        }


        public void linq2db_InvalidProgramException3()
        {
            var a3 = Parameter(typeof(IDataReader), "rd");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var int123 = Variable(typeof(int), "int123");

            var mapperBody = Block(
                new[] { ldr, int123 },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Assign(int123,
                    Coalesce(
                        Condition(
                             Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                             Constant(null, typeof(int?)),
                             New(
                                 typeof(int?).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 1),
                                 Call(ldr, nameof(SQLiteDataReader.GetInt32), null, Constant(0))
                                 )
                             ),
                           Call(
                              typeof(Issue83_linq2db).GetTypeInfo().GetMethod(nameof(Issue83_linq2db.GetDefault2)),
                              Condition(
                                 Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                                 Constant(0),
                                 Call(ldr, nameof(SQLiteDataReader.GetInt32), null, Constant(0))
                                 )
                             )
                        )
                        ));

            var expr = Lambda<Func<IDataReader, int>>(mapperBody, a3);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            var ff = expr.CompileFast(true);

            fs(new SQLiteDataReader(true));
            ff(new SQLiteDataReader(true));
        }


        public void linq2db_InvalidProgramException4()
        {
            var mapperBody = Coalesce(Constant(null, typeof(int?)), Constant(7));
            var mapper = Lambda<Func<int>>(mapperBody);
            var compiled = mapper.CompileFast(true);
            var c = mapper.CompileSys();
            compiled();
            c();
        }


        public void TestDoubleConvertSupported()
        {
            var lambda = Lambda<Func<object>>(Convert(
                Convert(
                    Constant("aa"),
                    typeof(object)),
                typeof(object)));


            var compiled1 = lambda.CompileSys();
            var compiled2 = lambda.CompileFast(true);

            Asserts.AreEqual("aa", compiled1());
            Asserts.AreEqual("aa", compiled2());
        }


        public void TestLambdaInvokeSupported()
        {
            var lambda = Lambda<Func<string>>(Invoke(Lambda<Func<String>>(Constant("aa"))));

            var compiled1 = lambda.CompileSys();
            var compiled2 = lambda.CompileFast(true);

            Asserts.AreEqual("aa", compiled1());
            Asserts.AreEqual("aa", compiled2());
        }


        public void TestLambdaInvokeSupported2()
        {
            var l = Lambda<Func<String>>(Constant("aa"));
            var lambda = Lambda<Func<string>>(Block(Invoke(l), Invoke(l), Invoke(l)));

            var compiled1 = lambda.CompileSys();
            var compiled2 = lambda.CompileFast(true);

            Asserts.AreEqual("aa", compiled1());
            Asserts.AreEqual("aa", compiled2());
        }


        public void TestLambdaInvokeSupported3()
        {
            var l = Lambda<Func<String>>(Block(Constant("aa"), Constant("aa"), Constant("aa"), Constant("aa"), Constant("aa")));
            var lambda = Lambda<Func<string>>(Block(Invoke(l), Invoke(l), Invoke(l)));

            var compiled1 = lambda.CompileSys();
            var compiled2 = lambda.CompileFast(true);

            Asserts.AreEqual("aa", compiled1());
            Asserts.AreEqual("aa", compiled2());
        }


        public void TestFirstLambda()
        {
            var a1 = Parameter(typeof(IQueryRunner), "qr");
            var a2 = Parameter(typeof(IDataContext), "dctx");
            var a3 = Parameter(typeof(IDataReader), "rd");
            var a4 = Parameter(typeof(Expression), "expr");
            var a5 = Parameter(typeof(object[]), "ps");

            var ldr = Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Block(
                new[] { ldr },
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader))),
                Convert(
                    Block(
                        Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Constant("Average")),
                        Condition(
                            Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Constant(0)),
                            Constant(0d),
                            Convert(
                                Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Convert(
                                        Convert(
                                            Call(ldr, nameof(SQLiteDataReader.GetValue), null, Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, a1, a2, a3, a4, a5);

            var compiled1 = mapper.CompileSys();
            var compiled2 = mapper.CompileFast(true);

            Asserts.Throws<NullReferenceException>(() => compiled1(null, null, null, null, null));
            Asserts.Throws<NullReferenceException>(() => compiled2(null, null, null, null, null));
        }


        public void TestConverterFailure()
        {
            var p = Parameter(typeof(int?), "p");

            var mapperBody = Condition(Property(p, "HasValue"), Property(p, "Value"), Constant(-1));
            var mapper = Lambda<Func<int?, int>>(mapperBody, p);

            var compiled1 = mapper.CompileSys();
            var compiled2 = mapper.CompileFast(true);

            compiled1(null);
            compiled2(null);
        }

        class sPrp
        {
            public short? v;
        }


        public void TestConverterNullable()
        {
            var p = Parameter(typeof(sPrp), "p");

            var mapperBody = /*Convert(*/Convert(Field(p, nameof(sPrp.v)), typeof(int?))/*, typeof(object))*/;
            var mapper = Lambda<Func<sPrp, int?>>(mapperBody, p);

            var compiled1 = mapper.CompileSys();
            var compiled2 = mapper.CompileFast(true);

            var a = compiled1(new sPrp() { v = short.MaxValue });
            var b = compiled2(new sPrp() { v = short.MaxValue });

            Asserts.AreEqual(a, b);

            var c = compiled1(new sPrp() { v = short.MinValue });
            var d = compiled2(new sPrp() { v = short.MinValue });

            Asserts.AreEqual(c, d);
        }

        public static string aa(int nr)
        {
            return nr.ToString();
        }


        public void TestLdArg()
        {
            var p = Parameter(typeof(int), "p");

            var mapperBody = Call(typeof(Issue83_linq2db).GetTypeInfo().GetMethod("aa"), p);
            var mapper = Lambda<Func<int, string>>(mapperBody, p);

            var compiled1 = mapper.CompileSys();
            var compiled2 = mapper.CompileFast(true);

            var a = compiled1(5);
            var b = compiled2(5);

            Asserts.AreEqual(a, b);
        }


        public void Jit_compiler_internal_limitation()
        {
            var objParam = Parameter(typeof(object), "obj");
            var valueParam = Parameter(typeof(object), "value");

            var varClass2 = Variable(typeof(TestClass2));
            var varClass3 = Variable(typeof(TestClass3));
            var varClass4 = Variable(typeof(TestClass4));

            var body = Block(
                typeof(int),
                new[] { varClass2, varClass3, varClass4 },
                Assign(varClass2, Field(Convert(objParam, typeof(TestClass1)), nameof(TestClass1.Class2))),
                IfThen(
                    Equal(varClass2, Constant(null)),
                    Block(
                        Assign(varClass2, New(typeof(TestClass2))),
                        Assign(Field(Convert(objParam, typeof(TestClass1)), nameof(TestClass1.Class2)), varClass2))),
                Assign(varClass3, Field(varClass2, nameof(TestClass2.Class3))),
                IfThen(
                    Equal(varClass3, Constant(null)),
                    Block(
                        Assign(varClass3, New(typeof(TestClass3))),
                        Assign(Field(varClass2, nameof(TestClass2.Class3)), varClass3))),
                Assign(varClass4, Field(varClass3, nameof(TestClass3.Class4))),
                IfThen(
                    Equal(varClass4, Constant(null)),
                    Block(
                        Assign(varClass4, New(typeof(TestClass4))),
                        Assign(Field(varClass3, nameof(TestClass3.Class4)), varClass4))),
                Assign(
                    Field(varClass4, nameof(TestClass4.Field1)),
                    Convert(valueParam, typeof(int)))
                );

            var expr = Lambda<Action<object, object>>(body, objParam, valueParam);
            expr.PrintCSharp();

            var compiled = expr.CompileFast(true);

            var obj = new TestClass1();

            compiled(obj, 42);

            Asserts.AreEqual(42, obj.Class2.Class3.Class4.Field1);
        }


        public void Struct_test()
        {
            var objParam = Parameter(typeof(object), "obj");
            var valueParam = Parameter(typeof(object), "value");

            var varClass2 = Variable(typeof(TestClass2));
            var varStruct1 = Variable(typeof(TestStruct1));
            var varClass3 = Variable(typeof(TestClass3));
            var varClass4 = Variable(typeof(TestClass4));

            var body = Block(
                typeof(int),
                new[] { varClass2, varStruct1, varClass3, varClass4 },
                Assign(varClass2, Field(Convert(objParam, typeof(TestClass1)), nameof(TestClass1.Class2))),
                IfThen(
                    Equal(varClass2, Constant(null)),
                    Block(
                        Assign(varClass2, New(typeof(TestClass2))),
                        Assign(Field(Convert(objParam, typeof(TestClass1)), nameof(TestClass1.Class2)), varClass2))),
                Assign(varStruct1, Field(varClass2, nameof(TestClass2.Struct1))),
                Assign(varClass3, Field(varStruct1, nameof(TestStruct1.Class3))),
                IfThen(
                    Equal(varClass3, Constant(null)),
                    Block(
                        Assign(varClass3, New(typeof(TestClass3))),
                        Assign(Field(varStruct1, nameof(TestStruct1.Class3)), varClass3),
                        Assign(Field(varClass2, nameof(TestClass2.Struct1)), varStruct1))
                        ),
                Assign(varClass4, Field(varClass3, nameof(TestClass3.Class4))),
                IfThen(
                    Equal(varClass4, Constant(null)),
                    Block(
                        Assign(varClass4, New(typeof(TestClass4))),
                        Assign(Field(varClass3, nameof(TestClass3.Class4)), varClass4))),
                Assign(
                    Field(varClass4, nameof(TestClass4.Field1)),
                    Convert(valueParam, typeof(int)))
                );

            var expr = Lambda<Action<object, object>>(body, objParam, valueParam);

            var compiled = expr.CompileFast(true);

            var obj = new TestClass1();

            compiled(obj, 42);

            Asserts.AreEqual(42, obj.Class2.Struct1.Class3.Class4.Field1);
        }


        public void Struct_test2()
        {
            var objParam = Parameter(typeof(object), "obj");
            var valueParam = Parameter(typeof(object), "value");

            var varClass2 = Variable(typeof(TestClass2));
            var varStruct1 = Variable(typeof(TestStruct1));
            var varClass3 = Variable(typeof(TestClass3));
            var varClass4 = Variable(typeof(TestClass4));

            var body = Block(
                typeof(int),
                new[] { varClass2, varStruct1, varClass3, varClass4 },
                Assign(varClass2, Field(Convert(objParam, typeof(TestClass1)), nameof(TestClass1.Class2))),
                IfThen(
                    Equal(varClass2, Constant(null)),
                    Block(
                        Assign(varClass2, New(typeof(TestClass2))),
                        Assign(Field(Convert(objParam, typeof(TestClass1)), nameof(TestClass1.Class2)), varClass2))),
                Assign(varStruct1, Property(varClass2, nameof(TestClass2.Struct1P))),
                Assign(varClass3, Property(varStruct1, nameof(TestStruct1.Class3P))),
                IfThen(
                    Equal(varClass3, Constant(null)),
                    Block(
                        Assign(varClass3, New(typeof(TestClass3))),
                        Assign(Property(varStruct1, nameof(TestStruct1.Class3P)), varClass3),
                        Assign(Property(varClass2, nameof(TestClass2.Struct1P)), varStruct1))
                        ),
                Assign(varClass4, Field(varClass3, nameof(TestClass3.Class4))),
                IfThen(
                    Equal(varClass4, Constant(null)),
                    Block(
                        Assign(varClass4, New(typeof(TestClass4))),
                        Assign(Field(varClass3, nameof(TestClass3.Class4)), varClass4))),
                Assign(
                    Field(varClass4, nameof(TestClass4.Field1)),
                    Convert(valueParam, typeof(int)))
                );

            var expr = Lambda<Action<object, object>>(body, objParam, valueParam);

            var compiled = expr.CompileFast(true);

            var obj = new TestClass1();

            compiled(obj, 42);

            Asserts.AreEqual(42, obj.Class2.Struct1P.Class3P.Class4.Field1);
        }


        public void NullableEnum()
        {
            var objParam = Parameter(typeof(TestClass2), "obj");

            var body = Block(
                Assign(Field(objParam, nameof(TestClass2.NullEnum2)), Constant(Enum2.Value1, typeof(Enum2?)))
                );

            var expr = Lambda<Action<TestClass2>>(body, objParam);

            var compiled = expr.CompileFast(true);

            var obj = new TestClass2();

            compiled(obj);

            Asserts.AreEqual(Enum2.Value1, obj.NullEnum2);
        }


        public void NullableEnum2()
        {
            var objParam = Parameter(typeof(TestClass2), "obj");

            var body = Block(
                Equal(Field(objParam, nameof(TestClass2.NullEnum2)), Constant(Enum2.Value1, typeof(Enum2?)))
            );

            var expr = Lambda<Action<TestClass2>>(body, objParam);


            var compiled = expr.CompileFast(true);

            var obj = new TestClass2();

            compiled(obj);
        }


        public void NewNullableTest()
        {
            var body = New(typeof(int?).GetTypeInfo().DeclaredConstructors.First(), Constant(6, typeof(int)));

            var expr = Lambda<Func<int?>>(body);

            var compiled = expr.CompileFast(true);

            compiled();
        }


        public void TestToString()
        {
            var body = Call(Constant(true),
                typeof(bool).GetTypeInfo().DeclaredMethods
                    .First(x => x.Name == "ToString" && x.GetParameters().Length == 0));

            var expr = Lambda<Func<string>>(body);

            var compiled = expr.CompileFast(true);

            var ret = compiled();

            Asserts.AreEqual("True", ret);
        }


        public void Test2ToString()
        {
            var p = Parameter(typeof(bool));
            var body = Call(p,
                typeof(bool).GetTypeInfo().DeclaredMethods
                    .First(x => x.Name == "ToString" && x.GetParameters().Length == 0));

            var expr = Lambda<Func<bool, string>>(body, p);

            var compiled = expr.CompileFast(true);

            var ret = compiled(true);

            Asserts.AreEqual("True", ret);
        }


        public void TestDecimal()
        {
            var body = Constant(5.64m);

            var expr = Lambda<Func<Decimal>>(body);

            var compiled = expr.CompileFast(true);

            var ret = compiled();
            Asserts.AreEqual(5.64m, ret);
        }


        public void TestDecimal1()
        {
            var body = Constant(5m);

            var expr = Lambda<Func<Decimal>>(body);

            var compiled = expr.CompileFast(true);

            var ret = compiled();
            Asserts.AreEqual(5m, ret);
        }


        public void Test3Bool()
        {
            var p = Parameter(typeof(bool));
            var body = Not(p);

            var expr = Lambda<Func<bool, bool>>(body, p);

            var compiled = expr.CompileFast(true);

            var ret = compiled(true);

            Asserts.AreEqual(false, ret);
        }


        public void Test4Bool()
        {
            var p = Parameter(typeof(bool));
            var body = Not(p);

            var expr = Lambda<Func<bool, bool>>(body, p);

            var compiled = expr.CompileFast(true);

            var ret = compiled(false);

            Asserts.AreEqual(true, ret);
        }


        public void ConvertNullableTest()
        {
            var body = Convert(ConvertChecked(Constant(long.MaxValue - 1, typeof(long)), typeof(int)), typeof(int?));

            var expr = Lambda<Func<int?>>(body);

            var compiled = expr.CompileFast(true);

            Asserts.Throws<OverflowException>(() => compiled());
        }


        public void ConvertNullable2Test()
        {
            var body = Convert(ConvertChecked(Constant(5L, typeof(long)), typeof(int)), typeof(int?));

            var expr = Lambda<Func<int?>>(body);

            var compiled = expr.CompileFast(true);

            compiled();
        }


        public void ConvertTest()
        {
            var body = ConvertChecked(Constant(0x10, typeof(int)), typeof(char));

            var expr = Lambda<Func<char>>(body);

            var compiled = expr.CompileFast(true);

            var ret = compiled();

            Asserts.AreEqual('\x10', ret);
        }


        public void ConvertTest2()
        {
            var body = ConvertChecked(Constant('\x10', typeof(char)), typeof(int));

            var expr = Lambda<Func<int>>(body);

            var compiled = expr.CompileFast(true);

            var ret = compiled();

            Asserts.AreEqual(0x10, ret);
        }


        public void AddNullTest()
        {
            var p = Parameter(typeof(int?));
            var body = Add(Constant(4, typeof(int?)), p);
            var expr = Lambda<Func<int?, int?>>(body, p);

            var fs = expr.CompileSys();
            fs.PrintIL();

            var fx = expr.CompileFast(true);
            fx.PrintIL();

            Asserts.AreEqual(9, fx(5));
            Asserts.AreEqual(null, fx(null));
        }


        public void AddNullTest2()
        {
            var p = Parameter(typeof(int?));
            var body = Add(p, Constant(4, typeof(int?)));
            var expr = Lambda<Func<int?, int?>>(body, p);
            var compiled = expr.CompileFast(true);
            Asserts.AreEqual(9, compiled(5));
            Asserts.AreEqual(null, compiled(null));
        }

        public class Patient2 : Patient { }

        public class Patient
        {
            public int PersonID;
            public string Diagnosis;

            public static bool operator ==(Patient a, Patient b)
            {
                return Equals(a, b);
            }
            public static bool operator !=(Patient a, Patient b)
            {
                return !Equals(a, b);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Patient);
            }

            public bool Equals(Patient other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.PersonID == PersonID && Equals(other.Diagnosis, Diagnosis);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var result = PersonID;
                    result = (result * 397) ^ (Diagnosis != null ? Diagnosis.GetHashCode() : 0);
                    return result;
                }
            }
        }

        class TestClass1
        {
            public int Prop1
            {
                get { return 0; }
            }

            public int Prop2 { get; set; }

            public TestClass2 Class2;

            int _prop3;
            public int Prop3
            {
                set { _prop3 = value; }
            }
        }

        class TestClass2
        {
            public Enum2? NullEnum2;
            public TestClass3 Class3;
            public TestStruct1 Struct1;
            public TestStruct1 Struct1P { get; set; }

        }

        class TestClass3
        {
            public TestClass4 Class4;
        }

        class TestClass4
        {
            public int Field1;
        }

        struct TestStruct1
        {
            public TestClass3 Class3;

            public TestClass3 Class3P { get; set; }
        }


        public void Triple_convert_with_decimal_nullables()
        {
            var body = Convert(
                Convert(
                    Convert(
                        Convert(Constant(2d), typeof(double?)),
                        typeof(decimal),
                        typeof(decimal).GetMethod("op_Explicit", new[] { typeof(double) })),
                    typeof(decimal?)),
                typeof(object));

            var expr = Lambda<Func<object>>(body);
            var compiled = expr.CompileFast(true);
            Asserts.AreEqual(2m, compiled());
        }



        public void Unbox_the_decimal()
        {
            var decObj = Parameter(typeof(object), "decObj");
            var expr = Lambda<Func<object, decimal>>(
                Unbox(decObj, typeof(decimal)),
                decObj
            );

            var f = expr.CompileFast(true);
            Asserts.AreEqual(2m, f((object)2m));
        }


        public void Type_as_nullable_decimal()
        {
            var decObj = Parameter(typeof(object), "decObj");
            var expr = Lambda<Func<object, decimal?>>(
                TypeAs(decObj, typeof(decimal?)),
                decObj
            );

            var f = expr.CompileFast(true);
            decimal? x = 2m;
            Asserts.AreEqual(2m, f((object)x));
        }


        public void Type_as_nullable_decimal_passing_the_null()
        {
            var decObj = Parameter(typeof(object), "decObj");
            var expr = Lambda<Func<object, decimal?>>(
                TypeAs(decObj, typeof(decimal?)),
                decObj
            );

            var f = expr.CompileFast(true);
            decimal? x = null;
            Asserts.AreEqual(null, f((object)x));
        }


        public void Negate_decimal()
        {
            var decObj = Parameter(typeof(object), "decObj");
            var expr = Lambda<Func<object, decimal>>(
                Negate(Unbox(decObj, typeof(decimal))),
                decObj
            );

            var f = expr.CompileFast(true);
            Asserts.AreEqual(-2m, f(2m));
        }


        public void Increment_decimal()
        {
            var decObj = Parameter(typeof(object), "decObj");
            var expr = Lambda<Func<object, decimal>>(
                Increment(Unbox(decObj, typeof(decimal))),
                decObj
            );

            var f = expr.CompileFast(true);
            Asserts.AreEqual(3m, f(2m));
        }


        public void Decrement_decimal()
        {
            var decObj = Parameter(typeof(object), "decObj");
            var expr = Lambda<Func<object, decimal>>(
                Decrement(Unbox(decObj, typeof(decimal))),
                decObj
            );

            var f = expr.CompileFast(true);
            Asserts.AreEqual(1m, f(2m));
        }
    }
}
