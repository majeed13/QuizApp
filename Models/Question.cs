using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineQuizApp.Models
{
    public class Question : TableEntity
    {
        public string description { get; set; }
        public string choices { get; set; }
        public string correct { get; set; }
    }
}