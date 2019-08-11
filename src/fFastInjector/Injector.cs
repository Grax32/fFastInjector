/*
New BSD License (BSD)
Copyright (c) 2013, David Walker (http://ffastinjector.codeplex.com/license)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that
the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the
following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace fFastInjector
{
    /// <summary>
    /// fFastInjector is a small and fast dependency injector which uses static variables and compiled
    /// expressions for high-performance dependency resolution and injection
    /// </summary>
    public static class Injector
    {
        const int DictionaryInitialSize = 60;
        private static Type _exceptionType = typeof(fFastInjectorException);

#if USEINTERNAL
        internal
#endif
 readonly static Dictionary<Type, Func<object>> _resolvers = new Dictionary<Type, Func<object>>(DictionaryInitialSize);
#if USEINTERNAL
        internal
#endif
 readonly static object _resolversLock = new object();

#if USEINTERNAL
        internal
#endif
 static Dictionary<Type, ResolverInfo> _genericResolverInfo = new Dictionary<Type, ResolverInfo>(DictionaryInitialSize);

#if USEINTERNAL
        internal
#endif
 static List<CovariantMatcher> _openTypeResolvers = new List<CovariantMatcher>();
#if USEINTERNAL
        internal
#endif
 readonly static object _openTypeResolversLock = new object();

        readonly static MethodInfo GenericResolve = ((Func<object>)Injector.Resolve<object>).Method.GetGenericMethodDefinition();
        readonly static MethodInfo ReturnDefaultValueOf = ((Func<object>)Injector.ResolveDefault<object>).Method.GetGenericMethodDefinition();
        readonly static MethodInfo UpdateCovariantMethod = ((Action)UpdateCovariantResolver<object>).Method.GetGenericMethodDefinition();

#if USEINTERNAL
        internal
#endif
 const string ErrorResolutionRecursionDetected = "Resolution recursion detected.  Resolve<{0}> is called by a dependency of Resolve<{0}> leading to an infinite loop.";
#if USEINTERNAL
        internal
#endif
 const string ErrorUnableToResolveInterface = "Error on {0}. Unable to resolve Interface and Abstract classes without a configuration.";
#if USEINTERNAL
        internal
#endif
 const string ErrorMustContainMemberExpression = "Must contain a MemberExpression";
#if USEINTERNAL
        internal
#endif
 const string ErrorMissingConstructor = "Error on {0}.  Unable to locate a suitable constructor.  To use a private constructor, locate with Reflection and pass the 'ConstructorInfo' object to SetResolver<{0}>(constructorInfo)";

        /// <summary>
        /// By default, fFastInjector returns an fFastInjectorException, but it can return a custom exception type
        /// specified by setting this property
        /// </summary>
        public static Type ExceptionType
        {
            get { return _exceptionType; }
            set { _exceptionType = value; } // CCOK (causes code coverage to be less than 100% but it has been reviewed and considered OK)
        }

        static T ResolveDefault<T>() { return default(T); }

        static Exception CreateException(string message, Exception innerException = null)
        {
            return (Exception)Activator.CreateInstance(ExceptionType, message, innerException);
        }

#if USEINTERNAL
        internal
#endif
 static string FancyTypeName(Type type)
        {
            var typeName = type.Name.Split('`')[0];
            if (type.IsGenericType)
            {
                typeName += string.Format(CultureInfo.CurrentCulture, "<{0}>", string.Join(",", type.GetGenericArguments().Select(v => FancyTypeName(v)).ToArray()));
            }
            return typeName;
        }

        internal static IEnumerable<T> WhereAvailableForRegistration<T>(this IEnumerable<T> members)
              where T : MemberInfo
        {
            return members.Where(v => v.IsAvailableForRegistration());
        }

        internal static bool IsAvailableForRegistration<T>(this T member)
                where T : MemberInfo
        {
            return !Attribute.IsDefined(member, typeof(Injector.IgnoreDuringRegistrationAttribute));
        }

        internal static bool IsInjectedDependencyMember<T>(this T member)
               where T : MemberInfo
        {
            return
                Attribute.IsDefined(member, typeof(InjectDependencyHereAttribute)) ||
                Attribute.IsDefined(member, typeof(Injector.SelectDuringRegistrationAttribute));
        }

#if USEINTERNAL
        internal
#endif
 class CovariantMatcher
        {
            public Type OpenType { get; set; }
            public Type[] Constraints { get; set; }

            public Type ConcreteType { get; set; }

            public override string ToString()
            {
                // CCOK
                return string.Format(CultureInfo.CurrentCulture, "{0} with constraints {1} will be resolved by type {2}", FancyTypeName(OpenType), string.Join(",", Constraints.Select(v => FancyTypeName(v)).ToArray()), FancyTypeName(ConcreteType));
            }
        }

#if USEINTERNAL
        internal
#endif
 struct ResolverInfo
        {
            public bool IsDefaultRegistration { get; set; }
            public bool IsCovariantRegistration { get; set; }
        }

        /// <summary>
        /// Return a new instance of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>()
            where T : class
        {
            return InternalResolver<T>.ActiveResolverFunction();
        }

        /// <summary>
        /// Return a new instace of type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Resolve(Type type)
        {
            Func<object> resolver;
            if (_resolvers.TryGetValue(type, out resolver))
            {
                return resolver();
            }

            // Not in dictionary yet, call Resolve<T> which will, in turn, set up and call the default Resolver
            // Which will result in an entry in the dictionary
            return GenericResolve.MakeGenericMethod(type).Invoke(null, new object[0]);
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
            where TConcreteType : class,T
        {
            InternalResolver<T>.SetResolver(() => Resolve<TConcreteType>());
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
        public static InjectorFluent<T> SetResolver<T, TConcreteType>(ILifetimeManager<T> manager)
            where T : class
            where TConcreteType : class,T
        {
            InternalResolver<T>.SetResolver(() => Resolve<TConcreteType>(), manager);
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
            where TConcreteType : class,T
        {
            var concreteInitializers = initializers.Select(v => ConvertFuncParameterType<T, TConcreteType>(v));
            InternalResolver<T>.SetResolver(() => Resolve<TConcreteType>(), concreteInitializers, null);
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
        public static InjectorFluent<T> SetResolver<T, TConcreteType>(ILifetimeManager<T> manager, params Expression<Action<TConcreteType>>[] initializers)
            where T : class
            where TConcreteType : class,T
        {
            var concreteInitializers = initializers.Select(v => ConvertFuncParameterType<T, TConcreteType>(v));
            InternalResolver<T>.SetResolver(() => Resolve<TConcreteType>(), concreteInitializers, manager);
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
                    throw CreateException("concreteOpenGeneric must be assignable to openGeneric"); // CCOK
                }
            }
            else
            {
                throw CreateException("If any artificial constraints are specified, then you must specify an artificial constraint for each open generic type argument.");
            }

            lock (_openTypeResolversLock)
            {
                _openTypeResolvers.RemoveAll(v => v.OpenType.Equals(openGeneric) && v.Constraints.SequenceEqual(artificialConstraints));

                _openTypeResolvers.Add(new CovariantMatcher
                {
                    OpenType = openGeneric,
                    ConcreteType = concreteOpenGeneric,
                    Constraints = artificialConstraints
                });

                // handle overrides
                // if there is an auto-generated closed-type resolver using a different open-type resolver that is less specific
                // we will replace the auto-generated closed-type resolver with our more specific auto-generated closed-type resolver based on this open-type resolver
                foreach (var resolverInfo in _genericResolverInfo.Where(v => (v.Value.IsCovariantRegistration || v.Value.IsDefaultRegistration) && v.Key.MatchesGenericResolver(openGeneric, artificialConstraints)).ToList())
                {
                    // replace current registration with our updated registration, if applicable
                    UpdateCovariantResolver(resolverInfo.Key);
                }
            }
        }

#if USEINTERNAL
        internal
#endif
 static void UpdateCovariantResolver(Type type)
        {
            UpdateCovariantMethod.MakeGenericMethod(type).Invoke(null, null);
        }

#if USEINTERNAL
        internal
#endif
 static void UpdateCovariantResolver<T>()
                   where T : class
        {
            InternalResolver<T>.UpdateCovariantResolver();
        }

#if USEINTERNAL
        internal
#endif
 static bool MatchesGenericResolver(this Type closedGeneric, Type openGeneric, Type[] genericConstraints)
        {
            var closedGenericArguments = closedGeneric.GetGenericArguments();
            bool retval = true;

            if (closedGenericArguments.Length != openGeneric.GetGenericArguments().Length)
            {
                retval = false;
            }
            else
            {
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
            }

            return retval;
        }

#if USEINTERNAL
        internal
#endif
 static bool MatchesGeneric(this Type openGeneric, Type closedGeneric, Type[] genericConstraints)
        {
            var retval = false;

            var closedGenericOpened = closedGeneric.GetGenericTypeDefinition();

            if (closedGenericOpened == openGeneric)
            {
                var parameterTypes = closedGeneric.GetGenericArguments();

                if (parameterTypes.Length == genericConstraints.Length)
                {
                    retval = genericConstraints
                        .Stitch(parameterTypes)
                        .All(v => v.Item1.IsAssignableFrom(v.Item2));
                }
                else if (genericConstraints.Length == 0)
                {
                    retval = true;
                }
            }

            return retval;
        }

#if USEINTERNAL
        internal
#endif
 static int MatchesQuality(this Type openGeneric, Type closedGeneric, Type[] genericConstraints)
        {
            var retval = Int32.MaxValue;

            var closedGenericOpened = closedGeneric.GetGenericTypeDefinition();

            if (closedGenericOpened.Equals(openGeneric) && genericConstraints.Length > 0) // CCOK
            {
                var parameterTypes = closedGeneric.GetGenericArguments();

                var stitched = genericConstraints.Stitch(parameterTypes).ToList();

                if (parameterTypes.Length == genericConstraints.Length)
                {
                    retval = stitched.Sum(v => InheritDistance(v.Item1, v.Item2));
                }
            }

            return retval;
        }

#if USEINTERNAL
        internal
#endif
 static int InheritDistance(Type baseClass, Type derived)
        {
            var retval = Int32.MaxValue;

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

                    retval = distance;

                    // this should never happen
                    if (derived == typeof(object))
                    {
                        retval = Int32.MaxValue;
                    }
                }
                else
                {
                    retval = 1;
                }
            }

            return retval;
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
            if (results.All(v => v)) { return true; }
            if (results.Any(v => v)) { throw new ArgumentException("Operation failed.  Sequences are not the same size."); } // CCOK
            return false;
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
            Contract.Requires(propertyExpression != null);
            Contract.Requires(valueExpression != null);
            Contract.Requires(propertyExpression.Body is MemberExpression);

            return Expression.Lambda<Action<T>>(
                Expression.Assign(
                    propertyExpression.Body,
                    valueExpression.Body
                ),
                propertyExpression.Parameters
            );
        }

        /// <summary>
        /// Return a new expression where originalExpression has been replaced by replacementExpression
        /// </summary>
        /// <typeparam name="TExpression"></typeparam>
        /// <param name="thisExpression"></param>
        /// <param name="originalExpression"></param>
        /// <param name="replacementExpression"></param>
        /// <returns></returns>
        static TExpression Replace<TExpression>(this TExpression thisExpression, Expression originalExpression, Expression replacementExpression)
             where TExpression : Expression
        {
            return (TExpression)(new ReplaceVisitor(originalExpression, replacementExpression)).Visit(thisExpression);
        }

        private class ReplaceVisitor : ExpressionVisitor
        {
            readonly Expression _originalExpression;
            readonly Expression _replacementExpression;

            public ReplaceVisitor(Expression originalExpression, Expression replacementExpression)
            {
                _originalExpression = originalExpression;
                _replacementExpression = replacementExpression;
            }

            public override Expression Visit(Expression node)
            {
                return _originalExpression == node ? _replacementExpression : base.Visit(node);
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
            where TConcreteType : class,T
        {
            Expression<Func<T>> resolve = () => Resolve<TConcreteType>();
            InternalResolver<T>.SetResolver(resolve, LifetimeManager<T>.Singleton);
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
            InternalResolver<T>.SetResolver(() => instance);
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// Set Resolver to resolve T using the specified constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T>(ConstructorInfo constructor)
            where T : class
        {
            InternalResolver<T>.SetResolver(constructor);
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
        /// Set Resolver to resolve T using the specified constructor, with the specified LifeTimeManager
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manager"></param>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public static InjectorFluent<T> SetResolver<T>(ILifetimeManager<T> manager, ConstructorInfo constructor)
            where T : class
        {
            InternalResolver<T>.SetResolver(constructor, manager);
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
            InternalResolver<T>.SetResolver(factoryExpression);
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
        public static InjectorFluent<T> SetResolver<T>(ILifetimeManager<T> manager, Expression<Func<T>> factoryExpression)
            where T : class
        {
            InternalResolver<T>.SetResolver(factoryExpression, manager);
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
            where TConcreteType : class,T
        {
            InternalResolver<T>.SetResolver(() => Resolve<TConcreteType>());
            return InjectorFluent<T>.Instance;
        }

        /// <summary>
        /// The fluent class enables extension methods on the type of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class InjectorFluent<T>
            where T : class
        {
            internal static InjectorFluent<T> Instance { get { return _instance; } }
            static InjectorFluent<T> _instance = new InjectorFluent<T>();
        }

        /// <summary>
        /// Deprecated. Use InjectDependencyHereAttribute instead.
        /// Used to select a particular constructor/method/member during auto-resolution
        /// </summary>
        [Obsolete("Use InjectDependencyHereAttribute instead.")]
        [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
        public sealed class SelectDuringRegistrationAttribute : Attribute { }

        /// <summary>
        /// Used to avoid a particular constructor/method/member during auto-resolution
        /// </summary>
        [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
        public sealed class IgnoreDuringRegistrationAttribute : Attribute { }

        /// <summary>
        /// The interface that a LifetimeManager must implement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface ILifetimeManager<T>
            where T : class
        {
            /// <summary>
            /// Return a value from the lifetime manager after the applicable lifetime criteria have been applied
            /// </summary>
            /// <returns></returns>
            T GetValue();

            /// <summary>
            /// Set the function to create a new instance in the lifetime manager
            /// </summary>
            /// <param name="func"></param>
            void SetResolver(Func<T> func);
        }

        /// <summary>
        /// Base class for a LifetimeManager&lt;T&gt;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class LifetimeManager<T> : ILifetimeManager<T>
            where T : class
        {
            /// <summary>
            /// Store the resolver to create a new instance of type T for this LifetimeManager
            /// </summary>
            protected Func<T> Resolver { get; set; }

            /// <summary>
            /// Return the appropriate value for type T for this LifetimeManager
            /// </summary>
            protected Func<T> ValueGetter { get; set; }

            /// <summary>
            /// Return a Singleton LifetimeManager for type T
            /// </summary>
            public static LifetimeManager<T> Singleton { get { return GetSingletonLifetimeManager(); } }

            private static LifetimeManager<T> GetSingletonLifetimeManager()
            {
                T singletonValue = null;
                var lockObject = new object();

                var manager = new LifetimeManager<T>();

                Func<T> optimizedValueGetter = () => singletonValue;
                Func<T> initialValueGetter = () =>
                {
                    if (singletonValue == null)
                    {
                        lock (lockObject)
                        {
                            if (singletonValue == null)
                            {
                                singletonValue = manager.Resolver();
                                manager.ValueGetter = optimizedValueGetter;
                            }
                        }
                    }
                    return singletonValue;
                };

                manager.ValueGetter = initialValueGetter;
                return manager;
            }

            /// <summary>
            /// Get a value of type T for this LifetimeManager
            /// </summary>
            /// <returns></returns>
            public virtual T GetValue()
            {
                return ValueGetter();
            }

            /// <summary>
            /// Set the resolver that will create a new instance of type T for this LifetimeManager
            /// </summary>
            /// <param name="func"></param>
            public virtual void SetResolver(Func<T> func)
            {
                Resolver = func;
            }
        }

        /// <summary>
        /// Exception type for fFastInjector exceptions
        /// </summary>
        public class fFastInjectorException : Exception
        {
            /// <summary>
            /// Constructor for fFastInjectorException
            /// </summary>
            public fFastInjectorException() : base("An unexpected/unknown error has occurred") { }
            /// <summary>
            /// Constructor for fFastInjectorException
            /// </summary>
            /// <param name="message"></param>
            public fFastInjectorException(string message) : base(message) { }
            /// <summary>
            /// Constructor for fFastInjectorException
            /// </summary>
            /// <param name="message"></param>
            /// <param name="innerException"></param>
            public fFastInjectorException(string message, Exception innerException) : base(message, innerException) { }
        }

#if USEINTERNAL
        internal
#endif
 static class InternalResolver<T>
               where T : class
        {
            static bool HasStaticConstructorCompleted = false;
            static bool IsCovariantRegistration = false;

            static InternalResolver()
            {
                Debug.WriteLine("Initializing auto-resolver for " + FancyTypeName(typeofT));

                try
                {
                    var resolver = ResolverFunctions<T>.GetCovariantResolverExpression();
                    bool isCovariant = resolver != null;

                    if (resolver == null)
                    {
                        resolver = ResolverFunctions<T>.GetDefaultResolverExpression();
                    }

                    InnerSetResolver(resolver, isCovariant, null);
                }
                catch (Exception ex) // CCOK
                {
                    ActiveResolverFunction = () =>
                    {
                        throw CreateException("Error initializing default resolver for " + FancyTypeName(typeofT) + ".  See 'InnerException' for details", ex);
                    };
                }

                HasStaticConstructorCompleted = true;
            }

#if USEINTERNAL
            internal
#endif
 static void InnerSetResolver(Expression<Func<T>> resolver, bool isCovariantRegistration, ILifetimeManager<T> lifetimeManager)
            {
                IsCovariantRegistration = isCovariantRegistration;
                LifetimeManager = lifetimeManager;
                BaseResolverExpression = resolver;
                CompileResolver();
            }

            static void CompileResolver()
            {
                var completedResolverExpression = BaseResolverExpression;
                completedResolverExpression = ResolverFunctions<T>.AddInitializers(completedResolverExpression, InitializerExpressions);
                completedResolverExpression = ResolverFunctions<T>.AddLifetimeManager(completedResolverExpression, LifetimeManager);

                lock (_resolversLock)
                {
                    ActiveResolverFunction = completedResolverExpression.Compile();
                    _resolvers[typeofT] = ActiveResolverFunction;

                    _genericResolverInfo[typeofT] = new ResolverInfo
                    {
                        IsDefaultRegistration = !HasStaticConstructorCompleted,
                        IsCovariantRegistration = IsCovariantRegistration
                    };
                }
            }

            internal static Func<T> ActiveResolverFunction;

            internal static Expression<Func<T>> BaseResolverExpression;

            internal static ILifetimeManager<T> LifetimeManager;

            static readonly Type typeofT = typeof(T);

            static readonly List<Expression<Action<T>>> InitializerExpressions = new List<Expression<Action<T>>>();

            internal static void UpdateCovariantResolver()
            {
                var resolver = ResolverFunctions<T>.GetCovariantResolverExpression();
                bool isCovariant = resolver != null;

                if (resolver == null)
                {
                    // CCOK
                    throw CreateException("UpdateCovariantResolver may not be called on this type because there is no covariant resolver that applies.");
                }

                InnerSetResolver(resolver, isCovariant, null);
            }

            internal static void SetResolver(ConstructorInfo constructor, ILifetimeManager<T> lifetimeManager = null)
            {
                Expression<Func<T>> resolver = ResolverFunctions<T>.GetInjectedDependencyExpression(constructor);
                resolver = ResolverFunctions<T>.GetInjectAllDependenciesExpression(resolver);

                InternalResolver<T>.InnerSetResolver(resolver, false, lifetimeManager);
            }

            internal static void SetResolver(Expression<Func<T>> resolver, ILifetimeManager<T> lifetimeManager = null)
            {
                InternalResolver<T>.InnerSetResolver(resolver, false, lifetimeManager);
            }

            internal static void SetResolver(Expression<Func<T>> resolver, IEnumerable<Expression<Action<T>>> initializers, ILifetimeManager<T> lifetimeManager = null)
            {
                InitializerExpressions.Clear();
                InitializerExpressions.AddRange(initializers);
                InternalResolver<T>.InnerSetResolver(resolver, false, lifetimeManager);
            }

            internal static void AddInitializer<TProp>(Expression<Func<T, TProp>> propertyExpression, Expression<Func<TProp>> valueExpression)
            {
                InitializerExpressions.Add(propertyExpression.ToPropertyAssignmentExpression(valueExpression));

                CompileResolver();
            }
        }

        static class ResolverFunctions<T>
              where T : class
        {
            static Type typeofT = typeof(T);

            internal static Expression<Func<T>> AddInitializers(Expression<Func<T>> resolver, IEnumerable<Expression<Action<T>>> initializers)
            {
                Expression<Func<T>> returnValue;

                if (initializers.Any())
                {
                    var body = resolver.Body;

                    var bodyExpressions = new List<Expression>();
                    ParameterExpression returnVar = Expression.Variable(typeofT);

                    bodyExpressions.Add(Expression.Assign(returnVar, body));

                    // replace the parameter in the initializer with the newly created object and then discard the parameters
                    var customInitializers = initializers.Select(v => v.Body.Replace(v.Parameters[0], returnVar));

                    bodyExpressions.AddRange(customInitializers);

                    // return value from returnVar
                    bodyExpressions.Add(returnVar);

                    body = Expression.Block(new[] { returnVar }, bodyExpressions);
                    returnValue = Expression.Lambda<Func<T>>(body);
                }
                else
                {
                    returnValue = resolver;
                }

                return returnValue;
            }

            internal static Expression<Func<T>> GetCovariantResolverExpression()
            {
                Expression<Func<T>> returnValue = null;

                if (typeofT.IsGenericType)
                {
                    var openResolver = _openTypeResolvers
                        .Where(v => v.OpenType == typeofT.GetGenericTypeDefinition())
                        .Where(v => v.OpenType.MatchesGeneric(typeofT, v.Constraints))
                        .OrderBy(v => v.OpenType.MatchesQuality(typeofT, v.Constraints))
                        .Select(v => v)
                        .FirstOrDefault();

                    if (openResolver != null)
                    {
                        var thisConcreteType = openResolver.ConcreteType.MakeGenericType(typeofT.GetGenericArguments());
                        returnValue = Expression.Lambda<Func<T>>(Expression.Call(GenericResolve.MakeGenericMethod(thisConcreteType)));
                    }
                }

                return returnValue;
            }

            /// <summary>
            /// Locate the Resolver that will be used by default, if another resolver is not configured
            /// </summary>
            /// <returns></returns>
            internal static Expression<Func<T>> GetDefaultResolverExpression()
            {
                Expression<Func<T>> returnValue = null;

                if (typeofT.IsInterface || typeofT.IsAbstract)
                {
                    // if we can not instantiate, set the resolver to throw an exception.
                    // this resolver will be replaced when the type is configured
                    returnValue = GetDefaultImplementationConstructorExpression();
                }
                else
                {
                    // try to find the default constructor and create a default resolver from it
                    returnValue = GetDefaultConstructorExpression();
                }

                return returnValue;
            }

            /// <summary>
            /// Get the constructor with the fewest number of parameters and create a factory for it
            /// </summary>
            internal static Expression<Func<T>> GetDefaultConstructorExpression()
            {
                return GetConstructorExpressionFromType(typeofT);
            }

            internal static Expression<Func<T>> GetConstructorExpressionFromType(Type type)
            {
                Expression<Func<T>> returnValue;

                // get first available constructor ordered by parameter count ascending
                var constructor = type
                    .GetConstructors()
                    .WhereAvailableForRegistration()
                    .OrderBy(v => v.IsInjectedDependencyMember() ? 0 : 1)
                    .ThenByDescending(v => v.GetParameters().Count())
                    .FirstOrDefault();

                if (constructor == null)
                {
                    returnValue = () => ThrowMissingConstructorException();
                }
                else
                {
                    returnValue = GetInjectedDependencyExpression(constructor);
                    returnValue = GetInjectAllDependenciesExpression(returnValue);
                }

                return returnValue;
            }

            internal static Expression<Func<T>> GetInjectAllDependenciesExpression(Expression<Func<T>> resolverExpression)
            {
                Expression<Func<T>> returnValue = resolverExpression;

                var properties = typeofT
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(v => v.IsInjectedDependencyMember())
                    .ToArray();

                var methods = typeofT
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(v => v.IsInjectedDependencyMember())
                    .ToArray();

                if (methods.Length > 0 || properties.Length > 0)
                {
                    var parameter = Expression.Parameter(typeof(T));

                    var propertyInitializers = properties.Select(property => GetInjectedPropertyExpression(parameter, property));
                    var methodInitializers = methods.Select(method => GetInjectedDependencyExpressionFromInstanceMethod(parameter, method));

                    returnValue = AddInitializers(returnValue, propertyInitializers.Concat(methodInitializers));
                }

                return returnValue;
            }

            internal static IEnumerable<Expression> GetParameterExpressions(MethodBase method)
            {
                return method
                        .GetParameters()
                        .Select(v => GetResolverExpressionFromType(v.ParameterType));
            }

            internal static MethodCallExpression GetResolverExpressionFromType(Type type)
            {
                MethodCallExpression retval;

                if (!type.IsValueType)
                {
                    retval = Expression.Call(null, GenericResolve.MakeGenericMethod(type));
                }
                else
                {
                    retval = Expression.Call(null, ReturnDefaultValueOf.MakeGenericMethod(type));
                }

                return retval;
            }

            internal static Expression<Func<T>> GetInjectedDependencyExpression(ConstructorInfo constructor)
            {
                return Expression.Lambda<Func<T>>(Expression.New(constructor, GetParameterExpressions(constructor)));
            }

            internal static Expression<Action<T>> GetInjectedPropertyExpression(ParameterExpression instance, PropertyInfo property)
            {
                var propertyExpression = Expression.Property(instance, property);
                var valueExpression = Expression.Call(GenericResolve.MakeGenericMethod(property.PropertyType));
                var assignmentExpression = Expression.Assign(propertyExpression, valueExpression);
                return Expression.Lambda<Action<T>>(assignmentExpression, instance);
            }

            internal static Expression<Action<T>> GetInjectedDependencyExpressionFromInstanceMethod(ParameterExpression instance, MethodInfo method)
            {
                return Expression.Lambda<Action<T>>(Expression.Call(instance, method, GetParameterExpressions(method)), instance);
            }

            internal static Expression<Func<T>> GetDefaultImplementationConstructorExpression()
            {
                Expression<Func<T>> returnValue;

                if (false)
                {
                    // find an implementer
                }
                else
                {
                    returnValue = (() => ThrowInterfaceException());
                }

                return returnValue;
            }

            static T ThrowInterfaceException()
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, ErrorUnableToResolveInterface, FancyTypeName(typeof(T))));
            }

            static T ThrowMissingConstructorException()
            {
                throw CreateException(string.Format(CultureInfo.CurrentCulture, ErrorMissingConstructor, FancyTypeName(typeof(T))));
            }

            internal static Expression<Func<T>> AddLifetimeManager(Expression<Func<T>> resolverExpression, ILifetimeManager<T> manager)
            {
                Expression<Func<T>> returnValue;

                if (manager == null)
                {
                    returnValue = resolverExpression;
                }
                else
                {
                    manager.SetResolver(resolverExpression.Compile());
                    returnValue = () => manager.GetValue();
                }

                return returnValue;
            }
        }
    }

    /// <summary>
    /// <para>This attribute is used by the auto-resolver to indicate that a dependency should be inserted.</para>
    /// <para>When attached to a constructor, that constructor will be selected by the auto-resolver.</para>
    /// <para>When attached to a method, that method will be called by the auto-resolver and each of the arguments will be fulfilled by the auto-resolver.
    /// i.e. MyClass.InjectStuff(IRepository repo) would be called with MyClass.InjectStuff(Injector.Resolve&lt;IRepository&gt;())</para>
    /// <para>When attached to a property or field, the dependency will be injected by setting the property/field to the result of a Resolve call.
    /// i.e. MyClass.MyInjectedRepositoryProperty = Injector.Resolve&lt;IRepository&gt;()</para>
    /// </summary>
    /// <example>
    /// <para>
    /// When attached to a constructor, this attribute will cause the auto-resolver to selected that constructor over any other.  In the example, the
    /// constructor with 2 parameters would be selected.
    /// <code source="..\DocumentationSampleCode\InjectDependencyHereAttributeUsage.cs" language="cs" region="Constructors" title="Constructor Usage"/>
    /// If the auto-resolver finds this attribute on more than one constructor, the auto-resolved will select one (it is undefined as to which one will be picked) and no error will be thrown.
    /// </para>
    /// </example>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class InjectDependencyHereAttribute : Attribute { }
}
