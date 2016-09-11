using System;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace ExpTree
{
    public class Trials
    {
        private readonly Int32 _iterations = 1000;
        private readonly PropertyInfo _propertyInfo;
        private readonly Func<LocalObject, Int32> _getter;
        private readonly Action<LocalObject, Int32> _setter;
        private readonly Func<Object, Int32> _dynamicGetter;
        private readonly Action<Object, Int32> _dynamicSetter;
        private readonly Type _SupplementalAssemblyType;


        public Trials()
        {
            _propertyInfo = typeof(LocalObject).GetProperty(nameof(LocalObject.Prop), BindingFlags.Public | BindingFlags.Instance);

            var param = Expression.Parameter(typeof(LocalObject), "param");
            var getPropertyValueExp = Expression.Lambda(Expression.Property(param, "Prop"), param);
            var getPropertyValueLambda = (Expression<Func<LocalObject, Int32>>)getPropertyValueExp;

            var paramo = Expression.Parameter(typeof(LocalObject), "param");
            var parami = Expression.Parameter(typeof(Int32), "newvalue");

            var methodCallSetterOfProperty = Expression.Call(paramo, _propertyInfo.GetSetMethod(), parami);
            var setPropertyValueExp = Expression.Lambda(methodCallSetterOfProperty, paramo, parami);
            var setPropertyValueLambda = (Expression<Action<LocalObject, Int32>>)setPropertyValueExp;


            _getter = getPropertyValueLambda.Compile();
            _setter = setPropertyValueLambda.Compile();


            //Since we does not known the real type, we cannot use effectively the expression tree technique.
            _SupplementalAssemblyType = typeof(SupplementalAssembly.ExternalObject);
            param = Expression.Parameter(typeof(Object), "param");
            getPropertyValueExp = Expression.Lambda(Expression.Property(Expression.Convert(param, _SupplementalAssemblyType), "Prop"), param);
            var dynamicGetterExpression = (Expression<Func<Object, Int32>>)getPropertyValueExp;
            _dynamicGetter = dynamicGetterExpression.Compile();

            paramo = Expression.Parameter(typeof(Object), "param");
            parami = Expression.Parameter(typeof(Int32), "newvalue");
            var setterMethodInfo = _SupplementalAssemblyType.GetProperty("Prop", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
            methodCallSetterOfProperty = Expression.Call(Expression.Convert(paramo, _SupplementalAssemblyType), setterMethodInfo, parami);
            setPropertyValueExp = Expression.Lambda(methodCallSetterOfProperty, paramo, parami);
            _dynamicSetter = ((Expression<Action<object, int>>)setPropertyValueExp).Compile();
        }

        [Benchmark]
        public void SetProperty()
        {
            var localObject = new LocalObject { Prop = 0 };
            for (var I = 0; I < _iterations; ++I)
            {
                localObject.Prop = localObject.Prop + 1;
            }
        }

        [Benchmark]
        public void SetPropertyValueWithReflection()
        {
            var localObject = new LocalObject { Prop = 0 };
            Object[] nullobj = {};
            for (var I = 0; I < _iterations; ++I)
            {
                _propertyInfo.SetValue(localObject,((Int32) _propertyInfo.GetValue(localObject, nullobj)) + 1, nullobj);
            }
        }

        [Benchmark]
        public void SetPropertyWithExpression()
        {
            var localObject = new LocalObject { Prop = 0 };
            for (var I = 0; I < _iterations; ++I)
            {
                _setter(localObject, _getter(localObject) + 1);
            }
        }

        [Benchmark]
        public void SetPropertyWithExpressionForUnknownObject()
        {
            var externalObject = Activator.CreateInstance(_SupplementalAssemblyType);
            for (var I = 0; I < _iterations; ++I)
            {
                Int32 actualValue = _dynamicGetter(externalObject);
                _dynamicSetter(externalObject, actualValue + 1);
            }
        }
    }
}
