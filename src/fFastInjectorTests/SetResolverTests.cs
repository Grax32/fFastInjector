using System;
using System.Linq.Expressions;
using fFastInjector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fFastInjectorTests
{
    [TestClass]
    public class SetResolverTests
    {
        static I10 MyI10Instance = new C10();

        [ClassInitialize]
        static public void InitClass(TestContext ctx)
        {
            Injector.SetResolver<I1, C1>();
            Injector.SetResolver<I2, C2>(Injector.ToPropertyAssignmentExpression((C2 v) => v.Name, () => "Horace"));
            Injector.SetResolver<I3>(() => new C3());
            Injector.SetResolver<I5, C5>(SingletonLifetimeManager<I5>.Instance);
            Injector.SetResolver<I6, C6>(SingletonLifetimeManager<I6>.Instance, Injector.ToPropertyAssignmentExpression((C6 v) => v.Name, () => "Michael"));
            Injector.SetResolver<I7>(SingletonLifetimeManager<I7>.Instance, () => new C7());

            Injector.SetSingletonResolver<I9, C9>();
            Injector.SetSingletonResolverAsInstance<I10>(MyI10Instance);
        }

        [TestMethod]
        public void SetResolverTest1()
        {
            var result = Injector.Resolve(typeof(I1));
            Assert.IsInstanceOfType(result, typeof(C1));
        }

        [TestMethod]
        public void SetResolverTest2()
        {
            var result = Injector.Resolve<I2>();

            Assert.IsInstanceOfType(result, typeof(C2));
            Assert.AreEqual("Horace", ((C2)result).Name);
        }

        [TestMethod]
        public void SetResolverTest3()
        {
            var result = Injector.Resolve<I3>();

            Assert.IsInstanceOfType(result, typeof(C3));
        }

        [TestMethod]
        public void SetResolverTest4()
        {
            var result = Injector.Resolve<I4>();

            Assert.IsInstanceOfType(result, typeof(C4));
            var myI2 = ((C4)result).MyI2;
            Assert.IsNotNull(myI2);
        }

        [TestMethod]
        public void SetResolverTest5()
        {
            var result1 = Injector.Resolve<I5>();
            var result2 = Injector.Resolve<I5>();
            var result3 = Injector.Resolve<I5>();

            Assert.IsInstanceOfType(result1, typeof(C5));
            Assert.AreSame(result1, result2);
            Assert.AreSame(result2, result3);
        }

        [TestMethod]
        public void SetResolverTest6()
        {
            var result1 = Injector.Resolve<I6>();
            var result2 = Injector.Resolve<I6>();
            var result3 = Injector.Resolve<I6>();

            Assert.IsInstanceOfType(result1, typeof(C6));
            Assert.AreSame(result1, result2);
            Assert.AreSame(result2, result3);
            Assert.AreEqual("Michael", ((C6)result1).Name);
        }

        [TestMethod]
        public void SetResolverTest7()
        {
            var result1 = Injector.Resolve<I7>();
            var result2 = Injector.Resolve<I7>();
            var result3 = Injector.Resolve<I7>();

            Assert.IsInstanceOfType(result1, typeof(C7));
            Assert.AreSame(result1, result2);
            Assert.AreSame(result2, result3);
        }

        [TestMethod]
        public void SetResolverTest8()
        {
            var result1 = Injector.Resolve<I8>();
            var result2 = Injector.Resolve<I8>();
            var result3 = Injector.Resolve<I8>();

            Assert.IsInstanceOfType(result1, typeof(C8));
            Assert.AreSame(result1, result2);
            Assert.AreSame(result2, result3);

            var myI2 = ((C8)result1).MyI2;
            Assert.IsNotNull(myI2);
        }

        [TestMethod]
        public void SetResolverTest9()
        {
            var result1 = Injector.Resolve<I9>();
            var result2 = Injector.Resolve<I9>();
            var result3 = Injector.Resolve<I9>();

            Assert.IsInstanceOfType(result1, typeof(C9));
            Assert.AreSame(result1, result2);
            Assert.AreSame(result2, result3);
        }

        [TestMethod]
        public void SetResolverTest10()
        {
            var result1 = Injector.Resolve<I10>();
            var result2 = Injector.Resolve<I10>();
            var result3 = Injector.Resolve<I10>();

            Assert.IsInstanceOfType(result1, typeof(C10));
            Assert.AreSame(result1, result2);
            Assert.AreSame(result2, result3);
            Assert.AreSame(MyI10Instance, result1);
        }

        [TestMethod]
        public void SetResolverTest11()
        {
            var type = typeof(C11);

            var resolved1 = Injector.Resolve(type);
            var resolved2 = Injector.Resolve(type);

            Assert.IsInstanceOfType(resolved1, type);
            Assert.IsInstanceOfType(resolved2, type);
        }

        interface I1 { }
        interface I2 { }
        interface I3 { }
        interface I4 { }
        interface I5 { }
        interface I6 { }
        interface I7 { }
        interface I8 { }
        interface I9 { }
        interface I10 { }
        interface I11 { }

        class C1 : I1 { }

        class C2 : I2 { public string Name { get; set; } }

        class C3 : I3 { }

        class C4 : I4
        {
            public C4(I2 param1, I3 param2) { MyI2 = param1; }
            public I2 MyI2 { get; set; }
        }

        class C5 : I5 { }

        class C6 : I6 { public string Name { get; set; } }

        class C7 : I7 { }


        class C8 : I8
        {
            public C8(I2 param1, I3 param2) { MyI2 = param1; }
            public I2 MyI2 { get; set; }
        }

        class C9 : I9 { }

        class C10 : I10 { }

        class C11 : I11 { }
    }
}
