using System;
using fFastInjector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fFastInjectorTests
{
    [TestClass]
    public class InjectionPointsTests
    {
        [TestMethod]
        public void TestInjectionPoints()
        {
            var resolved = Injector.Resolve<InjectionPointsClass>();

            Assert.IsNotNull(resolved.SomeProperty);
            Assert.IsNotNull(resolved.SomeMethodProperty);
        }

        class InjectionPointsClass
        {
            public InjectionPointsClass() { throw new Exception("don't call this one"); }

            [InjectDependencyHere]
            public InjectionPointsClass(InjectSomething ic) { }

            public InjectionPointsClass(InjectionPointsClass ic, string s2) { throw new Exception("don't call this one"); }

            [InjectDependencyHere]
            public InjectSomething SomeProperty { get; set; }

            public InjectSomething SomeMethodProperty { get; private set; }

            [InjectDependencyHere]
            public void SomeMethod(InjectSomething ic)
            {
                SomeMethodProperty = ic;
            }
        }

        class InjectSomething { }


    }
}
