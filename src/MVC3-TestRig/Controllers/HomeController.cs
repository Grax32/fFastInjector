using MVC3_TestRig.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVC3_TestRig.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(INeedSomething needSomething)
        {
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
