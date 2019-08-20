using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static fFastInjector.Functions;

namespace fFastInjector
{
    /// <summary>
    /// fFastInjector is a small and fast dependency injector which uses static variables and compiled
    /// expressions for high-performance dependency resolution and injection
    /// </summary>
    public static class Injector
    {
        //static Injector()
        //{
        //    var resolverGenericType = typeof(InternalResolver<object>).GetGenericTypeDefinition();
            
        //    var resolveMethod = ((Func<ResolutionContext, object>)InternalResolver<object>.Resolve).GetMethodInfo();
        //}

        // constants
        const int DictionaryInitialSize = 60;

        // lock objects
        internal readonly static object _openTypeResolversLock = new object();

        // generic method parts
        private readonly static MethodInfo _publicResolveMethod = ((Func<object>)Resolve<object>).GetMethodInfo().GetGenericMethodDefinition();
        private readonly static MethodInfo UpdateCovariantMethod = ((Action)UpdateCovariantResolver<object>).GetMethodInfo().GetGenericMethodDefinition();

        // resolvers and resolver info
        internal static readonly Dictionary<Type, ResolverInfo> ResolverInfo = new Dictionary<Type, ResolverInfo>(DictionaryInitialSize);
        internal static readonly List<CovariantMatcher> OpenTypeResolvers = new List<CovariantMatcher>();

        internal readonly static Dictionary<Type, Func<ResolutionContext, object>> Resolvers = new Dictionary<Type, Func<ResolutionContext, object>>(DictionaryInitialSize);

        internal static T ResolveDefault<T>(ResolutionContext resolutionContext) => default;

        /// <summary>
        /// By default, an fFastInjectorException is thrown, but you can inject your own
        /// exception handler here to throw different kinds of exceptions or log before 
        /// throwing
        /// </summary>
        public static Func<string, Exception, Exception> CreateNewException { get; set; }
            = (message, innerException) => new fFastInjectorException(message, innerException);

        internal static IEnumerable<T> WhereAvailableForRegistration<T>(this IEnumerable<T> members)
              where T : MemberInfo
        {
            return members.Where(v => v.IsAvailableForRegistration());
        }

        internal static bool IsAvailableForRegistration<T>(this T member)
                where T : MemberInfo
        {
            return !Attribute.IsDefined(member, typeof(IgnoreDuringRegistrationAttribute));
        }

        internal static bool IsInjectedDependencyMember<T>(this T member)
               where T : MemberInfo
        {
            return
                Attribute.IsDefined(member, typeof(InjectDependencyHereAttribute));
        }

        /// <summary>
        /// Return a new instance of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>() where T : class
        {
            var resolutionContext = new ResolutionContext();

            return InternalResolver<T>.Resolve(resolutionContext);
        }

        /// <summary>
        /// Return a new instance of type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Resolve(Type type)
        {
            var resolutionContext = new ResolutionContext();

            if (Resolvers.TryGetValue(type, out var resolver))
            {
                return resolver(resolutionContext);
            }

            // Not in dictionary yet, call Resolve<T> which will, in turn, set up and call the default Resolver
            // Which will result in an entry in the dictionary
            return _publicResolveMethod
                .MakeGenericMethod(type)
                .Invoke(null, new object[] { resolutionContext });
        }

