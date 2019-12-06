using System;
namespace OnlineQuizApp.Models
{
    public class Registration
    {
        public string studentName { get; set; }
        public string quizName { get; set; }
        public string registerDate { get; set; }
        public Guid token { get; set; }
    }
}
