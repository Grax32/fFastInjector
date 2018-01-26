using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace fFastInjector
{
    public class PerConnectionLifetimeManager<T> : Injector.ILifetimeManager<T>
        where T : class
    {
        Func<T> _resolver = () => { throw new Exception("No resolver has been set for type " + typeof(T).Name); };
        string _typeName = FancyTypeName(typeof(T));

        static T GetContextItemForConnection(HttpContext httpContext, string contextItemName, Func<T> newContextItemFunction)
        {
            if (httpContext != null && httpContext.Items[contextItemName] == null)
            {
                httpContext.Items[contextItemName] = newContextItemFunction();
            }

            return httpContext.Items[contextItemName] as T;
        }

        static string FancyTypeName(Type type)
        {
            var typeName = type.Name.Split('`')[0];
            if (type.IsGenericType)
            {
                typeName += string.Format(CultureInfo.CurrentCulture, "<{0}>", string.Join(",", type.GetGenericArguments().Select(v => FancyTypeName(v)).ToArray()));
            }
            return typeName;
        }

        public T GetValue()
        {
            return GetContextItemForConnection(HttpContext.Current, "fFastInjector.Instance.For." + _typeName, _resolver);
        }

        public void SetResolver(Func<T> func)
        {
            _resolver = func;
        }
    }
}