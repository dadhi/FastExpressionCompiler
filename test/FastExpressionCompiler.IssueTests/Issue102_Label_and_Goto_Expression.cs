﻿using System;
using System.Linq.Expressions;
using NUnit.Framework;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    class Issue102_Label_and_Goto_Expression
    {
        [Test]
        public void BlockWithGotoIsSupported()
        {
            LabelTarget returnTarget = Expression.Label("aaa");

            BlockExpression blockExpr =
                Expression.Block(
                    Expression.Call(typeof(Console).GetMethod("WriteLine", new[] {typeof(string)}),
                        Expression.Constant("GoTo")),
                    Expression.Goto(returnTarget),
                    Expression.Call(typeof(Console).GetMethod("WriteLine", new[] {typeof(string)}),
                        Expression.Constant("Other Work")),
                    Expression.Label(returnTarget),
                    Expression.Constant(7)
                );

            var lambda = Expression.Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast<Func<int>>(true);
            Assert.NotNull(fastCompiled);
        }

        [Test]
        public void UnkownLabelShouldThrow()
        {
            var lambda = Expression.Lambda(
                Expression.Return(Expression.Label(), Expression.Constant(1)));

            Assert.Throws<InvalidOperationException>(() => lambda.Compile());
            Assert.Throws<InvalidOperationException>(() => lambda.CompileFast(true));
        }
    }
}
