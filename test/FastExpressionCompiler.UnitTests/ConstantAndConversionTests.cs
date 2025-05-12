using System;
using System.Diagnostics;
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
#if LIGHT_EXPRESSION
        Issue457_The_constant_changing_in_a_loop_without_recompilation();
        Issue464_Bound_closure_constants_can_be_modified_afterwards();
        Issue465_The_primitive_constant_can_be_configured_to_put_in_closure();
        Issue466_The_constant_may_be_referenced_multiple_times();
#endif
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
        return 18;
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
        const uint maxUint = UInt32.MaxValue;
        Asserts.IsFalse(maxUint == -1);
        Asserts.IsFalse(ExpressionCompiler.CompileFast(((System.Linq.Expressions.Expression<Func<bool>>)(() => maxUint == -1)).FromSysExpression(), true)());
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

#if LIGHT_EXPRESSION

    public void Issue464_Bound_closure_constants_can_be_modified_afterwards()
    {
        var foo = new Foo<int> { Value = 43 };
        var expr = Lambda<Func<int>>(Field(Constant(foo), nameof(Foo<int>.Value)));
        expr.PrintCSharp();

        var fs = expr.CompileFast(true);
        fs.PrintIL();

        Asserts.AreEqual(43, fs());

        foo.Value = 45;
        Asserts.AreEqual(45, fs());
    }

    public void Issue465_The_primitive_constant_can_be_configured_to_put_in_closure()
    {
        var expr = Lambda<Func<int>>(ConstantRef(16, out var n));
        expr.PrintCSharp();

        var fs = expr.CompileFast(true);
        fs.PrintIL();

        Asserts.AreEqual(16, fs());

        n.Value = 45; // <-- WIN!
        Asserts.AreEqual(45, fs());
    }

    public void Issue466_The_constant_may_be_referenced_multiple_times()
    {
        var nExpr = ConstantRef(16, out var n);
        var expr = Lambda<Func<int>>(Add(nExpr, nExpr));
        expr.PrintCSharp();

        var fs = expr.CompileFast(true);
        fs.PrintIL();

        Asserts.AreEqual(32, fs());

        n.Value = 45;
        Asserts.AreEqual(90, fs());
    }

    public void Issue457_The_constant_changing_in_a_loop_without_recompilation()
    {
        var sw = Stopwatch.StartNew();
        var refConst = ConstantRef(0, out var val);
        var blockExpr = Block(refConst);
        var lambda = Lambda<Func<int>>(blockExpr);
        var fastCompiled = lambda.CompileFast(true);

        for (int n = -200; n < 200; n++)
        {
            val.Value = n;
            Asserts.AreEqual(n, fastCompiled());
        }

        Debug.WriteLine($"Issue457_The_constant_changing_in_a_loop_without_recompilation, elapsed: {sw.ElapsedMilliseconds}ms");
        sw.Stop();
    }
#endif

    public void The_constant_changing_in_a_loop()
    {
        var sw = Stopwatch.StartNew();
        for (int n = -200; n < 200; n++)
        {
            var blockExpr = Block(Constant(n));

            var lambda = Lambda<Func<int>>(blockExpr);

            var fastCompiled = lambda.CompileFast(true);

            Asserts.AreEqual(n, fastCompiled());
        }
        Debug.WriteLine($"The_constant_changing_in_a_loop, elapsed: {sw.ElapsedMilliseconds}ms");
        sw.Stop();
    }

    public enum XByte : byte { A }
}
