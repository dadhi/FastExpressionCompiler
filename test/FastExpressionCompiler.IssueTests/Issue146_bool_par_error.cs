using System;
using NUnit.Framework;

#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unaasigned fields

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
[TestFixture]
    public class Issue146_bool_par_error
    {
        
        class MyObject
        {
            public bool a<b>(b i)
            {
                return object.Equals(i, false);
            }
        }

        
        [Test]
        public void Test3Bool()
        {
            var objParam = Parameter(typeof(MyObject), "myObj");
            var boolParam = Parameter(typeof(bool), "isSomething");
            var myMethod = typeof(MyObject).GetMethod("a").MakeGenericMethod(typeof(bool));
            var call = Call(objParam, myMethod, boolParam);

            var lambda = Lambda<Func<MyObject, bool, bool>>(
                call,
                objParam,
                boolParam);

            var func = lambda.CompileFast();

            var ret = func.Invoke(new MyObject(), false);

            Assert.AreEqual(true, ret);
        }
    }
}
