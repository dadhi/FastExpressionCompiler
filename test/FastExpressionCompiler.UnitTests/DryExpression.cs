using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastExpressionCompiler
{
    /// <summary>Base expression.</summary>
    public abstract class DryExpression
    {
        /// <summary>Conversion allows to go back to Linq.Expression.</summary>
        /// <param name="dryExpr">To convert.</param>
        public static implicit operator Expression(DryExpression dryExpr)
        {
            return dryExpr.ToExpression();
        }

        /// <summary>All expressions should have a Type.</summary>
        public abstract Type Type { get; }

        /// <summary>Explicit conversion to Linq.Expression</summary> <returns>Linq.Expression</returns>
        public abstract Expression ToExpression();

        /// <summary>Analog of Expression.New</summary>
        /// <param name="ctor">constructor info</param>
        /// <param name="args">argument expressions</param>
        /// <returns>New expression.</returns>
        public static NewDryExpression New(ConstructorInfo ctor, params DryExpression[] args)
        {
            return new NewDryExpression(ctor, args);
        }

        /// <summary>Analog of Expression.Constant</summary>
        /// <param name="value">constant value</param>
        /// <param name="type">(optional) constant type</param>
        /// <returns>Constant expression.</returns>
        public static ConstantDryExpression Constant(object value, Type type = null)
        {
            return new ConstantDryExpression(value, type);
        }

        /// <summary>Analog of Expression.Parameter</summary>
        /// <param name="type">parameter type</param>
        /// <param name="name">parameter name</param>
        /// <returns>Parameter expression.</returns>
        public static ParameterDryExpression Parameter(Type type, string name)
        {
            return new ParameterDryExpression(type, name);
        }

        public static LambdaDryExpression Lambda(DryExpression body, params ParameterDryExpression[] parameters)
        {
           return new LambdaDryExpression(body, parameters);
        }
    }

    public sealed class ConstantDryExpression : DryExpression
    {
        public readonly object Value;

        public override Type Type { get; }

        public override Expression ToExpression()
        {
            return Expression.Constant(Value, Type);
        }

        public ConstantDryExpression(object value, Type type = null)
        {
            Value = value;
            Type = type ?? (value == null ? typeof(object) : value.GetType());
        }
    }

    public sealed class ParameterDryExpression : DryExpression
    {
        public readonly string Name;

        public override Type Type { get; }

        public ParameterExpression ToParameterExpression()
        {
            return Expression.Parameter(Type, Name);
        }

        public override Expression ToExpression()
        {
            return ToParameterExpression();
        }

        public ParameterDryExpression(Type type, string name)
        {
            Type = type;
            Name = name;
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

        public override Expression ToExpression()
        {
            return Expression.New(Constructor, Arguments.Select(a => a.ToExpression()));
        }

        public NewDryExpression(ConstructorInfo constructor, params DryExpression[] arguments) : base(arguments)
        {
            Constructor = constructor;
            Type = Constructor.DeclaringType;
        }
    }

    public class LambdaDryExpression : DryExpression
    {
        public readonly DryExpression Body;
        public readonly ParameterDryExpression[] Parameters;

        public override Type Type { get { return Body.Type; } }

        public override Expression ToExpression()
        {
            return Expression.Lambda(Body.ToExpression(), Parameters.Select(p => p.ToParameterExpression()));
        }

        public LambdaDryExpression(DryExpression body, ParameterDryExpression[] parameters)
        {
            Body = body;
            Parameters = parameters;
        }
    }

    public sealed class DryExpression<TDelegate> : LambdaDryExpression
    {
        public Type DelegateType { get { return typeof(TDelegate); } } 

        public DryExpression(DryExpression body, ParameterDryExpression[] parameters) : base(body, parameters) { }

        public Expression<TDelegate> ToLambdaExpression()
        {
            return (Expression<TDelegate>)ToExpression();
        }
    }
}