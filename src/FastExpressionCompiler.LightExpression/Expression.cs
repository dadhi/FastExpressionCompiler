/*
The MIT License (MIT)

Copyright (c) 2016 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included AddOrUpdateServiceFactory
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// ReSharper disable CoVariantArrayConversion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.LightExpression
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>Facade for constructing Expression.</summary>
    public abstract class Expression
    {
        /// <summary>Expression node type.</summary>
        public abstract ExpressionType NodeType { get; }

        /// <summary>All expressions should have a Type.</summary>
        public abstract Type Type { get; }

        /// <summary>Converts back to respective System Expression, so you may Compile it by usual means.</summary>
        public abstract SysExpr ToExpression();

        /// <summary>Converts to Expression and outputs its as string</summary>
        public override string ToString() => ToExpression().ToString();

        public static ParameterExpression Parameter(Type type, string name = null) =>
            new ParameterExpression(type.IsByRef ? type.GetElementType() : type, name, type.IsByRef);

        public static ParameterExpression Variable(Type type, string name = null) => Parameter(type, name);

        public static ConstantExpression Constant(object value, Type type = null) =>
            value == null && type == null ? _nullExprInfo
                : new ConstantExpression(value, type ?? value.GetType());

        private static readonly ConstantExpression _nullExprInfo = new ConstantExpression(null, typeof(object));

        public static NewExpression New(Type type) =>
            new NewExpression(type.GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0), Tools.Empty<Expression>());

        public static NewExpression New(ConstructorInfo ctor) =>
            new NewExpression(ctor, Tools.Empty<Expression>());

        public static NewExpression New(ConstructorInfo ctor, params Expression[] arguments) =>
            new NewExpression(ctor, arguments);

        public static MethodCallExpression Call(Expression instance, MethodInfo method, params Expression[] arguments) =>
            new MethodCallExpression(instance, method, arguments);

        public static MethodCallExpression Call(MethodInfo method, params Expression[] arguments) =>
            Call(null, method, arguments);

        public static MethodCallExpression Call(Type type, string methodName, Type[] typeArguments, params Expression[] arguments) => 
            Call(null, type.FindMethod(true, methodName, typeArguments, arguments), arguments);

        public static MethodCallExpression Call(Expression instance, string methodName, Type[] typeArguments, params Expression[] arguments) =>
            new MethodCallExpression(instance, instance.Type.FindMethod(true, methodName, typeArguments, arguments), arguments);

        public static MemberExpression Property(PropertyInfo property) =>
            new PropertyExpression(null, property);

        public static MemberExpression Property(Expression instance, PropertyInfo property) =>
            new PropertyExpression(instance, property);

        public static MemberExpression Property(Expression expression, string propertyName) => 
            Property(expression, expression.Type.FindProperty(propertyName));

        public static IndexExpression Property(Expression instance, PropertyInfo indexer, params Expression[] arguments) =>
            new IndexExpression(instance, indexer, arguments);

        public static IndexExpression Property(Expression instance, PropertyInfo indexer, IEnumerable<Expression> arguments) => 
            new IndexExpression(instance, indexer, arguments.AsReadOnlyList());

        public static MemberExpression PropertyOrField(Expression expression, string propertyName) =>
            expression.Type.FindProperty(propertyName) != null ? 
                (MemberExpression) new PropertyExpression(expression, expression.Type.FindProperty(propertyName)) :
                (MemberExpression) new FieldExpression(expression, expression.Type.FindField(propertyName));

        public static IndexExpression MakeIndex(Expression instance, PropertyInfo indexer, IEnumerable<Expression> arguments) => 
            indexer != null ? Property(instance, indexer, arguments) : ArrayAccess(instance, arguments);

        public static IndexExpression ArrayAccess(Expression array, params Expression[] indexes) =>
            new IndexExpression(array, null, indexes);

        public static IndexExpression ArrayAccess(Expression array, IEnumerable<Expression> indexes) => 
            new IndexExpression(array, null, indexes.AsReadOnlyList());

        public static MemberExpression Field(FieldInfo field) =>
            new FieldExpression(null, field);

        public static MemberExpression Field(Expression instance, FieldInfo field) =>
            new FieldExpression(instance, field);

        public static MemberExpression Field(Expression instance, string fieldName) => 
            new FieldExpression(instance, instance.Type.FindField(fieldName));

        public static LambdaExpression Lambda(Expression body) =>
            new LambdaExpression(null, body, Tools.Empty<ParameterExpression>());

        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters) =>
            new LambdaExpression(null, body, parameters);

        public static LambdaExpression Lambda(Type delegateType, Expression body, params ParameterExpression[] parameters) =>
            new LambdaExpression(delegateType, body, parameters);

        public static UnaryExpression Convert(Expression operand, Type targetType) =>
            new UnaryExpression(ExpressionType.Convert, operand, targetType);

        public static UnaryExpression Convert(Expression operand, Type targetType, MethodInfo method) =>
            new UnaryExpression(ExpressionType.Convert, operand, targetType, method);

        public static UnaryExpression PreIncrementAssign(Expression operand) =>
            new UnaryExpression(ExpressionType.PreIncrementAssign, operand, (System.Type)null);

        public static UnaryExpression PostIncrementAssign(Expression operand) =>
            new UnaryExpression(ExpressionType.PostIncrementAssign, operand, (System.Type)null);

        public static UnaryExpression PreDecrementAssign(Expression operand) =>
            new UnaryExpression(ExpressionType.PreDecrementAssign, operand, (System.Type)null);

        public static UnaryExpression PostDecrementAssign(Expression operand) =>
            new UnaryExpression(ExpressionType.PostDecrementAssign, operand, (System.Type)null);

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body) =>
            new Expression<TDelegate>(body, Tools.Empty<ParameterExpression>());

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters) =>
            new Expression<TDelegate>(body, parameters);

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string name, params ParameterExpression[] parameters) =>
            new Expression<TDelegate>(body, parameters);

        public static BinaryExpression ArrayIndex(Expression array, Expression index) =>
            new ArrayIndexExpression(array, index, array.Type.GetElementType());

        public static MemberAssignment Bind(MemberInfo member, Expression expression) => 
            new MemberAssignment(member, expression);

        public static MemberInitExpression MemberInit(NewExpression newExpr, params MemberBinding[] bindings) =>
            new MemberInitExpression(newExpr, bindings);

        /// <summary>Does not present in System Expression. Enables member assignment on existing instance expression.</summary>
        public static MemberInitExpression MemberInit(Expression instanceExpr, params MemberBinding[] assignments) =>
            new MemberInitExpression(instanceExpr, assignments);

        public static NewArrayExpression NewArrayInit(Type type, params Expression[] initializers) =>
            new NewArrayExpression(ExpressionType.NewArrayInit, type.MakeArrayType(), initializers);

        public static NewArrayExpression NewArrayBounds(Type type, params Expression[] bounds) =>
            new NewArrayExpression(ExpressionType.NewArrayBounds, 
                bounds.Length == 1 ? type.MakeArrayType() : type.MakeArrayType(bounds.Length), 
                bounds);

        public static BinaryExpression Assign(Expression left, Expression right) =>
            new AssignBinaryExpression(left, right, left.Type);

        public static BinaryExpression PowerAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.PowerAssign, left, right, left.Type);

        public static BinaryExpression AddAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.AddAssign, left, right, left.Type);

        public static BinaryExpression SubtractAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.SubtractAssign, left, right, left.Type);

        public static BinaryExpression MultiplyAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.MultiplyAssign, left, right, left.Type);

        public static BinaryExpression DivideAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.DivideAssign, left, right, left.Type);

        public static InvocationExpression Invoke(Expression lambda, params Expression[] args) =>
            new InvocationExpression(lambda, args, lambda.Type);

        public static InvocationExpression Invoke(Expression lambda, IEnumerable<Expression> args) =>
            new InvocationExpression(lambda, args.AsReadOnlyList(), lambda.Type);

        public static ConditionalExpression Condition(Expression test, Expression ifTrue, Expression ifFalse) =>
            new ConditionalExpression(test, ifTrue, ifFalse);

        public static ConditionalExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type type) =>
            new ConditionalExpression(test, ifTrue, ifFalse, type);

        public static ConditionalExpression IfThen(Expression test, Expression ifTrue) => 
            Condition(test, ifTrue, Empty(), typeof(void));

        public static DefaultExpression Empty() => new DefaultExpression(typeof(void));

        public static DefaultExpression Default(Type type) => 
            type == typeof(void) ? Empty() : new DefaultExpression(type);

        public static ConditionalExpression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse) => 
            Condition(test, ifTrue, ifFalse, typeof(void));

        public static Expression Add(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Add, left, right, left.Type);

        public static Expression Substract(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Subtract, left, right, left.Type);

        public static Expression Multiply(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Multiply, left, right, left.Type);

        public static Expression Divide(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Divide, left, right, left.Type);

        public static Expression Power(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Power, left, right, left.Type);

        public static Expression Equal(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Equal, left, right, left.Type);

        public static Expression GreaterThan(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.GreaterThan, left, right, left.Type);

        public static Expression GreaterThanOrEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.GreaterThanOrEqual, left, right, left.Type);

        public static Expression LessThan(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.LessThan, left, right, left.Type);

        public static Expression LessThanOrEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.LessThanOrEqual, left, right, left.Type);

        public static Expression NotEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.NotEqual, left, right, left.Type);

        public static BlockExpression Block(params Expression[] expressions) => 
            Block(Tools.Empty<ParameterExpression>(), expressions);

        public static BlockExpression Block(IEnumerable<ParameterExpression> variables, params Expression[] expressions) =>
            Block(expressions[expressions.Length - 1].Type, variables.AsReadOnlyList(), expressions);

        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, params Expression[] expressions) =>
            new BlockExpression(type, variables.AsReadOnlyList(), expressions);

        public static TryExpression TryCatch(Expression body, params CatchBlock[] handlers) =>
            new TryExpression(body, null, handlers);

        public static TryExpression TryCatchFinally(Expression body, Expression @finally, params CatchBlock[] handlers) =>
            new TryExpression(body, @finally, handlers);

        public static TryExpression TryFinally(Expression body, Expression @finally) =>
            new TryExpression(body, @finally, null);

        public static CatchBlock Catch(ParameterExpression variable, Expression body) =>
            new CatchBlock(variable, body, null, variable.Type);

        public static CatchBlock Catch(Type test, Expression body) =>
            new CatchBlock(null, body, null, test);

        public static UnaryExpression Throw(Expression value) =>
            new UnaryExpression(ExpressionType.Throw, value, typeof(void));

        public static LabelExpression Label(LabelTarget target, Expression defaultValue = null) => 
            new LabelExpression(target, defaultValue);

        public static LabelTarget Label(Type type = null, string name = null) => 
            SysExpr.Label(type ?? typeof(void), name);

        public static LabelTarget Label(string name) =>
            SysExpr.Label(typeof(void), name);

        public static GotoExpression MakeGoto(GotoExpressionKind kind, LabelTarget target, Expression value, Type type = null) => 
            new GotoExpression(kind, target, value, type ?? typeof(void));

        public static GotoExpression Break(LabelTarget target, Expression value = null, Type type = null) => 
            MakeGoto(GotoExpressionKind.Break, target, value, type);

        public static GotoExpression Continue(LabelTarget target, Type type = null) => 
            MakeGoto(GotoExpressionKind.Continue, target, null, type);

        public static GotoExpression Return(LabelTarget target, Expression value = null, Type type = null) => 
            MakeGoto(GotoExpressionKind.Return, target, value);

        public static GotoExpression Goto(LabelTarget target, Expression value = null, Type type = null) => 
            MakeGoto(GotoExpressionKind.Goto, target, value, type);

        public static BinaryExpression Coalesce(Expression left, Expression right) => Coalesce(left, right, null);

        public static BinaryExpression Coalesce(Expression left, Expression right, LambdaExpression conversion) => 
            conversion == null ?
                new SimpleBinaryExpression(ExpressionType.Coalesce, left, right, GetCoalesceType(left.Type, right.Type)) : 
                (BinaryExpression)new CoalesceConversionBinaryExpression(left, right, conversion);

        private static Type GetCoalesceType(Type left, Type right)
        {
            var leftNonNullable = left.UnpackNullableOrSelf();
            if (leftNonNullable != left && right.IsImplicitlyConvertibleTo(leftNonNullable))
                return leftNonNullable;

            if (right.IsImplicitlyConvertibleTo(left))
                return left;

            if (leftNonNullable.IsImplicitlyConvertibleTo(right))
                return right;

            throw new ArgumentException($"Unable to coalesce arguments of left type of {left} and right type of {right}.");
        }
    }

    internal static class TypeTools
    {
        internal static Type UnpackNullableOrSelf(this Type type) => 
            type.IsNullable() ? type.GetTypeInfo().GenericTypeArguments[0] : type;

        internal static bool IsImplicitlyConvertibleTo(this Type source, Type target) =>
            source == target ||
            target.GetTypeInfo().IsAssignableFrom(source.GetTypeInfo()) ||
            source.IsImplicitlyBoxingConvertibleTo(target) ||
            source.IsImplicitlyNumericConvertibleTo(target);

        internal static bool IsImplicitlyBoxingConvertibleTo(this Type source, Type target) => 
            source.IsValueType() &&
            (target == typeof(object) || 
             target == typeof(ValueType)) || 
             source.GetTypeInfo().IsEnum && target == typeof(Enum);

        internal static bool IsImplicitlyNumericConvertibleTo(this Type source, Type target)
        {
            if (source == typeof(Char))
                return 
                    target == typeof(UInt16) || 
                    target == typeof(Int32) || 
                    target == typeof(UInt32) || 
                    target == typeof(Int64) || 
                    target == typeof(UInt64) || 
                    target == typeof(Single) || 
                    target == typeof(Double) || 
                    target == typeof(Decimal);

            if (source == typeof(SByte))
                return
                    target == typeof(Int16) ||
                    target == typeof(Int32) ||
                    target == typeof(Int64) ||
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(Byte))
                return
                    target == typeof(Int16) ||
                    target == typeof(UInt16) ||
                    target == typeof(Int32) ||
                    target == typeof(UInt32) ||
                    target == typeof(Int64) ||
                    target == typeof(UInt64) ||
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(Int16))
                return
                    target == typeof(Int32) ||
                    target == typeof(Int64) ||
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(UInt16))
                return
                    target == typeof(Int32) ||
                    target == typeof(UInt32) ||
                    target == typeof(Int64) ||
                    target == typeof(UInt64) ||
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(Int32))
                return
                    target == typeof(Int64) ||
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(UInt32))
                return
                    target == typeof(UInt32) ||
                    target == typeof(UInt64) ||
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(Int64) ||
                source == typeof(UInt64))
                return
                    target == typeof(Single) ||
                    target == typeof(Double) ||
                    target == typeof(Decimal);

            if (source == typeof(Single))
                return target == typeof(Double);

            return false;
        }

        internal static MethodInfo FindMethod(this Type type, bool isStatic,
            string methodName, Type[] typeArguments, Expression[] arguments) => 
            type.GetTypeInfo().DeclaredMethods.GetFirst(m =>
            {
                if (isStatic && !m.IsStatic)
                    return false;

                if (m.Name != methodName)
                    return false;

                var mTypeArgs = m.GetGenericArguments();
                typeArguments = typeArguments ?? Type.EmptyTypes;
                if (mTypeArgs.Length != typeArguments.Length ||
                    mTypeArgs.Length != 0 && !mTypeArgs.SequenceEqual(typeArguments))
                    return false;

                var mParams = m.GetParameters();
                arguments = arguments ?? Tools.Empty<Expression>();

                if (mParams.Length != arguments.Length ||
                    mParams.Length != 0 && !mParams.Map(p => p.ParameterType).SequenceEqual(arguments.Map(a => a.Type)))
                    return false;

                return true;
            });

        internal static PropertyInfo FindProperty(this Type type, string propertyName) =>
            type.GetTypeInfo().GetDeclaredProperty(propertyName);

        internal static FieldInfo FindField(this Type type, string fieldName) =>
            type.GetTypeInfo().GetDeclaredField(fieldName);
    }

    public class UnaryExpression : Expression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly Expression Operand;
        public readonly MethodInfo Method;

        public override SysExpr ToExpression()
        {
            if (NodeType == ExpressionType.Convert)
                return SysExpr.Convert(Operand.ToExpression(), Type);
            throw new NotSupportedException("Cannot convert Expression to Expression of type " + NodeType);
        }

        public UnaryExpression(ExpressionType nodeType, Expression operand, Type type)
        {
            NodeType = nodeType;
            Operand = operand;
            Type = type;
        }

        public UnaryExpression(ExpressionType nodeType, Expression operand, MethodInfo method)
        {
            NodeType = nodeType;
            Operand = operand;
            Method = method;
            Type = Method.ReturnType; // todo: check that
        }

        public UnaryExpression(ExpressionType nodeType, Expression operand, Type type, MethodInfo method)
        {
            NodeType = nodeType;
            Operand = operand;
            Method = method;
            Type = type;
        }
    }

    public abstract class BinaryExpression : Expression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly Expression Left, Right;

        protected BinaryExpression(ExpressionType nodeType, Expression left, Expression right, Type type)
        {
            NodeType = nodeType;
            Type = type;
            Left = left;
            Right = right;
        }
    }

    public sealed class SimpleBinaryExpression : BinaryExpression
    {
        public override SysExpr ToExpression()
        {
            switch (NodeType) {
                case ExpressionType.Add:
                    return SysExpr.Add(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Subtract:
                    return SysExpr.Subtract(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Multiply:
                    return SysExpr.Multiply(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Divide:
                    return SysExpr.Divide(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Coalesce:
                    return SysExpr.Coalesce(Left.ToExpression(), Right.ToExpression());
                default:
                    throw new NotSupportedException($"Not a valid {NodeType} for arithmetic binary expression.");
            }
        }

        internal SimpleBinaryExpression(ExpressionType nodeType, Expression left, Expression right, Type type)
            : base(nodeType, left, right, type) { }
    }

    public class CoalesceConversionBinaryExpression : BinaryExpression
    {
        public readonly LambdaExpression Conversion;

        public override SysExpr ToExpression() =>
            SysExpr.Coalesce(Left.ToExpression(), Right.ToExpression(), Conversion.ToLambdaExpression());

        internal CoalesceConversionBinaryExpression(Expression left, Expression right, LambdaExpression conversion)
            : base(ExpressionType.Coalesce, left, right, null)
        {
            Conversion = conversion;
        }
    }

    public sealed class ArrayIndexExpression : BinaryExpression
    {
        public override SysExpr ToExpression() => SysExpr.ArrayIndex(Left.ToExpression(), Right.ToExpression());

        internal ArrayIndexExpression(Expression left, Expression right, Type type)
            : base(ExpressionType.ArrayIndex, left, right, type) { }
    }

    public sealed class AssignBinaryExpression : BinaryExpression
    {
        public override SysExpr ToExpression() => SysExpr.Assign(Left.ToExpression(), Right.ToExpression());

        internal AssignBinaryExpression(Expression left, Expression right, Type type)
            : base(ExpressionType.Assign, left, right, type) { }

        internal AssignBinaryExpression(ExpressionType expressionType, Expression left, Expression right, Type type)
            : base(expressionType, left, right, type) { }
    }

    public sealed class MemberInitExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.MemberInit;
        public override Type Type => Expression.Type;

        public NewExpression NewExpression => Expression as NewExpression;

        public readonly Expression Expression;
        public readonly IReadOnlyList<MemberBinding> Bindings;

        public override SysExpr ToExpression() =>
            SysExpr.MemberInit(NewExpression.ToNewExpression(), Bindings.Map(b => b.ToMemberBinding()));

        internal MemberInitExpression(NewExpression newExpression, MemberBinding[] bindings)
            : this((Expression)newExpression, bindings) { }

        internal MemberInitExpression(Expression expression, MemberBinding[] bindings)
        {
            Expression = expression;
            Bindings = bindings ?? Tools.Empty<MemberBinding>();
        }
    }

    public sealed class ParameterExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Parameter;
        public override Type Type { get; }

        public readonly string Name;
        public readonly bool IsByRef;

        public override SysExpr ToExpression() => ToParamExpr();
        public System.Linq.Expressions.ParameterExpression ToParamExpr() => SysExpr.Parameter(Type, Name);

        internal ParameterExpression(Type type, string name, bool isByRef)
        {
            Type = type;
            Name = name;
            IsByRef = isByRef;
        }
    }

    public sealed class ConstantExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Constant;
        public override Type Type { get; }

        public readonly object Value;

        public override SysExpr ToExpression() => SysExpr.Constant(Value, Type);

        internal ConstantExpression(object value, Type type)
        {
            Value = value;
            Type = type;
        }
    }

    public abstract class ArgumentsExpression : Expression
    {
        public readonly IReadOnlyList<Expression> Arguments;

        protected SysExpr[] ArgumentsToExpressions() => Arguments.Map(info => info.ToExpression());

        protected ArgumentsExpression(IReadOnlyList<Expression> arguments)
        {
            Arguments = arguments ?? Tools.Empty<Expression>();
        }
    }

    public sealed class NewExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType => ExpressionType.New;
        public override Type Type => Constructor.DeclaringType;

        public readonly ConstructorInfo Constructor;

        public override SysExpr ToExpression() => ToNewExpression();

        public System.Linq.Expressions.NewExpression ToNewExpression() => SysExpr.New(Constructor, ArgumentsToExpressions());

        internal NewExpression(ConstructorInfo constructor, params Expression[] arguments) : 
            base(arguments)
        {
            Constructor = constructor;
        }
    }

    public sealed class NewArrayExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        // todo: That it is a ReadOnlyCollection<Expression> in original NewArrayExpression. 
        // I made it a ICollection for now to use Arguments as input, without changing Arguments type
        public IReadOnlyList<Expression> Expressions => Arguments;

        public override SysExpr ToExpression() => NodeType == ExpressionType.NewArrayInit
            // ReSharper disable once AssignNullToNotNullAttribute
            ? SysExpr.NewArrayInit(Type.GetElementType(), ArgumentsToExpressions())
            // ReSharper disable once AssignNullToNotNullAttribute
            : SysExpr.NewArrayBounds(Type.GetElementType(), ArgumentsToExpressions());

        internal NewArrayExpression(ExpressionType expressionType, Type arrayType, IReadOnlyList<Expression> elements) : base(elements)
        {
            NodeType = expressionType;
            Type = arrayType;
        }
    }

    public class MethodCallExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType => ExpressionType.Call;
        public override Type Type => Method.ReturnType;

        public readonly MethodInfo Method;
        public readonly Expression Object;

        public override SysExpr ToExpression() => 
            SysExpr.Call(Object?.ToExpression(), Method, ArgumentsToExpressions());

        internal MethodCallExpression(Expression @object, MethodInfo method, params Expression[] arguments)
            : base(arguments)
        {
            Object = @object;
            Method = method;
        }
    }

    public abstract class MemberExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.MemberAccess;
        public readonly MemberInfo Member;

        public readonly Expression Expression;

        protected MemberExpression(Expression expression, MemberInfo member)
        {
            Expression = expression;
            Member = member;
        }
    }

    public sealed class PropertyExpression : MemberExpression
    {
        public override Type Type => PropertyInfo.PropertyType;
        public PropertyInfo PropertyInfo => (PropertyInfo)Member;

        public override SysExpr ToExpression() => SysExpr.Property(Expression.ToExpression(), PropertyInfo);

        internal PropertyExpression(Expression instance, PropertyInfo property) : base(instance, property) { }
    }

    public sealed class FieldExpression : MemberExpression
    {
        public override Type Type => FieldInfo.FieldType;
        public FieldInfo FieldInfo => (FieldInfo)Member;

        public override SysExpr ToExpression() => SysExpr.Field(Expression.ToExpression(), FieldInfo);

        internal FieldExpression(Expression instance, FieldInfo field)
            : base(instance, field) { }
    }

    public abstract class MemberBinding
    {
        public readonly MemberInfo Member;

        public abstract MemberBindingType BindingType { get; }
        public abstract System.Linq.Expressions.MemberBinding ToMemberBinding();

        internal MemberBinding(MemberInfo member)
        {
            Member = member;
        }
    }

    public sealed class MemberAssignment : MemberBinding
    {
        public readonly Expression Expression;

        public override MemberBindingType BindingType => MemberBindingType.Assignment;

        public override System.Linq.Expressions.MemberBinding ToMemberBinding() => 
            SysExpr.Bind(Member, Expression.ToExpression());

        internal MemberAssignment(MemberInfo member, Expression expression) : base(member)
        {
            Expression = expression;
        }
    }

    public sealed class InvocationExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType => ExpressionType.Invoke;
        public override Type Type { get; }

        public readonly Expression Expression;

        public override SysExpr ToExpression() => SysExpr.Invoke(Expression.ToExpression(), ArgumentsToExpressions());

        internal InvocationExpression(Expression expression, IReadOnlyList<Expression> arguments, Type type) : base(arguments)
        {
            Expression = expression;
            Type = type;
        }
    }

    public sealed class DefaultExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Default;
        public override Type Type { get; }

        public override SysExpr ToExpression() => Type == typeof(void) ? SysExpr.Empty() : SysExpr.Default(Type);

        internal DefaultExpression(Type type)
        {
            Type = type;
        }
    }

    public sealed class ConditionalExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Conditional;
        public override Type Type => _type ?? IfTrue.Type;

        public readonly Expression Test;
        public readonly Expression IfTrue;
        public readonly Expression IfFalse;
        private readonly Type _type;

        public override SysExpr ToExpression() => _type == null 
            ? SysExpr.Condition(Test.ToExpression(), IfTrue.ToExpression(), IfFalse.ToExpression())
            : SysExpr.Condition(Test.ToExpression(), IfTrue.ToExpression(), IfFalse.ToExpression(), _type);

        internal ConditionalExpression(Expression test, Expression ifTrue, Expression ifFalse, Type type = null)
        {
            Test = test;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
            _type = type;
        }
    }

    /// <summary>For indexer property or array access.</summary>
    public sealed class IndexExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType => ExpressionType.Index;
        public override Type Type => Indexer != null ? Indexer.PropertyType : Object.Type.GetElementType();

        public readonly Expression Object;
        public readonly PropertyInfo Indexer;

        public override SysExpr ToExpression() => 
            SysExpr.MakeIndex(Object.ToExpression(), Indexer, ArgumentsToExpressions());

        internal IndexExpression(Expression @object, PropertyInfo indexer, IReadOnlyList<Expression> arguments) 
            : base(arguments)
        {
            Object = @object;
            Indexer = indexer;
        }
    }

    public sealed class BlockExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Block;
        public override Type Type { get; }

        public readonly IReadOnlyList<ParameterExpression> Variables;
        public readonly IReadOnlyList<Expression> Expressions;
        public readonly Expression Result;

        public override SysExpr ToExpression() => SysExpr.Block(Expressions.Map(info => info.ToExpression()));

        internal BlockExpression(Type type, IReadOnlyList<ParameterExpression> variables, IReadOnlyList<Expression> expressions)
        {
            Variables = variables;
            Expressions = expressions;
            Result = expressions[expressions.Count - 1];
            Type = type;
        }
    }

    public sealed class TryExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Try;
        public override Type Type => Body.Type;

        public readonly Expression Body;
        public readonly IReadOnlyList<CatchBlock> Handlers;
        public readonly Expression Finally;

        public override SysExpr ToExpression() =>
            Finally == null ? SysExpr.TryCatch(Body.ToExpression(), ToCatchBlocks(Handlers)) :
            Handlers == null ? SysExpr.TryFinally(Body.ToExpression(), Finally.ToExpression()) :
            SysExpr.TryCatchFinally(Body.ToExpression(), Finally.ToExpression(), ToCatchBlocks(Handlers));

        private static System.Linq.Expressions.CatchBlock[] ToCatchBlocks(IReadOnlyList<CatchBlock> hs)
        {
            if (hs == null)
                return Tools.Empty<System.Linq.Expressions.CatchBlock>();
            var catchBlocks = new System.Linq.Expressions.CatchBlock[hs.Count];
            for (var i = 0; i < hs.Count; ++i)
                catchBlocks[i] = hs[i].ToCatchBlock();
            return catchBlocks;
        }

        internal TryExpression(Expression body, Expression @finally, IReadOnlyList<CatchBlock> handlers)
        {
            Body = body;
            Handlers = handlers;
            Finally = @finally;
        }
    }

    public struct CatchBlock
    {
        public readonly ParameterExpression Variable;
        public readonly Expression Body;
        public readonly Expression Filter;
        public readonly Type Test;

        internal CatchBlock(ParameterExpression variable, Expression body, Expression filter, Type test)
        {
            Variable = variable;
            Body = body;
            Filter = filter;
            Test = test;
        }

        internal System.Linq.Expressions.CatchBlock ToCatchBlock() => SysExpr.Catch(Variable.ToParamExpr(), Body.ToExpression());
    }

    public sealed class LabelExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Label;
        public override Type Type => Target.Type;

        public readonly LabelTarget Target;
        public readonly Expression DefaultValue;

        public override SysExpr ToExpression() => SysExpr.Label(Target, DefaultValue.ToExpression());

        internal LabelExpression(LabelTarget target, Expression defaultValue)
        {
            Target = target;
            DefaultValue = defaultValue;
        }
    }

    public sealed class GotoExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Goto;
        public override Type Type { get; }

        public override SysExpr ToExpression() => SysExpr.Goto(Target, Value.ToExpression(), Type);

        public readonly Expression Value;
        public readonly LabelTarget Target;
        public readonly GotoExpressionKind Kind;

        internal GotoExpression(GotoExpressionKind kind, LabelTarget target, Expression value, Type type)
        {
            Type = type;
            Kind = kind;
            Value = value;
            Target = target;
        }
    }

    public class LambdaExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Lambda;
        public override Type Type { get; }

        public readonly Type ReturnType;
        public readonly Expression Body;
        public readonly ParameterExpression[] Parameters;

        public override SysExpr ToExpression() => ToLambdaExpression();

        public System.Linq.Expressions.LambdaExpression ToLambdaExpression() =>
            SysExpr.Lambda(Body.ToExpression(), Parameters.Map(p => p.ToParamExpr()));

        internal LambdaExpression(Type delegateType, Expression body, ParameterExpression[] parameters)
        {
            Body = body;
            Parameters = parameters;

            if (delegateType == null || delegateType == typeof(Delegate))
            {
                ReturnType = body.Type;
                Type = Tools.GetFuncOrActionType(Tools.GetParamTypes(parameters), ReturnType);
            }
            else
            {
                ReturnType = delegateType.GetTypeInfo().GetDeclaredMethod("Invoke").ReturnType;
                Type = delegateType;
            }
        }
    }

    public sealed class Expression<TDelegate> : LambdaExpression
    {
        public new System.Linq.Expressions.Expression<TDelegate> ToLambdaExpression() =>
            SysExpr.Lambda<TDelegate>(Body.ToExpression(), Parameters.Map(p => p.ToParamExpr()));

        internal Expression(Expression body, ParameterExpression[] parameters)
            : base(typeof(TDelegate), body, parameters) { }
    }
}
