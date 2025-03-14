using System;
using System.Numerics;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions; // todo: @remove later
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    public class ArithmeticOperationsTests : ITest
    {
        public int Run()
        {
            Can_add_string_and_not_string();

            Can_modulus_custom_in_Action();
            Can_modulus_custom();
            Can_sum_bytes_converted_to_ints();
            Can_sum_signed_bytes_converted_to_ints();
            Can_sum_all_primitive_numeric_types_that_define_binary_operator_add();
            Can_not_sum_with_checked_overflow();
            Can_substract_all_primitive_numeric_types_that_define_binary_operator_substract();
            Can_substract_with_unchecked_overflow();
            Can_not_substract_with_checked_overflow();
            Can_modulus();
            Can_bit_or_1();
            Can_bit_and_1();
            Can_bit_xor_1();
            Can_shift_left_1();
            Can_shift_right_1();
            Can_multiply_all_primitive_numeric_types_that_define_binary_operator_multiply();
            Can_multiply_with_unchecked_overflow();
            Can_not_multiply_with_checked_overflow();
            Can_divide_bytes();
            Can_divide_signed_bytes();
            Can_sum_decimal_numbers();
            Can_substract_decimal_numbers();
            Can_multiply_decimal_numbers();
            Can_divide_decimal_numbers();
            Can_divide_all_primitive_numeric_types_that_define_binary_operator_divide();
            Can_calculate_arithmetic_operation_on_non_primitive_class();
            Can_calculate_arithmetic_operation_on_non_primitive_value_type();

            Can_calculate_arithmetic_operation_with_vectors();
            Can_add_strings();

            return 29;
        }

        public void Can_sum_bytes_converted_to_ints()
        {
            var a = Parameter(typeof(byte), "a");
            var b = Parameter(typeof(byte), "b");
            var expr = Lambda<Func<byte, byte, int>>(Add(Convert(a, typeof(int)), Convert(b, typeof(int))), a, b);

            var sumFunc = expr.CompileFast(true);

            Asserts.IsNotNull(sumFunc);
            Asserts.AreEqual(sumFunc(1, 3), 4);
        }

        public void Can_sum_signed_bytes_converted_to_ints()
        {
            var a = Parameter(typeof(sbyte), "a");
            var b = Parameter(typeof(sbyte), "b");
            var expr = Lambda<Func<sbyte, sbyte, int>>(Add(Convert(a, typeof(int)), Convert(b, typeof(int))), a, b);

            var sumFunc = expr.CompileFast(true);

            Asserts.IsNotNull(sumFunc);
            Asserts.AreEqual(sumFunc(1, 3), 4);
        }

        void AssertAdd<T, R>(T x, T y, R expected, bool convertArgsToResult = false)
        {
            var a = Parameter(typeof(T), "a");
            var b = Parameter(typeof(T), "b");
            var add = !convertArgsToResult ? Add(a, b) : Add(Convert(a, typeof(R)), Convert(b, typeof(R)));
            var expr = Lambda<Func<T, T, R>>(add, a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(expected, f(x, y));
        }


        public void Can_sum_all_primitive_numeric_types_that_define_binary_operator_add()
        {
            AssertAdd<byte, int>(1, 2, 3, true);
            AssertAdd<sbyte, int>(1, 2, 3, true);
            AssertAdd<short, short>(1, 2, 3);
            AssertAdd<ushort, ushort>(1, 2, 3);
            AssertAdd<int, int>(1, 2, 3);
            AssertAdd<uint, uint>(1, 2, 3);
            AssertAdd<long, long>(1, 2, 3);
            AssertAdd<ulong, ulong>(1, 2, 3);
            AssertAdd<float, float>(1, 2, 3);
            AssertAdd<double, double>(1, 2, 3);
        }


        public void Can_sum_with_unchecked_overflow()
        {
            AssertAdd(int.MaxValue, 1, int.MinValue);
        }


        public void Can_not_sum_with_checked_overflow()
        {
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(AddChecked(a, b), a, b);

            var sumFunc = expr.CompileFast(true);

            Asserts.IsNotNull(sumFunc);
            Asserts.Throws<OverflowException>(() => sumFunc(int.MaxValue, 1));
        }

        void AssertSub<T, R>(T x, T y, R expected, bool convertArgsToResult = false)
        {
            var a = Parameter(typeof(T), "a");
            var b = Parameter(typeof(T), "b");
            var sub = !convertArgsToResult ? Subtract(a, b) : Subtract(Convert(a, typeof(R)), Convert(b, typeof(R)));
            var expr = Lambda<Func<T, T, R>>(sub, a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(expected, f(x, y));
        }


        public void Can_substract_all_primitive_numeric_types_that_define_binary_operator_substract()
        {
            AssertSub<byte, int>(5, 1, 4, true);
            AssertSub<sbyte, int>(5, 1, 4, true);
            AssertSub<short, short>(5, 1, 4);
            AssertSub<ushort, ushort>(5, 1, 4);
            AssertSub<int, int>(5, 1, 4);
            AssertSub<uint, uint>(5, 1, 4);
            AssertSub<long, long>(5, 1, 4);
            AssertSub<ulong, ulong>(5, 1, 4);
            AssertSub<float, float>(5, 1, 4);
            AssertSub<double, double>(5, 1, 4);
        }


        public void Can_substract_with_unchecked_overflow()
        {
            AssertSub<int, int>(int.MinValue, 1, int.MaxValue);
        }


        public void Can_not_substract_with_checked_overflow()
        {
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(SubtractChecked(a, b), a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.Throws<OverflowException>(() => f(int.MinValue, 1));
        }


        public void Can_modulus_custom()
        {
            var a = Parameter(typeof(BigInteger), "a");
            var b = Parameter(typeof(BigInteger), "b");
            var expr = Lambda<Func<BigInteger, BigInteger, BigInteger>>(Modulo(a, b), a, b);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var ff = expr.CompileFast(true);
            ff.PrintIL();

            Asserts.AreEqual(new BigInteger(1), fs(7, 6));
            Asserts.AreEqual(new BigInteger(1), ff(7, 6));
        }

        public void Can_modulus_custom_in_Action()
        {
            var a = Parameter(typeof(BigInteger), "a");
            var b = Parameter(typeof(BigInteger), "b");
            var expr = Lambda<Action<BigInteger, BigInteger>>(Modulo(a, b), a, b);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var fx = expr.CompileFast(true);
            fx.PrintIL();
            fx(7, 6);
        }


        public void Can_modulus()
        {
            var a = Parameter(typeof(short), "a");
            var b = Parameter(typeof(short), "b");
            var expr = Lambda<Func<short, short, long>>(Modulo(Convert(a, typeof(long)), Convert(b, typeof(long))), a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(1, f(7, 6));
        }


        public void Can_bit_or_1()
        {
            // Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 | arg2;
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(Or(a, b), a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(1, f(1, 0));
            Asserts.AreEqual(1, f(1, 1));
        }


        public void Can_bit_and_1()
        {
            // Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 & arg2;
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(And(a, b), a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(1, f(1, 1));
            Asserts.AreEqual(0, f(1, 0));
        }


        public void Can_bit_xor_1()
        {
            // Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 ^ arg2;
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(ExclusiveOr(a, b), a, b);

            var fs = expr.CompileSys();
            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(fs(231, 785), f(231, 785));
        }


        public void Can_shift_right_1()
        {
            // Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 >> arg2;
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(RightShift(a, b), a, b);

            var fs = expr.CompileSys();
            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(fs(231, 785), f(231, 785));
        }


        public void Can_shift_left_1()
        {
            // Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 << arg2;
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(LeftShift(a, b), a, b);

            var fs = expr.CompileSys();
            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(fs(231, 785), f(231, 785));
        }

        void AssertMultiple<T, R>(T x, T y, R expected, Type convertTo = null)
        {
            var a = Parameter(typeof(T), "a");
            var b = Parameter(typeof(T), "b");

            var mul = convertTo == null ? Multiply(a, b) : Multiply(Convert(a, convertTo), Convert(b, convertTo));
            var expr = Lambda<Func<T, T, R>>(mul, a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(expected, f(x, y));
        }


        public void Can_multiply_all_primitive_numeric_types_that_define_binary_operator_multiply()
        {
            AssertMultiple<sbyte, short>(3, 2, 6, typeof(short));
            AssertMultiple<byte, ushort>(3, 2, 6, typeof(ushort));
            AssertMultiple<short, short>(3, 2, 6);
            AssertMultiple<ushort, ushort>(3, 2, 6);
            AssertMultiple<int, int>(3, 2, 6);
            AssertMultiple<uint, uint>(3, 2, 6);
            AssertMultiple<long, long>(3, 2, 6);
            AssertMultiple<ulong, ulong>(3, 2, 6);
            AssertMultiple<float, float>(3, 2, 6);
            AssertMultiple<double, double>(3, 2, 6);
        }


        public void Can_multiply_with_unchecked_overflow()
        {
            AssertMultiple<int, int>(int.MaxValue, int.MaxValue, 1);
        }


        public void Can_not_multiply_with_checked_overflow()
        {
            var a = Parameter(typeof(int), "a");
            var b = Parameter(typeof(int), "b");
            var expr = Lambda<Func<int, int, int>>(MultiplyChecked(a, b), a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.Throws<OverflowException>(() => f(int.MaxValue, int.MaxValue));
        }

        void AssertDivide<T, R>(T x, T y, R expected, bool convertArgsToResult = false)
        {
            var a = Parameter(typeof(T), "a");
            var b = Parameter(typeof(T), "b");

            var div = !convertArgsToResult ? Divide(a, b) : Divide(Convert(a, typeof(R)), Convert(b, typeof(R)));
            var expr = Lambda<Func<T, T, R>>(div, a, b);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(expected, f(x, y));
        }


        public void Can_divide_bytes()
        {
            AssertDivide<byte, int>(7, 3, 2, true);
        }


        public void Can_divide_signed_bytes()
        {
            AssertDivide<sbyte, int>(7, 3, 2, true);
        }


        public void Can_sum_decimal_numbers()
        {
            AssertAdd(1.0m, 2.1m, 1.0m + 2.1m);
        }


        public void Can_substract_decimal_numbers()
        {
            AssertSub(2.0m, 1.0m, 2.0m - 1.0m);
        }


        public void Can_multiply_decimal_numbers()
        {
            AssertMultiple(2.0m, 1.0m, 2.0m * 1.0m);
        }


        public void Can_divide_decimal_numbers()
        {
            AssertDivide(2.0m, 1.0m, 2.0m * 1.0m);
        }


        public void Can_divide_all_primitive_numeric_types_that_define_binary_operator_divide()
        {
            AssertDivide<short, short>(6, 3, 2);
            AssertDivide<ushort, ushort>(6, 3, 2);
            AssertDivide(6, 3, 2);
            AssertDivide<uint, uint>(6, 3, 2);
            AssertDivide<long, long>(6, 3, 2);
            AssertDivide<ulong, ulong>(6, 3, 2);
            AssertDivide<float, float>(6, 3, 2);
            AssertDivide<double, double>(6, 3, 2);
        }


        public void Can_calculate_arithmetic_operation_on_non_primitive_class()
        {
            AssertAdd(new NonPrimitiveInt32Class(1), new NonPrimitiveInt32Class(2), new NonPrimitiveInt32Class(3));
        }


        public void Can_calculate_arithmetic_operation_on_non_primitive_value_type()
        {
            AssertAdd(new NonPrimitiveInt32ValueType(1), new NonPrimitiveInt32ValueType(2), new NonPrimitiveInt32ValueType(3));
        }


        public void Can_calculate_arithmetic_function_obeying_operator_precedence()
        {
            System.Linq.Expressions.Expression<Func<int, int, int, int, int>> sExpr = (arg1, arg2, arg3, arg4) => arg1 + arg2 * arg3 / arg4;
            var expr = sExpr.FromSysExpression();

            var arithmeticFunc = expr.CompileFast(true);

            Asserts.IsNotNull(arithmeticFunc);
            Asserts.AreEqual(arithmeticFunc(1, 2, 3, 4), 2);
        }

        public void Can_calculate_arithmetic_operation_with_vectors()
        {
            System.Linq.Expressions.Expression<Func<Vector2>> sExpr = () => Vector2.One - new Vector2(2.0f, 2.0f);
            var expr = sExpr.FromSysExpression();

            var vectorMethod = expr.CompileFast(true);

            Asserts.IsNotNull(vectorMethod);
            var result = vectorMethod();
            Asserts.AreEqual(result, new Vector2(-1.0f, -1.0f));
        }


        public void Can_add_strings()
        {
            // AssertAdd("a", "b", "ab");

            var s1 = "a";
            var s2 = "b";
            System.Linq.Expressions.Expression<Func<string>> sExpr = () => s1 + s2;
            var expr = sExpr.FromSysExpression();

            var f = expr.CompileFast(true);
            Asserts.AreEqual("ab", f());
        }

        public void Can_add_string_and_not_string()
        {
            var s1 = "a";
            var s2 = 1;
            System.Linq.Expressions.Expression<Func<string>> sExpr = () => s1 + s2;
            var expr = sExpr.FromSysExpression();
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual("a1", fs());

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual("a1", ff());
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        private sealed class NonPrimitiveInt32Class
        {
            private readonly int _value;

            public NonPrimitiveInt32Class(int value) => _value = value;
            public override bool Equals(object obj) => (obj as NonPrimitiveInt32Class)?._value.Equals(_value) ?? false;
            public override int GetHashCode() => _value.GetHashCode();

            public static NonPrimitiveInt32Class operator +(NonPrimitiveInt32Class left, NonPrimitiveInt32Class right) =>
                new NonPrimitiveInt32Class(left._value + right._value);
        }

        private struct NonPrimitiveInt32ValueType
        {
            private readonly int _value;

            public NonPrimitiveInt32ValueType(int value) => _value = value;

            public static NonPrimitiveInt32ValueType operator +(NonPrimitiveInt32ValueType left, NonPrimitiveInt32ValueType right) =>
                new NonPrimitiveInt32ValueType(left._value + right._value);
        }
    }
}
