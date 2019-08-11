using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExecutableHost
{
    class Program
    {
        static CustomDictionary myDict = new CustomDictionary();

        static void Main(string[] args)
        {
            myDict.Add(typeof(int), () => 5);

            myDict.Add(typeof(DateTime), () => DateTime.Now);


            int count = 1000000;
            Type stringType = typeof(string);
            var watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < count; i++)
            {
                var resolved = (string)GetObject1(stringType);
                someString = resolved;
            }

            Console.WriteLine(watch.ElapsedTicks);
            watch.Restart();

            for (int i = 0; i < count; i++)
            {
                var resolved = (string)GetObject2(stringType);
                someString = resolved;
            }

            Console.WriteLine(watch.ElapsedTicks);
            watch.Restart();

            myDict.Add(typeof(string), () => "Boogity");

            for (int i = 0; i < count; i++)
            {
                var resolved = (string)GetObject1(stringType);
                someString = resolved;
            }

            Console.WriteLine(watch.ElapsedTicks);
            watch.Restart();

            for (int i = 0; i < count; i++)
            {
                var resolved = (string)GetObject2(stringType);
                someString = resolved;
            }

            Console.WriteLine(watch.ElapsedTicks);
            watch.Restart();

        }

        static string someString;

        static object GetObject1(Type objectType)
        {
            var func = myDict.GetValueOrDefault(objectType) ?? DefaultFuncForType(objectType);
            return func();
        }

        static object GetObject2(Type objectType)
        {
            Func<object> outVar;

            if (myDict.TryGetValue(objectType, out outVar))
            {
                return outVar();
            }
            else
            {
                var func = DefaultFuncForType(objectType);
                return func();
            }
        }

        static Func<object> DefaultFuncForType(Type thisType)
        {
            if (thisType.IsClass || thisType.IsInterface)
            {
                return (() => null);
            }
            else
            {
                return (() => Activator.CreateInstance(thisType));
            }
        }
    }



    class CustomDictionary : Dictionary<Type, Func<object>>
    {
        //public Func<Type, int> FindEntry = FindEntryFunc;

        //static Func<Type, int> FindEntryFunc = GetFindEntry();

        //static Func<Type, int> GetFindEntry()
        //{
        //    var type = typeof(CustomDictionary).BaseType;

        //    var findEntryMethod = type.GetMethod("FindEntry", new Type[] { typeof(int) });

        //    var parameterExpression = Expression.Parameter(typeof(Type));
        //    var methodCallExpression = Expression.Call(parameterExpression, findEntryMethod);

        //    return Expression.Lambda<Func<Type, int>>(methodCallExpression, parameterExpression).Compile();
        //}

        public Func<object> GetValueOrDefault(Type type)
        {
            return GetValueOrDefaultFunction(this, type);
        }

        public static Func<CustomDictionary, Type, Func<object>> GetValueOrDefaultFunction = GetFuncForGetValueOrDefault();

        static Func<CustomDictionary, Type, Func<object>> GetFuncForGetValueOrDefault()
        {
            var type = typeof(CustomDictionary).BaseType;

            var method = type.GetMethod("GetValueOrDefault", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Type) }, null);

            var parameterExpression1 = Expression.Parameter(typeof(CustomDictionary));
            var parameterExpression2 = Expression.Parameter(typeof(Type));
            var methodCallExpression = Expression.Call(parameterExpression1, method, parameterExpression2);

            return Expression.Lambda<Func<CustomDictionary, Type, Func<object>>>(methodCallExpression, parameterExpression1, parameterExpression2).Compile();
        }
    }
}
