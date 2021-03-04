using System;
using System.Linq;
using System.Reflection;
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
    public class ClosureConstantTests : ITest
    {
        public int Run()
        {
            Should_load_value_type_constant();
            Repeating_the_constants_should_load_variables();
            return 2;
        }

        /*
        Expression.Compiled:
        ```
        IL_0000: ldarg.0    
        IL_0001: ldfld      System.Object[] Constants/System.Runtime.CompilerServices.Closure
        IL_0006: ldc.i4.0   
        IL_0007: ldelem.ref 
        IL_0008: unbox.any  FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests+V
        IL_000d: stloc.0    
        IL_000e: ldloc.0    
        IL_000f: ldloc.0    
        IL_0010: newobj     Void .ctor(V)/FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests+B1
        IL_0015: ldloc.0    
        IL_0016: newobj     Void .ctor(V)/FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests+C1
        IL_001b: newobj     Void .ctor(V, B1, C1)/FastExpressionCompiler.LightExpression.UnitTests.ClosureConstantTests+A1
        IL_0020: ret        
        ```

        CompileFast:
        ```
        IL_0000: ldarg.0    
        IL_0001: ldfld      System.Object[] ConstantsAndNestedLambdas/FastExpressionCompiler.ExpressionCompiler+ArrayClosure
        IL_0006: stloc.0    
        IL_0007: ldloc.0    
        IL_0008: ldc.i4.0   
        IL_0009: ldelem.ref 
        IL_000a: unbox.any  FastExpressionCompiler.UnitTests.ClosureConstantTests+V
        IL_000f: stloc.1    
        IL_0010: ldloc.1    
        IL_0011: ldloc.1    
        IL_0012: newobj     Void .ctor(V)/FastExpressionCompiler.UnitTests.ClosureConstantTests+B1
        IL_0017: ldloc.1    
        IL_0018: newobj     Void .ctor(V)/FastExpressionCompiler.UnitTests.ClosureConstantTests+C1
        IL_001d: newobj     Void .ctor(V, B1, C1)/FastExpressionCompiler.UnitTests.ClosureConstantTests+A1
        IL_0022: ret        
        ```
         */

        [Test]
        public void Should_load_value_type_constant()
        {
            var v = Constant(new V { N = 3 });

            var fe = Lambda<Func<A1>>(
                New(typeof(A1).GetTypeInfo().DeclaredConstructors.First(),
                    v, New(typeof(B1).GetTypeInfo().DeclaredConstructors.First(),
                        v), New(typeof(C1).GetTypeInfo().DeclaredConstructors.First(),
                        v)));

            var fs = fe.CompileSys();
            Assert.IsNotNull(fs());

            var f = fe.CompileFast(true);
            Assert.IsNotNull(f());
        }

        /*
        `Expression.Compile` IL is 37 lines:

        ```
        IL_0000: ldarg.0    
        IL_0001: ldfld      System.Object[] Constants/System.Runtime.CompilerServices.Closure
        IL_0006: dup        
        IL_0007: ldc.i4.0   
        IL_0008: ldelem.ref 
        IL_0009: castclass  FastExpressionCompiler.UnitTests.ClosureConstantTests+Q
        IL_000e: stloc.0    
        IL_000f: dup        
        IL_0010: ldc.i4.1   
        IL_0011: ldelem.ref 
        IL_0012: castclass  FastExpressionCompiler.UnitTests.ClosureConstantTests+X
        IL_0017: stloc.1    
        IL_0018: dup        
        IL_0019: ldc.i4.2   
        IL_001a: ldelem.ref 
        IL_001b: castclass  FastExpressionCompiler.UnitTests.ClosureConstantTests+Y
        IL_0020: stloc.2    
        IL_0021: ldc.i4.3   
        IL_0022: ldelem.ref 
        IL_0023: castclass  FastExpressionCompiler.UnitTests.ClosureConstantTests+Z
        IL_0028: stloc.3    
        IL_0029: ldloc.0    
        IL_002a: ldloc.1    
        IL_002b: ldloc.2    
        IL_002c: ldloc.3    
        IL_002d: ldloc.0    
        IL_002e: ldloc.1    
        IL_002f: ldloc.2    
        IL_0030: ldloc.3    
        IL_0031: newobj     Void .ctor(Q, X, Y, Z)/FastExpressionCompiler.UnitTests.ClosureConstantTests+B
        IL_0036: ldloc.0    
        IL_0037: ldloc.1    
        IL_0038: ldloc.2    
        IL_0039: ldloc.3    
        IL_003a: newobj     Void .ctor(Q, X, Y, Z)/FastExpressionCompiler.UnitTests.ClosureConstantTests+C
        IL_003f: newobj     Void .ctor(Q, X, Y, Z, B, C)/FastExpressionCompiler.UnitTests.ClosureConstantTests+A
        IL_0044: ret
        ```

        `CompileFast(true)` IL is 35 lines:

        ```
        IL_0000: ldarg.0    
        IL_0001: ldfld      System.Object[] ConstantsAndNestedLambdas/FastExpressionCompiler.ExpressionCompiler+ArrayClosure
        IL_0006: stloc.0    
        IL_0007: ldloc.0    
        IL_0008: ldc.i4.0   
        IL_0009: ldelem.ref 
        IL_000a: stloc.1    
        IL_000b: ldloc.0    
        IL_000c: ldc.i4.1   
        IL_000d: ldelem.ref 
        IL_000e: stloc.2    
        IL_000f: ldloc.0    
        IL_0010: ldc.i4.2   
        IL_0011: ldelem.ref 
        IL_0012: stloc.3    
        IL_0013: ldloc.0    
        IL_0014: ldc.i4.3   
        IL_0015: ldelem.ref 
        IL_0016: stloc.s    V_4
        IL_0018: ldloc.1    
        IL_0019: ldloc.2    
        IL_001a: ldloc.3    
        IL_001b: ldloc.s    V_4
        IL_001d: ldloc.1    
        IL_001e: ldloc.2    
        IL_001f: ldloc.3    
        IL_0020: ldloc.s    V_4
        IL_0022: newobj     Void .ctor(Q, X, Y, Z)/FastExpressionCompiler.UnitTests.ClosureConstantTests+B
        IL_0027: ldloc.1    
        IL_0028: ldloc.2    
        IL_0029: ldloc.3    
        IL_002a: ldloc.s    V_4
        IL_002c: newobj     Void .ctor(Q, X, Y, Z)/FastExpressionCompiler.UnitTests.ClosureConstantTests+C
        IL_0031: newobj     Void .ctor(Q, X, Y, Z, B, C)/FastExpressionCompiler.UnitTests.ClosureConstantTests+A
        IL_0036: ret        
    ```
        */

        [Test]
        public void Repeating_the_constants_should_load_variables()
        {
            var q = Constant(new Q());
            var x = Constant(new X());
            var y = Constant(new Y());
            var z = Constant(new Z());
            var v = Constant(new V { N = 3 });

            var fe = Lambda<Func<A>>(
            New(typeof(A).GetTypeInfo().DeclaredConstructors.First(), 
                q, x, y, z, v, New(typeof(B).GetTypeInfo().DeclaredConstructors.First(),
                    q, x, y, z, v), New(typeof(C).GetTypeInfo().DeclaredConstructors.First(),
                    q, x, y, z, v)));

            var fs = fe.CompileSys();
            Assert.IsNotNull(fs());

            var f = fe.CompileFast(true);
            Assert.IsNotNull(f);
            var result = f();
            Assert.AreEqual(3, result.V.N);
            Assert.AreEqual(result.V.N, result.B.V.N);
        }

        public class Q { }
        public class X { }
        public class Y { }
        public class Z { }

        public struct V
        {
            public int N;
        }

        public class A1
        {
            public V V { get; }
            public B1 B { get; }
            public C1 C { get; }

            public A1(V v, B1 b, C1 c)
            {
                B = b;
                C = c;
                V = v;
            }
        }

        public class A
        {
            public Q Q { get; }
            public X X { get; }
            public Y Y { get; }
            public Z Z { get; }
            public V V { get; }
            public B B { get; }
            public C C { get; }

            public A(Q q, X x, Y y, Z z, V v, B b, C c)
            {
                Q = q;
                X = x;
                Y = y;
                Z = z;
                B = b;
                C = c;
                V = v;
            }
        }

        public class B1
        {
            public V V { get; }

            public B1(V v)
            {
                V = v;
            }
        }

        public class B
        {
            public Q Q { get; }
            public X X { get; }
            public Y Y { get; }
            public Z Z { get; }
            public V V { get; }

            public B(Q q, X x, Y y, Z z, V v)
            {
                Q = q;
                X = x;
                Y = y;
                Z = z;
                V = v;
            }
        }

        public class C1
        {
            public V V { get; }

            public C1(V v)
            {
                V = v;
            }
        }

        public class C
        {
            public Q Q { get; }
            public X X { get; }
            public Y Y { get; }
            public Z Z { get; }
            public V V { get; }

            public C(Q q, X x, Y y, Z z, V v)
            {
                Q = q;
                X = x;
                Y = y;
                Z = z;
                V = v;
            }
        }
    }
}
