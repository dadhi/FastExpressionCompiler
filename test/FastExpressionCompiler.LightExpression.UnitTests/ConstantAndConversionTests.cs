using System;
using System.Linq.Expressions;
using NUnit.Framework;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class ConstantAndConversionTests
    {
        [Test]
        public void Expressions_with_small_long_casts_should_not_crash()
        {
            var x = 65535;
            var y = 65535;
            Assert.IsTrue(ExpressionCompiler.Compile(() => x == (long)y)());
        }

        [Test]
        public void Expressions_with_larger_long_casts_should_not_crash()
        {
            var y = 65536;
            var yn1 = y + 1;
            Assert.IsTrue(ExpressionCompiler.Compile(() => yn1 != (long)y)());
        }

        [Test]
        public void Expressions_with_long_constants_and_casts()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0L == (long)"x".Length)());
        }

        [Test]
        public void Expressions_with_ulong_constants_and_casts()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0UL == (ulong)"x".Length)());
        }

        [Test]
        public void Expressions_with_DateTime()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0 == DateTime.Now.Day)());
        }

        [Test]
        public void Expressions_with_DateTime_and_long_constant()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0L == (long)DateTime.Now.Day)());
        }

        [Test]
        public void Expressions_with_DateTime_and_ulong_constant()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0UL == (ulong)DateTime.Now.Day)());
        }

        [Test]
        public void Expressions_with_DateTime_and_uint_constant()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0u == (uint)DateTime.Now.Day)());
        }

        [Test]
        public void Expressions_with_max_uint_constant()
        {
            const uint maxuint = UInt32.MaxValue;
            Assert.IsFalse(maxuint == -1);
            Assert.IsFalse(ExpressionCompiler.Compile(() => maxuint == -1)());
        }

        [Test]
        public void Expressions_with_DateTime_and_double_constant()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => (double)DateTime.Now.Day == 0d)());
        }

        [Test]
        public void Expressions_with_DateTime_and_float_constant()
        {
            Assert.IsFalse(ExpressionCompiler.Compile(() => 0f == (float)DateTime.Now.Day)());
        }

        [Test]
        public void Expressions_with_char_and_int()
        {
            Assert.IsTrue(ExpressionCompiler.Compile(() => 'z' != 0)());
        }

        [Test]
        public void Expressions_with_char_and_short()
        {
            Assert.IsTrue(ExpressionCompiler.Compile(() => 'z' != (ushort)0)());
        }

        [Test(Description = "Issue #7 InvalidCastException for enum constant of unsigned type")]
        public void Can_use_constant_of_byte_Enum_type()
        {
            object obj = XByte.A;
            var e = Expression.Lambda(Expression.Constant(obj));

            var f = ExpressionCompiler.TryCompile<Func<XByte>>(e);

            Assert.AreEqual(XByte.A, f());
        }

        [Test(Description = "Support all types and operations from System.Numerics")]
        public void Can_return_constant()
        {
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1u)(), 1u);
            Assert.AreEqual(ExpressionCompiler.Compile(() => (short)1)(), (short)1);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1L)(), 1L);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1uL)(), 1uL);
            Assert.AreEqual(ExpressionCompiler.Compile(() => (byte)1)(), (byte)1);
            Assert.AreEqual(ExpressionCompiler.Compile(() => (sbyte)1)(), (sbyte)1);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1)(), 1);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1.1f)(), 1.1f);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1.1d)(), 1.1d);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 1.1M)(), 1.1M);
            Assert.AreEqual(ExpressionCompiler.Compile(() => 'c')(), 'c');
            Assert.AreEqual(ExpressionCompiler.Compile(() => true)(), true);
        }

        public enum XByte : byte { A }
    }
}