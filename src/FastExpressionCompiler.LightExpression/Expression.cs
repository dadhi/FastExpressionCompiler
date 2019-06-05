/*
The MIT License (MIT)

Copyright (c) 2016-2019 Maksim Volkau

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
//#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.LightExpression
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    // todo: To reduce allocations we may consider to introduce IExpression and made implementations a struct.
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

        /// <summary>Reduces the Expression to simple ones</summary>
        public virtual Expression Reduce() => this;

        internal static SysExpr[] ToExpressions(IReadOnlyList<Expression> exprs)
        {
            if (exprs.Count == 0)
                return Tools.Empty<SysExpr>();

            if (exprs.Count == 1)
                return new[] { exprs[0].ToExpression() };

            var result = new SysExpr[exprs.Count];
            for (var i = 0; i < result.Length; ++i)
                result[i] = exprs[i].ToExpression();
            return result;
        }

        public static ParameterExpression Parameter(Type type, string name = null) =>
            new ParameterExpression(type.IsByRef ? type.GetElementType() : type, name, type.IsByRef);

        public static ParameterExpression Variable(Type type, string name = null) => Parameter(type, name);

        public static ConstantExpression Constant(object value, Type type = null)
        {
            if (value is bool b)
                return b ? _trueExpr : _falseExpr;
            if (type == null)
                return value != null ? new ConstantExpression(value, value.GetType()) : _nullExpr;
            return new ConstantExpression(value, type);
        }

        private static readonly ConstantExpression _nullExpr  = new ConstantExpression(null,  typeof(object));
        private static readonly ConstantExpression _falseExpr = new ConstantExpression(false, typeof(bool));
        private static readonly ConstantExpression _trueExpr  = new ConstantExpression(true,  typeof(bool));

        public static NewExpression New(Type type)
        {
            ConstructorInfo ctor = null;
            foreach (var x in type.GetTypeInfo().DeclaredConstructors)
                if (x.GetParameters().Length == 0)
                    ctor = x;
            return new NewExpression(ctor, Tools.Empty<Expression>());
        }

        public static NewExpression New(ConstructorInfo ctor) =>
            new NewExpression(ctor, Tools.Empty<Expression>());

        // todo: Reduce allocations
        //public static NewExpression New(ConstructorInfo ctor, Expression argument) =>
        //    new NewExpression<Expression>(ctor, argument);

        public static NewExpression New(ConstructorInfo ctor, params Expression[] arguments) =>
            new NewExpression(ctor, arguments);

        public static NewExpression New(ConstructorInfo ctor, IEnumerable<Expression> arguments) =>
            new NewExpression(ctor, arguments.AsReadOnlyList());

        public static MethodCallExpression Call(Expression instance, MethodInfo method, params Expression[] arguments) =>
            new MethodCallExpression(instance, method, arguments);

        public static MethodCallExpression Call(Expression instance, MethodInfo method, IEnumerable<Expression> arguments) =>
            new MethodCallExpression(instance, method, arguments.AsReadOnlyList());

        public static MethodCallExpression Call(MethodInfo method, params Expression[] arguments) =>
            Call(null, method, arguments);

        public static MethodCallExpression Call(MethodInfo method, IEnumerable<Expression> arguments) =>
            Call(null, method, arguments.AsReadOnlyList());

        public static MethodCallExpression Call(Type type, string methodName, Type[] typeArguments, params Expression[] arguments) =>
            Call(null, type.FindMethod(methodName, typeArguments, arguments, isStatic: true), arguments);

        public static MethodCallExpression Call(Type type, string methodName, Type[] typeArguments, IEnumerable<Expression> arguments)
        {
            var args = arguments.AsReadOnlyList();
            return Call(null, type.FindMethod(methodName, typeArguments, args, isStatic: true), args);
        }

        public static MethodCallExpression Call(Expression instance, string methodName, Type[] typeArguments, params Expression[] arguments) =>
            new MethodCallExpression(instance, instance.Type.FindMethod(methodName, typeArguments, arguments), arguments);

        public static MethodCallExpression Call(Expression instance, string methodName, Type[] typeArguments, IEnumerable<Expression> arguments)
        {
            var args = arguments.AsReadOnlyList();
            return new MethodCallExpression(instance, instance.Type.FindMethod(methodName, typeArguments, args), args);
        }

        public static MemberExpression Property(PropertyInfo property) =>
            new PropertyExpression(null, property);

        public static MemberExpression Property(Expression instance, PropertyInfo property) =>
            new PropertyExpression(instance, property);

        public static MemberExpression Property(Expression expression, string propertyName) =>
            Property(expression, expression.Type.FindProperty(propertyName) 
                ?? throw new ArgumentException($"Declared property with the name '{propertyName}' is not found in '{expression.Type}'", nameof(propertyName)));

        public static IndexExpression Property(Expression instance, PropertyInfo indexer, params Expression[] arguments) =>
            new IndexExpression(instance, indexer, arguments);

        public static IndexExpression Property(Expression instance, PropertyInfo indexer, IEnumerable<Expression> arguments) =>
            new IndexExpression(instance, indexer, arguments.AsReadOnlyList());

        public static MemberExpression PropertyOrField(Expression expression, string propertyName) =>
            expression.Type.FindProperty(propertyName) != null ?
                (MemberExpression)new PropertyExpression(expression, expression.Type.FindProperty(propertyName)
                    ?? throw new ArgumentException($"Declared property with the name '{propertyName}' is not found in '{expression.Type}'", nameof(propertyName))) :
                new FieldExpression(expression, expression.Type.FindField(propertyName)
                    ?? throw new ArgumentException($"Declared field with the name '{propertyName}' is not found '{expression.Type}'", nameof(propertyName)));

        public static MemberExpression MakeMemberAccess(Expression expression, MemberInfo member)
        {
            if (member is FieldInfo field)
                return Field(expression, field);
            if (member is PropertyInfo property)
                return Property(expression, property);
            throw new ArgumentException($"Member is not field or property: {member}", nameof(member));
        }

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

        /// <summary>Creates a UnaryExpression that represents a bitwise complement operation.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to Not and the Operand property set to the specified value.</returns>
        public static UnaryExpression Not(Expression expression) =>
            new UnaryExpression(ExpressionType.Not, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents an explicit reference or boxing conversion where null is supplied if the conversion fails.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <param name="type">A Type to set the Type property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to TypeAs and the Operand and Type properties set to the specified values.</returns>
        public static UnaryExpression TypeAs(Expression expression, Type type) =>
            new UnaryExpression(ExpressionType.TypeAs, expression, type);

        public static TypeBinaryExpression TypeEqual(Expression operand, Type type) =>
            new TypeBinaryExpression(ExpressionType.TypeEqual, operand, type);

        public static TypeBinaryExpression TypeIs(Expression operand, Type type) =>
            new TypeBinaryExpression(ExpressionType.TypeIs, operand, type);

        /// <summary>Creates a UnaryExpression that represents an expression for obtaining the length of a one-dimensional array.</summary>
        /// <param name="array">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to ArrayLength and the Operand property equal to array.</returns>
        public static UnaryExpression ArrayLength(Expression array) =>
            new UnaryExpression(ExpressionType.ArrayLength, array, typeof(int));

        /// <summary>Creates a UnaryExpression that represents a type conversion operation.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <param name="type">A Type to set the Type property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to Convert and the Operand and Type properties set to the specified values.</returns>
        public static UnaryExpression Convert(Expression expression, Type type) =>
            Convert(expression, type, null);

        /// <summary>Creates a UnaryExpression that represents a conversion operation for which the implementing method is specified.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <param name="type">A Type to set the Type property equal to.</param>
        /// <param name="method">A MethodInfo to set the Method property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to Convert and the Operand, Type, and Method properties set to the specified values.</returns>
        public static UnaryExpression Convert(Expression expression, Type type, MethodInfo method) =>
            new UnaryExpression(ExpressionType.Convert, expression, type, method);

        /// <summary>Creates a UnaryExpression that represents a conversion operation that throws an exception if the target type is overflowed.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <param name="type">A Type to set the Type property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to ConvertChecked and the Operand and Type properties set to the specified values.</returns>
        public static UnaryExpression ConvertChecked(Expression expression, Type type) =>
            ConvertChecked(expression, type, null);

        /// <summary>Creates a UnaryExpression that represents a conversion operation that throws an exception if the target type is overflowed and for which the implementing method is specified.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <param name="type">A Type to set the Type property equal to.</param>
        /// <param name="method">A MethodInfo to set the Method property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to ConvertChecked and the Operand, Type, and Method properties set to the specified values.</returns>
        public static UnaryExpression ConvertChecked(Expression expression, Type type, MethodInfo method) =>
            new UnaryExpression(ExpressionType.ConvertChecked, expression, type, method);

        /// <summary>Creates a UnaryExpression that represents the decrementing of the expression by 1.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the decremented expression.</returns>
        public static UnaryExpression Decrement(Expression expression) =>
            new UnaryExpression(ExpressionType.Decrement, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents the incrementing of the expression value by 1.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the incremented expression.</returns>
        public static UnaryExpression Increment(Expression expression) =>
            new UnaryExpression(ExpressionType.Increment, expression, expression.Type);

        /// <summary>Returns whether the expression evaluates to false.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>An instance of UnaryExpression.</returns>
        public static UnaryExpression IsFalse(Expression expression) =>
            new UnaryExpression(ExpressionType.IsFalse, expression, typeof(bool));

        /// <summary>Returns whether the expression evaluates to true.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>An instance of UnaryExpression.</returns>
        public static UnaryExpression IsTrue(Expression expression) =>
            new UnaryExpression(ExpressionType.IsTrue, expression, typeof(bool));

        /// <summary>Creates a UnaryExpression, given an operand, by calling the appropriate factory method.</summary>
        /// <param name="unaryType">The ExpressionType that specifies the type of unary operation.</param>
        /// <param name="operand">An Expression that represents the operand.</param>
        /// <param name="type">The Type that specifies the type to be converted to (pass null if not applicable).</param>
        /// <returns>The UnaryExpression that results from calling the appropriate factory method.</returns>
        public static UnaryExpression MakeUnary(ExpressionType unaryType, Expression operand, Type type) =>
            new UnaryExpression(unaryType, operand, type);

        /// <summary>Creates a UnaryExpression that represents an arithmetic negation operation.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to Negate and the Operand property set to the specified value.</returns>
        public static UnaryExpression Negate(Expression expression) =>
            new UnaryExpression(ExpressionType.Negate, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents an arithmetic negation operation that has overflow checking.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to NegateChecked and the Operand property set to the specified value.</returns>
        public static UnaryExpression NegateChecked(Expression expression) =>
            new UnaryExpression(ExpressionType.NegateChecked, expression, expression.Type);

        /// <summary>Returns the expression representing the ones complement.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>An instance of UnaryExpression.</returns>
        public static UnaryExpression OnesComplement(Expression expression) =>
            new UnaryExpression(ExpressionType.OnesComplement, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that increments the expression by 1 and assigns the result back to the expression.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the resultant expression.</returns>
        public static UnaryExpression PreIncrementAssign(Expression expression) =>
            new UnaryExpression(ExpressionType.PreIncrementAssign, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents the assignment of the expression followed by a subsequent increment by 1 of the original expression.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the resultant expression.</returns>
        public static UnaryExpression PostIncrementAssign(Expression expression) =>
            new UnaryExpression(ExpressionType.PostIncrementAssign, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that decrements the expression by 1 and assigns the result back to the expression.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the resultant expression.</returns>
        public static UnaryExpression PreDecrementAssign(Expression expression) =>
            new UnaryExpression(ExpressionType.PreDecrementAssign, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents the assignment of the expression followed by a subsequent decrement by 1 of the original expression.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the resultant expression.</returns>
        public static UnaryExpression PostDecrementAssign(Expression expression) =>
            new UnaryExpression(ExpressionType.PostDecrementAssign, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents an expression that has a constant value of type Expression.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to Quote and the Operand property set to the specified value.</returns>
        public static UnaryExpression Quote(Expression expression) =>
            new UnaryExpression(ExpressionType.Quote, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents a unary plus operation.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to UnaryPlus and the Operand property set to the specified value.</returns>
        public static UnaryExpression UnaryPlus(Expression expression) =>
            new UnaryExpression(ExpressionType.UnaryPlus, expression, expression.Type);

        /// <summary>Creates a UnaryExpression that represents an explicit unboxing.</summary>
        /// <param name="expression">An Expression to set the Operand property equal to.</param>
        /// <param name="type">A Type to set the Type property equal to.</param>
        /// <returns>A UnaryExpression that has the NodeType property equal to unbox and the Operand and Type properties set to the specified values.</returns>
        public static UnaryExpression Unbox(Expression expression, Type type) =>
            new UnaryExpression(ExpressionType.Unbox, expression, type);

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body) =>
            new Expression<TDelegate>(body, Tools.Empty<ParameterExpression>());

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters) =>
            new Expression<TDelegate>(body, parameters);

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, string name, params ParameterExpression[] parameters) =>
            new Expression<TDelegate>(body, parameters);

        /// <summary>Creates a BinaryExpression that represents applying an array index operator to an array of rank one.</summary>
        /// <param name="array">A Expression to set the Left property equal to.</param>
        /// <param name="index">A Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to ArrayIndex and the Left and Right properties set to the specified values.</returns>
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

        /// <summary>Creates a BinaryExpression that represents an assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Assign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Assign(Expression left, Expression right) =>
            new AssignBinaryExpression(left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents raising an expression to a power and assigning the result back to the expression.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to PowerAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression PowerAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.PowerAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an addition assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to AddAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression AddAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.AddAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an addition assignment operation that has overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to AddAssignChecked and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression AddAssignChecked(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.AddAssignChecked, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise AND assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to AndAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression AndAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.AndAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise XOR assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to ExclusiveOrAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.ExclusiveOrAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise left-shift assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to LeftShiftAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression LeftShiftAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.LeftShiftAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a remainder assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to ModuloAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression ModuloAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.ModuloAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise OR assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to OrAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression OrAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.OrAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise right-shift assignment operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to RightShiftAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression RightShiftAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.RightShiftAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a subtraction assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to SubtractAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.SubtractAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a subtraction assignment operation that has overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to SubtractAssignChecked and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssignChecked(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.SubtractAssignChecked, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a multiplication assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to MultiplyAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.MultiplyAssign, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a multiplication assignment operation that has overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to MultiplyAssignChecked and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.MultiplyAssignChecked, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a division assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to DivideAssign and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression DivideAssign(Expression left, Expression right) =>
            new AssignBinaryExpression(ExpressionType.DivideAssign, left, right, left.Type);

        public static InvocationExpression Invoke(Expression lambda, IEnumerable<Expression> args) =>
            new InvocationExpression(lambda, args.AsReadOnlyList(),
                (lambda as LambdaExpression)?.ReturnType ?? lambda.Type.FindDelegateInvokeMethod().ReturnType);

        public static InvocationExpression Invoke(Expression lambda, params Expression[] args) =>
            Invoke(lambda, (IEnumerable<Expression>)args);

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

        /// <summary>Creates a BinaryExpression that represents an arithmetic addition operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Add and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Add(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Add, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic addition operation that has overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to AddChecked and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression AddChecked(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.AddChecked, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise XOR operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to ExclusiveOr and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression ExclusiveOr(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.ExclusiveOr, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise left-shift operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to LeftShift and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression LeftShift(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.LeftShift, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic remainder operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Modulo and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Modulo(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Modulo, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise OR operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Or and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Or(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Or, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise right-shift operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to RightShift and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression RightShift(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.RightShift, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic subtraction operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Subtract and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Subtract(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Subtract, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic subtraction operation that has overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to SubtractChecked and the Left, Right, and Method properties set to the specified values.</returns>
        public static BinaryExpression SubtractChecked(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.SubtractChecked, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic multiplication operation that does not have overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Multiply and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Multiply(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Multiply, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic multiplication operation that has overflow checking.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to MultiplyChecked and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression MultiplyChecked(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.MultiplyChecked, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an arithmetic division operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Divide and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Divide(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Divide, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents raising a number to a power.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Power and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Power(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Power, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a bitwise AND operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to And, and the Left and Right properties are set to the specified values.</returns>
        public static BinaryExpression And(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.And, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a conditional AND operation that evaluates the second operand only if the first operand evaluates to true.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to AndAlso and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression AndAlso(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.AndAlso, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a conditional OR operation that evaluates the second operand only if the first operand evaluates to false.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to OrElse and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression OrElse(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.OrElse, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an equality comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Equal and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Equal(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Equal, left, right, typeof(bool));

        /// <summary>Creates a BinaryExpression that represents a "greater than" numeric comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to GreaterThan and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression GreaterThan(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.GreaterThan, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a "greater than or equal" numeric comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to GreaterThanOrEqual and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.GreaterThanOrEqual, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a "less than" numeric comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to LessThan and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression LessThan(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.LessThan, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents a " less than or equal" numeric comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to LessThanOrEqual and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression LessThanOrEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.LessThanOrEqual, left, right, left.Type);

        /// <summary>Creates a BinaryExpression that represents an inequality comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to NotEqual and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression NotEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.NotEqual, left, right, typeof(bool));

        public static BlockExpression Block(params Expression[] expressions) =>
            Block(Tools.Empty<ParameterExpression>(), expressions);

        public static BlockExpression Block(IEnumerable<ParameterExpression> variables, params Expression[] expressions) =>
            Block(expressions[expressions.Length - 1].Type, variables.AsReadOnlyList(), expressions);

        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, params Expression[] expressions) =>
            new BlockExpression(type, variables.AsReadOnlyList(), expressions);

        public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions) =>
            new BlockExpression(type, variables.AsReadOnlyList(), expressions.AsReadOnlyList());

        /// <summary>
        /// Creates a LoopExpression with the given body and (optional) break target.
        /// </summary>
        /// <param name="body">The body of the loop.</param>
        /// <param name="break">The break target used by the loop body, if required.</param>
        /// <returns>The created LoopExpression.</returns>
        public static LoopExpression Loop(Expression body, LabelTarget @break = null) =>
            @break == null ? new LoopExpression(body, null, null) : new LoopExpression(body, @break, null);

        /// <summary>
        /// Creates a LoopExpression with the given body.
        /// </summary>
        /// <param name="body">The body of the loop.</param>
        /// <param name="break">The break target used by the loop body.</param>
        /// <param name="continue">The continue target used by the loop body.</param>
        /// <returns>The created LoopExpression.</returns>
        public static LoopExpression Loop(Expression body, LabelTarget @break, LabelTarget @continue) =>
            new LoopExpression(body, @break, @continue);

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

        /// <summary>Creates a UnaryExpression that represents a throwing of an exception.</summary>
        /// <param name="value">An Expression to set the Operand property equal to.</param>
        /// <returns>A UnaryExpression that represents the exception.</returns>
        public static UnaryExpression Throw(Expression value) => Throw(value, typeof(void));

        /// <summary>Creates a UnaryExpression that represents a throwing of an exception with a given type.</summary>
        /// <param name="value">An Expression to set the Operand property equal to.</param>
        /// <param name="type">The Type of the expression.</param>
        /// <returns>A UnaryExpression that represents the exception.</returns>
        public static UnaryExpression Throw(Expression value, Type type) =>
            new UnaryExpression(ExpressionType.Throw, value, type);

        public static LabelExpression Label(LabelTarget target, Expression defaultValue = null) =>
            new LabelExpression(target, defaultValue);

        public static LabelTarget Label(Type type = null, string name = null) =>
            SysExpr.Label(type ?? typeof(void), name);

        public static LabelTarget Label(string name) =>
            SysExpr.Label(typeof(void), name);

        /// <summary>Creates a BinaryExpression, given the left and right operands, by calling an appropriate factory method.</summary>
        /// <param name="binaryType">The ExpressionType that specifies the type of binary operation.</param>
        /// <param name="left">An Expression that represents the left operand.</param>
        /// <param name="right">An Expression that represents the right operand.</param>
        /// <returns>The BinaryExpression that results from calling the appropriate factory method.</returns>
        public static BinaryExpression MakeBinary(ExpressionType binaryType, Expression left, Expression right)
        {
            switch (binaryType)
            {
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AndAssign:
                case ExpressionType.Assign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    return new AssignBinaryExpression(binaryType, left, right, left.Type);
                case ExpressionType.ArrayIndex:
                    return ArrayIndex(left, right);
                case ExpressionType.Coalesce:
                    return Coalesce(left, right);
                default:
                    return new SimpleBinaryExpression(binaryType, left, right, left.Type);
            }
        }

        public static GotoExpression MakeGoto(GotoExpressionKind kind, LabelTarget target, Expression value, Type type = null) =>
            new GotoExpression(kind, target, value, type ?? typeof(void));

        public static GotoExpression Break(LabelTarget target, Expression value = null, Type type = null) =>
            MakeGoto(GotoExpressionKind.Break, target, value, type);

        public static GotoExpression Continue(LabelTarget target, Type type = null) =>
            MakeGoto(GotoExpressionKind.Continue, target, null, type);

        /// <summary>Creates a BinaryExpression that represents a reference equality comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Equal and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression ReferenceEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.Equal, left, right, typeof(bool));

        /// <summary>Creates a BinaryExpression that represents a reference inequality comparison.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to NotEqual and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression ReferenceNotEqual(Expression left, Expression right) =>
            new SimpleBinaryExpression(ExpressionType.NotEqual, left, right, typeof(bool));

        public static GotoExpression Return(LabelTarget target, Expression value = null, Type type = null) =>
            MakeGoto(GotoExpressionKind.Return, target, value, type);

        public static GotoExpression Goto(LabelTarget target, Expression value = null, Type type = null) =>
            MakeGoto(GotoExpressionKind.Goto, target, value, type);

        public static SwitchExpression Switch(Expression switchValue, Expression defaultBody, params SwitchCase[] cases) =>
            new SwitchExpression(defaultBody.Type, switchValue, defaultBody, null, cases);

        public static SwitchExpression Switch(Expression switchValue, Expression defaultBody, MethodInfo comparison, params SwitchCase[] cases) =>
            new SwitchExpression(defaultBody.Type, switchValue, defaultBody, comparison, cases);

        public static SwitchExpression Switch(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison, params SwitchCase[] cases) =>
            new SwitchExpression(type, switchValue, defaultBody, comparison, cases);

        public static SwitchExpression Switch(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison, IEnumerable<SwitchCase> cases) =>
            new SwitchExpression(type, switchValue, defaultBody, comparison, cases);

        public static SwitchExpression Switch(Expression switchValue, params SwitchCase[] cases) =>
            new SwitchExpression(null, switchValue, null, null, cases);

        public static SwitchCase SwitchCase(Expression body, IEnumerable<Expression> testValues) =>
            new SwitchCase(body, testValues);

        public static SwitchCase SwitchCase(Expression body, params Expression[] testValues) =>
            new SwitchCase(body, testValues);

        /// <summary>Creates a BinaryExpression that represents a coalescing operation.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Coalesce and the Left and Right properties set to the specified values.</returns>
        public static BinaryExpression Coalesce(Expression left, Expression right) => Coalesce(left, right, null);

        /// <summary>Creates a BinaryExpression that represents a coalescing operation, given a conversion function.</summary>
        /// <param name="left">An Expression to set the Left property equal to.</param>
        /// <param name="right">An Expression to set the Right property equal to.</param>
        /// <param name="conversion">A LambdaExpression to set the Conversion property equal to.</param>
        /// <returns>A BinaryExpression that has the NodeType property equal to Coalesce and the Left, Right and Conversion properties set to the specified values.</returns>
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

        internal static PropertyInfo FindProperty(this Type type, string propertyName)
        {
            foreach (var x in type.GetTypeInfo().DeclaredProperties)
                if (x.Name == propertyName)
                    return x;
            return type.GetTypeInfo().BaseType?.FindProperty(propertyName);
        }

        internal static FieldInfo FindField(this Type type, string fieldName)
        {
            foreach (var x in type.GetTypeInfo().DeclaredFields)
                if (x.Name == fieldName)
                    return x;
            return type.GetTypeInfo().BaseType?.FindField(fieldName);
        }

        internal static MethodInfo FindMethod(this Type type,
            string methodName, Type[] typeArgs, IReadOnlyList<Expression> args, bool isStatic = false)
        {
            foreach(var m in type.GetTypeInfo().DeclaredMethods)
            {
                if (isStatic == m.IsStatic && methodName == m.Name)
                {
                    typeArgs = typeArgs ?? Type.EmptyTypes;
                    var mTypeArgs = m.GetGenericArguments();

                    if (typeArgs.Length == mTypeArgs.Length &&
                        (typeArgs.Length == 0 || AreTypesTheSame(typeArgs, mTypeArgs)))
                    {
                        args = args ?? Tools.Empty<Expression>();
                        var pars = m.GetParameters();
                        if (args.Count == pars.Length && 
                            (args.Count == 0 || AreArgExpressionsAndParamsOfTheSameType(args, pars)))
                            return m;
                    }
                }
            }

            return type.GetTypeInfo().BaseType?.FindMethod(methodName, typeArgs, args, isStatic);
        }

        private static bool AreTypesTheSame(Type[] source, Type[] target)
        {
            for (var i = 0; i < source.Length; i++)
                if (source[i] != target[i])
                    return false;
            return true;
        }

        private static bool AreArgExpressionsAndParamsOfTheSameType(IReadOnlyList<Expression> args, ParameterInfo[] pars)
        {
            for (var i = 0; i < pars.Length; i++)
                if (pars[i].ParameterType != args[i].Type)
                    return false;
            return true;
        }

        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> xs) =>
            xs as IReadOnlyList<T> ?? xs.ToArray();

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
    }

    public class UnaryExpression : Expression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public readonly Expression Operand;
        public readonly MethodInfo Method;

        public override SysExpr ToExpression()
        {
            switch (NodeType)
            {
                case ExpressionType.ArrayLength:
                    return SysExpr.ArrayLength(Operand.ToExpression());
                case ExpressionType.Convert:
                    return SysExpr.Convert(Operand.ToExpression(), Type);
                case ExpressionType.Decrement:
                    return SysExpr.Decrement(Operand.ToExpression());
                case ExpressionType.Increment:
                    return SysExpr.Increment(Operand.ToExpression());
                case ExpressionType.IsFalse:
                    return SysExpr.IsFalse(Operand.ToExpression());
                case ExpressionType.IsTrue:
                    return SysExpr.IsTrue(Operand.ToExpression());
                case ExpressionType.Negate:
                    return SysExpr.Negate(Operand.ToExpression());
                case ExpressionType.NegateChecked:
                    return SysExpr.NegateChecked(Operand.ToExpression());
                case ExpressionType.OnesComplement:
                    return SysExpr.OnesComplement(Operand.ToExpression());
                case ExpressionType.PostDecrementAssign:
                    return SysExpr.PostDecrementAssign(Operand.ToExpression());
                case ExpressionType.PostIncrementAssign:
                    return SysExpr.PostIncrementAssign(Operand.ToExpression());
                case ExpressionType.PreDecrementAssign:
                    return SysExpr.PreDecrementAssign(Operand.ToExpression());
                case ExpressionType.PreIncrementAssign:
                    return SysExpr.PreIncrementAssign(Operand.ToExpression());
                case ExpressionType.Quote:
                    return SysExpr.Quote(Operand.ToExpression());
                case ExpressionType.UnaryPlus:
                    return SysExpr.UnaryPlus(Operand.ToExpression());
                case ExpressionType.Unbox:
                    return SysExpr.Unbox(Operand.ToExpression(), Type);
                case ExpressionType.Throw:
                    return SysExpr.Throw(Operand.ToExpression(), Type);
                default:
                    throw new NotSupportedException("Cannot convert Expression to Expression of type " + NodeType);
            }
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
            Type = Method.ReturnType;
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

            Left = left;
            Right = right;

            if (nodeType == ExpressionType.Equal ||
                nodeType == ExpressionType.NotEqual ||
                nodeType == ExpressionType.GreaterThan ||
                nodeType == ExpressionType.GreaterThanOrEqual ||
                nodeType == ExpressionType.LessThan ||
                nodeType == ExpressionType.LessThanOrEqual ||
                nodeType == ExpressionType.And ||
                nodeType == ExpressionType.AndAlso ||
                nodeType == ExpressionType.Or ||
                nodeType == ExpressionType.OrElse)
            {
                Type = typeof(bool);
            }
            else
                Type = type;
        }
    }

    public class TypeBinaryExpression : Expression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public Type TypeOperand { get; }

        public readonly Expression Expression;

        public override SysExpr ToExpression() => SysExpr.TypeIs(Expression.ToExpression(), TypeOperand);

        internal TypeBinaryExpression(ExpressionType nodeType, Expression expression, Type typeOperand)
        {
            NodeType = nodeType;
            Expression = expression;
            Type = typeof(bool);
            TypeOperand = typeOperand;
        }
    }

    public sealed class SimpleBinaryExpression : BinaryExpression
    {
        public override SysExpr ToExpression()
        {
            switch (NodeType)
            {
                case ExpressionType.Add:
                    return SysExpr.Add(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Subtract:
                    return SysExpr.Subtract(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Multiply:
                    return SysExpr.Multiply(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Divide:
                    return SysExpr.Divide(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Power:
                    return SysExpr.Power(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Coalesce:
                    return SysExpr.Coalesce(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.And:
                    return SysExpr.And(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.AndAlso:
                    return SysExpr.AndAlso(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Or:
                    return SysExpr.Or(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.OrElse:
                    return SysExpr.OrElse(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.Equal:
                    return SysExpr.Equal(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.NotEqual:
                    return SysExpr.NotEqual(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.GreaterThan:
                    return SysExpr.GreaterThan(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.GreaterThanOrEqual:
                    return SysExpr.GreaterThanOrEqual(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.LessThan:
                    return SysExpr.LessThan(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.LessThanOrEqual:
                    return SysExpr.LessThanOrEqual(Left.ToExpression(), Right.ToExpression());
                default:
                    throw new NotSupportedException($"Not a valid {NodeType} for arithmetic or boolean binary expression.");
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
        public override SysExpr ToExpression()
        {
            switch (NodeType)
            {
                case ExpressionType.Assign:
                    return SysExpr.Assign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.AddAssign:
                    return SysExpr.AddAssign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.AddAssignChecked:
                    return SysExpr.AddAssignChecked(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.SubtractAssign:
                    return SysExpr.SubtractAssign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.SubtractAssignChecked:
                    return SysExpr.SubtractAssignChecked(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.MultiplyAssign:
                    return SysExpr.MultiplyAssign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.MultiplyAssignChecked:
                    return SysExpr.MultiplyAssignChecked(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.DivideAssign:
                    return SysExpr.DivideAssign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.PowerAssign:
                    return SysExpr.PowerAssign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.AndAssign:
                    return SysExpr.AndAssign(Left.ToExpression(), Right.ToExpression());
                case ExpressionType.OrAssign:
                    return SysExpr.OrAssign(Left.ToExpression(), Right.ToExpression());
                default:
                    throw new NotSupportedException($"Not a valid {NodeType} for Assign binary expression.");
            }
        }

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
            SysExpr.MemberInit(NewExpression.ToNewExpression(), MemberBinding.BindingsToExpressions(Bindings));

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

        public override SysExpr ToExpression() => ToParameterExpression();

        public System.Linq.Expressions.ParameterExpression ToParameterExpression() =>
            _paramExpr ?? (_paramExpr = SysExpr.Parameter(IsByRef ? Type.MakeByRefType() : Type, Name));

        internal static System.Linq.Expressions.ParameterExpression[] ToParameterExpressions(
            IReadOnlyList<ParameterExpression> ps)
        {
            if (ps.Count == 0)
                return Tools.Empty<System.Linq.Expressions.ParameterExpression>();

            if (ps.Count == 1)
                return new[] { ps[0].ToParameterExpression() };

            var result = new System.Linq.Expressions.ParameterExpression[ps.Count];
            for (var i = 0; i < result.Length; ++i)
                result[i] = ps[i].ToParameterExpression();
            return result;
        }

        internal ParameterExpression(Type type, string name, bool isByRef)
        {
            Type = type;
            Name = name;
            IsByRef = isByRef;
            _paramExpr = null;
        }

        private System.Linq.Expressions.ParameterExpression _paramExpr;
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

        protected SysExpr[] ArgumentsToExpressions() => ToExpressions(Arguments);

        protected ArgumentsExpression(IReadOnlyList<Expression> arguments)
        {
            Arguments = arguments ?? Tools.Empty<Expression>();
        }
    }

    // todo: The whole idea is to reduce allocations
    public sealed class NewExpression<TArgs> : Expression
    {
        public override ExpressionType NodeType => ExpressionType.New;
        public override Type Type => Constructor.DeclaringType;

        public readonly ConstructorInfo Constructor;
        public readonly TArgs Arguments;

        public override SysExpr ToExpression() =>
            Arguments is Expression expr
                ? SysExpr.New(Constructor, expr.ToExpression())
                : SysExpr.New(Constructor, ToExpressions((IReadOnlyList<Expression>)Arguments));

        internal NewExpression(ConstructorInfo constructor, TArgs arguments)
        {
            Constructor = constructor;
            Arguments = arguments;
        }
    }

    public sealed class NewExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType => ExpressionType.New;
        public override Type Type => Constructor.DeclaringType;

        public readonly ConstructorInfo Constructor;

        public override SysExpr ToExpression() => ToNewExpression();

        public System.Linq.Expressions.NewExpression ToNewExpression() => SysExpr.New(Constructor, ArgumentsToExpressions());

        internal NewExpression(ConstructorInfo constructor, IReadOnlyList<Expression> arguments) :
            base(arguments)
        {
            Constructor = constructor;
        }
    }

    public sealed class NewArrayExpression : ArgumentsExpression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

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

        internal MethodCallExpression(Expression @object, MethodInfo method, IReadOnlyList<Expression> arguments)
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

        public override SysExpr ToExpression() => SysExpr.Property(Expression?.ToExpression(), PropertyInfo);

        internal PropertyExpression(Expression instance, PropertyInfo property) : base(instance, property) { }
    }

    public sealed class FieldExpression : MemberExpression
    {
        public override Type Type => FieldInfo.FieldType;
        public FieldInfo FieldInfo => (FieldInfo)Member;

        public override SysExpr ToExpression() => SysExpr.Field(Expression?.ToExpression(), FieldInfo);

        internal FieldExpression(Expression instance, FieldInfo field)
            : base(instance, field) { }
    }

    public abstract class MemberBinding
    {
        public readonly MemberInfo Member;

        public abstract MemberBindingType BindingType { get; }
        public abstract System.Linq.Expressions.MemberBinding ToMemberBinding();

        internal static System.Linq.Expressions.MemberBinding[] BindingsToExpressions(IReadOnlyList<MemberBinding> ms)
        {
            if (ms.Count == 0)
                return Tools.Empty<System.Linq.Expressions.MemberBinding>();

            if (ms.Count == 1)
                return new[] { ms[0].ToMemberBinding() };

            var result = new System.Linq.Expressions.MemberBinding[ms.Count];
            for (var i = 0; i < result.Length; ++i)
                result[i] = ms[i].ToMemberBinding();
            return result;
        }

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

        public override SysExpr ToExpression() => SysExpr.Block(
            Type,
            ParameterExpression.ToParameterExpressions(Variables),
            ToExpressions(Expressions));

        internal BlockExpression(Type type, IReadOnlyList<ParameterExpression> variables, IReadOnlyList<Expression> expressions)
        {
            Variables = variables ?? Tools.Empty<ParameterExpression>();
            Expressions = expressions ?? Tools.Empty<Expression>();
            Result = Expressions[Expressions.Count - 1];
            Type = type;
        }
    }

    public sealed class LoopExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Loop;

        public override Type Type => typeof(void);

        public readonly Expression Body;
        public readonly LabelTarget BreakLabel;
        public readonly LabelTarget ContinueLabel;

        public override SysExpr ToExpression() =>
            BreakLabel == null ? SysExpr.Loop(Body.ToExpression()) :
            ContinueLabel == null ? SysExpr.Loop(Body.ToExpression(), BreakLabel) :
            SysExpr.Loop(Body.ToExpression(), BreakLabel, ContinueLabel);

        internal LoopExpression(Expression body, LabelTarget breakLabel, LabelTarget continueLabel)
        {
            Body = body;
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
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

        internal System.Linq.Expressions.CatchBlock ToCatchBlock() => SysExpr.Catch(Variable.ToParameterExpression(), Body.ToExpression());
    }

    public sealed class LabelExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Label;
        public override Type Type => Target.Type;

        public readonly LabelTarget Target;
        public readonly Expression DefaultValue;

        public override SysExpr ToExpression() =>
            DefaultValue == null ? SysExpr.Label(Target) :
            SysExpr.Label(Target, DefaultValue.ToExpression());

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

        public override SysExpr ToExpression() =>
            Value == null ? SysExpr.Goto(Target, Type) :
            SysExpr.Goto(Target, Value.ToExpression(), Type);

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

    public class SwitchCase
    {
        public readonly IReadOnlyList<Expression> TestValues;
        public readonly Expression Body;

        public System.Linq.Expressions.SwitchCase ToSwitchCase() =>
            SysExpr.SwitchCase(Body.ToExpression(), TestValues.Select(x => x.ToExpression()));

        public SwitchCase(Expression body, IEnumerable<Expression> testValues)
        {
            Body = body;
            TestValues = testValues.AsReadOnlyList();
        }
    }

    public class SwitchExpression : Expression
    {
        public override ExpressionType NodeType { get; }
        public override Type Type { get; }

        public override SysExpr ToExpression() => SysExpr.Switch(
            SwitchValue.ToExpression(), DefaultBody.ToExpression(),
            Comparison, ToSwitchCaseExpressions(Cases));

        internal static System.Linq.Expressions.SwitchCase[] ToSwitchCaseExpressions(IReadOnlyList<SwitchCase> sw)
        {
            if (sw.Count == 0)
                return Tools.Empty<System.Linq.Expressions.SwitchCase>();

            if (sw.Count == 1)
                return new[] { sw[0].ToSwitchCase() };

            var result = new System.Linq.Expressions.SwitchCase[sw.Count];
            for (var i = 0; i < result.Length; ++i)
                result[i] = sw[i].ToSwitchCase();
            return result;
        }

        public readonly Expression SwitchValue;
        public readonly IReadOnlyList<SwitchCase> Cases;
        public readonly Expression DefaultBody;
        public readonly MethodInfo Comparison;

        public SwitchExpression(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison,
            IEnumerable<SwitchCase> cases)
        {
            NodeType = ExpressionType.Switch;
            Type = type;
            SwitchValue = switchValue;
            DefaultBody = defaultBody;
            Comparison = comparison;
            Cases = cases.AsReadOnlyList();
        }
    }

    public class LambdaExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Lambda;
        public override Type Type { get; }

        public readonly Type ReturnType;
        public readonly Expression Body;
        public readonly IReadOnlyList<ParameterExpression> Parameters;

        public override SysExpr ToExpression() => ToLambdaExpression();

        public System.Linq.Expressions.LambdaExpression ToLambdaExpression() =>
            SysExpr.Lambda(Type, Body.ToExpression(), ParameterExpression.ToParameterExpressions(Parameters));

        internal LambdaExpression(Type delegateType, Expression body, IReadOnlyList<ParameterExpression> parameters)
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
                ReturnType = delegateType.FindDelegateInvokeMethod().ReturnType;
                Type = delegateType;
            }
        }
    }

    public sealed class Expression<TDelegate> : LambdaExpression
    {
        public new System.Linq.Expressions.Expression<TDelegate> ToLambdaExpression() =>
            SysExpr.Lambda<TDelegate>(Body.ToExpression(), ParameterExpression.ToParameterExpressions(Parameters));

        internal Expression(Expression body, IReadOnlyList<ParameterExpression> parameters)
            : base(typeof(TDelegate), body, parameters) { }
    }
}
//#endif
