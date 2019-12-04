using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineQuizApp.Models
{
    public class Student
    {
        //public string firstName { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public List<Registration> registrations = new List<Registration>();
    }
}
