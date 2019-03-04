using Xunit;
using using System.Linq.Expressions;

namespace FastExpressionCompiler.UnitTests
{
        public class GeneralContainer
        {
            public byte? NullableByte { get; set; }
            public decimal Decimal { get; set; }
        }
        
    public class Nullable_Decimal_Issue
    {
      [Fact(Skip="fixme")]
      public void NullableDecimalIssue()
      {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(GeneralContainer));
            MemberExpression left = Expression.Property(parameterExpression, nameof(GeneralContainer.NullableByte));
            MemberExpression right = Expression.Property(parameterExpression, nameof(GeneralContainer.Decimal));
            BinaryExpression body = Expression.Equal(Expression.Convert(left, typeof(decimal?)), Expression.Convert(right, typeof(decimal?)));
            ParameterExpression[] obj = new ParameterExpression[1];
            obj[0] = parameterExpression;
            Func<GeneralContainer, bool> fctn = Expression.Lambda<Func<GeneralContainer, bool>>(body, obj).CompileFast();

            var exception = Record.Exception(() => fctn(new GeneralContainer() { Decimal = 1 }));
           
            Assert.Null(exception);
      }
    }
}
