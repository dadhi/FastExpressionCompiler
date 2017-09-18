using System;
using System.Linq.Expressions;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ArithmeticOperationsTests
    {
        [Test]
        public void Can_sum()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 + arg2;
            var sumFunc = expr.CompileFast(true);

            Assert.IsNotNull(sumFunc);
            Assert.AreEqual(sumFunc(1, 3), 4);
        }

        [Test]
        public void Can_sum_bytes()
        {
            Expression<Func<byte, byte, int>> expr = (arg1, arg2) => arg1 + arg2;
            var sumFunc = expr.CompileFast(true);

            Assert.IsNotNull(sumFunc);
            Assert.AreEqual(sumFunc(1, 3), 4);
        }

        [Test]
        public void Can_sum_signed_bytes()
        {
            Expression<Func<sbyte, sbyte, int>> expr = (arg1, arg2) => arg1 + arg2;
            var sumFunc = expr.CompileFast(true);

            Assert.IsNotNull(sumFunc);
            Assert.AreEqual(sumFunc(1, 3), 4);
        }

        [Test]
        [TestCase(1, 2, 3)]
        [TestCase((short)1, (short)2, (short)3)]
        [TestCase((ushort)1, (ushort)2, (ushort)3)]
        [TestCase(2u, 3u, 5u)]
        [TestCase(2ul, 3ul, 5ul)]
        [TestCase(3L, 4L, 7L)]
        [TestCase(4f, 5f, 9f)]
        [TestCase(5d, 6d, 11d)]
        public void Can_sum_all_primitive_numeric_types_that_define_binary_operator_add(object param1, object param2, object expectedResult) =>
            expectedResult.ShouldBeResultOfArithmeticOperation((a1, a2) => Add(a1, a2), param1, param2);

        [Test]
        public void Can_sum_with_unchecked_overflow()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => unchecked(arg1 + arg2);

            var sumFunc = expr.CompileFast(true);

            Assert.IsNotNull(sumFunc);
            Assert.AreEqual(sumFunc(int.MaxValue, 1), int.MinValue);
        }

        [Test]
        public void Can_not_sum_with_checked_overflow()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => checked(arg1 + arg2);

            var sumFunc = expr.CompileFast(true);

            Assert.IsNotNull(sumFunc);
            Assert.Throws<OverflowException>(() => sumFunc(int.MaxValue, 1));
        }

        [Test]
        public void Can_substract()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 - arg2;

            var substractFunc = expr.CompileFast(true);

            Assert.IsNotNull(substractFunc);
            Assert.AreEqual(substractFunc(7, 3), 4);
        }

        [Test]
        public void Can_substract_bytes()
        {
            Expression<Func<byte, byte, int>> expr = (arg1, arg2) => arg1 - arg2;

            var substractFunc = expr.CompileFast(true);

            Assert.IsNotNull(substractFunc);
            Assert.AreEqual(substractFunc(7, 3), 4);
        }

        [Test]
        public void Can_substract_signed_bytes()
        {
            Expression<Func<sbyte, sbyte, int>> expr = (arg1, arg2) => arg1 - arg2;

            var substractFunc = expr.CompileFast(true);

            Assert.IsNotNull(substractFunc);
            Assert.AreEqual(substractFunc(7, 3), 4);
        }

        [Test]
        [TestCase(3, 2, 1)]
        [TestCase((short)3, (short)2, (short)1)]
        [TestCase((ushort)3, (ushort)2, (ushort)1)]
        [TestCase(3u, 2u, 1u)]
        [TestCase(3ul, 2ul, 1ul)]
        [TestCase(3L, 2L, 1L)]
        [TestCase(3f, 2f, 1f)]
        [TestCase(3d, 2d, 1d)]
        public void Can_substract_all_primitive_numeric_types_that_define_binary_operator_substract(object param1, object param2, object expectedResult) =>
            expectedResult.ShouldBeResultOfArithmeticOperation((a1, a2) => Subtract(a1, a2), param1, param2);

        [Test]
        public void Can_substarct_with_unchecked_overflow()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => unchecked(arg1 - arg2);

            var substractFunc = expr.CompileFast(true);

            Assert.IsNotNull(substractFunc);
            Assert.AreEqual(substractFunc(int.MinValue, 1), int.MaxValue);
        }

        [Test]
        public void Can_not_substarct_with_checked_overflow()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => checked(arg1 - arg2);

            var substractFunc = expr.CompileFast(true);

            Assert.IsNotNull(substractFunc);
            Assert.Throws<OverflowException>(() => substractFunc(int.MinValue, 1));
        }

        [Test]
        public void Can_multiply()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 * arg2;

            var multiplyFunc = expr.CompileFast(true);

            Assert.IsNotNull(multiplyFunc);
            Assert.AreEqual(multiplyFunc(7, 3), 21);
        }

        [Test]
        public void Can_multiply_bytes()
        {
            Expression<Func<byte, byte, int>> expr = (arg1, arg2) => arg1 * arg2;

            var multiplyFunc = expr.CompileFast(true);

            Assert.IsNotNull(multiplyFunc);
            Assert.AreEqual(multiplyFunc(7, 3), 21);
        }

        [Test]
        public void Can_multiply_signed_bytes()
        {
            Expression<Func<sbyte, sbyte, int>> expr = (arg1, arg2) => arg1 * arg2;

            var multiplyFunc = expr.CompileFast(true);

            Assert.IsNotNull(multiplyFunc);
            Assert.AreEqual(multiplyFunc(7, 3), 21);
        }

        [Test]
        [TestCase(3, 2, 6)]
        [TestCase((short)3, (short)2, (short)6)]
        [TestCase((ushort)3, (ushort)2, (ushort)6)]
        [TestCase(3u, 2u, 6u)]
        [TestCase(3ul, 2ul, 6ul)]
        [TestCase(3L, 2L, 6L)]
        [TestCase(3f, 2f, 6f)]
        [TestCase(3d, 2d, 6d)]
        public void Can_multiply_all_primitive_numeric_types_that_define_binary_operator_multiply(object param1, object param2, object expectedResult) =>
            expectedResult.ShouldBeResultOfArithmeticOperation((a1, a2) => Multiply(a1, a2), param1, param2);


        [Test]
        public void Can_multiply_with_unchecked_overflow()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => unchecked(arg1 * arg2);

            var multiplyFunc = expr.CompileFast(true);

            Assert.IsNotNull(multiplyFunc);
            Assert.AreEqual(multiplyFunc(int.MaxValue, int.MaxValue), 1);
        }

        [Test]
        public void Can_not_multiply_with_checked_overflow()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => checked(arg1 * arg2);

            var multiplyFunc = expr.CompileFast(true);

            Assert.IsNotNull(multiplyFunc);
            Assert.Throws<OverflowException>(() => multiplyFunc(int.MaxValue, int.MaxValue));
        }

        [Test]
        public void Can_divide()
        {
            Expression<Func<int, int, int>> expr = (arg1, arg2) => arg1 / arg2;

            var divideFunc = expr.CompileFast(true);

            Assert.IsNotNull(divideFunc);
            Assert.AreEqual(divideFunc(7, 3), 2);
        }

        [Test]
        public void Can_divide_bytes()
        {
            Expression<Func<byte, byte, int>> expr = (arg1, arg2) => arg1 / arg2;

            var divideFunc = expr.CompileFast(true);

            Assert.IsNotNull(divideFunc);
            Assert.AreEqual(divideFunc(7, 3), 2);
        }

        [Test]
        public void Can_divide_signed_bytes()
        {
            Expression<Func<sbyte, sbyte, int>> expr = (arg1, arg2) => arg1 / arg2;

            var divideFunc = expr.CompileFast(true);

            Assert.IsNotNull(divideFunc);
            Assert.AreEqual(divideFunc(7, 3), 2);
        }

        /*TODO: Add support of decimal
        [Test]
        public void Can_sum_decimal_numbers()
        {
            Expression<Func<decimal, decimal, decimal>> expr = (arg1, arg2) => arg1 + arg2;

            var sumFunc = expr.CompileFast(true);

            Assert.IsNotNull(sumFunc);
            Assert.AreEqual(sumFunc(1.0m, 2.1m), 1.0m + 2.1m);
        }

        [Test]
        public void Can_substract_decimal_numbers()
        {
            Expression<Func<decimal, decimal, decimal>> expr = (arg1, arg2) => arg1 - arg2;

            var substractFunc = expr.CompileFast(true);

            Assert.IsNotNull(substractFunc);
            Assert.AreEqual(substractFunc(2.0m, 1.0m), 2.0m - 1.0m);
        }

        [Test]
        public void Can_multiply_decimal_numbers()
        {
            Expression<Func<decimal, decimal, decimal>> expr = (arg1, arg2) => arg1 * arg2;

            var multiplyFunc = expr.CompileFast(true);

            Assert.IsNotNull(multiplyFunc);
            Assert.AreEqual(multiplyFunc(2.0m, 1.0m), 2.0m * 1.0m);
        }

        [Test]
        public void Can_divide_decimal_numbers()
        {
            Expression<Func<decimal, decimal, decimal>> expr = (arg1, arg2) => arg1 / arg2;

            var divideFunc = expr.CompileFast(true);

            Assert.IsNotNull(divideFunc);
            Assert.AreEqual(divideFunc(2.0m, 1.0m), 2.0m / 1.0m);
        }*/

        [Test]
        [TestCase(6, 3, 2)]
        [TestCase((short)6, (short)3, (short)2)]
        [TestCase((ushort)6, (ushort)3, (ushort)2)]
        [TestCase(6u, 3u, 2u)]
        [TestCase(6L, 3L, 2L)]
        [TestCase(6f, 3f, 2f)]
        [TestCase(6d, 3d, 2d)]
        public void Can_divide_all_primitive_numeric_types_that_define_binary_operator_divide(object param1, object param2, object expectedResult) =>
            expectedResult.ShouldBeResultOfArithmeticOperation((a1, a2) => Divide(a1, a2), param1, param2);


        [Test]
        public void Can_calculate_arithmetic_function_obeying_operator_precedence()
        {
            Expression<Func<int, int, int, int, int>> expr = (arg1, arg2, arg3, arg4) => arg1 + arg2 * arg3 / arg4;

            var arithmeticFunc = expr.CompileFast(true);

            Assert.IsNotNull(arithmeticFunc);
            Assert.AreEqual(arithmeticFunc(1, 2, 3, 4), 2);
        }
    }

    internal static class ArithmeticAssertationExtensions
    {
        public static void ShouldBeResultOfArithmeticOperation(this object expectedResult, Func<ParameterExpression, ParameterExpression, Expression> arithmeticOperation, object param1, object param2)
        {
            AssertArithmeticOperation((dynamic)expectedResult, arithmeticOperation, (dynamic)param1, (dynamic)param2);
        }

        private static void AssertArithmeticOperation<T>(T expectedResult, Func<ParameterExpression, ParameterExpression, Expression> arithmeticOperation, T param1, T param2)
        {
            var arg1 = Parameter(typeof(T), "arg1");
            var arg2 = Parameter(typeof(T), "arg2");
            var expr = Lambda(arithmeticOperation(arg1, arg2), arg1, arg2);

            var arithmeticFunc = expr.CompileFast<Func<T, T, T>>(true);

            Assert.IsNotNull(arithmeticFunc);
            Assert.AreEqual(arithmeticFunc(param1, param2), expectedResult);
        }
    }
}
