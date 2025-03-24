
using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

    public class DefaultTests : ITest
    {
        public int Run()
        {
            void Can_compile_default_values_of<T>() => Can_compile_default_values(typeof(T), default(T));

            Can_compile_default_values_of<int>();
            Can_compile_default_values_of<bool>();
            Can_compile_default_values_of<byte>();
            Can_compile_default_values_of<char>();
            Can_compile_default_values_of<ushort>();
            Can_compile_default_values_of<uint>();
            Can_compile_default_values_of<ulong>();
            Can_compile_default_values_of<sbyte>();
            Can_compile_default_values_of<short>();
            Can_compile_default_values_of<long>();
            Can_compile_default_values_of<string>();
            Can_compile_default_values_of<float>();
            Can_compile_default_values_of<double>();
            Can_compile_default_values_of<object>();
            Can_compile_default_decimal_value();
            Can_compile_default_datetime_value();
            Can_compile_default_timespan_value();

            return 17;
        }

        public void Can_compile_default_values(Type type, object expectedResult)
        {
            var dlgt = Lambda<Func<object>>(Convert(Default(type), typeof(object))).CompileFast(true);

            Asserts.IsNotNull(dlgt);
            Asserts.AreEqual(expectedResult, dlgt());
        }


        public void Can_compile_default_decimal_value()
        {
            var dlgt = Lambda<Func<decimal>>(Default(typeof(decimal))).CompileFast(true);

            Asserts.IsNotNull(dlgt);
            Asserts.AreEqual(default(decimal), dlgt());
        }


        public void Can_compile_default_datetime_value()
        {
            var dlgt = Lambda<Func<DateTime>>(Default(typeof(DateTime))).CompileFast(true);

            Asserts.IsNotNull(dlgt);
            Asserts.AreEqual(default(DateTime), dlgt());
        }


        public void Can_compile_default_timespan_value()
        {
            var dlgt = Lambda<Func<TimeSpan>>(Default(typeof(TimeSpan))).CompileFast(true);

            Asserts.IsNotNull(dlgt);
            Asserts.AreEqual(default(TimeSpan), dlgt());
        }
    }
}
