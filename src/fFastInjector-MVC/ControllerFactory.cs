using fFastInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace fFastInjector
{
    public class fFastInjectorControllerFactory : DefaultControllerFactory
    {
        public static void RegisterControllerFactory()
        {
            ControllerBuilder.Current.SetControllerFactory(new fFastInjectorControllerFactory());
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            return (IController)Injector.Resolve(controllerType);
        }
    }
}


