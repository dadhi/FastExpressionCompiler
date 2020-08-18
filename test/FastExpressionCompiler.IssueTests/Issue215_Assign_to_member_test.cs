using System;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue215_Assign_to_member_test : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
            var type = Type.GetType("System.Reflection.RtFieldInfo", false);
            var fieldInfo_m_Attributes = type?.GetField("m_fieldAttributes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var fieldInfoParam = Parameter(typeof(FieldInfo));
            var castedType = Convert(fieldInfoParam, type);
            var returnLabel = Label();
            var nr = Lambda(
                Block(
                    Assign(MakeMemberAccess(castedType, fieldInfo_m_Attributes),
                        Convert(And(Convert(MakeMemberAccess(castedType, fieldInfo_m_Attributes), typeof(int)), Constant((int)(~FieldAttributes.InitOnly)))
                            ,typeof(System.Reflection.FieldAttributes))
                        )
                    , Return(returnLabel)
                    , Label(returnLabel)
                    )
                , fieldInfoParam
                );

            nr.PrintCSharpString();
            var nra = (Action<FieldInfo>)nr.CompileFast(true);
            Assert.IsNotNull(nra);

            nra(typeof(A).GetField(nameof(A.F)));

            // SetFieldInfoReadonly = (Action<FieldInfo>)Expression.Lambda(
            //     Expression.Block(
            //         Expression.Assign(Expression.MakeMemberAccess(castedType, fieldInfo_m_Attributes),
            //             Expression.Convert(Expression.Or(Expression.Convert(Expression.MakeMemberAccess(castedType, fieldInfo_m_Attributes), typeof(int)), Expression.Constant((int)(FieldAttributes.InitOnly)))
            //                 , typeof(FieldAttributes))
            //             )
            //         , Expression.Return(returnLabel),
            //         Expression.Label(returnLabel)
            //         )
            //     , fieldInfoParam
            //     ).CompileFast();
        }

        public class A 
        {
            public int F;
        }
    }
}
