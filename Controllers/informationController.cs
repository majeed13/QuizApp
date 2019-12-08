using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace OnlineQuizApp.Controllers
{
    public class informationController : ApiController
    {
        // GET: api/information
        public IEnumerable<string> Get()
        {
            return new string[] { "Status=OK", "Please add a valid token to get quiz information" };
        }

        // GET: api/information/_303ff46f_28b3_4870_afa5_a31d6831a991
        public HttpResponseMessage Get(string id)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("quizcontainer");
            var newBlockBlob = blobContainer.GetBlockBlobReference(id);
            if (!newBlockBlob.Exists())
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent("Quiz with passed in token cannot be found");
                return response;
            }
            string contents = newBlockBlob.DownloadText();
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(contents, Encoding.UTF8, "application/json");
            return response;
        }

        // POST: api/information
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/information/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/information/5
        public void Delete(int id)
        {
        }
    }
}
