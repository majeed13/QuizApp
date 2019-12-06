using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineQuizApp.Models
{
    public class AnswerModel
    {
        public AnswerModel()
        {
            userAnswers = new List<string>(10);
            correctAnswers = 0;
            totalSubmitted = 0;
        }
        public List<string> userAnswers { get; set; }
        public int correctAnswers { get; set; }
        public int totalSubmitted { get; set; }
    }
}