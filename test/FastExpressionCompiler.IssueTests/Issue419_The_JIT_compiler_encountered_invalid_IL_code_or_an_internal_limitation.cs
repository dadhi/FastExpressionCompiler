using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue419_The_JIT_compiler_encountered_invalid_IL_code_or_an_internal_limitation : ITest
{
  public int Run()
  {
    Original_Case_1();
    Case_1_Simplified_less();
    Case_1_simplified();
    Case_1_Simplified_less_reversed_mul_args();
    Original_Case_2();
    return 5;
  }

  public class Obj
  {
    public double? X { get; }
    public Nested Nested { get; }
    public Obj(double? x, Nested nested) => (X, Nested) = (x, nested);
  }

  public class Nested
  {
    public double? Y { get; }
    public Nested(double? y) => Y = y;
  }

  [Test]
  public void Original_Case_1()
  {
    System.Linq.Expressions.Expression<Func<Obj, bool>> se = d =>
        (d == null ? null : d.X) >
        (double?)3 * ((d == null ? null : d.Nested) == null ? null : d.Nested.Y);

    var e = se.FromSysExpression();

    e.PrintCSharp();

    var @cs = (Func<Obj, bool>)((Obj d) => //bool
        (d == null ? (double?)null :
        d.X) > (((double?)3) * (((d == null ? (Nested)null :
        d.Nested) == null) ? (double?)null :
        d.Nested.Y)));

    var data = new Obj(10, new Nested(5));
    Asserts.IsFalse(@cs(data));

    var fs = e.CompileSys();
    fs.PrintIL();

    var ff = e.CompileFast(true);
    ff.PrintIL();

    Asserts.IsFalse(fs(data));
    Asserts.IsFalse(ff(data));
  }

  [Test]
  public void Case_1_Simplified_less_reversed_mul_args()
  {
    System.Linq.Expressions.Expression<Func<Obj, bool>> se = d =>
        (d == null ? null : d.X) >
        (d.Nested == null ? null : d.Nested.Y) * (double?)3;

    var e = se.FromSysExpression();

    e.PrintCSharp();

    var data = new Obj(10, new Nested(5));

    var fs = e.CompileSys();
    fs.PrintIL();

    var ff = e.CompileFast(true);
    ff.PrintIL();

    Asserts.IsFalse(fs(data));
    Asserts.IsFalse(ff(data));
  }

  [Test]
  public void Case_1_Simplified_less()
  {
    System.Linq.Expressions.Expression<Func<Obj, bool>> se = d =>
        (d == null ? null : d.X) >
        (double?)3 * (d.Nested == null ? null : d.Nested.Y);

    var e = se.FromSysExpression();

    e.PrintCSharp();

    var data = new Obj(10, new Nested(5));

    var fs = e.CompileSys();
    fs.PrintIL();

    var ff = e.CompileFast(true);
    ff.PrintIL();

    Asserts.IsFalse(fs(data));
    Asserts.IsFalse(ff(data));
  }

  [Test]
  public void Case_1_simplified()
  {
    System.Linq.Expressions.Expression<Func<Obj, bool>> se = d =>
        (d == null ? null : d.X) >
        (double?)3;

    var e = se.FromSysExpression();

    e.PrintCSharp();

    var data = new Obj(10, new Nested(5));

    var fs = e.CompileSys();
    fs.PrintIL();

    var ff = e.CompileFast(true);
    ff.PrintIL();

    Asserts.IsTrue(fs(data));
    Asserts.IsTrue(ff(data));
  }
  // #endif

  public delegate TOut RefFunc<TIn, TOut>(in TIn t);

  [Test]
  public void Original_Case_2()
  {
    var p = new ParameterExpression[1]; // the parameter expressions
    var e = new Expression[19]; // the unique expressions
    var expr = Lambda<RefFunc<Obj, bool>>(
      e[0] = MakeBinary(ExpressionType.GreaterThan,
        e[1] = Condition(
          e[2] = MakeBinary(ExpressionType.Equal,
            p[0] = Parameter(typeof(Obj).MakeByRefType(), "p"),
            e[3] = Constant(null)),
          e[4] = Constant(null, typeof(double?)),
          e[5] = Property(
            p[0 // (Obj p)
              ],
            typeof(Obj).GetTypeInfo().GetDeclaredProperty("X")),
          typeof(double?)),
        e[6] = MakeBinary(ExpressionType.Multiply,
          e[7] = Convert(
            e[8] = Constant(2),
            typeof(double?)),
          e[9] = Condition(
            e[10] = MakeBinary(ExpressionType.Equal,
              e[11] = Condition(
                e[12] = MakeBinary(ExpressionType.Equal,
                  p[0 // (Obj p)
                    ],
                  e[13] = Constant(null)),
                e[14] = Constant(null, typeof(Nested)),
                e[15] = Property(
                  p[0 // (Obj p)
                    ],
                  typeof(Obj).GetTypeInfo().GetDeclaredProperty("Nested")),
                typeof(Nested)),
              e[16] = Constant(null)),
            e[17] = Constant(null, typeof(double?)),
            e[18] = Property(
              e[11 // Conditional of Nested
                ],
              typeof(Nested).GetTypeInfo().GetDeclaredProperty("Y")),
            typeof(double?)),
          liftToNull: true,
          null)),
      p[0 // (Obj p)
        ]);

    expr.PrintCSharp();

    var fs = expr.CompileSys();
    fs.PrintIL();

    var ff = expr.CompileFast(true);
    ff.PrintIL();

    var data = new Obj(5, new Nested(6));
    Asserts.IsFalse(fs(data));
    Asserts.IsFalse(ff(data));
  }
}
