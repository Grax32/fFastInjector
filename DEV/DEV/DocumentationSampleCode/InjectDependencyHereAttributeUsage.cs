using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fFastInjector;

namespace DocumentationSampleCode
{
    public class InjectDependencyHereAttributeUsage
    {
        public void DemonstrateUsage()
        {

        }

        #region Constructors
        public class MyClass
        {
            public MyClass() { }

            [InjectDependencyHere]
            public MyClass(Person owner, Place location) { }

            public MyClass(Person owner, Place location, Thing item) { }

        }
        #endregion

        public class Person { }
        public class Place { }
        public class Thing { }
    }
}
