using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
{
    public partial class LightExpressionTests
    {
        public void Can_roundtrip_compound_assignment_without_conversion()
        {
            var value = Variable(typeof(int), "value");
            var source = Lambda<Func<int>>(Block(new[] { value },
                Assign(value, Constant(3)), AddAssign(value, Constant(2)), value));

            var light = source.ToLightExpression();

            Asserts.AreEqual(5, light.CompileFast(true)());
            Asserts.AreEqual(5, light.ToLambdaExpression().Compile()());
        }

        public void Can_roundtrip_compound_assignment_with_conversion()
        {
            var value = Variable(typeof(byte), "value");
            var result = Parameter(typeof(int), "result");
            var conversion = Lambda(Convert(result, typeof(byte)), result);
            var method = typeof(LightExpressionTests).GetMethod(nameof(AddBytes));
            var source = Lambda<Func<byte>>(Block(new[] { value },
                Assign(value, Constant((byte)3)),
                AddAssign(value, Constant((byte)2), method, conversion), value));

            var light = source.ToLightExpression();

            Asserts.AreEqual((byte)5, light.ToLambdaExpression().Compile()());
        }

        public static int AddBytes(byte left, byte right) => left + right;
    }
}
