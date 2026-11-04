using System;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct WritableReceiverAddressRegressionTests : ITestX
{
 public struct Mutable {public int Value; public int Next(){return Value++;} public int Add(int value){return Value+=value;}}
 public static void Bump(ref Mutable value){value.Value++;}
 public void Run(TestRun t){Unbox_receiver_mutates_box_Convert_keeps_copy(t);Unbox_arguments_and_nullable_values(t);Multidimensional_Get_receiver_mutates_element(t);Address_receivers_survive_exception_arguments(t);}
 public void Unbox_receiver_mutates_box_Convert_keeps_copy(TestContext t)
 {
  var x=S.Parameter(typeof(object));var next=typeof(Mutable).GetMethod(nameof(Mutable.Next));
  var unbox=S.Lambda<Func<object,int>>(S.Call(S.Unbox(x,typeof(Mutable)),next),x).FromSysExpression().CompileFast(true);
  var convert=S.Lambda<Func<object,int>>(S.Call(S.Convert(x,typeof(Mutable)),next),x).FromSysExpression().CompileFast(true);
  if(!t.IsNotNull(unbox)||!t.IsNotNull(convert))return;object box=new Mutable();
  t.AreEqual(0,unbox(box));t.AreEqual(1,unbox(box));t.AreEqual(2,((Mutable)box).Value);
  t.AreEqual(2,convert(box));t.AreEqual(2,convert(box));t.AreEqual(2,((Mutable)box).Value);
  var field=S.Lambda<Action<object>>(S.Assign(S.Field(S.Unbox(x,typeof(Mutable)),nameof(Mutable.Value)),S.Constant(8)),x).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(field)){field(box);t.AreEqual(8,((Mutable)box).Value);}
 }
 public void Unbox_arguments_and_nullable_values(TestContext t)
 {
  var x=S.Parameter(typeof(object));
  var dynamicMethod=new System.Reflection.Emit.DynamicMethod("AddressRegressionControl",typeof(int),Type.EmptyTypes);
  var il=dynamicMethod.GetILGenerator();il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4,37);il.Emit(System.Reflection.Emit.OpCodes.Ret);
  var dynamicCall=S.Lambda<Func<int>>(S.Call(dynamicMethod)).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(dynamicCall))t.AreEqual(37,dynamicCall());
  var bump=S.Lambda<Action<object>>(S.Call(typeof(WritableReceiverAddressRegressionTests).GetMethod(nameof(Bump)),S.Unbox(x,typeof(Mutable))),x).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(bump)){object box=new Mutable();bump(box);bump(box);t.AreEqual(2,((Mutable)box).Value);}
  var nullable=S.Lambda<Func<object,bool>>(S.Property(S.Unbox(x,typeof(int?)),nameof(Nullable<int>.HasValue)),x).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(nullable)){t.AreEqual(false,nullable(null));t.AreEqual(true,nullable(3));}
  var value=S.Lambda<Func<object,Mutable>>(S.Unbox(x,typeof(Mutable)),x).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(value)){object box=new Mutable{Value=5};var copy=value(box);copy.Value++;t.AreEqual(5,((Mutable)box).Value);}
 }
 public void Multidimensional_Get_receiver_mutates_element(TestContext t)
 {
  var array=S.Parameter(typeof(Mutable[,]));var get=S.ArrayIndex(array,S.Constant(0),S.Constant(1));
  var next=S.Lambda<Func<Mutable[,],int>>(S.Call(get,typeof(Mutable).GetMethod(nameof(Mutable.Next))),array).FromSysExpression().CompileFast(true);
  if(!t.IsNotNull(next))return;var values=new Mutable[1,2];t.AreEqual(0,next(values));t.AreEqual(1,next(values));t.AreEqual(2,values[0,1].Value);
  var bump=S.Lambda<Action<Mutable[,]>>(S.Call(typeof(WritableReceiverAddressRegressionTests).GetMethod(nameof(Bump)),get),array).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(bump)){bump(values);t.AreEqual(3,values[0,1].Value);}
  var discarded=S.Lambda<Action<Mutable[,]>>(S.Block(S.Call(get,typeof(Mutable).GetMethod(nameof(Mutable.Next))),S.Empty()),array).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(discarded)){discarded(values);t.AreEqual(4,values[0,1].Value);t.Throws<NullReferenceException>(()=>discarded(null));t.Throws<IndexOutOfRangeException>(()=>discarded(new Mutable[0,0]));}
  var copy=S.Lambda<Func<Mutable[,],Mutable>>(get,array).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(copy)){var value=copy(values);value.Value++;t.AreEqual(4,values[0,1].Value);}
 }
 public void Address_receivers_survive_exception_arguments(TestContext t)
 {
  var add=typeof(Mutable).GetMethod(nameof(Mutable.Add));var x=S.Parameter(typeof(object));var rhs=S.TryFinally(S.Constant(3),S.Empty());
  var boxed=S.Lambda<Func<object,int>>(S.Call(S.Unbox(x,typeof(Mutable)),add,rhs),x).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(boxed)){object box=new Mutable();t.AreEqual(3,boxed(box));t.AreEqual(6,boxed(box));t.AreEqual(6,((Mutable)box).Value);}
  var array=S.Parameter(typeof(Mutable[,,]));
  var get=S.ArrayIndex(array,S.Constant(0),S.TryFinally(S.Constant(1),S.Empty()),S.Constant(0));
  var indexed=S.Lambda<Func<Mutable[,,],int>>(S.Call(get,add,rhs),array).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(indexed)){var values=new Mutable[1,2,1];t.AreEqual(3,indexed(values));t.AreEqual(6,indexed(values));t.AreEqual(6,values[0,1,0].Value);}
 }
}
