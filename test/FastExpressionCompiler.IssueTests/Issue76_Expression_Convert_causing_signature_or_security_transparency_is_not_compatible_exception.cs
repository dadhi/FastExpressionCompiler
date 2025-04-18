﻿using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue76_Expression_Convert_causing_signature_or_security_transparency_is_not_compatible_exception : ITest
    {
        public int Run()
        {
            When_using_fast_expression_compilation();
            CanConvert();
            return 2;
        }

        public void When_using_fast_expression_compilation()
        {
            var id = Guid.NewGuid();
            var instance = new TestTarget();

            var expr = CreateWriter<TestTarget>();
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            fs(instance, id);
            Asserts.AreEqual(instance.ID, new CustomID(id));

            // both ExpressionCompiler.Compile and .CompileFast should work with action
            var ff = expr.CompileFast(true);
            ff.PrintIL();
            ff(instance, id);
            Asserts.AreEqual(instance.ID, new CustomID(id));
        }

        public void CanConvert()
        {
            var idParam = Parameter(typeof(Guid), "id");
            var cast = Convert(idParam, typeof(CustomID));
            var lambda = Lambda<Func<Guid, CustomID>>(cast, idParam);
            var convert = lambda.CompileFast();

            var id = Guid.NewGuid();
            var account = convert(id);

            Asserts.AreEqual(account, new CustomID(id));
        }

        private Expression<Action<T, Guid>> CreateWriter<T>()
        {
            var docParam = Parameter(typeof(T), "doc");
            var idParam = Parameter(typeof(Guid), "id");

            var cast = Convert(idParam, typeof(CustomID));

            var member = PropertyOrField(docParam, nameof(TestTarget.ID));
            var assign = Assign(member, cast);

            var lambda = Lambda<Action<T, Guid>>(assign, docParam, idParam);

            return lambda;
        }

        private class TestTarget
        {
            public CustomID ID { get; set; }
        }

        public struct CustomID : IEquatable<CustomID>
        {
            private readonly Guid _id;

            public CustomID(Guid id) => _id = id;

            public bool Equals(CustomID other) => _id.Equals(other._id);

            public override bool Equals(object obj) => obj is CustomID id && Equals(id);

            public override int GetHashCode() => _id.GetHashCode();
            public override string ToString() => _id.ToString();

            public static bool operator ==(CustomID left, CustomID right) => left.Equals(right);
            public static bool operator !=(CustomID left, CustomID right) => !left.Equals(right);

            public static implicit operator CustomID(Guid id) => new CustomID(id);
        }
    }
}
