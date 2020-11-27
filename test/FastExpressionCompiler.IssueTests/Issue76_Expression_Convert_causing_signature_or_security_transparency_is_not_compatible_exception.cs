using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
[TestFixture]
    public class Issue76_Expression_Convert_causing_signature_or_security_transparency_is_not_compatible_exception : ITest
    {
        public int Run()
        {
            When_using_fast_expression_compilation();
            CanConvert();
            return 2;
        }

        [Test]
        public void When_using_fast_expression_compilation()
        {
            var id = Guid.NewGuid();
            var instance = new TestTarget();

            var expr = CreateWriter<TestTarget>();

            // both ExpressionCompiler.Compile and .CompileFast should work with action
            var write = expr.CompileFast(true);

            write(instance, id);

            Assert.AreEqual(instance.ID, new CustomID(id));
        }

        [Test]
        public void CanConvert()
        {
            var idParam = Expression.Parameter(typeof(Guid), "id");
            var cast = Expression.Convert(idParam, typeof(CustomID));
            var lambda = Expression.Lambda<Func<Guid, CustomID>>(cast, idParam);
            var convert = lambda.CompileFast();

            var id = Guid.NewGuid();
            var account = convert(id);

            Assert.AreEqual(account, new CustomID(id));
        }

        private Expression<Action<T, Guid>> CreateWriter<T>()
        {
            var docParam = Expression.Parameter(typeof(T), "doc");
            var idParam = Expression.Parameter(typeof(Guid), "id");

            var cast = Expression.Convert(idParam, typeof(CustomID));

            var member = Expression.PropertyOrField(docParam, nameof(TestTarget.ID));
            var assign = Expression.Assign(member, cast);

            var lambda = Expression.Lambda<Action<T, Guid>>(assign, docParam, idParam);

            return lambda;
        }

        private class TestTarget
        {
            public CustomID ID { get; set; }
        }

        public struct CustomID : IEquatable<CustomID>
        {
            private readonly Guid _id;

            public CustomID(Guid id)
            {
                _id = id;
            }

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
