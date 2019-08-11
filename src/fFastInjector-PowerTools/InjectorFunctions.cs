using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace fFastInjector
{
    public static class InjectorFunctions
    {
        static Expression<Func<Injector.InjectorFluent<object>>> resolverExpression = () => Injector.SetResolver<object, object>();
        static MethodInfo setResolverMethod = ((MethodCallExpression)resolverExpression.Body).Method.GetGenericMethodDefinition();

        /// <summary>
        ///  Register all types in Assembly as Resolvers for Interfaces that they implement
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Assembly RegisterAllTypesAsImplementedInterfaces(this Assembly assembly)
        {
            var concreteToInterfaceMap = new List<ClassInterfaceMapItem>
                {
                    new ClassInterfaceMapItem{ ConcreteClass=typeof(object), Interface=typeof(IDisposable), BlockType=true }
                };


            foreach (var type in assembly.GetTypes().WhereAvailableForRegistration())
            {
                foreach (var typeInterface in type.GetInterfaces().WhereAvailableForRegistration())
                {
                    // do not register if multiple types implements same interface within this assembly
                    var blockType = concreteToInterfaceMap.Any(v => v.Interface == typeInterface);
                    concreteToInterfaceMap.Add(new ClassInterfaceMapItem { ConcreteClass = type, Interface = typeInterface, BlockType = blockType });
                    if (blockType)
                    {
                        foreach (var item in concreteToInterfaceMap.Where(v => v.Interface == typeInterface))
                        {
                            item.BlockType = true;
                        }
                    }
                }
            }

            foreach (var mapping in concreteToInterfaceMap
                .Where(v => !v.BlockType &&
                            !v.Interface.ContainsGenericParameters &&
                            !v.ConcreteClass.ContainsGenericParameters))
            {
                setResolverMethod.MakeGenericMethod(mapping.Interface, mapping.ConcreteClass).Invoke(null, new object[0]);
            }

            return assembly;
        }

        class ClassInterfaceMapItem
        {
            public Type ConcreteClass;
            public Type Interface;
            public bool BlockType;
        }
    }
}