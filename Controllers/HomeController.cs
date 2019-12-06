using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
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
        private CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

        // random for shuffling choices
        private Random rng = new Random();
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
              
            if (quiz == null || string.IsNullOrEmpty(quiz.name))
            {
                TempData["message"] = "Invalid details. Please re-enter your information and try again";
                return RedirectToAction("Index");
            }

            // retrieve storage account access info for students
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableRef = tableClient.GetTableReference("UserTable");  // get the reference to user table
            tableRef.CreateIfNotExists();   // create a table if there was no reference

            // Creating a new Student registration process
            Student user = new Student(quiz.email, quiz.name)
            {
                name = quiz.name,
                email = quiz.email
            };

            // Testing Student object
            Console.WriteLine("New User Created");
            Console.WriteLine("Student User Name: " + user.name);
            Console.WriteLine("Student email: " + user.email);
            Console.WriteLine("------------------------------");

            // if the user already exists, it'll just merge (registration time updated)
            // if the user does not exist, it'll insert the new user
            tableRef.Execute(TableOperation.InsertOrMerge(user));

            // Creating test registration and adding it into the student's profile?
            Registration registration = new Registration()
            {
                //var token = Guid.NewGuid();
                studentName = user.name,
                quizName = quiz.quizName,
                registerDate = DateTime.UtcNow
                //var score = 0;
            };

            QuizResponse q = null;
            using (var client = new HttpClient())
            {
                // set the base url address to get geo cords of passed in city
                client.BaseAddress = new Uri("https://opentdb.com/api.php?amount=10&category=18&difficulty=easy&type=multiple");
                // get the response message for the city desired and using the correct API key
                HttpResponseMessage response = client.GetAsync("").Result;
                string result = response.Content.ReadAsStringAsync().Result;
                q = JsonConvert.DeserializeObject<QuizResponse>(result);
                this.Session["TOKEN"] = Guid.NewGuid(); //registration.token;
            }

            //this.Session["SessionUser"] = user;
            //this.Session["SessionRegistration"] = registration;
            

            this.Session["SessionQuestions"] = q;
            return RedirectToAction("QuizPage", new {@token = Session["TOKEN"]});
        }


        public ActionResult QuizPage(Guid token, int? qNum)
        {
            /**
            if(token == null)
            {
                TempData["message"] = "Invalid token. Please register and try again.";
                return RedirectToAction("Index");
            }
            */
            // check if the expiration time has passed
            QuizResponse model = (QuizResponse)Session["SessionQuestions"];
            //TempData["q"] = model;
            if (qNum.GetValueOrDefault() < 1 )
            {
                qNum = 1;
            }
            var ques = model.Results[(int)qNum - 1].Question;
            ViewBag.question = ques;
            ViewBag.qNum = qNum;
            List<string> list = model.Results[(int)qNum - 1].IncorrectAnswers.ToList();
            list.Add(model.Results[(int)qNum - 1].CorrectAnswer);
            Shuffle(list);
            ViewBag.qList = list;
            return View(model);
        }

        public ActionResult RecordAnswer(AnswerModel ans, int? qNum)
        {
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

        private void Shuffle(List<string> list )
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var val = list[k];
                list[k] = list[n];
                list[n] = val;
            }
        }
    }
}
