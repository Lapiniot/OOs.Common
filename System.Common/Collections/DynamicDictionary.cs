using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace System.Common.Collections
{
    public class DynamicMap<T> : IDynamicMetaObjectProvider
    {
        private IDictionary<string, T> properties;

        public DynamicMap(IDictionary<string, T> properties)
        {
            this.properties = properties;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            throw new NotImplementedException();
        }

        public static explicit operator DynamicMap<T>(Dictionary<string, T> parameters)
        {
            return new DynamicMap<T>(parameters);
        }
    }
}