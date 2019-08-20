using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace fFastInjector
{
    internal static class Functions
    {
        internal static string FancyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                return type.Name.Split('`')[0] +
                    string.Format(CultureInfo.InvariantCulture, "<{0}>", string.Join(",", type.GetGenericArguments().Select(v => FancyTypeName(v)).ToArray()));
            }

            return type.Name;
        }

        internal static Exception CreateExceptionInternal(Exception exception) =>
            CreateExceptionInternal(exception.Message, exception);

        internal static Exception CreateExceptionInternal(string message, Exception innerException = null) =>
            Injector.CreateNewException(message, innerException);

        internal static T ArgumentNullGuard<T>(T argumentValue, string argumentName)
            where T : class
        {
            return argumentValue ?? throw CreateExceptionInternal(new ArgumentNullException(argumentName));
        }

        [Conditional("DEBUG")]
        internal static void Trace(string message) => Console.WriteLine(message);

        /// <summary>
        /// Return a new expression where originalExpression has been replaced by replacementExpression
        /// </summary>
        /// <typeparam name="TExpression"></typeparam>
        /// <param name="thisExpression"></param>
        /// <param name="originalExpression"></param>
        /// <param name="replacementExpression"></param>
        /// <returns></returns>
        internal static TExpression Replace<TExpression>(this TExpression thisExpression, Expression originalExpression, Expression replacementExpression)
                     where TExpression : Expression
        {
            return (TExpression)(new ReplaceVisitor(originalExpression, replacementExpression)).Visit(thisExpression);
        }

        internal static TExpression ReplaceParameterWith<TExpression>(this TExpression thisExpression, Expression replacementExpression)
             where TExpression : LambdaExpression
        {
            var originalExpression = thisExpression.Parameters.Where(v => v.GetType() == replacementExpression.GetType()).Single();
            return (TExpression)(new ReplaceVisitor(originalExpression, replacementExpression)).Visit(thisExpression);
        }
    }
}
