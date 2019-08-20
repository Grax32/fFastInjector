using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace fFastInjector
{
    internal class DefaultResolverBuilder
    {
        internal static BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.Instance;

        private const string METHOD_NAME_GetInternalResolveExpression =
            nameof(DefaultResolverBuilder<object>.GetInternalResolveExpression);

        private const string METHOD_NAME_GetDefaultResolverExpression =
            nameof(DefaultResolverBuilder<object>.GetDefaultResolverExpression);

        internal static Expression GetInternalResolveExpression(Type type) =>
            GetExpressionFromGenericMethod(type, METHOD_NAME_GetInternalResolveExpression);

        internal static Expression GetDefaultResolverExpression(Type type) =>
            GetExpressionFromGenericMethod(type, METHOD_NAME_GetDefaultResolverExpression);

        /// <summary>
        /// Call a generic method that returns an expression
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static Expression GetExpressionFromGenericMethod(Type type, string methodName, params object[] parameters)
        {
            return typeof(DefaultResolverBuilder<>)
                .MakeGenericType(type)
                .GetMethod(methodName)
                .Invoke(null, parameters)
                as Expression;
        }
    }

    internal class DefaultResolverBuilder<T>
        where T : class
    {
        private static readonly Type _typeofT = typeof(T);

        internal static Expression<Func<ResolutionContext, T>> GetInternalResolveExpression() =>
            (ResolutionContext resolutionContext) => InternalResolver<T>.Resolve(resolutionContext);

        internal static Expression<Func<ResolutionContext, T>> GetDefaultResolverExpression()
        {
            var constructorExpression = GetConstructorInjectExpression();

            var expressions = _typeofT.GetMembers(DefaultResolverBuilder.BindingFlags)
                .Select(v => GetInternalMemberInjectExpression(v))
                .Where(v => v != null)
                .ToList<Expression>();

            if (expressions.Any())
            {
                var resolutionContextParameter = Expression.Parameter(typeof(ResolutionContext), "resolutionContext");

                var constructed = Expression.Variable(_typeofT, "constructed");
                var assignConstructed = Expression.Assign(constructed, constructorExpression);

                expressions.Insert(0, assignConstructed);
                expressions.Add(constructed);

                var blockExpression = Expression.Block(expressions);

                return Expression.Lambda<Func<ResolutionContext, T>>(blockExpression, resolutionContextParameter);
            }
            else
            {
                return constructorExpression;
            }
        }

        private static Expression<Func<ResolutionContext, T>> GetConstructorInjectExpression()
        {
            // get first available constructor
            // priority to attribute decorated constructor
            // then to most parameters
            var constructor = _typeofT
                .GetConstructors(DefaultResolverBuilder.BindingFlags)
                .WhereAvailableForRegistration()
                .OrderBy(v => v.IsInjectedDependencyMember() ? 0 : 1)
                .ThenByDescending(v => v.GetParameters().Count())
                .FirstOrDefault();

            if (constructor == null)
            {
                return rc => ResolverFunctions<T>.ThrowMissingConstructorException();
            }

            return GetConstructorExpression(constructor);
        }

        private static IEnumerable<Expression> GetParameterExpressions(MethodBase method, ParameterExpression resolutionContextParameter)
        {
            return method.GetParameters()
                    .Select(GetParameterExpressionFromType(v.ParameterType));
        }

        static readonly MethodInfo InternalResolverMethod = ((Func<ResolutionContext, object>)InternalResolver<object>.Resolve)
            .Method
            .GetGenericMethodDefinition();

        private static Expression GetParameterExpressionFromType(Type type) =>
            typeof(InternalResolver<>)
            .MakeGenericType(type)
            .GetMethod(nameof(InternalResolver<object>.Resolve))
            .


        private static Expression<Func<ResolutionContext, T>> GetConstructorExpression(ConstructorInfo constructor)
        {
            var resolutionContextParameter = Expression.Parameter(typeof(ResolutionContext), nameof(ResolutionContext));
            var parameterExpressions = GetParameterExpressions(constructor, resolutionContextParameter);
            var newExpression = Expression.New(constructor, parameterExpressions);

            return Expression.Lambda<Func<ResolutionContext, T>>(newExpression, resolutionContextParameter);
        }

        private static Expression<Action<ResolutionContext, T>> GetInternalMemberInjectExpression(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case MethodInfo methodInfo:
                    return GetInternalMethodInjectExpression(methodInfo);
                case PropertyInfo propertyInfo:
                    return GetInternalPropertySetExpression(propertyInfo);
                default:
                    Debug.WriteLine($"No injectors exist for members of type {memberInfo.GetType().Name}");
                    return null;
            }
        }

        private static Expression<Action<ResolutionContext, T>> GetInternalMethodInjectExpression(MethodInfo methodInfo)
        {
            var resolutionContextParameter = Expression.Parameter(typeof(ResolutionContext), nameof(ResolutionContext));
            var parameterExpressions = GetParameterExpressions(methodInfo, resolutionContextParameter);
            var methodCallExpression = Expression.Call(methodInfo, parameterExpressions);

            return Expression.Lambda<Action<ResolutionContext, T>>(methodCallExpression, resolutionContextParameter);
        }

        private static Expression<Action<ResolutionContext, T>> GetInternalPropertySetExpression(PropertyInfo propertyInfo)
        {
            var resolveExpression = GetInternalResolveExpression();

            var instanceParameter = Expression.Parameter(_typeofT, "instance");
            var propertyExpression = Expression.Property(instanceParameter, propertyInfo);

            var property = propertyExpression.Expression;
            var newValue = resolveExpression.Body;

            return Expression.Lambda<Action<ResolutionContext, T>>(
                Expression.Assign(property, newValue),
                resolveExpression.Parameters.Single(),
                instanceParameter);
        }
    }
}
