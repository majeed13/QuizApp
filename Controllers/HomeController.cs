using Newtonsoft.Json;
using OnlineQuizApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

                if (ViewBag.quizName == "Computer Science")
                {
                    ViewBag.quizDescription = "This is a quiz that tests your basic/general knowledge of Computer Science";
                }
                else if (ViewBag.quizName == "Sports")
                {
                    ViewBag.quizDescription = "This is a quiz that tests your basic/general knowledge of Sports";
                }
                else if (ViewBag.quizName == "Movies")
                {
                    ViewBag.quizDescription = "This is a quiz that tests your basic/general knowledge of Movies";
                }
                //ViewBag.quizDescription = "This is a sample description of one of the 3 types.";
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

        // Two things to do:
        // 1. Check if the user already exists; add them as the user database 
        // 2. Register them for the test
        public ActionResult Register(SessionQuiz quiz)
        {
            if (quiz != null)
            {
                Session["SessionQuiz"] = quiz;
            }
              
            if (quiz == null || string.IsNullOrEmpty(quiz.userName))
            {
                TempData["message"] = "Invalid details. Please re-enter your information and try again";
                return RedirectToAction("Index");
            }

            // Check if user exists in the user database
            // add user to database table
            // Database portion not done yet


            /*
             * if (user == null)
             * {
             *     Create a new Student object (Done below)
             *     and add it into the database
             * }
            */

            // Creating a new Student registration process
            Student user = new Student()
            {
                userName = quiz.userName,
                email = quiz.email
            };

            // Testing Student object
            Console.WriteLine("New User Created");
            Console.WriteLine("Student User Name: " + user.userName);
            Console.WriteLine("Student email: " + user.email);
            Console.WriteLine("------------------------------");

            // Add newly created user into the database

            // Creating test registration and adding it into the student's profile?
            Registration registration = new Registration()
            {
                studentName = user.userName,
                quizName = quiz.quizName,
                registerDate = DateTime.UtcNow
            };

            user.registrations.Add(registration);   // Adding the quiz registration into student's registration list

            // Testing Registration object
            foreach (Registration regi in user.registrations)
            {
                Console.WriteLine("New Registration for User");
                Console.WriteLine("Student User Name: " + regi.studentName);
                Console.WriteLine("Quiz Name: " + regi.quizName);
                Console.WriteLine("Registration Date (UTC): " + regi.registerDate);
            }

            Console.WriteLine("------------------------------");

            QuizResponse q = null;
            using (var client = new HttpClient())
            {
                // set the base url address to get geo cords of passed in city
                client.BaseAddress = new Uri("https://opentdb.com/api.php?amount=10&category=18&difficulty=easy&type=multiple");
                // get the response message for the city desired and using the correct API key
                HttpResponseMessage response = client.GetAsync("").Result;
                string result = response.Content.ReadAsStringAsync().Result;
                q = JsonConvert.DeserializeObject<QuizResponse>(result);
                this.Session["QuizResponse"] = q;
            }
                return RedirectToAction("QuizPage", new {@token = Session["QuizResponse"] });
        }


        public ActionResult QuizPage(QuizResponse q)
        {
            /**
            if(token == null)
            {
                TempData["message"] = "Invalid token. Please register and try again.";
                return RedirectToAction("Index");
            }
            */
            // check if the expiration time has passed
            QuizResponse model = q;
            ViewBag.question = model.Results[0].Question;
            return View(model);
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
