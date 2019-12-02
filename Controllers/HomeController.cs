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
            return View(curQuiz);
        }

        public ActionResult Instruction(SessionQuiz quiz)
        {
            if(quiz != null)
            {
                ViewBag.quizName = quiz.quizName;
                ViewBag.quizDescription = "This is a sample description of one of the 3 types.";
                ViewBag.quizDuration = "10";
                ViewBag.numberOfQuestions = "10";
            }
            // Handle null quiz model passed in from Index home page
            else
            {
                ViewBag.quizName = "None";
                ViewBag.quizDescription = "This is a sample description of one of the 3 types.";
                ViewBag.quizDuration = "10";
                ViewBag.numberOfQuestions = "10";
            }
            return View(quiz);
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