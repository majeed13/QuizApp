using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace OnlineQuizApp.Controllers
{
    public class quizController : ApiController
    {
        // GET: api/quiz
        public HttpResponseMessage Get()
        {
            string url = CloudConfigurationManager.GetSetting("ComputerScience");
            HttpResponseMessage resp = new HttpResponseMessage();
            using (var client = new HttpClient())
            {
                // set the base url address to get quiz
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = client.GetAsync("").Result;
                resp = response;
            }
            return resp;
        }

        // GET: api/quiz/Sports
        public HttpResponseMessage Get(string id)
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            string url = CloudConfigurationManager.GetSetting(id);
            if(url == null || url.Length == 0)
            {
                resp.StatusCode = HttpStatusCode.NotFound;
                resp.Content = new StringContent("This passed in Category does not Exist" + Environment.NewLine
                    + "Please choose from { ComputerScience, Movies, Sports } Thank you.");
                return resp;
            }
            using (var client = new HttpClient())
            {
                // set the base url address to get quiz
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = client.GetAsync("").Result;
                resp = response;
            }
            return resp;
        }

        // POST: api/quiz
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/quiz/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/quiz/5
        public void Delete(int id)
        {
        }
    }
}
