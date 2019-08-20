using fFastInjector.LifetimeManagers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace fFastInjector
{
    internal class RegistrationData<T> where T : class
    {
        public LifetimeManager<T> LifetimeManager { get; set; }
            = new TransientLifetimeManager<T>();

        public Expression<Func<ResolutionContext, T>> Creator { get; set; }
            = DefaultResolverBuilder<T>.GetDefaultResolverExpression();

        public List<Expression<Action<ResolutionContext, T>>> Configurators { get; set; }
            = new List<Expression<Action<ResolutionContext, T>>>();
    }
}
