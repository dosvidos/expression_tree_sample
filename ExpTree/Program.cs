using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Linq.Expressions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;

namespace ExpTree
{
	class Program
	{
		static void Main(string[] args)
		{
			Int32 iterations = 100000;
			Stopwatch watch = new Stopwatch();
		    MyObject obj = new MyObject {Prop = 0};
		    watch.Start();
			for (Int32 I = 0; I < iterations; ++I)
			{
				obj.Prop = obj.Prop + 1;
			}
			watch.Stop();
			Console.WriteLine("Direct invocation {0} iterations {1} ms",
				 iterations, watch.ElapsedMilliseconds);

			PropertyInfo pinfo = typeof(MyObject).GetProperty("Prop", BindingFlags.Public | BindingFlags.Instance);
			obj.Prop = 0;
			Object[] nullobj = new Object[] { };
			watch.Reset();
			watch.Start();
			for (Int32 I = 0; I < iterations; ++I)
			{
				pinfo.SetValue(
					 obj,
					 ((Int32)pinfo.GetValue(obj, nullobj)) + 1,
					 nullobj);
			}
			watch.Stop();
			Console.WriteLine("Reflection invocation {0} iterations {1} ms",
				iterations, watch.ElapsedMilliseconds);

			//Expression exp = new property
			//System.Linq.Expressions.
			//Expression<Func<MyObject, Int32>> exprLambda = o => o.Prop;
			//Expression<Func<MyObject, Int32>> exprLambda2 = o => o.Test() ;

			MethodInfo SetterMethodInfo = pinfo.GetSetMethod();
			ParameterExpression param = Expression.Parameter(typeof(MyObject), "param");
			Expression GetPropertyValueExp = Expression.Lambda(
				 Expression.Property(param, "Prop"), param);
			Expression<Func<MyObject, Int32>> GetPropertyValueLambda =
				 (Expression<Func<MyObject, Int32>>)GetPropertyValueExp;

			ParameterExpression paramo = Expression.Parameter(typeof(MyObject), "param");
			ParameterExpression parami = Expression.Parameter(typeof(Int32), "newvalue");

			MethodCallExpression MethodCallSetterOfProperty = Expression.Call(paramo, SetterMethodInfo, parami);
			Expression SetPropertyValueExp = Expression.Lambda(MethodCallSetterOfProperty, paramo, parami);
			Expression<Action<MyObject, Int32>> SetPropertyValueLambda = (Expression<Action<MyObject, Int32>>)SetPropertyValueExp;
			obj.Prop = 0;
			Func<MyObject, Int32> Getter = GetPropertyValueLambda.Compile();
			Action<MyObject, Int32> Setter = SetPropertyValueLambda.Compile();
			watch.Reset();
			watch.Start();
			for (Int32 I = 0; I < iterations; ++I)
			{
				Setter(obj, Getter(obj) + 1);
			}
			watch.Stop();
			Console.WriteLine("Expression Tree getter {0} iterations {1} ms",
				iterations, watch.ElapsedMilliseconds);


			//Since we does not known the real type, we cannot use effectively the expression tree technique.
			Type otherType = Type.GetType("Other.OtherMyObject, Other");
			PropertyInfo ISomePinfo = otherType.GetProperty("Prop", BindingFlags.Public | BindingFlags.Instance);

			param = Expression.Parameter(typeof(Object), "param");
			Expression convertedParam = Expression.Convert(param, otherType);
			GetPropertyValueExp = Expression.Lambda(Expression.Property(convertedParam, "Prop"), param);
			Expression<Func<Object, Int32>> dynamicGetterExpression = (Expression<Func<Object, Int32>>)GetPropertyValueExp;
			Func<Object, Int32> dynamicGetter = dynamicGetterExpression.Compile();

			paramo = Expression.Parameter(typeof(Object), "param");
			Expression convertedParamo = Expression.Convert(paramo, otherType);
			parami = Expression.Parameter(typeof(Int32), "newvalue");
			SetterMethodInfo = ISomePinfo.GetSetMethod();
			MethodCallSetterOfProperty = Expression.Call(convertedParamo, SetterMethodInfo, parami);
			SetPropertyValueExp = Expression.Lambda(MethodCallSetterOfProperty, paramo, parami);
			Expression<Action<Object, Int32>> dynamicSetterExpression = (Expression<Action<Object, Int32>>)SetPropertyValueExp;
			Action<Object, Int32> dynamicSetter = dynamicSetterExpression.Compile();

			watch.Reset();
			watch.Start();
			Object otherObj = Activator.CreateInstance(otherType);
			for (Int32 I = 0; I < iterations; ++I)
			{
				Int32 actualValue = dynamicGetter(otherObj);
				dynamicSetter(otherObj, actualValue + 1);
			}
			watch.Stop();
			Console.WriteLine("Expression tree with unknown object getter {0} iterations {1} ms",
				iterations, watch.ElapsedMilliseconds);
			Console.ReadKey();
		}
	}
}
