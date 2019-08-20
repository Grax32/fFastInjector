using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static fFastInjector.Functions;
using static fFastInjector.ErrorConstants;

namespace fFastInjector
{
    internal static class ResolverFunctions<T>
          where T : class
    {
        static readonly Type typeofT = typeof(T);
        static readonly MethodInfo ReturnDefaultValueOf = ((Func<ResolutionContext, object>)Injector.ResolveDefault<object>).GetMethodInfo().GetGenericMethodDefinition();

        internal static Expression<Func<ResolutionContext, T>> AddInitializers(Expression<Func<ResolutionContext, T>> resolver, IEnumerable<Expression<Action<T>>> initializers)
        {
            Expression<Func<ResolutionContext, T>> returnValue;

            if (initializers.Any())
            {
                var body = resolver.Body;

                var bodyExpressions = new List<Expression>();
                ParameterExpression returnVar = Expression.Variable(typeofT);
                ParameterExpression contextParameter = Expression.Variable(typeof(ResolutionContext));

                bodyExpressions.Add(Expression.Assign(returnVar, body));

                // replace the parameter in the initializer with the newly created object and then discard the parameters
                var customInitializers = initializers.Select(v => v.Body.Replace(v.Parameters[0], returnVar));

                bodyExpressions.AddRange(customInitializers);

                // return value from returnVar
                bodyExpressions.Add(returnVar);

                body = Expression.Block(new[] { returnVar }, bodyExpressions);
                returnValue = Expression.Lambda<Func<ResolutionContext, T>>(body, contextParameter);
            }
            else
            {
                returnValue = resolver;
            }

            return returnValue;
        }

        internal static Expression<Func<ResolutionContext, T>> GetCovariantResolverExpression()
        {
            if (typeofT.IsGenericType)
            {
                var genericResolversForType = Injector.OpenTypeResolvers.Where(v => v.OpenType == typeofT.GetGenericTypeDefinition());
                var whereMatchesConstraints = genericResolversForType.Where(v => v.OpenType.MatchesGeneric(typeofT, v.Constraints));
                var orderedByMatchQuality = whereMatchesConstraints.OrderBy(v => v.OpenType.MatchesQuality(typeofT, v.Constraints));
                var openResolver = orderedByMatchQuality.FirstOrDefault();

                if (openResolver != null)
                {
                    var thisConcreteType = openResolver.ConcreteType.MakeGenericType(typeofT.GetGenericArguments());
                    return (Expression<Func<ResolutionContext, T>>)ResolverExpressions.InternalResolveExpression(thisConcreteType);
                }
            }

            return null;
        }

        internal static T ThrowMissingConstructorException()
        {
            throw CreateExceptionInternal(string.Format(CultureInfo.CurrentCulture, ErrorMissingConstructor, FancyTypeName(typeof(T))));
        }

        internal static Expression<Func<ResolutionContext, T>> AddLifetimeManager(Expression<Func<ResolutionContext, T>> resolverExpression, LifetimeManager<T> manager)
        {
            if (manager == null)
            {
                return resolverExpression;
            }
            else
            {
                manager.SetResolver(resolverExpression.Compile());
                return resolutionContext => manager.GetValue();
            }
        }
    }
}
