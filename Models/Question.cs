using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineQuizApp.Models
{
    public class Question
    {
        public string description { get; set; }
        public List<string> choices { get; set; }
        public string correct { get; set; }
    }
}