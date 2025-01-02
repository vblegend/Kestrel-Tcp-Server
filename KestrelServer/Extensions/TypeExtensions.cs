using System.Linq.Expressions;
using System;

namespace System
{
    public static class TypeExtensions
    {

        public static Func<TObject> CreateDefaultConstructor<TObject>(this Type type)
        {
            return Expression.Lambda<Func<TObject>>(Expression.New(type.GetConstructor([]))).Compile();
        }
    }
}
