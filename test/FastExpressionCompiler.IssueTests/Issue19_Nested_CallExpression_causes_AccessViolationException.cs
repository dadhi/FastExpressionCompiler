using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue19_Nested_CallExpression_causes_AccessViolationException
    {
        [Test]
        public void TestString()
        {
            var param = Parameter(typeof(Test), "x");
            var predicate = Lambda<Func<Test, Test>>(
                MemberInit(
                    New(typeof(Test)),
                    Bind(typeof(Test).GetTypeInfo().GetProperty("Name"),
                        Call(
                            Call(
                                _innerMethod.MakeGenericMethod(typeof(int)),
                                Property(param, "Id")
                            ),
                            _toStringMethod
                        )
                    )
                ),
                param
            );

            // build
            var fn = predicate.Compile();
            var ffn = predicate.CompileFast();

            // test
            var entity = new Test();

            var fnRes = fn(entity);
            Assert.AreEqual(fnRes.Name, fnRes.Id.ToString());

            var ffnRes = ffn(entity);
            Assert.AreEqual(ffnRes.Name, ffnRes.Id.ToString());
        }

        static readonly MethodInfo _toStringMethod = MethodOf(() => new object().ToString());
        static readonly MethodInfo _innerMethod = MethodOf(() => InnerCall(default(int))).GetGenericMethodDefinition();

        public static TReturn InnerCall<TReturn>(TReturn input)
        {
            return input; // here we would do some transformation
        }

        public static MethodInfo MethodOf(Expression<Func<object>> func)
        {
            var mi = GetMemberInfo(func);
            return mi is PropertyInfo ? ((PropertyInfo)mi).GetGetMethod() : (MethodInfo)mi;
        }

        public static MemberInfo GetMemberInfo(LambdaExpression func)
        {
            var ex = func.Body;

            if (ex is UnaryExpression)
                ex = ((UnaryExpression)ex).Operand;

            if (ex.NodeType == ExpressionType.New)
                return ((NewExpression)ex).Constructor;

            return
                ex is MemberExpression ? ((MemberExpression)ex).Member :
                ex is MethodCallExpression ? ((MethodCallExpression)ex).Method : (MemberInfo)((NewExpression)ex).Constructor;
        }
   
        public class Test
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public Test()
            {
                Id = 1;
            }
        }
    }
}
