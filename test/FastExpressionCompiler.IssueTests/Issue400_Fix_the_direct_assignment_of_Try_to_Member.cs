using System;
using NUnit.Framework;
using System.Reflection;
using System.Linq;

#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue400_Fix_the_direct_assignment_of_Try_to_Member : ITest
{
    public int Run()
    {
        Test_original();
        return 1;
    }

    class PropertyMap {}

    public class ModelObject
    {
        public string DifferentBaseString { get; set; }
    }

    public class ModelSubObject : ModelObject
    {
        public string SubString { get; set; }
    }

    public class DtoObject
    {
        public string BaseString { get; set; }
    }

    public class DtoSubObject : DtoObject
    {
        public string SubString { get; set; }
    }

    public sealed class ResolutionContext {}

    public class TypeMapPlanBuilder
    {
        private static Exception MemberMappingError(Exception innerException, object memberMap) => new("Error mapping types.");
    }

    [Test]
    public void Test_original()
    {
        var p = new ParameterExpression[7]; // the parameter expressions
        var e = new Expression[33]; // the unique expressions
        var expr = Lambda<Func<ModelSubObject, DtoSubObject, ResolutionContext, DtoSubObject>>(
          e[0] = Condition(
            e[1] = MakeBinary(ExpressionType.Equal,
              p[0] = Parameter(typeof(ModelSubObject), "source"),
              e[2] = Default(typeof(object))),
            e[3] = Condition(
              e[4] = MakeBinary(ExpressionType.Equal,
                p[1] = Parameter(typeof(DtoSubObject), "destination"),
                e[2 // Default of object
                  ]),
              e[5] = Default(typeof(DtoSubObject)),
              p[1 // (DtoSubObject destination)
                ],
              typeof(DtoSubObject)),
            e[6] = Block(
              typeof(DtoSubObject),
              new[] {
                p[2]=Parameter(typeof(DtoSubObject), "typeMapDestination")
              },
              e[7] = Block(
                typeof(DtoSubObject),
                new ParameterExpression[0],
                e[8] = MakeBinary(ExpressionType.Assign,
                  p[2 // (DtoSubObject typeMapDestination)
                    ],
                  e[9] = Coalesce(
                    p[1 // (DtoSubObject destination)
                      ],
                    e[10] = New( // 0 args
                      typeof(DtoSubObject).GetTypeInfo().DeclaredConstructors.AsArray()[0], new Expression[0]))),
                e[11] = TryCatch(
                  e[12] = Block(
                    typeof(string),
                    new[] {
              p[3]=Parameter(typeof(string), "resolvedValue")
                    },
                    e[13] = MakeBinary(ExpressionType.Assign,
                      p[3 // (string resolvedValue)
                        ],
                      e[14] = Property(
                        p[0 // (ModelSubObject source)
                          ],
                        typeof(ModelSubObject).GetTypeInfo().GetDeclaredProperty("SubString"))),
                    e[15] = MakeBinary(ExpressionType.Assign,
                      e[16] = Property(
                        p[2 // (DtoSubObject typeMapDestination)
                          ],
                        typeof(DtoSubObject).GetTypeInfo().GetDeclaredProperty("SubString")),
                      p[3 // (string resolvedValue)
                        ])),
                  MakeCatchBlock(
                    typeof(Exception),
                    p[4] = Parameter(typeof(Exception), "ex"),
                    e[17] = Throw(
                      e[18] = Call(
                        null,
                        typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(object) })),
                        p[4 // (System.Exception ex)
                          ],
                        e[19] = Constant(default(PropertyMap)/*Please provide the non-default value for the constant!*/)),
                      typeof(string)),
                    null)),
                e[20] = TryCatch(
                  e[21] = Block(
                    typeof(string),
                    new[] {
              p[5]=Parameter(typeof(string), "resolvedValue")
                    },
                    e[22] = MakeBinary(ExpressionType.Assign,
                      p[5 // (string resolvedValue)
                        ],
                      e[23] = Coalesce(
                        e[24] = TryCatch(
                          e[25] = Property(
                            p[0 // (ModelSubObject source)
                              ],
                            typeof(ModelObject).GetTypeInfo().GetDeclaredProperty("DifferentBaseString")),
                          MakeCatchBlock(
                            typeof(NullReferenceException),
                            null,
                            e[26] = Default(typeof(string)),
                            null),
                          MakeCatchBlock(
                            typeof(ArgumentNullException),
                            null,
                            e[26 // Default of string
                              ],
                            null)),
                        e[27] = Constant("12345"))),
                    e[28] = MakeBinary(ExpressionType.Assign,
                      e[29] = Property(
                        p[2 // (DtoSubObject typeMapDestination)
                          ],
                        typeof(DtoObject).GetTypeInfo().GetDeclaredProperty("BaseString")),
                      p[5 // (string resolvedValue)
                        ])),
                  MakeCatchBlock(
                    typeof(Exception),
                    p[4 // (System.Exception ex)
                      ],
                    e[30] = Throw(
                      e[31] = Call(
                        null,
                        typeof(TypeMapPlanBuilder).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(x => !x.IsGenericMethod && x.Name == "MemberMappingError" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Exception), typeof(object) })),
                        p[4 // (System.Exception ex)
                          ],
                        e[32] = Constant(default(PropertyMap)/*Please provide the non-default value for the constant!*/)),
                      typeof(string)),
                    null)),
                p[2 // (DtoSubObject typeMapDestination)
                  ])),
            typeof(DtoSubObject)),
          p[0 // (ModelSubObject source)
            ],
          p[1 // (DtoSubObject destination)
            ],
          p[6] = Parameter(typeof(ResolutionContext), "context"));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true);
        ff.PrintIL();
    }
}
