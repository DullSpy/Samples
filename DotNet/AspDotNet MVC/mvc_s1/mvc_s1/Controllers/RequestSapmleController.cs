using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using mvc_s1.Models;

namespace mvc_s1.Controllers
{
    public class RequestSapmleController : Controller
    {
        // GET: RequestSapmle
        public ActionResult Index(string name)
        {
            try
            {
                ViewBag.Name = name;
            }
            catch (Exception ex)
            {
                
                
            }
            
            return View();
        }

        public ActionResult Multi(FormCollection form)
        {
            ViewBag.Name = form["name"];
            ViewBag.Age = form["age"];
            return View("Index");
        }

        public ActionResult TranPerson(Person person)
        {
            return View("PersonV",person);
        }
    }
}