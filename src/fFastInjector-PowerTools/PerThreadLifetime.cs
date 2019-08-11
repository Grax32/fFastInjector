using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fFastInjector
{
    public class PerThreadLifetime<T> : Injector.ILifetimeManager<T>
        where T : class
    {
        [ThreadStatic]
        static T _threadInstance;

        Func<T> _resolver = () => { throw new Exception("No resolver has been set for type " + typeof(T).Name); };

        public T GetValue()
        {
            // No locking is needed since we are definitely the only thread accessing this thread static variable
            return _threadInstance ?? (_threadInstance = _resolver());
        }

        public void SetResolver(Func<T> func)
        {
            _resolver = func;
        }
    }
}
