using NUnit.Framework;
using System;

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
    public class DefaultTests
    {
        [Test]
        [TestCase(typeof(int), default(int))]
        [TestCase(typeof(bool), default(bool))]
        [TestCase(typeof(byte), default(byte))]
        [TestCase(typeof(char), default(char))]
        [TestCase(typeof(ushort), default(ushort))]
        [TestCase(typeof(uint), default(uint))]
        [TestCase(typeof(ulong), default(ulong))]
        [TestCase(typeof(sbyte), default(sbyte))]
        [TestCase(typeof(short), default(short))]
        [TestCase(typeof(long), default(long))]
        [TestCase(typeof(string), default(string))]
        [TestCase(typeof(float), default(float))]
        [TestCase(typeof(double), default(double))]
        [TestCase(typeof(object), default(object))]
        public void Can_compile_default_values(Type type, object expectedResult)
        {
            var dlgt = Lambda<Func<object>>(Convert(Default(type), typeof(object))).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreEqual(expectedResult, dlgt());
        }

        [Test]
        public void Can_compile_default_decimal_value()
        {
            var dlgt = Lambda<Func<decimal>>(Default(typeof(decimal))).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreEqual(default(decimal), dlgt());
        }

        [Test]
        public void Can_compile_default_datetime_value()
        {
            var dlgt = Lambda<Func<DateTime>>(Default(typeof(DateTime))).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreEqual(default(DateTime), dlgt());
        }

        [Test]
        public void Can_compile_default_timespan_value()
        {
            var dlgt = Lambda<Func<TimeSpan>>(Default(typeof(TimeSpan))).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreEqual(default(TimeSpan), dlgt());
        }
    }
}
