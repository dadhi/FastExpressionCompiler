using System;
using System.Reflection;

namespace FastExpressionCompiler.Benchmarks
{
    public abstract class DryExpression
    {
        public abstract Type Type { get; }

        public static NewDryExpression New(ConstructorInfo ctor, params DryExpression[] args)
        {
            return new NewDryExpression(ctor, args);
        }

        public static ConstantDryExpression Constant(object value, Type type = null)
        {
            return new ConstantDryExpression(value, type);
        }
    }

    public sealed class ConstantDryExpression : DryExpression
    {
        public readonly object Value;

        public override Type Type { get; }

        public ConstantDryExpression(object value, Type type = null)
        {
            Value = value;
            Type = type ?? (value == null ? typeof(object) : value.GetType());
        }
    }

    public abstract class ArgumentsDryExpression : DryExpression
    {
        public readonly DryExpression[] Arguments;

        protected ArgumentsDryExpression(DryExpression[] arguments)
        {
            Arguments = arguments;
        }
    }

    public sealed class NewDryExpression : ArgumentsDryExpression
    {
        public readonly ConstructorInfo Constructor;

        public override Type Type { get; }

        public NewDryExpression(ConstructorInfo constructor, params DryExpression[] arguments) : base(arguments)
        {
            Constructor = constructor;
            Type = Constructor.DeclaringType;
        }
    }
}