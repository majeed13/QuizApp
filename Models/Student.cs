using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace OnlineQuizApp.Models
{
    public class Student : TableEntity
    {
        public Student() { }
        public Student(string email, string name)
        {
            PartitionKey = email;
            RowKey = name;
        }

        public string name { get; set; }
        public string email { get; set; }
    }
}
