using fFastInjector.LifetimeManagers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using static fFastInjector.Functions;

namespace fFastInjector
{
    internal static class InternalResolver<T> where T : class
    {
        private static bool _isCovariantRegistration = false;
        private static readonly Type typeofT = typeof(T);
        private static RegistrationData<T> _registrationData = new RegistrationData<T>();

        static InternalResolver()
        {
            Trace("Initializing auto-resolver for " + FancyTypeName(typeofT));

            try
            {
                var resolver = ResolverFunctions<T>.GetCovariantResolverExpression();

                bool isCovariant = resolver != null;

                if (resolver == null)
                {
                    resolver = DefaultResolverBuilder<T>.GetDefaultResolverExpression();
                }

                InnerSetResolver(resolver, isCovariant, lifetimeManager: null, initComplete: false);
            }
            catch (Exception ex)
            {
                ActiveResolverFunction = resolutionContext =>
                {
                    throw CreateExceptionInternal("Error initializing default resolver for " + FancyTypeName(typeofT) + ".  See 'InnerException' for details", ex);
                };
            }
        }

        internal static void InnerSetResolver(Expression<Func<ResolutionContext, T>> resolver, bool isCovariantRegistration, LifetimeManager<T> lifetimeManager, bool initComplete = true)
        {
            _isCovariantRegistration = isCovariantRegistration;
            LifetimeManager = lifetimeManager;
            BaseResolverExpression = resolver;
            CompileResolver(initComplete);
        }

        static void CompileResolver(bool initComplete = true)
        {
            var completedResolverExpression = BaseResolverExpression;
            completedResolverExpression = ResolverFunctions<T>.AddInitializers(completedResolverExpression, InitializerExpressions);
            completedResolverExpression = ResolverFunctions<T>.AddLifetimeManager(completedResolverExpression, LifetimeManager);

            ActiveResolverFunction = completedResolverExpression.Compile();
            Injector.Resolvers[typeofT] = ActiveResolverFunction;
            Injector.ResolverInfo[typeofT] = new ResolverInfo
            {
                IsDefaultRegistration = !initComplete,
                IsCovariantRegistration = _isCovariantRegistration
            };
        }

        //private static Func<ResolutionContext, T> ActiveResolverFunction;
        //private static Expression<Func<ResolutionContext, T>> BaseResolverExpression;
        //private static LifetimeManager<T> LifetimeManager;
        //private static readonly List<Expression<Action<T>>> InitializerExpressions = new List<Expression<Action<T>>>();

        internal static T Resolve(ResolutionContext context) => ActiveResolverFunction(context);

        internal static void UpdateCovariantResolver()
        {
            var resolver = ResolverFunctions<T>.GetCovariantResolverExpression();
            bool isCovariant = resolver != null;

            if (resolver == null)
            {
                throw CreateExceptionInternal("UpdateCovariantResolver may not be called on this type because there is no covariant resolver that applies.");
            }

            InnerSetResolver(resolver, isCovariant, null);
        }

        internal static void SetResolver(Expression<Func<ResolutionContext, T>> resolver, LifetimeManager<T> lifetimeManager = null)
        {
            InnerSetResolver(resolver, false, lifetimeManager);
        }

        internal static void SetResolver(Expression<Func<ResolutionContext, T>> resolver, IEnumerable<Expression<Action<T>>> initializers, LifetimeManager<T> lifetimeManager = null)
        {
            InitializerExpressions.Clear();
            InitializerExpressions.AddRange(initializers);
            InnerSetResolver(resolver, false, lifetimeManager);
        }

        internal static void AddInitializer<TProp>(Expression<Func<T, TProp>> propertyExpression, Expression<Func<TProp>> valueExpression)
        {
            InitializerExpressions.Add(propertyExpression.ToPropertyAssignmentExpression(valueExpression));

            CompileResolver();
        }
    }
}