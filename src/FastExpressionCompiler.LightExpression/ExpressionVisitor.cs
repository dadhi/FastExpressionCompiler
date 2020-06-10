using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace FastExpressionCompiler.LightExpression
{
    public abstract class ExpressionVisitor
    {
        public virtual Expression Visit(Expression node) => node?.Accept(this);

        public IReadOnlyList<Expression> Visit(IReadOnlyList<Expression> nodes)
        {
            Expression[] newNodes = null;
            var count = nodes.Count;
            for (var i = 0; i < count; ++i)
            {
                var newNode = Visit(nodes[i]);
                if (newNodes != null)
                    newNodes[i] = newNode;
                else if (newNode != nodes[i])
                {
                    newNodes = new Expression[count];
                    for (var j = 0; j < i; ++j)
                        newNodes[j] = nodes[j];
                    newNodes[i] = newNode;
                }
            }

            return newNodes ?? nodes;
        }

        public IReadOnlyList<T> VisitAndConvert<T>(IReadOnlyList<T> nodes) where T : Expression
        {
            T[] newNodes = null;
            var count = nodes.Count;
            for (var i = 0; i < count; ++i)
            {
                var newNode = VisitAndConvert(nodes[i]);
                if (newNodes != null)
                    newNodes[i] = newNode;
                else if (newNode != nodes[i])
                {
                    newNodes = new T[count];
                    for (var j = 0; j < i; ++j)
                        newNodes[j] = nodes[j];
                    newNodes[i] = newNode;
                }
            }
            return newNodes ?? nodes;
        }

        public IReadOnlyList<T> VisitAndConvert<T>(IReadOnlyList<T> nodes, Func<T, T> visit)
        {
            T[] newNodes = null;
            var count = nodes.Count;
            for (var i = 0; i < count; ++i)
            {
                var newNode = visit(nodes[i]);
                if (newNodes != null)
                    newNodes[i] = newNode;
                else if (!Equals(newNode, nodes[i]))
                {
                    newNodes = new T[count];
                    for (var j = 0; j < i; ++j)
                        newNodes[j] = nodes[j];
                    newNodes[i] = newNode;
                }
            }
            return (IReadOnlyList<T>)newNodes ?? nodes;
        }

        public T VisitAndConvert<T>(T node) where T : Expression
        {
            if (node == null)
                return default;
            var x = Visit(node);
            if (x is T converted)
                return converted;
            throw new InvalidOperationException($"Converting visited node is not compatible from {x?.GetType()} to {typeof(T)}");
        }

        protected internal virtual Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left); 
            var right = Visit(node.Right);
            if (node.Left == left && node.Right == right)
                return node;
            return Expression.MakeBinary(node.NodeType, left, right);
        }

        protected internal virtual Expression VisitBlock(BlockExpression node)
        {
            var expressions = Visit(node.Expressions);
            var variables = VisitAndConvert(node.Variables);
            if (ReferenceEquals(expressions, node.Expressions) && ReferenceEquals(variables, node.Variables))
                return node;
            return new BlockExpression(node.Type, variables, expressions);
        }

        protected internal virtual Expression VisitConditional(ConditionalExpression node)
        {
            var test = Visit(node.Test);
            var ifTrue = Visit(node.IfTrue);
            var ifFalse = Visit(node.IfFalse);
            if (test == node.Test && ifTrue == node.IfTrue && ifFalse == node.IfFalse)
                return node;
            return Expression.Condition(test, ifTrue, ifFalse, node.Type);
        }

        protected internal virtual Expression VisitConstant(ConstantExpression node) => node;

        protected internal virtual Expression VisitDefault(DefaultExpression node) => node;

        protected virtual LabelTarget VisitLabelTarget(LabelTarget node) => node;

        protected internal virtual Expression VisitGoto(GotoExpression node)
        {
            var target = VisitLabelTarget(node.Target);
            var value = Visit(node.Value);
            if (target == node.Target && value == node.Value)
                return node;
            return Expression.Goto(target, value, node.Type);
        }

        protected internal virtual Expression VisitInvocation(InvocationExpression node)
        {
            var expression = Visit(node.Expression);
            var arguments = Visit(node.Arguments);
            if (expression == node.Expression && ReferenceEquals(arguments, node.Arguments))
                return node;
            return Expression.Invoke(expression, arguments);
        }

        protected internal virtual Expression VisitLabel(LabelExpression node)
        {
            var target = VisitLabelTarget(node.Target);
            var value = Visit(node.DefaultValue);
            if (target == node.Target && value == node.DefaultValue)
                return node;
            return Expression.Label(target, value);
        }

        protected internal virtual Expression VisitLambda(LambdaExpression node)
        {
            var body = Visit(node.Body);
            var parameters = VisitAndConvert(node.Parameters);
            if (body == node.Body && ReferenceEquals(parameters, node.Parameters))
                return node;

            return parameters.Count == 0 
                ? new LambdaExpression(node.Type, body, node.ReturnType) 
                : new ManyParametersLambdaExpression(node.Type, body, parameters, node.ReturnType);
        }

        protected internal virtual Expression VisitLambda<T>(Expression<T> node)
        {
            var body = Visit(node.Body);
            var parameters = VisitAndConvert(node.Parameters);
            if (body == node.Body && ReferenceEquals(parameters, node.Parameters))
                return node;

            return parameters.Count == 0 
                ? new Expression<T>(body, node.ReturnType) 
                : new ManyParametersExpression<T>(body, parameters, node.ReturnType);
        }

        protected internal virtual Expression VisitLoop(LoopExpression node)
        {
            var breakLabel = VisitLabelTarget(node.BreakLabel);
            var continueLabel = VisitLabelTarget(node.ContinueLabel);
            var body = Visit(node.Body);
            if (breakLabel == node.BreakLabel && continueLabel == node.ContinueLabel && body == node.Body)
                return node;
            return Expression.Loop(body, breakLabel, continueLabel);
        }

        protected internal virtual Expression VisitMember(MemberExpression node)
        {
            var expression = Visit(node.Expression);
            if (expression == node.Expression)
                return node;
            return Expression.MakeMemberAccess(expression, node.Member);
        }

        protected internal virtual Expression VisitIndex(IndexExpression node)
        {
            var instance = Visit(node.Object);
            var arguments = Visit(node.Arguments);
            if (instance == node.Object && ReferenceEquals(arguments, node.Arguments))
                return node;
            return Expression.MakeIndex(instance, node.Indexer, arguments);
        }

        protected internal virtual Expression VisitMethodCall(MethodCallExpression node)
        {
            var instance = Visit(node.Object);
            var arguments = Visit(node.Arguments);
            if (instance == node.Object && arguments == null)
                return node;
            return Expression.Call(instance, node.Method, node.Arguments);
        }

        protected internal virtual Expression VisitNewArray(NewArrayExpression node)
        {
            var expressions = Visit(node.Expressions);
            if (ReferenceEquals(expressions, node.Expressions))
                return node;
            return new NewArrayExpression(node.NodeType, node.Type, expressions);
        }

        protected internal virtual Expression VisitNew(NewExpression node)
        {
            var arguments = Visit(node.Arguments);
            if (ReferenceEquals(arguments, node.Arguments))
                return node;
            return Expression.New(node.Constructor, arguments);
        }

        protected internal virtual Expression VisitParameter(ParameterExpression node) => node;

        protected virtual SwitchCase VisitSwitchCase(SwitchCase node)
        {
            var testValues = Visit(node.TestValues);
            var body = Visit(node.Body);
            if (ReferenceEquals(testValues, node.TestValues) && body == node.Body)
                return node;
            return new SwitchCase(body, testValues);
        }

        protected internal virtual Expression VisitSwitch(SwitchExpression node)
        {
            var switchValue = Visit(node.SwitchValue);
            var cases = VisitAndConvert(node.Cases, VisitSwitchCase);
            var defaultBody = Visit(node.DefaultBody);
            if (switchValue == node.SwitchValue && ReferenceEquals(cases, node.Cases) && defaultBody == node.DefaultBody)
                return node;
            return new SwitchExpression(node.Type, switchValue, defaultBody, node.Comparison, cases.AsArray());
        }

        protected virtual CatchBlock VisitCatchBlock(CatchBlock node)
        {
            var variable = VisitAndConvert(node.Variable);
            var filter = Visit(node.Filter);
            var body = Visit(node.Body);
            if (variable == node.Variable && filter == node.Filter && body == node.Body)
                return node;
            return new CatchBlock(variable, body, filter, node.Test);
        }

        protected internal virtual Expression VisitTry(TryExpression node)
        {
            var body = Visit(node.Body);
            var handlers = VisitAndConvert(node.Handlers, VisitCatchBlock);
            var @finally = Visit(node.Finally);
            if (body == node.Body && ReferenceEquals(handlers, node.Handlers) && @finally == node.Finally)
                return node;
            return new TryExpression(body, @finally, handlers.AsArray());
        }

        protected internal virtual Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            var expression = Visit(node.Expression);
            if (expression == node.Expression)
                return node;
            return new TypeBinaryExpression(node.NodeType, expression, node.TypeOperand);
        }

        protected internal virtual Expression VisitUnary(UnaryExpression node)
        {
            var operand = Visit(node.Operand);
            if (operand == node.Operand)
                return node;
            return Expression.MakeUnary(node.NodeType, operand, node.Type);
        }

        protected internal virtual Expression VisitMemberInit(MemberInitExpression node)
        {
            var newExpression = Visit(node.NewExpression);
            var bindings = VisitAndConvert(node.Bindings, VisitMemberBinding);
            if (newExpression == node.NewExpression && ReferenceEquals(bindings, node.Bindings))
                return node;
            return new MemberInitExpression(newExpression, bindings.AsArray());
        }

        protected virtual MemberBinding VisitMemberBinding(MemberBinding node)
        {
            switch (node.BindingType)
            {
                case MemberBindingType.Assignment:
                    return VisitMemberAssignment((MemberAssignment)node);
                //case MemberBindingType.MemberBinding:
                //case MemberBindingType.ListBinding:
                default:
                    throw new NotSupportedException($"Unhandled Binding Type: {node.BindingType}");
            }
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            var expression = Visit(node.Expression);
            if (expression == node.Expression)
                return node;
            return new MemberAssignment(node.Member, expression);
        }
    }
}
