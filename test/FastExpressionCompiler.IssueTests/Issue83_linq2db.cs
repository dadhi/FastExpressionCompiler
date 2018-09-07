﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unaasigned fields

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
[TestFixture]
    public class Issue83_linq2db
    {
        [Test]
        public void String_to_number_conversion_using_convert_with_method()
        {
            var from = typeof(string);
            var to = typeof(int);

            var p = Parameter(from, "p");

            var body = Condition(
                NotEqual(p, Constant(null, from)),
                Convert(p, to, to.GetTypeInfo().DeclaredMethods.First(x=> x.Name == "Parse" && x.GetParameters().Length==1 && x.GetParameters()[0].ParameterType == from)),
                Constant(0));

            var expr = Lambda<Func<string, int>>(body, p);

            var compiled = expr.CompileFast();

            Assert.AreEqual(10, compiled("10"));
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

        [Test]
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
                Assign(ldr, Convert(a3, typeof(SQLiteDataReader)) ),
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

            var lambda = Lambda<Func<IQueryRunner, IDataReader, InheritanceTests.InheritanceA>>(body, p1, p2);


            var compiled = lambda.CompileFast();
           
            // NRE during execution of nested function
            var res = compiled(new QueryRunner(), new SQLiteDataReader(false));

            Assert.IsNotNull(res);
            Assert.AreEqual(TypeCodeEnum.A2, res.TypeCode);
            Assert.AreEqual(new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), res.GuidValue);
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

        [Test]
        public void Enum_to_enum_conversion()
        {
            var from = typeof(Enum3);
            var to = typeof(Enum2);

            var p = Parameter(from, "p");

            var body = Convert(
                Convert(p, typeof(int)),
                to);

            var expr = Lambda<Func<Enum3, Enum2>>(body, p);

            var compiled = expr.CompileFast();

            Assert.AreEqual(Enum2.Value2, compiled(Enum3.Value2));
        }

        [Test]
        public void AccessViolationException_on_nullable_char_convert_to_object()
        {
            var body = Convert(
                Constant(' ', typeof(char?)),
                typeof(object));

            var expr = Lambda<Func<object>>(body);

            var compiled = expr.CompileFast();

            Assert.AreEqual(' ', compiled());
        }

        public static int CheckNullValue(IDataRecord reader, object context)
        {
            if (reader.IsDBNull(0))
                throw new InvalidOperationException(
                    $"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
            return 0;
        }

#if !LIGHT_EXPRESSION
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

        [Test]
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


            var compiled = lambda.CompileFast();
            var c = lambda.Compile();

            Assert.Throws<InvalidOperationException>(() => compiled(new QueryRunner(), new SQLiteDataReader(true)));
        }

        [Test]
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


            var compiled = lambda.CompileFast();
            var c = lambda.Compile();

            Assert.Throws<InvalidOperationException>(() => compiled(new QueryRunner(), new SQLiteDataReader(true)));
        }

        public static int GetDefault2(int n)
        {
            return n;
        }

        [Test]
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

            var mapper = Lambda<Func<IDataReader, int>>(mapperBody, a3);

            var compiled = mapper.CompileFast();
            var c = mapper.Compile();

            compiled(new SQLiteDataReader(true));
            c(new SQLiteDataReader(true));
        }

        [Test]
        public void linq2db_InvalidProgramException4()
        {
            var mapperBody = Coalesce(Constant(null, typeof(int?)), Constant(7));
            var mapper = Lambda<Func<int>>(mapperBody);
            var compiled = mapper.CompileFast();
            var c = mapper.Compile();
            compiled();
            c();
        }

        [Test]
        public void TestDoubleConvertSupported()
        {
            var lambda = Lambda<Func<object>>(Convert(
                Convert(
                    Constant("aa"),
                    typeof(object)),
                typeof(object)));


            var compiled1 = lambda.Compile();
            var compiled2 = lambda.CompileFast(true);

            Assert.AreEqual("aa", compiled1());
            Assert.AreEqual("aa", compiled2());
        }

        [Test]
        public void TestLambdaInvokeSupported()
        {
            var lambda = Lambda<Func<string>>(Invoke(Lambda<Func<String>>(Constant("aa"))));

            var compiled1 = lambda.Compile();
            var compiled2 = lambda.CompileFast(true);

            Assert.AreEqual("aa", compiled1());
            Assert.AreEqual("aa", compiled2());
        }

        [Test]
        public void TestLambdaInvokeSupported2()
        {
            var l = Lambda<Func<String>>(Constant("aa"));
            var lambda = Lambda<Func<string>>(Block(Invoke(l), Invoke(l), Invoke(l)));

            var compiled1 = lambda.Compile();
            var compiled2 = lambda.CompileFast(true);

            Assert.AreEqual("aa", compiled1());
            Assert.AreEqual("aa", compiled2());
        }


        [Test]
        public void TestLambdaInvokeSupported3()
        {
            var l = Lambda<Func<String>>(Block(Constant("aa"), Constant("aa"), Constant("aa"),Constant("aa"),Constant("aa")));
            var lambda = Lambda<Func<string>>(Block(Invoke(l), Invoke(l), Invoke(l)));

            var compiled1 = lambda.Compile();
            var compiled2 = lambda.CompileFast(true);

            Assert.AreEqual("aa", compiled1());
            Assert.AreEqual("aa", compiled2());
        }

        [Test]
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

            var compiled1 = mapper.Compile();
            var compiled2 = mapper.CompileFast(true);

            Assert.Throws<NullReferenceException>(() => compiled1(null, null, null, null, null));
            Assert.Throws<NullReferenceException>(() => compiled2(null, null, null, null, null));
        }

        [Test]
        public void TestConverterFailure()
        {
            var p = Parameter(typeof(int?), "p");

            var mapperBody = Condition(Property(p, "HasValue"), Property(p, "Value"), Constant(-1));
            var mapper = Lambda<Func<int?, int>>(mapperBody, p);

            var compiled1 = mapper.Compile();
            var compiled2 = mapper.CompileFast(true);

            compiled1(null);
            compiled2(null);
        }
#endif

        [Test]
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

            var compiled = expr.CompileFast();

            var obj = new TestClass1();

            compiled(obj, 42);

            Assert.That(obj.Class2.Class3.Class4.Field1, Is.EqualTo(42));
        }

        [Test]
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

            Assert.That(obj.Class2.Struct1.Class3.Class4.Field1, Is.EqualTo(42));
        }

        [Test]
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

            Assert.That(obj.Class2.Struct1P.Class3P.Class4.Field1, Is.EqualTo(42));
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

    }
}
