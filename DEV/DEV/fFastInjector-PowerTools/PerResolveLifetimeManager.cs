using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fFastInjector
{
    internal static class PerResolveLifetimeManagerVariables
    {
        [ThreadStatic]
        internal static bool HasResolutionStarted = false;
    }

    public class PerResolveLifetimeManager<T> : Injector.ILifetimeManager<T>
        where T : class
    {
        [ThreadStatic]
        static T _threadInstance;

        Func<T> _resolver = () => { throw new Exception("No resolver has been set for type " + typeof(T).Name); };

        public T GetValue()
        {
            // No locking is needed since we are definitely the only thread accessing this thread static variable
            T returnValue = null;
            
            if (!PerResolveLifetimeManagerVariables.HasResolutionStarted)
            {
                try
                {
                    PerResolveLifetimeManagerVariables.HasResolutionStarted = true;
                    returnValue = (_threadInstance = _resolver());
                }
                finally
                {
                    PerResolveLifetimeManagerVariables.HasResolutionStarted = false;
                    _threadInstance = null;
                }
            }

            return returnValue;

        }

        public void SetResolver(Func<T> func)
        {
            _resolver = func;
        }
    }
}