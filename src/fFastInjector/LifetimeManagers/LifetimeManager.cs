using System;

namespace fFastInjector.LifetimeManagers
{
    /// <summary>
    /// Base class for a LifetimeManager&lt;T&gt;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LifetimeManager<T>
        where T : class
    {
        /// <summary>
        /// Store the resolver to create a new instance of type T for this LifetimeManager
        /// </summary>
        protected Func<ResolutionContext, T> Resolver { get; set; }

        /// <summary>
        /// Return the appropriate value for type T for this LifetimeManager
        /// </summary>
        protected Func<ResolutionContext, T> ValueGetter { get; set; }

        /// <summary>
        /// Get a value of type T for this LifetimeManager
        /// </summary>
        /// <returns></returns>
        internal virtual T GetValue(ResolutionContext resolutionContext) => ValueGetter(resolutionContext);

        /// <summary>
        /// Set the resolver that will create a new instance of type T for this LifetimeManager
        /// </summary>
        /// <param name="func"></param>
        internal virtual void SetResolver(Func<ResolutionContext, T> func) => Resolver = func;
    }
}