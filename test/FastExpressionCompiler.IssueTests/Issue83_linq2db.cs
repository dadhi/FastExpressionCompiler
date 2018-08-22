using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue83_linq2db
    {
        [Test, Ignore("todo: fix")]
        public void String_to_number_conversion_using_convert_with_method()
        {
            var from = typeof(string);
            var to = typeof(int);

            var p = Expression.Parameter(from, "p");

            var body = Expression.Condition(
                Expression.NotEqual(p, Expression.Constant(null, from)),
                Expression.Convert(p, to, to.GetMethod(nameof(int.Parse), new[] { from })),
                Expression.Constant(0));

            var expr = Expression.Lambda<Func<string, int>>(body, p);

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

            Expression IQueryRunner.Expression => Expression.Constant(null);

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
                return DBNull.Value;
            }
        }

        class InheritanceTests
        {
            public enum TypeCodeEnum
            {
                Base,
                A,
                A1,
                A2,
            }

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

        [Test, Ignore("todo: fix")]
        public void linq2db_NullReferenceException()
        {
            var a1 = Expression.Parameter(typeof(IQueryRunner), "qr");
            var a2 = Expression.Parameter(typeof(IDataContext), "dctx");
            var a3 = Expression.Parameter(typeof(IDataReader), "rd");
            var a4 = Expression.Parameter(typeof(Expression), "expr");
            var a5 = Expression.Parameter(typeof(object[]), "ps");

            var ldr = Expression.Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Expression.Block(
                new[] { ldr },
                Expression.Assign(ldr, Expression.Convert(a3, typeof(SQLiteDataReader)) ),
                Expression.Condition(
                    Expression.Equal(
                        Expression.Condition(
                            Expression.Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Expression.Constant(0)),
                            Expression.Constant(InheritanceTests.TypeCodeEnum.Base),
                            Expression.Convert(
                                Expression.Call(ldr, nameof(SQLiteDataReader.GetInt32), null, Expression.Constant(0)),
                                typeof(InheritanceTests.TypeCodeEnum))),
                        Expression.Constant(InheritanceTests.TypeCodeEnum.A1)),
                    Expression.Convert(
                        Expression.Convert(
                            Expression.Call(
                                typeof(TableBuilder.TableContext).GetMethod(nameof(TableBuilder.TableContext.OnEntityCreated)),
                                a2,
                                Expression.MemberInit(
                                    Expression.New(typeof(InheritanceTests.InheritanceA1)),
                                    Expression.Bind(
                                        typeof(InheritanceTests.InheritanceA1).GetProperty("GuidValue"),
                                        Expression.Condition(
                                            Expression.Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Expression.Constant(1)),
                                            Expression.Constant(Guid.Empty),
                                            Expression.Call(ldr, nameof(SQLiteDataReader.GetGuid), null, Expression.Constant(1))))
                                    )
                                ),
                            typeof(InheritanceTests.InheritanceA1)),
                        typeof(InheritanceTests.InheritanceA)),
                    Expression.Convert(
                        Expression.Convert(
                            Expression.Call(
                                typeof(TableBuilder.TableContext).GetMethod(nameof(TableBuilder.TableContext.OnEntityCreated)),
                                a2,
                                Expression.MemberInit(
                                    Expression.New(typeof(InheritanceTests.InheritanceA2)),
                                    Expression.Bind(
                                        typeof(InheritanceTests.InheritanceA2).GetProperty("GuidValue"),
                                        Expression.Condition(
                                            Expression.Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Expression.Constant(1)),
                                            Expression.Constant(Guid.Empty),
                                            Expression.Call(ldr, nameof(SQLiteDataReader.GetGuid), null, Expression.Constant(1))))
                                    )
                                ),
                            typeof(InheritanceTests.InheritanceA2)),
                        typeof(InheritanceTests.InheritanceA))));

            var mapper = Expression.Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], InheritanceTests.InheritanceA>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Expression.Parameter(typeof(IQueryRunner), "qr");
            var p2 = Expression.Parameter(typeof(IDataReader), "dr");


            var body = Expression.Invoke(
                mapper,
                p1,
                Expression.Property(p1, nameof(IQueryRunner.DataContext)),
                p2,
                Expression.Property(p1, nameof(IQueryRunner.Expression)),
                Expression.Property(p1, nameof(IQueryRunner.Parameters)));

            var lambda = Expression.Lambda<Func<IQueryRunner, IDataReader, InheritanceTests.InheritanceA>>(body, p1, p2);


            var compiled = lambda.CompileFast();

            // NRE during execution of nested function
            var res = compiled(new QueryRunner(), new SQLiteDataReader(false));

            Assert.IsNotNull(res);
            Assert.AreEqual(InheritanceTests.TypeCodeEnum.A, res.TypeCode);
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

        [Test, Ignore("todo: fix")]
        public void Enum_to_enum_conversion()
        {
            var from = typeof(Enum3);
            var to = typeof(Enum2);

            var p = Expression.Parameter(from, "p");

            var body = Expression.Convert(
                Expression.Convert(p, typeof(int)),
                to);

            var expr = Expression.Lambda<Func<Enum3, Enum2>>(body, p);

            var compiled = expr.CompileFast();

            Assert.AreEqual(Enum2.Value2, compiled(Enum3.Value2));
        }

        [Test]
        [Ignore("Test kills test runner process")]
        public void AccessViolationException_on_nullable_char_convert_to_object()
        {
            var body = Expression.Convert(
                Expression.Constant(' ', typeof(char?)),
                typeof(object));

            var expr = Expression.Lambda<Func<object>>(body);

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

        public static object ConvertDefault(object value, Type conversionType)
        {
            try
            {
                return Convert.ChangeType(value, conversionType
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

        [Test, Ignore("todo: fix")]
        public void linq2db_InvalidProgramException()
        {
            var a1 = Expression.Parameter(typeof(IQueryRunner), "qr");
            var a2 = Expression.Parameter(typeof(IDataContext), "dctx");
            var a3 = Expression.Parameter(typeof(IDataReader), "rd");
            var a4 = Expression.Parameter(typeof(Expression), "expr");
            var a5 = Expression.Parameter(typeof(object[]), "ps");

            var ldr = Expression.Variable(typeof(SQLiteDataReader), "ldr");
            var mapperBody = Expression.Block(
                new[] { ldr },
                Expression.Assign(ldr, Expression.Convert(a3, typeof(SQLiteDataReader))),
                Expression.Convert(
                    Expression.Block(
                        Expression.Call(GetType().GetMethod(nameof(CheckNullValue)), a3, Expression.Constant("Average")),
                        Expression.Condition(
                            Expression.Call(ldr, nameof(SQLiteDataReader.IsDBNull), null, Expression.Constant(0)),
                            Expression.Constant(0d),
                            Expression.Convert(
                                Expression.Call(
                                    GetType().GetMethod(nameof(ConvertDefault)),
                                    Expression.Convert(
                                        Expression.Convert(
                                            Expression.Call(ldr, nameof(SQLiteDataReader.GetValue), null, Expression.Constant(0)),
                                            typeof(object)),
                                        typeof(object)),
                                    Expression.Constant(typeof(double))),
                                typeof(double)))),
                    typeof(object)));

            var mapper = Expression.Lambda<Func<IQueryRunner, IDataContext, IDataReader, Expression, object[], object>>(mapperBody, a1, a2, a3, a4, a5);

            var p1 = Expression.Parameter(typeof(IQueryRunner), "qr");
            var p2 = Expression.Parameter(typeof(IDataReader), "dr");


            var body = Expression.Invoke(
                mapper,
                p1,
                Expression.Property(p1, nameof(IQueryRunner.DataContext)),
                p2,
                Expression.Property(p1, nameof(IQueryRunner.Expression)),
                Expression.Property(p1, nameof(IQueryRunner.Parameters)));

            var lambda = Expression.Lambda<Func<IQueryRunner, IDataReader, object>>(body, p1, p2);


            var compiled = lambda.CompileFast();

            Assert.Throws<InvalidOperationException>(() => compiled(new QueryRunner(), new SQLiteDataReader(true)));
        }
    }
}
