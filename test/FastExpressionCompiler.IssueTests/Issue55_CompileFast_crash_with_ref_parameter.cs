﻿using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
#pragma warning disable 649
#pragma warning disable 219

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    // considers in/out/ref in C# represented by ByRef in expressions (i.e. single representation for 3 C# keywords)
    public class Issue55_CompileFast_crash_with_ref_parameter : ITest
    {
        public int Run()
        {
            RefMethodCallingRefMethodWithLocal_OfStruct();
            RefMethodCallingRefMethodWithLocal_OfString();

            BlockWithNonRefStatementLast();
            RefDoNothingShouldNoCrash();
            RefDoNothingShouldNoCrashCustomStruct();
            RefFromConstant();
            RefMethodCallingRefMethod();
            RefMethodCallingRefMethodCustomStruct();
            RefMethodCallingRefMethodWithLocal_OfInt();
            OutRefMethodCallingRefMethodWithLocal();
            RefMethodCallingRefMethodWithLocalReturnLocalCalled();
            VariableVariableRefVariableRefParameterReturn();
            Ref1Ref2Ref3();
            SetMinusOneAndOneForDoubleRefParameterInCall();
            AsValueAndAsRef();
            GenericRefFromConstant();
            GenericRefFromConstantReturn();
            RefReturnToVoid();
            RefRefVoid();
            RefRefReturnToVoid();
            RefSetSetForFields();
            RefDoNothingReturnConstant();
            RefFromLargeConstant();
            RefSetFromParameter();
            NonGenericSetterFieldShould_not_crash();
            GenericRefStructFieldShould_not_crash();
            RefAssign();
            RefAssignCustomStruct();
            CoalesceIntoByRef();
            ConstantOutInCondition();
            ConstantOut();

            IntPtrZeroReturn();
            NewIntPtr13Return();

            return 33;
        }

        delegate TResult FuncRef<T, out TResult>(ref T a1);
        delegate TResult FuncRefIn<T1, in T2, out TResult>(ref T1 a1, T2 a2);
        delegate void ActionRef<T>(ref T a1);
        delegate void ActionRefIn<T1, in T2>(ref T1 obj, T2 value);
        delegate void ActionRefRef<T1, T2>(ref T1 obj, ref T2 value);
        delegate void ActionRefRefRef<T1, T2, T3>(ref T1 obj, ref T2 value, ref T3 ref3);
        delegate TResult FuncRefRef<T1, T2, out TResult>(ref T1 obj, ref T2 value);

        struct StructWithIntField { public int IntField; }

        public void RefDoNothingShouldNoCrash()
        {
            void DoNothing(ref int ignore) { }
            var lambda = Lambda<ActionRef<int>>(Empty(), Parameter(typeof(int).MakeByRefType()));

            void LocalAssert(ActionRef<int> invoke)
            {
                var exampleA = default(int);
                invoke(ref exampleA);
                Asserts.AreEqual(0, exampleA);
            }
            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            LocalAssert(compiledB);

            ActionRef<int> direct = DoNothing;
            LocalAssert(direct);
        }

        public void RefDoNothingShouldNoCrashCustomStruct()
        {
            void DoNothing(ref BigInteger ignore) { }
            var lambda = Lambda<ActionRef<BigInteger>>(Empty(), Parameter(typeof(BigInteger).MakeByRefType()));

            void LocalAssert(ActionRef<BigInteger> invoke)
            {
                var exampleA = default(BigInteger);
                invoke(ref exampleA);
                Asserts.AreEqual(new BigInteger(0), exampleA);
            }
            var compiledB = lambda.CompileFast<ActionRef<BigInteger>>(true);
            LocalAssert(compiledB);

            ActionRef<BigInteger> direct = DoNothing;
            LocalAssert(direct);
        }


        public void RefFromConstant()
        {
            void SetSmallConstant(ref int localByRef)
            {
                const int objVal = 3;
                localByRef = objVal;
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<ActionRef<int>>(Assign(objRef, Constant(3)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Asserts.AreEqual(3, exampleB);

            ActionRef<int> direct = SetSmallConstant;
            var exampleC = default(int);
            direct(ref exampleC);
            Asserts.AreEqual(3, exampleC);
        }
        private static void SetMinus1(ref int localByRef) { localByRef = -1; }


        public void RefMethodCallingRefMethod()
        {
            void CallOtherRef(ref int localByRef) => SetMinus1(ref localByRef);
            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinus1));
            var lambda = Lambda<ActionRef<int>>(Call(call, objRef), objRef);

            void LocalAssert(ActionRef<int> invoke)
            {
                var exampleA = default(int);
                invoke(ref exampleA);
                Asserts.AreEqual(-1, exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            LocalAssert(compiledB);

            ActionRef<int> direct = CallOtherRef;
            LocalAssert(direct);
        }

        private static void SetMinusBigInteger1(ref BigInteger localByRef) { localByRef = new BigInteger(-1); }


        public void RefMethodCallingRefMethodCustomStruct()
        {
            void CallOtherRef(ref BigInteger localByRef) => SetMinusBigInteger1(ref localByRef);
            var objRef = Parameter(typeof(BigInteger).MakeByRefType());
            var variable = Variable(typeof(BigInteger));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinusBigInteger1));
            var lambda = Lambda<ActionRef<BigInteger>>(Call(call, objRef), objRef);

            void LocalAssert(ActionRef<BigInteger> invoke)
            {
                var exampleA = default(BigInteger);
                invoke(ref exampleA);
                Asserts.AreEqual(new BigInteger(-1), exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<BigInteger>>(true);
            LocalAssert(compiledB);

            ActionRef<BigInteger> direct = CallOtherRef;
            LocalAssert(direct);
        }


        public void RefMethodCallingRefMethodWithLocal_OfInt()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal = localByRef;
                SetMinus1(ref objVal);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinus1));
            var lambda = Lambda<ActionRef<int>>(Block(new[] { variable }, Assign(variable, objRef), Call(call, variable)), objRef);

            lambda.PrintCSharp();

            var compiledB = lambda.CompileFast(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Asserts.AreEqual(0, exampleB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Asserts.AreEqual(0, exampleC);
        }

        private static void SetMinus1_OfString(ref string localByRef) { localByRef = "-1"; }


        public void RefMethodCallingRefMethodWithLocal_OfString()
        {
            static void SetIntoLocalVariableAndCallOtherRef(ref string localByRef)
            {
                var objVal = localByRef;
                SetMinus1_OfString(ref objVal);
            }

            var objRef = Parameter(typeof(string).MakeByRefType());
            var variable = Variable(typeof(string));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinus1_OfString));
            var lambda = Lambda<ActionRef<string>>(Block(new[] { variable }, Assign(variable, objRef), Call(call, variable)), objRef);

            lambda.PrintCSharp();
            var @cs = (ActionRef<string>)((ref string string__58225482) =>
            {
                string string__54267293 = null;
                string__54267293 = string__58225482;
                SetMinus1_OfString(ref string__54267293);
            });
            var a = "0";
            @cs(ref a);
            Asserts.AreEqual("0", a);

            var expectedIL = new[]
            {
                OpCodes.Ldarg_1,
                OpCodes.Ldind_Ref,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call,
                OpCodes.Ret
            };

            var fs = lambda.CompileSys();
            fs.PrintIL();
            a = "0";
            fs(ref a);
            Asserts.AreEqual("0", a);

            var ff = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            ff.AssertOpCodes(expectedIL);
            a = "0";
            ff(ref a);
            Asserts.AreEqual("0", a);

            ActionRef<string> direct = SetIntoLocalVariableAndCallOtherRef;
            a = "0";
            direct(ref a);
            Asserts.AreEqual("0", a);
        }

        record struct RecVal(string S);

        private static void SetMinus1_OfStruct(ref RecVal localByRef) { localByRef = new RecVal("-1"); }


        public void RefMethodCallingRefMethodWithLocal_OfStruct()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref RecVal localByRef)
            {
                var objVal = localByRef;
                SetMinus1_OfStruct(ref objVal);
            }

            var objRef = Parameter(typeof(RecVal).MakeByRefType());
            var variable = Variable(typeof(RecVal));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetMinus1_OfStruct));
            var lambda = Lambda<ActionRef<RecVal>>(Block(new[] { variable }, Assign(variable, objRef), Call(call, variable)), objRef);

            lambda.PrintCSharp();
            var expectedIL = new[]
            {
                OpCodes.Ldarg_1,
                OpCodes.Ldobj,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call,
                OpCodes.Ret
            };

            var fs = lambda.CompileSys();
            fs.AssertOpCodes(expectedIL);

            var ff = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            ff.AssertOpCodes(expectedIL);

            var exampleB = new RecVal("0");
            ff(ref exampleB);
            Asserts.AreEqual("0", exampleB.S);

            ActionRef<RecVal> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = new RecVal("0");
            direct(ref exampleC);
            Asserts.AreEqual("0", exampleC.S);
        }

        private static void OutSetMinus1(out int localByRef) { localByRef = -1; }


        public void OutRefMethodCallingRefMethodWithLocal()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal = localByRef;
                OutSetMinus1(out objVal);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(OutSetMinus1));
            var lambda = Lambda<ActionRef<int>>(Block(new[] { variable }, Assign(variable, objRef), Call(call, variable)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Asserts.AreEqual(0, exampleB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            var exampleC = default(int);
            direct(ref exampleC);
            Asserts.AreEqual(0, exampleC);
        }

        private static void Set1AndMinus1(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1; }


        public void RefMethodCallingRefMethodWithLocalReturnLocalCalled()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var objVal1 = localByRef;
                var objVal2 = localByRef;
                Set1AndMinus1(ref localByRef, ref objVal2);
                return objVal2;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set1AndMinus1));
            var lambda = Lambda<FuncRef<int, int>>(Block(new[] { variable1, variable2 },
                                                    Assign(variable1, objRef),
                                                    Assign(variable2, objRef),
                                                    Call(call, objRef, variable2),
                                                    variable2
                                                    ),
                                                objRef);

            void LocalAssert(FuncRef<int, int> invoke)
            {
                var exampleA = default(int);
                Asserts.AreEqual(-1, invoke(ref exampleA));
                Asserts.AreEqual(1, exampleA);
            }

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            LocalAssert(compiledB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }

        private static void SetVariableOneAndMinusForParameter(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1; }


        public void VariableVariableRefVariableRefParameterReturn()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                var x = localByRef;
                var z = localByRef;
                SetVariableOneAndMinusForParameter(ref x, ref localByRef);
                return 1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var variable1 = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(SetVariableOneAndMinusForParameter));
            var lambda = Lambda<FuncRef<int, int>>(Block(new ParameterExpression[] { variable1, variable2 },
                                                    Assign(variable1, objRef),
                                                    Assign(variable2, objRef),
                                                    Call(call, variable1, objRef),
                                                    Constant(1)
                                                    ),
                                                objRef);

            void LocalAssert(FuncRef<int, int> invoke)
            {
                var exampleA = default(int);
                Asserts.AreEqual(1, invoke(ref exampleA));
                Asserts.AreEqual(-1, exampleA);
            }

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            LocalAssert(compiledB);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }

        private static void Set123(ref long ref1, ref byte ref2, ref short ref3) { ref1 = 1; ref2 = 2; ref3 = 3; }




        public void Ref1Ref2Ref3()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref long ref1, ref byte ref2, ref short ref3)
            {
                Set123(ref ref1, ref ref2, ref ref3);
            }

            var ref1E = Parameter(typeof(long).MakeByRefType());
            var ref2E = Parameter(typeof(byte).MakeByRefType());
            var ref3E = Parameter(typeof(short).MakeByRefType());

            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(Set123));
            var lambda = Lambda<ActionRefRefRef<long, byte, short>>(Call(call, ref1E, ref2E, ref3E), ref1E, ref2E, ref3E);


            void AssertLocal(ActionRefRefRef<long, byte, short> sut)
            {
                var example1 = default(long);
                var example2 = default(byte);
                var example3 = default(short);
                sut(ref example1, ref example2, ref example3);
                Asserts.AreEqual(1, example1);
                Asserts.AreEqual(2, example2);
                Asserts.AreEqual(3, example3);
            }


            var compiledB = lambda.CompileFast<ActionRefRefRef<long, byte, short>>(true);
            AssertLocal(compiledB);

            ActionRefRefRef<long, byte, short> direct = SetIntoLocalVariableAndCallOtherRef;
            AssertLocal(direct);
        }

        private static void SetMinusOneAndOneForDoubleRefParameterInCallCall(ref int ref1, ref int ref2) { ref2 = -1; ref1 = 1; }


        public void SetMinusOneAndOneForDoubleRefParameterInCall()
        {
            int SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                Set1AndMinus1(ref localByRef, ref localByRef);
                return -1;
            }

            var objRef = Parameter(typeof(int).MakeByRefType());
            var call = GetType().GetTypeInfo().GetDeclaredMethod(nameof(SetMinusOneAndOneForDoubleRefParameterInCallCall));
            var lambda = Lambda<FuncRef<int, int>>(
                Block(new ParameterExpression[0],
                    Call(call, objRef, objRef),
                    Constant(-1)
                ),
                objRef);

            var ff = lambda.CompileFast<FuncRef<int, int>>(true, CompilerFlags.EnableDelegateDebugInfo);
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_1,
                OpCodes.Call,
                OpCodes.Ldc_I4_M1,
                OpCodes.Ret);

            LocalAssert(ff);

            FuncRef<int, int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);

            void LocalAssert(FuncRef<int, int> invoke)
            {
                var exampleA = default(int);
                Asserts.AreEqual(-1, invoke(ref exampleA));
                Asserts.AreEqual(1, exampleA);
            }
        }

        private static void AsValueAndSetMinusOneAsRefCall(int ref1, ref int ref2) { ref2 = -1; }


        public void AsValueAndAsRef()
        {
            void SetIntoLocalVariableAndCallOtherRef(ref int localByRef)
            {
                AsValueAndSetMinusOneAsRefCall(localByRef, ref localByRef);
            }

            var objRef = Parameter(typeof(int).MakeByRefType());

            var call = typeof(Issue55_CompileFast_crash_with_ref_parameter).GetTypeInfo().DeclaredMethods.First(m => m.Name == nameof(AsValueAndSetMinusOneAsRefCall));
            var lambda = Lambda<ActionRef<int>>(Call(call, objRef, objRef), objRef);

            void LocalAssert(ActionRef<int> invoke)
            {
                var exampleA = default(int);
                invoke(ref exampleA);
                Asserts.AreEqual(-1, exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            LocalAssert(compiledB);

            ActionRef<int> direct = SetIntoLocalVariableAndCallOtherRef;
            LocalAssert(direct);
        }


        public void GenericRefFromConstant()
        {
            void SetSmallConstant(ref byte localByRef)
            {
                const byte objVal = 3;
                localByRef = objVal;
            }
            var objRef = Parameter(typeof(byte).MakeByRefType());
            var lambda = Lambda<ActionRef<byte>>(Assign(objRef, Constant((byte)3)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<byte>>(true);
            var exampleB = default(byte);
            compiledB(ref exampleB);
            Asserts.AreEqual(3, exampleB);

            ActionRef<byte> direct = SetSmallConstant;
            var exampleC = default(byte);
            direct(ref exampleC);
            Asserts.AreEqual(3, exampleC);
        }


        public void GenericRefFromConstantReturn()
        {
            ushort SetSmallConstant(ref uint localByRef)
            {
                localByRef = 3;
                return 7;
            }
            var objRef = Parameter(typeof(uint).MakeByRefType());
            var lambda = Lambda<FuncRef<uint, ushort>>(Block(Assign(objRef, Constant((uint)3)), Constant((ushort)7)), objRef);

            var compiledB = lambda.CompileFast<FuncRef<uint, ushort>>(true);
            var exampleB = default(uint);
            Asserts.AreEqual(7, compiledB(ref exampleB));
            Asserts.AreEqual(3U, exampleB);

            FuncRef<uint, ushort> direct = SetSmallConstant;
            var exampleC = default(uint);
            Asserts.AreEqual(7, direct(ref exampleC));
            Asserts.AreEqual(3U, exampleC);
        }


        public void BlockWithNonRefStatementLast()
        {
            void SetSmallConstant(ref uint localByRef)
            {
                localByRef = 3;
                var x = 0.0;
            }
            var objRef = Parameter(typeof(uint).MakeByRefType());
            var variable = Variable(typeof(double));
            var lambda = Lambda<ActionRef<uint>>(Block(new[] { variable }, Assign(objRef, Constant((uint)3)), Assign(variable, Constant(0.0))), objRef);

            lambda.PrintCSharp();

            var compiledB = lambda.CompileFast(true);
            var exampleB = default(uint);
            compiledB(ref exampleB);
            Asserts.AreEqual(3U, exampleB);

            ActionRef<uint> direct = SetSmallConstant;
            var exampleC = default(uint);
            direct(ref exampleC);
            Asserts.AreEqual(3U, exampleC);
        }


        public void RefReturnToVoid()
        {
            ushort SetSmallConstant(ref uint localByRef)
            {
                localByRef = 3;
                return 7;
            }
            var objRef = Parameter(typeof(uint).MakeByRefType());
            var lambda = Lambda<ActionRef<uint>>(Block(Assign(objRef, Constant((uint)3)), Constant((ushort)7)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<uint>>(true);
            var exampleB = default(uint);
            compiledB(ref exampleB);
            Asserts.AreEqual(3U, exampleB);

            ActionRef<uint> direct = (ref uint x) => SetSmallConstant(ref x);
            var exampleC = default(uint);
            direct(ref exampleC);
            Asserts.AreEqual(3U, exampleC);
        }


        public void RefRefVoid()
        {
            void SetSmallConstant(ref int a1, ref float a2)
            {
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objRef2 = Parameter(typeof(float).MakeByRefType());
            var lambda = Lambda<ActionRefRef<int, float>>(Constant(default(object)), objRef, objRef2);

            var compiledB = lambda.CompileFast<ActionRefRef<int, float>>(true);
            var exampleB = default(int);
            var exampleB2 = default(float);
            compiledB(ref exampleB, ref exampleB2);
            Asserts.AreEqual(0, exampleB);

            ActionRefRef<int, float> direct = SetSmallConstant;
            var exampleC = default(int);
            var exampleC2 = default(float);
            direct(ref exampleC, ref exampleC2);
            Asserts.AreEqual(0, exampleC);
        }

        public void RefRefReturnToVoid()
        {
            string SetSmallConstant(ref int a1, ref float a2)
            {
                return default(string);
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objRef2 = Parameter(typeof(float).MakeByRefType());
            var lambda = Lambda<ActionRefRef<int, float>>(Constant(default(object)), objRef, objRef2);

            var compiledB = lambda.CompileFast<ActionRefRef<int, float>>(true);
            var exampleB = default(int);
            var exampleB2 = default(float);
            compiledB(ref exampleB, ref exampleB2);
            Asserts.AreEqual(0, exampleB);

            ActionRefRef<int, float> direct = (ref int x, ref float y) => SetSmallConstant(ref x, ref y);
            var exampleC = default(int);
            var exampleC2 = default(float);
            direct(ref exampleC, ref exampleC2);
            Asserts.AreEqual(0, exampleC);
        }


        public void IntPtrZeroReturn()
        {
            System.Linq.Expressions.Expression<Func<IntPtr>> sLambda = () => IntPtr.Zero;
            var lambda = sLambda.FromSysExpression();
            var compiled = lambda.CompileFast<Func<IntPtr>>(true);
            Asserts.AreEqual(IntPtr.Zero, compiled());
        }


        public void NewIntPtr13Return()
        {
            System.Linq.Expressions.Expression<Func<IntPtr>> sLambda = () => new IntPtr(13);
            var lambda = sLambda.FromSysExpression();
            var compiled = lambda.CompileFast<Func<IntPtr>>(true);
            Asserts.AreEqual(new IntPtr(13), compiled());
        }



        public void RefSetSetForFields()
        {
            UIntPtr Set2RefsWithPtrAndNewObject(ref IntPtr a1, ref object a2)
            {
                a1 = IntPtr.Zero;
                a2 = new object();
                return UIntPtr.Zero;
            }
            var objRef1 = Parameter(typeof(IntPtr).MakeByRefType());
            var objRef2 = Parameter(typeof(object).MakeByRefType());
            var intPtrZero = typeof(IntPtr).GetTypeInfo().DeclaredFields.First(m => m.Name == nameof(IntPtr.Zero));
            var uIntPtrZero = typeof(UIntPtr).GetTypeInfo().DeclaredFields.First(m => m.Name == nameof(UIntPtr.Zero));

            var lambda = Lambda<FuncRefRef<IntPtr, object, UIntPtr>>(Block(
                                                                Assign(objRef1, Field(null, intPtrZero)),
                                                                Assign(objRef2, New(typeof(object))),
                                                                Field(null, uIntPtrZero)
                                                            ), objRef1, objRef2
                                                       );

            var compiledB = lambda.CompileFast<FuncRefRef<IntPtr, object, UIntPtr>>(true);
            var exampleB = default(IntPtr);
            var exampleB2 = default(object);
            compiledB(ref exampleB, ref exampleB2);
            Asserts.IsNotNull(exampleB2);

            var exampleC = default(IntPtr);
            var exampleC2 = default(object);
            Set2RefsWithPtrAndNewObject(ref exampleC, ref exampleC2);
            Asserts.IsNotNull(exampleC2);
        }


        public void RefDoNothingReturnConstant()
        {
            int DoNothing(ref int localByRef) => default(int);
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<FuncRef<int, int>>(Constant(default(int)), objRef);

            var compiledB = lambda.CompileFast<FuncRef<int, int>>(true);
            var exampleB = default(int);
            Asserts.AreEqual(0, compiledB(ref exampleB));
            Asserts.AreEqual(0, exampleB);

            FuncRef<int, int> direct = DoNothing;
            var exampleC = default(int);
            Asserts.AreEqual(0, direct(ref exampleC));
            Asserts.AreEqual(0, exampleC);
        }


        public void RefFromLargeConstant()
        {
            void SetSmallConstant(ref int localByRef)
            {
                const int objVal = 42_123_666;
                localByRef = objVal;
            }
            var objRef = Parameter(typeof(int).MakeByRefType());
            var lambda = Lambda<ActionRef<int>>(Assign(objRef, Constant(42_123_666)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB);
            Asserts.AreEqual(42_123_666, exampleB);

            ActionRef<int> direct = SetSmallConstant;
            var exampleC = default(int);
            direct(ref exampleC);
            Asserts.AreEqual(42_123_666, exampleC);
        }


        public void RefSetFromParameter()
        {
            var objRef = Parameter(typeof(int).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var body = Assign(objRef, objVal);
            var lambda = Lambda<ActionRefIn<int, int>>(body, objRef, objVal);

            var compiledB = lambda.CompileFast<ActionRefIn<int, int>>(true);
            var exampleB = default(int);
            compiledB(ref exampleB, 7);
            Asserts.AreEqual(7, exampleB);
        }


        public void NonGenericSetterFieldShould_not_crash()
        {
            var objRef = Parameter(typeof(StructWithIntField).MakeByRefType());
            var objVal = Parameter(typeof(int));
            var lambda = Lambda<ActionRefIn<StructWithIntField, int>>(
                Assign(PropertyOrField(objRef, nameof(StructWithIntField.IntField)), objVal),
                objRef, objVal);

            var s = lambda.CompileSys();
            s.PrintIL();

            var f = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Stfld,
                OpCodes.Ret
            );

            var exampleB = default(StructWithIntField);
            f(ref exampleB, 7);
            Asserts.AreEqual(7, exampleB.IntField);
        }



        public void GenericRefStructFieldShould_not_crash()
        {
            var objRef = Parameter(typeof(StructWithIntField).MakeByRefType());
            var objVal = Parameter(typeof(int));

            var body = Assign(
                PropertyOrField(objRef, nameof(StructWithIntField.IntField)),
                objVal);

            var e = Lambda<ActionRefIn<StructWithIntField, int>>(body, objRef, objVal);

            var fs = e.CompileFast<ActionRefIn<StructWithIntField, int>>(true, CompilerFlags.EnableDelegateDebugInfo);

            fs.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Stfld,
                OpCodes.Ret);

            var x1 = default(StructWithIntField);
            fs(ref x1, 7);
            Asserts.AreEqual(7, x1.IntField);
        }

        public void RefAssign()
        {
            void AddSet(ref double byRef)
            {
                byRef = byRef + 3.0;
            }
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(
                Assign(objRef, Add(objRef, Constant(3.0))), objRef);

            void LocalAssert(ActionRef<double> invoke)
            {
                var exampleA = 5.0;
                invoke(ref exampleA);
                Asserts.AreEqual(8.0, exampleA);
            }

            var fs = lambda.CompileSys();
            fs.PrintIL();
            LocalAssert(fs);

            var ff = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_1,
                OpCodes.Ldind_R8,
                OpCodes.Ldc_R8,
                OpCodes.Add,
                OpCodes.Stind_R8,
                OpCodes.Ret);

            LocalAssert(ff);

            ActionRef<double> direct = AddSet;
            LocalAssert(direct);
        }

        public void RefAssignCustomStruct()
        {
            void AddSet(ref BigInteger byRef)
            {
                byRef = byRef + new BigInteger(3);
            }
            var objRef = Parameter(typeof(BigInteger).MakeByRefType());
            var ctor = typeof(BigInteger).GetTypeInfo().DeclaredConstructors
                .Single(x => x.GetParameters().Count() == 1 && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(int));
            var lambda = Lambda<ActionRef<BigInteger>>(Assign(objRef, Add(objRef, New(ctor, Constant(3)))), objRef);

            void LocalAssert(ActionRef<BigInteger> invoke)
            {
                BigInteger exampleA = 5;
                invoke(ref exampleA);
                Asserts.AreEqual(new BigInteger(8), exampleA);
            }

            var compiledB = lambda.CompileFast<ActionRef<BigInteger>>(true);
            LocalAssert(compiledB);

            ActionRef<BigInteger> direct = AddSet;
            LocalAssert(direct);
        }


        public void CoalesceIntoByRef()
        {
            void DynamicDeserializer(ref object value) =>
                value = value ?? new object();

            void DynamicDeserializerGeneric<T>(ref T value) where T : class, new() =>
                value = value ?? new T();

            var specificType = typeof(object);
            var valueRef = Parameter(typeof(object).MakeByRefType(), "value");
            var newObj = Coalesce(valueRef, New(specificType.GetConstructor(Type.EmptyTypes)));
            var xVar = Variable(specificType, "x");
            var lambda = Lambda<ActionRef<object>>(
                    Block(new[] { xVar }, Assign(valueRef, newObj)),
                    valueRef
                );

            var compiledFast = lambda.CompileFast(true);

            void LocalAssert(ActionRef<object> invoke)
            {
                var obj = default(object);
                invoke(ref obj);
                Asserts.AreNotEqual(default(object), obj);
            }

            LocalAssert(DynamicDeserializer);
            LocalAssert(DynamicDeserializerGeneric);
            LocalAssert(compiledFast);
        }


        public void ConstantOutInCondition()
        {
            int TryParseCondition()
            {
                int intValue;
                return int.TryParse("123", out intValue) ? intValue : default(int);
            }

            var intValueParameter = Parameter(typeof(int), "intValue");
            var tryParseMethod = typeof(int)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "TryParse" && m.GetParameters().Length == 2);
            var tryParseCall = Call(
                tryParseMethod,
                Constant("123", typeof(string)),
                intValueParameter);
            var parsedValueOrDefault = Condition(
                tryParseCall,
                intValueParameter,
                Default(typeof(int)));
            var conditionBlock = Block(new[] { intValueParameter }, parsedValueOrDefault);
            var conditionLambda = Lambda<Func<int>>(conditionBlock);

            var conditionFunc = conditionLambda.CompileSys();
            var conditionFuncFast = conditionLambda.CompileFast();

            void LocalAssert(Func<int> invoke)
            {
                var x = invoke();
                Asserts.AreEqual(123, x);
            }
            LocalAssert(conditionFunc);
            LocalAssert(conditionFuncFast);
            LocalAssert(TryParseCondition);
        }


        public void ConstantOut()
        {
            int TryParseReturn()
            {
                int intValue;
                int.TryParse("123", out intValue);
                return intValue;
            }

            var intValueParameter = Parameter(typeof(int), "intValue");
            var tryParseMethod = typeof(int)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "TryParse" && m.GetParameters().Length == 2);
            var tryParseCall = Call(
                tryParseMethod,
                Constant("123", typeof(string)),
                intValueParameter);
            var conditionBlock = Block(new[] { intValueParameter }, tryParseCall, intValueParameter);
            var conditionLambda = Lambda<Func<int>>(conditionBlock);

            void LocalAssert(Func<int> invoke) => Asserts.AreEqual(123, invoke());
            var func = conditionLambda.CompileSys();
            var funcFast = conditionLambda.CompileFast();

            LocalAssert(func);
            LocalAssert(funcFast);
            LocalAssert(TryParseReturn);
        }
    }
}
