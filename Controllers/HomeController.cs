using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using OnlineQuizApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using System.Configuration;

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

            // Creating a new Student registration process
            Student user = new Student(quiz.email, quiz.name);

            // Creating test registration and adding it into the student's profile?
            Registration registration = new Registration()
            {
                token = Guid.NewGuid(),
                studentName = user.RowKey,
                quizName = quiz.quizName,
                registerDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
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
            QuizResponse q = (QuizResponse)Session["SessionQuestions"];
            SessionQuiz sQuiz = (SessionQuiz)Session["SessionQuiz"];
            Registration registration = (Registration)Session["SessionRegistration"];
            Student user = (Student)Session["SessionUser"];

            // create the JSON MODEL to upload to blob storage
            JsonModel blob = new JsonModel
            {
                name = user.RowKey,
                quizCategory = sQuiz.quizName,
                finalScore = ans.correctAnswers.ToString() + " out of 10"
            };
            // fill questions list for JSON MODEL
            for (int i = 0; i < q.Results.Length; i++)
            {
                QuestionModel question = new QuestionModel();
                question.description = q.Results[i].Question;
                question.choices = q.Results[i].IncorrectAnswers.ToList();
                question.choices.Add(q.Results[i].CorrectAnswer);
                question.correct = q.Results[i].CorrectAnswer;
                blob.questions.Add(question);
                blob.userChoice = ans.userAnswers[i];
            }
            uploadBlob(blob);
            // call method to upload blob to storage container
            ViewBag.questionModel = q;
            ViewBag.ansList = ans.userAnswers;
            ViewBag.cat = sQuiz.quizName;
            ViewBag.score = ans.correctAnswers.ToString() + " correct out of 10";

            // retrieve storage account access info for students
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableRef = tableClient.GetTableReference("UserTable");  // get the reference to user table
            tableRef.CreateIfNotExists();   // create a table if there was no reference

            Console.WriteLine("-------- Registration Information --------");
            Console.WriteLine("Token: " + registration.token.ToString());
            Console.WriteLine("Student name: " + registration.studentName);
            Console.WriteLine("Quiz type: " + registration.quizName);
            Console.WriteLine("Registration Date: " + registration.registerDate);
            Console.WriteLine("Score: " + ans.correctAnswers);

            // if the user already exists, it'll just merge (registration time updated)
            // if the user does not exist, it'll insert the new user
            tableRef.Execute(TableOperation.InsertOrMerge(user));

            string value = registration.registerDate.ToString() + "/" + registration.quizName + "/" + ans.correctAnswers;
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

            Session["JsonSession"] = blob;
            sendEmailAsync();

            return View();
        }

        public ActionResult About()
        {
            // create a user obj to bind user input info
            Student sModel = new Student();
            // bind the fields in the from VIEW

            ViewBag.Message = "Your application description page.";
            // retrieve storage account access info for students
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableRef = tableClient.GetTableReference("UserTable");  // get the reference to user table
            tableRef.CreateIfNotExists();   // create a table if there was no reference


            return View(sModel);
        }


        public ActionResult Display(Student st)
        {
            // retrieve storage account access info for students
            var tableClient = storageAccount.CreateCloudTableClient();
            var tableRef = tableClient.GetTableReference("UserTable");  // get the reference to user table
            tableRef.CreateIfNotExists();   // create a table if there was no reference
            TableQuery<DynamicTableEntity> q = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, st.PartitionKey)
                );
            List<List<string>> quizResults = new List<List<string>>();
            var result = tableRef.ExecuteQuery(q);
            if (result.Count() != 0)
            {
                foreach (DynamicTableEntity c in result)
                {
                    ViewBag.queryEmail = "Query Email: " + c.PartitionKey;
                    ViewBag.queryName = "Query Name: " + c.RowKey;
                    ViewBag.queryCount = c.Properties.Count;
                    foreach (var pair in c.Properties)
                    {
                        //QueryResult_TextBox.Text += pair.Key + ": " + pair.Value.StringValue + Environment.NewLine;
                        string[] v = pair.Value.StringValue.Split('/');
                        List<string> toAdd = new List<string>();
                        toAdd.Add(v[0]);
                        toAdd.Add(v[1]);
                        toAdd.Add(v[2]);
                        toAdd.Add(pair.Key);
                        quizResults.Add(toAdd);
                    }
                    //QueryResult_TextBox.Text += "-----------------------------------" + Environment.NewLine;
                }
            }
            else
            {
                ViewBag.zero = "No matching entries in storage";
            }
            ViewBag.queryResults = quizResults;
            return View(st);
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

        private void uploadBlob(JsonModel j)
        {
            // serialize the JSON MODEL object
            var json = JsonConvert.SerializeObject(j);
            // grab registration for token
            Registration r = (Registration)Session["SessionRegistration"];
            // create the clients needed to access blob
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("quizcontainer");
            blobContainer.CreateIfNotExists();
            // give a unique name to the blob
            var newBlockBlob = blobContainer.GetBlockBlobReference(
                "_" + r.token.ToString().Replace("-","_")
                );
            newBlockBlob.Properties.ContentType = "application/json";
            using (MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(json)))
            {
                newBlockBlob.UploadFromStream(ms);
            }
        }

        private async Task sendEmailAsync()
        {
            string senderEmail = ConfigurationManager.AppSettings.Get("Email");
            string senderInfo = ConfigurationManager.AppSettings.Get("SenderInfo");

            AnswerModel ans = (AnswerModel)Session["SessionAnswerModel"];
            QuizResponse q = (QuizResponse)Session["SessionQuestions"];
            SessionQuiz sQuiz = (SessionQuiz)Session["SessionQuiz"];
            JsonModel test = (JsonModel)Session["JsonSession"];
            Registration register = (Registration)Session["SessionRegistration"];

            try
            {
                var apiKey = ConfigurationManager.AppSettings.Get("SendGridAPIKey");
                var client = new SendGridClient(apiKey);

                var msg = new SendGrid.Helpers.Mail.SendGridMessage();

                var from = new SendGrid.Helpers.Mail.EmailAddress();
                from.Email = senderEmail;
                from.Name = senderInfo;
                msg.From = from;
                msg.TemplateId = ConfigurationManager.AppSettings.Get("TemplateID");
                msg.AddTo(sQuiz.email);

                var message = new SendGrid.Helpers.Mail.Personalization
                {
                    TemplateData = new {
                        name = sQuiz.name,
                        quiz = sQuiz.quizName,
                        score = ans.correctAnswers,
                        numOfQuestions = ans.totalSubmitted,
                        date = register.registerDate,
                        token = register.token,
                        question1 = test.questions[0].description,
                        question2 = test.questions[1].description,
                        question3 = test.questions[2].description,
                        question4 = test.questions[3].description,
                        question5 = test.questions[4].description,
                        question6 = test.questions[5].description,
                        question7 = test.questions[6].description,
                        question8 = test.questions[7].description,
                        question9 = test.questions[8].description,
                        question10 = test.questions[9].description,
                        correctAnswer1 = q.Results[0].CorrectAnswer,
                        correctAnswer2 = q.Results[1].CorrectAnswer,
                        correctAnswer3 = q.Results[2].CorrectAnswer,
                        correctAnswer4 = q.Results[3].CorrectAnswer,
                        correctAnswer5 = q.Results[4].CorrectAnswer,
                        correctAnswer6 = q.Results[5].CorrectAnswer,
                        correctAnswer7 = q.Results[6].CorrectAnswer,
                        correctAnswer8 = q.Results[7].CorrectAnswer,
                        correctAnswer9 = q.Results[8].CorrectAnswer,
                        correctAnswer10 = q.Results[9].CorrectAnswer,
                        userAnswer1 = ans.userAnswers[0],
                        userAnswer2 = ans.userAnswers[1],
                        userAnswer3 = ans.userAnswers[2],
                        userAnswer4 = ans.userAnswers[3],
                        userAnswer5 = ans.userAnswers[4],
                        userAnswer6 = ans.userAnswers[5],
                        userAnswer7 = ans.userAnswers[6],
                        userAnswer8 = ans.userAnswers[7],
                        userAnswer9 = ans.userAnswers[8],
                        userAnswer10 = ans.userAnswers[9]
                    }
                };

                message.Tos = new List<SendGrid.Helpers.Mail.EmailAddress>();
                message.Tos.Add(new SendGrid.Helpers.Mail.EmailAddress { Email = sQuiz.email, Name = sQuiz.name });

                msg.Personalizations = new List<SendGrid.Helpers.Mail.Personalization>() { message };

                var response = await client.SendEmailAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
