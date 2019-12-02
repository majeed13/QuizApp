using OnlineQuizApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OnlineQuizApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            List<string> quizTypes = new List<string>(){ "Computer Science", "Sports", "Movies" };
            ViewBag.Tests = quizTypes;
            SessionQuiz curQuiz = null;
            if (Session["SessionQuiz"] == null)
            {
                curQuiz = new SessionQuiz();
            }
            else
            {
                curQuiz = (SessionQuiz)Session["SessionQuiz"];
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}