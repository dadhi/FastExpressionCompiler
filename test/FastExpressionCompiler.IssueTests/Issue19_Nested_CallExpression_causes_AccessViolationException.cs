using System;
using System.Linq;
using System.Reflection;
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
    public class Issue19_Nested_CallExpression_causes_AccessViolationException : ITest
    {
        public int Run()
        {
            TestString();
            return 1;
        }

        [Test]
        public void TestString()
        {
            var param = Parameter(typeof(Test), "x");
            var predicate = Lambda<Func<Test, Test>>(
                MemberInit(
                    New(typeof(Test)),
                    Bind(typeof(Test).GetTypeInfo().GetDeclaredProperty("Name"),
                        Call(
                            Call(_innerMethod.MakeGenericMethod(typeof(int)),
                                Property(param, "Id")
                            ),
                            _toStringMethod
                        )
                    )
                ),
                param);

            // build
            var ffn = predicate.CompileFast();

            // test
            var entity = new Test();
        
            var ffnRes = ffn(entity);
            Assert.AreEqual(ffnRes.Name, ffnRes.Id.ToString());
        }

        static readonly MethodInfo _toStringMethod = typeof(object).GetTypeInfo().DeclaredMethods.First(x => x.Name == "ToString" && x.GetParameters().Length == 0);

        static readonly MethodInfo _innerMethod = typeof(Issue19_Nested_CallExpression_causes_AccessViolationException).GetTypeInfo().GetMethod("InnerCall");

        public static TReturn InnerCall<TReturn>(TReturn input)
        {
            return input; // here we would do some transformation
        }

        public static MemberInfo GetMemberInfo(LambdaExpression func)
        {
            var ex = func.Body;

            if (ex is UnaryExpression)
                ex = ((UnaryExpression)ex).Operand;

            if (ex.NodeType == System.Linq.Expressions.ExpressionType.New)
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
