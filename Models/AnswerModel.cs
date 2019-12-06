using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineQuizApp.Models
{
    public class Choices
    {
        public string option { get; set; }
        public bool isChecked { get; set; }

    }

    public class AnswerModel
    {
        public string userAnswer { get; set; }

    }
}