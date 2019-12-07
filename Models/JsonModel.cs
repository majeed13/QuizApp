using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineQuizApp.Models
{
    public class QuestionModel
    {
        public string description { get; set; }
        public List<string> choices { get; set; }
        public string correct { get; set; }
    }

    public class JsonModel
    {
        public JsonModel()
        {
            questions = new List<QuestionModel>();
        }
        public string name { get; set; }
        public string quizCategory { get; set; }
        public List<QuestionModel> questions { get; set; }
        public string userChoice { get; set; }
        public string finalScore { get; set; }
    }
}