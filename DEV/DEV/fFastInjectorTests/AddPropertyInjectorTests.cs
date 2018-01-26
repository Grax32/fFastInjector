using System;
using System.Linq.Expressions;
using fFastInjector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fFastInjectorTests
{
    [TestClass]
    public class AddPropertyInjectorTests
    {
        [TestMethod]
        public void AddPropertyInjector1Test()
        {
            Expression<Func<I1, C1>> property1 = v => v.SomeObject1;
            Expression<Func<I1, C1>> property2 = v => v.SomeObject2;
            Expression<Func<I1, C1>> property3 = v => v.SomeObject3;
            Expression<Func<I1, C1>> property4 = v => v.SomeObject4;

            Injector.AddPropertyInjector(property1);
            Injector.AddPropertyInjector(property2, () => new C1(null));

            Injector
                .ForType<I1>()
                .AddPropertyInjector(property3)
                .AddPropertyInjector(property4, () => new C1(null));

            Injector.SetResolver<I1, CI1>();

            var resolved = Injector.Resolve<I1>();

            Assert.IsInstanceOfType(resolved.SomeObject1.Helper, typeof(C1Helper));
            Assert.IsInstanceOfType(resolved.SomeObject3.Helper, typeof(C1Helper));
            Assert.IsNull(resolved.SomeObject2.Helper);
            Assert.IsNull(resolved.SomeObject4.Helper);
        }

        interface I1
        {
            C1 SomeObject1 { get; set; }
            C1 SomeObject2 { get; set; }
            C1 SomeObject3 { get; set; }
            C1 SomeObject4 { get; set; }
        }

        class CI1 : I1
        {

            public C1 SomeObject1 { get; set; }
            public C1 SomeObject2 { get; set; }
            public C1 SomeObject3 { get; set; }
            public C1 SomeObject4 { get; set; }
        }

        class C1
        {
            public C1(C1Helper helper) { this.Helper = helper; }

            public C1Helper Helper { get; set; }
        }

        class C1Helper { }
    }
}