        /// <summary>
        /// Set up TConcreteType as the concrete type for T.  TConcreteType may
        /// have additional dependencies that will be resolved before it is returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TConcreteType"></typeparam>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T, TConcreteType>()
            where T : class
            where TConcreteType : class, T
        {
            InternalResolver<T>.SetResolver(resolutionContext => InternalResolver<TConcreteType>.Resolve(resolutionContext));

            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set up TConcreteType as the concrete type for T.  TConcreteType may
        /// have additional dependencies that will be resolved before it is returned
        /// Also. specify a LifeTimeManager
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TConcreteType"></typeparam>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T, TConcreteType>(LifetimeManager<T> manager)
            where T : class
            where TConcreteType : class, T
        {
            InternalResolver<T>.SetResolver(context => InternalResolver<T>.Resolve(context), manager);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set up TConcreteType as the concrete type for T.  TConcreteType may
        /// have additional dependencies that will be resolved before it is returned
        /// After TConcreteType is created, execute some additionary initializers against TConcreteType
        /// (These initializers execute only when T is resolved)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TConcreteType"></typeparam>
        /// <param name="initializers"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T, TConcreteType>(params Expression<Action<TConcreteType>>[] initializers)
            where T : class
            where TConcreteType : class, T
        {
            var concreteInitializers = initializers.Select(v => ConvertFuncParameterType<T, TConcreteType>(v));
            InternalResolver<T>.SetResolver(resolutionContext => Resolve<TConcreteType>(), concreteInitializers, null);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set up TConcreteType as the concrete type for T.  TConcreteType may
        /// have additional dependencies that will be resolved before it is returned
        /// After TConcreteType is created, execute some additionary initializers against TConcreteType
        /// (These initializers execute only when T is resolved)
        /// Also. specify a LifeTimeManager
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TConcreteType"></typeparam>
        /// <param name="manager"></param>
        /// <param name="initializers"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T, TConcreteType>(LifetimeManager<T> manager, params Expression<Action<TConcreteType>>[] initializers)
            where T : class
            where TConcreteType : class, T
        {
            var concreteInitializers = initializers.Select(v => ConvertFuncParameterType<T, TConcreteType>(v));
            InternalResolver<T>.SetResolver(resolutionContext => Resolve<TConcreteType>(), concreteInitializers, manager);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Specify an open generic and an open generic that resolves it.
        /// Use the optional parameters to specify limits on the generic parameters
        /// i.e. SetGenericResolver(typeof(IEnumerable&lt;&gt;),typeof(PersonList&lt;&gt;),typeof(Person)) to specify that a PersonList&lt;T&gt;
        /// be returned for a Resolver&lt;IEnumerable&lt;Employee&gt;&gt;() if Employee inherits from Person.
        /// </summary>
        /// <param name="openGeneric">The type to be resolved.  typeof(IEnumerable&lt;&gt;) in the example</param>
        /// <param name="concreteOpenGeneric">The concrete type, typeof(PersonList&lt;&gt;) in the example</param>
        /// <param name="artificialConstraints">The optional constraints. typeof(Person) in the example.  If specified, there must be a constraint specified for each generic parameter</param>
        public static void SetGenericResolver(Type openGeneric, Type concreteOpenGeneric, params Type[] artificialConstraints)
        {
            Contract.Requires(openGeneric != null);
            Contract.Requires(concreteOpenGeneric != null);
            Contract.Requires(openGeneric.IsGenericType);
            Contract.Requires(concreteOpenGeneric.IsGenericType);

            var openGenericTypeArguments = openGeneric.GetGenericArguments().ToArray();

            if (artificialConstraints.Length == 0)
            { }
            else if (artificialConstraints.Length == openGenericTypeArguments.Length)
            {
                var closedGenericFromOpenForComparison = openGeneric.MakeGenericType(artificialConstraints);
                var closedGenericFromConcrete = concreteOpenGeneric.MakeGenericType(artificialConstraints);

                if (!closedGenericFromOpenForComparison.IsAssignableFrom(closedGenericFromConcrete))
                {
                    throw CreateExceptionInternal("concreteOpenGeneric must be assignable to openGeneric"); // CCOK
                }
            }
            else
            {
                throw CreateExceptionInternal("If any artificial constraints are specified, then you must specify an artificial constraint for each open generic type argument.");
            }

            lock (_openTypeResolversLock)
            {
                OpenTypeResolvers.RemoveAll(v => v.OpenType.Equals(openGeneric) && v.Constraints.SequenceEqual(artificialConstraints));

                OpenTypeResolvers.Add(new CovariantMatcher
                {
                    OpenType = openGeneric,
                    ConcreteType = concreteOpenGeneric,
                    Constraints = artificialConstraints
                });

                // handle overrides
                // if there is an auto-generated closed-type resolver using a different open-type resolver that is less specific
                // we will replace the auto-generated closed-type resolver with our more specific auto-generated closed-type resolver based on this open-type resolver
                foreach (var resolverInfo in ResolverInfo.Where(v => (v.Value.IsCovariantRegistration || v.Value.IsDefaultRegistration) && v.Key.MatchesGenericResolver(openGeneric, artificialConstraints)).ToList())
                {
                    // replace current registration with our updated registration, if applicable
                    UpdateCovariantResolver(resolverInfo.Key);
                }
            }
        }

        internal static void UpdateCovariantResolver(Type type) =>
            UpdateCovariantMethod.MakeGenericMethod(type).Invoke(null, null);

        internal static void UpdateCovariantResolver<T>() where T : class =>
            InternalResolver<T>.UpdateCovariantResolver();

        internal static bool MatchesGenericResolver(this Type closedGeneric, Type openGeneric, Type[] genericConstraints)
        {
            var closedGenericArguments = closedGeneric.GetGenericArguments();

            if (closedGenericArguments.Length != openGeneric.GetGenericArguments().Length)
            {
                return false;
            }
            else
            {
                var retval = true;

                var openGenericAsClosed = openGeneric.MakeGenericType(closedGenericArguments);

                if (closedGenericArguments.Length == genericConstraints.Length)
                {
                    retval &= genericConstraints
                        .Stitch(closedGenericArguments)
                        .All(v => v.Item1.IsAssignableFrom(v.Item2));
                }
                else if (genericConstraints.Length != 0)
                {
                    retval = false; // CCOK
                }

                retval &= openGenericAsClosed.IsAssignableFrom(closedGeneric);
                retval &= openGeneric.Equals(closedGeneric.GetGenericTypeDefinition());

                return retval;
            }
        }

        internal static bool MatchesGeneric(this Type openGeneric, Type closedGeneric, Type[] genericConstraints)
        {
            var closedGenericOpened = closedGeneric.GetGenericTypeDefinition();

            if (closedGenericOpened == openGeneric)
            {
                var parameterTypes = closedGeneric.GetGenericArguments();

                if (parameterTypes.Length == genericConstraints.Length)
                {
                    return genericConstraints
                        .Stitch(parameterTypes)
                        .All(v => v.Item1.IsAssignableFrom(v.Item2));
                }
                else if (genericConstraints.Length == 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal static int MatchesQuality(this Type openGeneric, Type closedGeneric, Type[] genericConstraints)
        {
            var closedGenericOpened = closedGeneric.GetGenericTypeDefinition();

            if (closedGenericOpened.Equals(openGeneric) && genericConstraints.Length > 0) // CCOK
            {
                var parameterTypes = closedGeneric.GetGenericArguments();

                if (parameterTypes.Length == genericConstraints.Length)
                {
                    var stitched = genericConstraints.Stitch(parameterTypes);
                    return stitched.Sum(v => InheritDistance(v.Item1, v.Item2));
                }
            }

            return int.MaxValue;
        }

        internal static int InheritDistance(Type baseClass, Type derived)
        {
            if (baseClass.IsAssignableFrom(derived))
            {
                if (derived.IsSubclassOf(baseClass))
                {
                    var distance = 0;
                    while (derived != typeof(object) && derived != baseClass) // CCOK
                    {
                        distance++;
                        derived = derived.BaseType;
                    }

                    // this should never happen
                    if (derived == typeof(object))
                    {
                        return int.MaxValue;
                    }

                    return distance;
                }
                else
                {
                    return 1;
                }
            }

            return int.MaxValue;
        }

        static IEnumerable<Tuple<T, U>> Stitch<T, U>(this IEnumerable<T> left, IEnumerable<U> right)
        {
            var leftEnumerator = left.GetEnumerator();
            var rightEnumerator = right.GetEnumerator();

            while (MoveAllEnumerators(leftEnumerator, rightEnumerator))
            {
                yield return Tuple.Create(leftEnumerator.Current, rightEnumerator.Current);
            }
        }

        static bool MoveAllEnumerators(params System.Collections.IEnumerator[] enumerators)
        {
            var results = enumerators.Select(v => v.MoveNext()).ToArray();

            for (var i = 1; i < results.Length; i++)
            {
                if (results[i] != results[0])
                {
                    throw new ArgumentException("Operation failed.  Sequences are not the same size.");
                }
            }

            return results[0];
        }

        static Expression<Action<T>> ConvertFuncParameterType<T, TConcreteType>(Expression<Action<TConcreteType>> action)
        {
            var parm = Expression.Parameter(typeof(T));
            var convertParm = Expression.Convert(parm, typeof(TConcreteType));
            var body = action.Body.Replace(action.Parameters[0], convertParm);

            return Expression.Lambda<Action<T>>(body, parm);
        }

        /// <summary>
        /// This helper method creates a property assignment expression from a property expression and an expression that
        /// returns a value of that type.  As of C# 5.0, a property assignment expression i.e. v => v.FullName = "Marco Polo"
        /// cannot be created directly in the source code, but can be created using expressions. i.e. 
        /// ToPropertyAssignmentExpression(v => v.FullName, () => "Marco Polo")
        /// </summary>
        /// <typeparam name="T">The type of the entity that contains the property</typeparam>
        /// <typeparam name="TProp">The type of the property itself</typeparam>
        /// <param name="propertyExpression">Expression representing the property</param>
        /// <param name="valueExpression">Expression representing the value to assign to the property</param>
        /// <returns>An expresssion that when executed will assign the value to the property</returns>
        public static Expression<Action<T>> ToPropertyAssignmentExpression<T, TProp>(this Expression<Func<T, TProp>> propertyExpression, Expression<Func<TProp>> valueExpression)
        {
            if (propertyExpression == null) throw CreateExceptionInternal(new ArgumentNullException(nameof(propertyExpression)));

            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                return Expression.Lambda<Action<T>>(
                    Expression.Assign(memberExpression, valueExpression.Body),
                    propertyExpression.Parameters
                );
            }
            else
            {
                throw CreateExceptionInternal("propertyExpression.Body must be a MemberExpression that can be assigned to");
            }
        }


        /// <summary>
        /// Set Resolver to a Singleton lifetime with the concrete type of TConcreteType
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TConcreteType"></typeparam>
        /// <returns></returns>
        public static InjectorFluent<T> SetSingletonResolver<T, TConcreteType>()
            where T : class
            where TConcreteType : class, T
        {
            Expression<Func<ResolutionContext, T>> resolve = resolutionContext => Resolve<TConcreteType>();
            var lifetime = new SingletonLifetimeManager<T>();
            InternalResolver<T>.SetResolver(resolve, lifetime);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set Resolver to return a specific instance for all resolutions of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetSingletonResolverAsInstance<T>(T instance)
            where T : class
        {
            InternalResolver<T>.SetResolver(resolutionContext => instance);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Return a reference to the fluent object for this type without disrupting any other configured constructor.
        /// i.e. If nothing is configured, use the default resolution or if a resolver was previously configured, don't change it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static InjectorFluent<T> ForType<T>()
            where T : class
        {
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set up an expression that returns an instance of T as the Resolver for T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factoryExpression"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T>(Expression<Func<T>> factoryExpression)
            where T : class
        {
            //InternalResolver<T>.SetResolver(factoryExpression);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set up an expression that returns an instance of T as the Resolver for T
        /// and specify a LifeTimeManager
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factoryExpression"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T>(LifetimeManager<T> manager, Expression<Func<T>> factoryExpression)
            where T : class
        {
            InternalResolver<T>.SetResolver((Expression<Func<ResolutionContext, T>>)null, manager);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Add a property injector to the resolution expression for type T using the default injector for type TPropertyType
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TPropertyType"></typeparam>
        /// <param name="propertyExpression"></param>
        /// <returns></returns>
        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
            where T : class
            where TPropertyType : class
        {
            InternalResolver<T>.AddInitializer(propertyExpression, () => Resolve<TPropertyType>());
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Add a property injector to the resolution for type T using the specified expression to inject and instance of TPropertyType
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TPropertyType"></typeparam>
        /// <param name="propertyExpression"></param>
        /// <param name="setter"></param>
        /// <returns></returns>
        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
            where T : class
        {
            InternalResolver<T>.AddInitializer(propertyExpression, setter);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Fluently add a property injector to the resolution expression for type T using the default injector for type TPropertyType
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TPropertyType"></typeparam>
        /// <param name="fluent"></param>
        /// <param name="propertyExpression"></param>
        /// <returns></returns>
        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(this InjectorFluent<T> fluent, Expression<Func<T, TPropertyType>> propertyExpression)
            where T : class
            where TPropertyType : class
        {
            InternalResolver<T>.AddInitializer(propertyExpression, () => Resolve<TPropertyType>());
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Fluently add a property injector to the resolution for type T using the specified expression to inject and instance of TPropertyType
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TPropertyType"></typeparam>
        /// <param name="fluent"></param>
        /// <param name="propertyExpression"></param>
        /// <param name="setter"></param>
        /// <returns></returns>
        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(this InjectorFluent<T> fluent, Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
            where T : class
        {
            InternalResolver<T>.AddInitializer(propertyExpression, setter);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Register TConcreteType as the resolver type for T
        /// </summary>
        /// <typeparam name="TConcreteType"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="concreteFluent"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        public static InjectorFluent<T> As<TConcreteType, T>(this InjectorFluent<TConcreteType> concreteFluent, InjectorFluent<T> toType)
            where T : class
            where TConcreteType : class, T
        {
            InternalResolver<T>.SetResolver(resolutionContext => Resolve<TConcreteType>());
            return InjectorFluent<T>.Instance;
        }
    }
}
