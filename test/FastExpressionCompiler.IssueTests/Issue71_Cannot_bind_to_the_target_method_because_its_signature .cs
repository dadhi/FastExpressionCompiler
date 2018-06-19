using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Jit
    {
        [Test]
        public void MultiplePropertiesFailure()
        {
            var expr = BuildExpression<object, object>(typeof(Person), "Boss.Name.First");
            var dlg = expr.CompileFast();

            var data = new Person()
            {
                Boss = new Person()
                {
                    Name = new Name()
                    {
                        First = "John",
                        Last = "Smith"
                    }
                }
            };

            dlg.Invoke(data, "Kevin");

            Assert.AreEqual("Kevin", data.Boss.Name.First);
        }

        [Test]
        public void MultiplePropertiesSuccess()
        {
            var expr = BuildExpression<object, object>(typeof(Person), "Boss.Name.First");
            var dlg = expr.Compile();

            var data = new Person()
            {
                Boss = new Person()
                {
                    Name = new Name()
                    {
                        First = "John",
                        Last = "Smith"
                    }
                }

            };

            dlg.Invoke(data, "Kevin");
            Assert.AreEqual("Kevin", data.Boss.Name.First);
        }

        [Test]
        public void SinglePropertyFailure()
        {
            var expr = BuildExpression<object, object>(typeof(Person), "Age");
            var dlg = expr.CompileFast();

            var data = new Person()
            {
                Age = 21
            };

            dlg.Invoke(data, 40);
            Assert.AreEqual(40, data.Age);
        }

        [Test]
        public void SinglePropertySuccess()
        {
            var expr = BuildExpression<object, object>(typeof(Person), "Age");
            var dlg = expr.Compile();

            var data = new Person()
            {
                Age = 21
            };

            dlg.Invoke(data, 40);
            Assert.AreEqual(40, data.Age);
        }

        public class Person
        {
            public int Age { get; set; }

            public Name Name { get; set; }
            public decimal Salary { get; set; }
            public Person Boss { get; set; }
        }

        public class Name
        {
            public string First { get; set; }
            public string Middle { get; set; }
            public string Last { get; set; }
        }

        private static Expression<Action<TTarget, TValue>> BuildExpression<TTarget, TValue>(Type declaringType, string path)
        {
            var targetType = typeof(TTarget);
            var valueType = typeof(TValue);

            var targetParameter = Expression.Parameter(targetType, "target");
            var valueParameter = Expression.Parameter(valueType, "value");

            Expression current = targetParameter;

            if (targetType != declaringType)
            {
                // we need to cast the target to the declaring type so the property or field can be accessed.
                current = Expression.Convert(current, declaringType);
            }

            current = BuildMemberGetterExpression(current, path);

            Expression value = valueParameter;
            if (current.Type != valueType)
            {
                value = Expression.Convert(valueParameter, current.Type);
            }

            var assignment = Expression.Assign(current, value);

            // lambda is one of the following depending on the casts required. 
            // '(target, value) => ((declaringType)target).PropertyOrField[.PropertyOrField ...] = (PropertyOrFieldType)value'
            // '(target, value) => target.PropertyOrField[.PropertyOrField ...] = (PropertyOrFieldType)value'
            // '(target, value) => ((declaringType)target).PropertyOrField[.PropertyOrField ...] = value'
            // '(target, value) => target.PropertyOrField[.PropertyOrField ...] = value'

            var lambda = Expression.Lambda<Action<TTarget, TValue>>(assignment,
                $"Setter_{declaringType.Name}_{path.Replace('.', '_')}", new[] { targetParameter, valueParameter });

            return lambda;
        }

        public static Expression BuildMemberGetterExpression(Expression expression, string memberPath)
        {
            string[] segments = memberPath.Split('.');
            foreach (var segment in segments)
            {
                MemberInfo member = FindMember(expression.Type, segment);

                var property = member as PropertyInfo;
                var field = member as FieldInfo;

                if (property == null && field == null)
                {
                    throw new ArgumentException($"A {member.MemberType} member is not supported. Expected a Property or Field member.");
                }

                expression = field == null ? Expression.Property(expression, property) : Expression.Field(expression, field);
            }
            return expression;
        }

        public static MemberInfo FindMember(Type declaringType, string memberName)
        {
            MemberInfo member = declaringType.GetProperty(
                                memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (member == null)
            {
                member = declaringType.GetField(
                    memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }

            if (member == null)
            {
                throw new ArgumentException($"Type '{declaringType.FullName}' does not contain a property of field named {memberName}", nameof(memberName));
            }

            return member;
        }
    }
}
