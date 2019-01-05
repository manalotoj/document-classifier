using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AzureSearchDocumentClassiferLib;

namespace WebSearch.Controllers
{
    public class CategoryController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<DocumentCategoryPoco> Get()
        {
            return AzureStorageHelper.GetTableRows();
        }

        // POST api/<controller>
        public void Post(DocumentCategoryPoco value)
        {
            try
            {
                AzureStorageHelper.AddRowToTable(value);
            }
            catch (Exception ex)
            {
                HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(ex.Message),
                    ReasonPhrase = ex.Message
                };
                throw new HttpResponseException(msg);
            }
        }
        
        // DELETE api/<controller>/5
        public void Delete(DocumentCategoryPoco value)
        {
            AzureStorageHelper.RemoveRowFromTable(value);
        }
    }
}