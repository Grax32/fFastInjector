using System;
using System.Collections.Generic;
using System.Linq;
using fFastInjector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fFastInjectorTests
{
    [TestClass]
    public class ThrowExceptionTests
    {
        [TestMethod]
        public void ThrowInterfaceExceptionTest()
        {
            Exception ex = null;
            try
            {
                Injector.Resolve<IThrowInterface>();
            }
            catch (Injector.fFastInjectorException exc)
            {
                ex = exc;
            }

            Assert.IsInstanceOfType(ex, typeof(Injector.fFastInjectorException));
            Assert.IsTrue(ex.Message.Contains("Interface"));
        }

        [TestMethod]
        public void ThrowGenericInterfaceExceptionTest()
        {
            Exception ex = null;
            try
            {
                Injector.Resolve<IQueryable<IThrowInterface>>();
            }
            catch (Injector.fFastInjectorException exc)
            {
                ex = exc;
            }

            Assert.IsInstanceOfType(ex, typeof(Injector.fFastInjectorException));
            Assert.IsTrue(ex.Message.Contains("Interface"));
        }

        [TestMethod]
        public void ThrowNoConstructorExceptionTest()
        {
            Exception ex = null;
            try
            {
                Injector.Resolve<NoConstructor>();
            }
            catch (Injector.fFastInjectorException exc)
            {
                ex = exc;
            }

            Assert.IsInstanceOfType(ex, typeof(Injector.fFastInjectorException));
            Assert.IsTrue(ex.Message.Contains("Constructor"));
        }

        interface IThrowInterface { }

        class NoConstructor { private NoConstructor() { } }

        [TestMethod]
        public void ExceptionTypeTest()
        {
            var ex1 = new Injector.fFastInjectorException();
            Assert.IsTrue(ex1.Message.Contains("error"));

            var msg = "Some Error Message";
            var ex2 = new Injector.fFastInjectorException(msg);
            Assert.AreEqual(msg, ex2.Message);
        }

        class BadICollectionImplementer<T> : ICollection<T>
        {
            private BadICollectionImplementer() { }

            public void Add(T item) { throw new NotImplementedException(); }

            public void Clear() { throw new NotImplementedException(); }
            public bool Contains(T item) { throw new NotImplementedException(); }

            public void CopyTo(T[] array, int arrayIndex) { throw new NotImplementedException(); }

            public int Count { get { throw new NotImplementedException(); } }

            public bool IsReadOnly { get { throw new NotImplementedException(); } }

            public bool Remove(T item) { throw new NotImplementedException(); }

            public IEnumerator<T> GetEnumerator() { throw new NotImplementedException(); }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            { throw new NotImplementedException(); }
        }

        class C1 { }

        [TestMethod]
        public void ExceptionInAutoResolver()
        {
            Injector.SetGenericResolver(typeof(ICollection<>), typeof(BadICollectionImplementer<>));

            Exception ex = null;
            try
            {
                var resolved = Injector.Resolve<ICollection<C1>>();
            }
            catch (Exception exc)
            {
                ex = exc;
            }

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("private constructor"));
        }
    }
}
