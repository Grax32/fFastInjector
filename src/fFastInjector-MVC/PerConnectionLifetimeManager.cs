using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace fFastInjector
{
    public class PerConnectionLifetimeManager<T> : LifetimeManager<T>
        where T : class
    {
        private Func<T> _resolver = () => { throw new Exception("No resolver has been set for type " + typeof(T).Name); };
        private readonly string _typeName = Injector.FancyTypeName(typeof(T));

        private static T GetContextItemForConnection(HttpContext httpContext, string contextItemName, Func<T> newContextItemFunction)
        {
            if (httpContext.Items[contextItemName] == null)
            {
                httpContext.Items[contextItemName] = newContextItemFunction();
            }

            return httpContext.Items[contextItemName] as T;
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