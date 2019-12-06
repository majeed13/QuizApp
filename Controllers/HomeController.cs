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

            // Creating test registration and adding it into the student's profile?
            Registration registration = new Registration()
            {
                token = Guid.NewGuid(),
                studentName = user.name,
                quizName = quiz.quizName,
                registerDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                score = 0
            };

            Console.WriteLine("-------- Registration Information --------");
            Console.WriteLine("Token: " + registration.token.ToString());
            Console.WriteLine("Student name: " + registration.studentName);
            Console.WriteLine("Quiz type: " + registration.quizName);
            Console.WriteLine("Registration Date: " + registration.registerDate);
            Console.WriteLine("Score: " + registration.score);

            // if the user already exists, it'll just merge (registration time updated)
            // if the user does not exist, it'll insert the new user
            tableRef.Execute(TableOperation.InsertOrMerge(user));

            string value = registration.registerDate.ToString() + "/" + registration.quizName + "/" + registration.score;
            Console.WriteLine("Entity (Token): " + registration.token.ToString());
            Console.WriteLine("Entity Attribute (date/quiz/score): " + value);

            string propertyName = "_" + registration.token.ToString().Replace("-", "_");
            var entity = new DynamicTableEntity(user.PartitionKey, user.RowKey, "*",
                new Dictionary<string, EntityProperty>{
                {propertyName, new EntityProperty(value)},
            });
            
            try
            {
                Console.WriteLine("Inserting Token and its values");
                tableRef.Execute(TableOperation.InsertOrMerge(entity));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Insert Error Occurred");
                Console.WriteLine(ex);
            }
            

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

            this.Session["SessionUser"] = user;
            this.Session["SessionRegistration"] = registration;
            this.Session["SessionAnswerModel"] = new AnswerModel();
            this.Session["SessionQuestions"] = q;
            this.Session["QuizTimeExpire"] = DateTime.UtcNow.AddSeconds(240); 

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
            TempData["qNum"] = qNum;
            TempData["correct"] = model.Results[(int)qNum - 1].CorrectAnswer;
            var ques = model.Results[(int)qNum - 1].Question;
            ViewBag.question = ques;
            ViewBag.qNum = qNum;
            List<string> list = model.Results[(int)qNum - 1].IncorrectAnswers.ToList();
            list.Add(model.Results[(int)qNum - 1].CorrectAnswer);
            Shuffle(list);
            ViewBag.qList = list;
            return View(model);
        }

        [ValidateInput(false)]
        public ActionResult RecordAnswer(FormCollection frm)
        {
            //update Session AnswerModel
            AnswerModel userAns = (AnswerModel)Session["SessionAnswerModel"];
            string ans = frm["answer"].ToString();
            int qNum = (int)TempData["qNum"];
            userAns.userAnswers.Insert((qNum - 1), ans);
            if (ans.Equals((string)TempData["correct"]))
            {
                userAns.correctAnswers++;
            }
            userAns.totalSubmitted++;
            Session["SessionAnswerModel"] = userAns;
            // check if user has submitted 10 total answers
            // if so, go to finish page
            if (userAns.totalSubmitted == 10)
            {
                return RedirectToAction("FinishPage", "Home", new { @token = Session["TOKEN"] } );
            }
            return RedirectToAction("QuizPage", new { @token = Session["TOKEN"], @qNum = qNum + 1 });
        }

        public string Results()
        {
            AnswerModel ans = (AnswerModel)Session["SessionAnswerModel"];
            string toR = "";
            foreach(string s in ans.userAnswers)
            {
                toR += s + " ; ";
            }
            string cor = ans.correctAnswers.ToString();
            return toR + "<br>" + "Number Correct Answers = " + cor;
        }

        public ActionResult FinishPage(Guid token)
        {
            AnswerModel ans = (AnswerModel)Session["SessionAnswerModel"];
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
