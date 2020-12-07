using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class ListInitTests : ITest
    {
        public int Run()
        {
            Simple_ListInit_works();
            return 1;
        }

        public static Expression<Func<ListInitTests, IEnumerable<PropertyValue>>> Get_Simple_ListInit_Expression()
        {
            var cp = new ClassProperty();

            var p = new ParameterExpression[1]; // the parameter expressions 
            var e = new Expression[12]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda<Func<ListInitTests, IEnumerable<PropertyValue>>>( // $
                e[0] =
                    // NOT_SUPPORTED_EXPRESSION: ListInit
                    ListInit(
                        (NewExpression)(e[1] = New(/*0 args*/
                            typeof(List<PropertyValue>).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])),
                        ElementInit(
                            typeof(List<PropertyValue>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(PropertyValue) })),
                            e[2] = New(/*3 args*/
                                typeof(PropertyValue).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                                e[3] = Constant("Id"),
                                e[4] = Convert(
                                    e[5] = Property(
                                        p[0] = Parameter(typeof(ListInitTests), "obj"),
                                        typeof(ListInitTests).GetTypeInfo().GetDeclaredProperty("Id")),
                                    typeof(object)),
                                e[6] = Constant(cp)
                            )),
                        ElementInit(
                            typeof(List<PropertyValue>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Add" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(PropertyValue) })),
                            e[7] = New(/*3 args*/
                                typeof(PropertyValue).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                                e[8] = Constant("Property1"),
                                e[9] = Convert(
                                    e[10] = Property(
                                        p[0 // (ListInitTests obj)
                                            ],
                                        typeof(ListInitTests).GetTypeInfo().GetDeclaredProperty("Property1")),
                                    typeof(object)),
                                e[11] = Constant(cp))
                        )),
                    p[0 // (ListInitTests obj)
                        ]);
            return expr;
        }

        [Test]
        public void Simple_ListInit_works()
        {
            var expr = Get_Simple_ListInit_Expression();

            var fs = expr.CompileSys();
            fs.PrintIL();
            var lt = new ListInitTests();
            var ps = fs(lt).ToArray();
            Assert.AreEqual("id42", ps[0].Value);
            Assert.AreEqual("prop42", ps[1].Value);

            expr.PrintCSharp();
            var f = expr.CompileFast(true);
            f.PrintIL();
            ps = f(lt).ToArray();
            Assert.AreEqual("id42", ps[0].Value);
            Assert.AreEqual("prop42", ps[1].Value);
        }

        public object Id { get; set; } = "id42";
        public object Property1 { get; set; } = "prop42";

        public class ClassProperty { }
        public class PropertyValue
        {
            public string Name;
            public object Value;
            public ClassProperty Property;
            public PropertyValue(string name, object value, ClassProperty prop)
            {
                Name = name;
                Value = value;
                Property = prop;
            }
        }
    }
}