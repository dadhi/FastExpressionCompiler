using System;
using NUnit.Framework;

#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unaasigned fields

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
namespace FastExpressionCompiler.UnitTests
#endif
{
        public class GeneralContainer
        {
            public byte? NullableByte { get; set; }
            public decimal Decimal { get; set; }
        }
        
    public class Issue183_NullableDecimal
    {
      [Test, Ignore("fixme")]
      public void NullableDecimalIssue()
      {
            var parameterExpression = Expression.Parameter(typeof(GeneralContainer));
            var left = Expression.Property(parameterExpression, nameof(GeneralContainer.NullableByte));
            var right = Expression.Property(parameterExpression, nameof(GeneralContainer.Decimal));
            var body = Expression.Equal(Expression.Convert(left, typeof(decimal?)), Expression.Convert(right, typeof(decimal?)));
            var obj = new ParameterExpression[1];
            obj[0] = parameterExpression;
            var fctn = Expression.Lambda<Func<GeneralContainer, bool>>(body, obj).CompileFast();

            Assert.DoesNotThrow(() => fctn(new GeneralContainer() { Decimal = 1 }) );
      }
    }
}
