using System;
using System.Linq.Expressions;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public class ConstantAndConversionTests : ITest
{
    public int Run()
    {
        Can_the_closure_be_modified_afterwards();
        The_constant_changing_in_a_loop();

        Expressions_with_small_long_casts_should_not_crash();
        Expressions_with_larger_long_casts_should_not_crash();
        Expressions_with_long_constants_and_casts();
        Expressions_with_ulong_constants_and_casts();
        Expressions_with_DateTime();
        Expressions_with_DateTime_and_long_constant();
        Expressions_with_DateTime_and_ulong_constant();
        Expressions_with_DateTime_and_uint_constant();
        Expressions_with_max_uint_constant();
        Expressions_with_DateTime_and_double_constant();
        Expressions_with_DateTime_and_float_constant();
        Expressions_with_char_and_int();
        Expressions_with_char_and_short();
        Can_use_constant_of_byte_Enum_type();
        Can_return_constant();
        return 16;
    }

    public void Expressions_with_small_long_casts_should_not_crash()
    {
        var x = 65535;
        var y = 65535;
        Asserts.IsTrue(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => x == (long)y)).FromSysExpression(), true)());
    }

    public void Expressions_with_larger_long_casts_should_not_crash()
    {
        var y = 65536;
        var yn1 = y + 1;
        Asserts.IsTrue(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => yn1 != (long)y)).FromSysExpression(), true)());
    }

    public void Expressions_with_long_constants_and_casts()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0L == (long)"x".Length)).FromSysExpression(), true)());
    }

    public void Expressions_with_ulong_constants_and_casts()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0UL == (ulong)"x".Length)).FromSysExpression(), true)());
    }

    public void Expressions_with_DateTime()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0 == DateTime.Now.Day)).FromSysExpression(), true)());
    }

    public void Expressions_with_DateTime_and_long_constant()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0L == (long)DateTime.Now.Day)).FromSysExpression(), true)());
    }

    public void Expressions_with_DateTime_and_ulong_constant()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0UL == (ulong)DateTime.Now.Day)).FromSysExpression(), true)());
    }

    public void Expressions_with_DateTime_and_uint_constant()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0u == (uint)DateTime.Now.Day)).FromSysExpression(), true)());
    }

    public void Expressions_with_max_uint_constant()
    {
        const uint maxuint = UInt32.MaxValue;
        Asserts.IsFalse(maxuint == -1);
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => maxuint == -1)).FromSysExpression(), true)());
    }

    public void Expressions_with_DateTime_and_double_constant()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => (double)DateTime.Now.Day == 0d)).FromSysExpression(), true)());
    }

    public void Expressions_with_DateTime_and_float_constant()
    {
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 0f == (float)DateTime.Now.Day)).FromSysExpression(), true)());
    }

    public void Expressions_with_char_and_int()
    {
        Asserts.IsTrue(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 'z' != 0)).FromSysExpression(), true)());
    }

    public void Expressions_with_char_and_short()
    {
        Asserts.IsTrue(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => 'z' != (ushort)0)).FromSysExpression(), true)());
    }

    public void Can_use_constant_of_byte_Enum_type()
    {
        object obj = XByte.A;
        var e = Lambda(Constant(obj));

        var f = ExpressionCompiler.CompileFast<Func<XByte>>(e, true);

        Asserts.AreEqual(XByte.A, f());
    }

    public void Can_return_constant()
    {
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<uint>>)(() => 1u)).FromSysExpression(), true)(), 1u);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<short>>)(() => (short)1)).FromSysExpression(), true)(), (short)1);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<long>>)(() => 1L)).FromSysExpression(), true)(), 1L);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<ulong>>)(() => 1uL)).FromSysExpression(), true)(), 1uL);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<byte>>)(() => (byte)1)).FromSysExpression(), true)(), (byte)1);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<sbyte>>)(() => (sbyte)1)).FromSysExpression(), true)(), (sbyte)1);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<int>>)(() => 1)).FromSysExpression(), true)(), 1);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<float>>)(() => 1.1f)).FromSysExpression(), true)(), 1.1f);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<double>>)(() => 1.1d)).FromSysExpression(), true)(), 1.1d);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<decimal>>)(() => 1.1M)).FromSysExpression(), true)(), 1.1M);
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<char>>)(() => 'c')).FromSysExpression(), true)(), 'c');
        Asserts.AreEqual(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => true)).FromSysExpression(), true)(), true);
    }

    public class Foo<T>
    {
        public T Value;
    }

    public void Can_the_closure_be_modified_afterwards()
    {
        var expr = Lambda<Func<int>>(Field(Constant(new Foo<int> { Value = 43 }), nameof(Foo<int>.Value)));
        expr.PrintCSharp();

        var fs = expr.CompileFast(out var closure, true);
        Asserts.AreEqual(43, fs());

        if (closure.ConstantsAndNestedLambdas[0] is Foo<int> foo)
        {
            foo.Value = 44;
            Asserts.AreEqual(44, fs());

            closure.ConstantsAndNestedLambdas[0] = new Foo<int> { Value = 45 };
            Asserts.AreEqual(45, fs());
        }
    }

    public void The_constant_changing_in_a_loop()
    {
        for (int n = -200; n < 200; n++)
        {
            var blockExpr = Block(Constant(n));

            var lambda = Lambda<Func<int>>(blockExpr);

            var fastCompiled = lambda.CompileFast(true);

            Asserts.AreEqual(n, fastCompiled());
        }
    }

    public enum XByte : byte { A }
}
