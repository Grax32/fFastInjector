using System;
using System.Collections.Generic;
using fFastInjector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fFastInjectorTests
{
    [TestClass]
    public class CovariantResolverTests
    {
        [TestMethod]
        public void GenericResolverTest()
        {
            Injector.SetGenericResolver(typeof(IEnumerable<>), typeof(List<>));
            Injector.SetGenericResolver(typeof(IEnumerable<>), typeof(List<>), typeof(Cat));
            Injector.SetGenericResolver(typeof(IEnumerable<>), typeof(AnimalList<>), typeof(Animal));
            Injector.SetGenericResolver(typeof(IEnumerable<>), typeof(CatList<>), typeof(Cat));

            var whatevers = Injector.Resolve<IEnumerable<Other>>();
            var animals = Injector.Resolve<IEnumerable<Animal>>();
            var cats = Injector.Resolve<IEnumerable<Tiger>>();
            var dogs = Injector.Resolve<IEnumerable<Dog>>();

            Assert.IsInstanceOfType(whatevers, typeof(List<Other>));
            Assert.IsInstanceOfType(animals, typeof(AnimalList<Animal>));
            Assert.IsInstanceOfType(cats, typeof(CatList<Tiger>));
            Assert.IsInstanceOfType(dogs, typeof(List<Dog>));

            Injector.SetGenericResolver(typeof(IEnumerable<>), typeof(List<>), typeof(Cat));
            cats = Injector.Resolve<IEnumerable<Tiger>>();
            Assert.IsInstanceOfType(cats, typeof(List<Tiger>));

        }

        [TestMethod]
        public void GenericResolverExceptionTest()
        {
            Exception ex = null;
            try
            {
                Injector.SetGenericResolver(typeof(IEnumerable<>), typeof(List<>), typeof(Cat), typeof(Cat));
            }
            catch (Exception exc)
            {
                ex = exc;
            }

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("must specify an artificial constraint"));
        }

        class Other { }

        class Animal { }

        class Cat : Animal { }
        class Tiger : Cat { }

        class Dog : Animal { }

        class CatList<T> : List<T> where T : Cat { }
        class AnimalList<T> : List<T> where T : Animal { }
    }
}
