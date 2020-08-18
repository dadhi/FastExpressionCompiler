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
                    , Label(returnLabel))
                , fieldInfoParam);

            nr.PrintCSharpString();
            var nra = (Action<FieldInfo>)nr.CompileFast(true);
            Assert.IsNotNull(nra);

            nra(typeof(A).GetField(nameof(A.F)));

            var r = Lambda(
                Block(
                    Assign(MakeMemberAccess(castedType, fieldInfo_m_Attributes),
                        Convert(Or(Convert(MakeMemberAccess(castedType, fieldInfo_m_Attributes), typeof(int)), Constant((int)(FieldAttributes.InitOnly)))
                            , typeof(FieldAttributes))
                        )
                    , Return(returnLabel)
                    , Label(returnLabel))
                , fieldInfoParam);

            r.PrintCSharpString();
            var ra = (Action<FieldInfo>)r.CompileFast(true);
            Assert.IsNotNull(ra);
            ra(typeof(A).GetField(nameof(A.F)));
        }

        public class A 
        {
            public int F;
        }
    }
}
