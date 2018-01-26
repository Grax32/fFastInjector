using System;
using fFastInjector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fFastInjectorTests
{
    [TestClass]
    public class FluentTests
    {
        [TestMethod]
        public void FluentBeforeAfter()
        {
            Injector
                .ForType<FluentTestConcrete>()
                .AddPropertyInjector(v => v.SpecialNumber, () => 17)
                .AddPropertyInjector(v => v.SomeProperty)
                .As(Injector.ForType<IFluentTest>())
                .AddPropertyInjector(v => v.IFaceProperty, () => "Interface");

            var resolved = Injector.Resolve<IFluentTest>();

            Assert.AreEqual("Interface", resolved.IFaceProperty);
            Assert.IsInstanceOfType(resolved, typeof(FluentTestConcrete));
            Assert.AreEqual(17, ((FluentTestConcrete)resolved).SpecialNumber);
        }

        interface IFluentTest
        {
            string IFaceProperty { get; set; }
        }

        class FluentTestConcrete : IFluentTest
        {
            public int SpecialNumber { get; set; }

            public Other SomeProperty { get; set; }

            public string IFaceProperty { get; set; }
        }

        class Other{}
    }
}
