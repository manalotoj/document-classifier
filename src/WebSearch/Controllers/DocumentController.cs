using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Web.Http;
using AzureSearchDocumentClassiferLib;

namespace WebSearch.Controllers
{
    public class DocumentController : ApiController
    {
        // DELETE api/<controller>
        public void Delete()
        {
            AzureSearchHelper.ResetIndexes();
        }

        // GET api/<controller>
        public IEnumerable<SearchResult> Get()
        {
            SearchRequest req = new SearchRequest {SearchTerm = "*"};
            return AzureSearchHelper.DoSearch(req);
        }

        // POST api/<controller>
        public string Post()
        {
            var httpPostedFile = HttpContext.Current.Request.Files["file"];
            if (httpPostedFile != null)
            {
                // check for existing file?
                Trace.TraceInformation("File posted to controller: " + httpPostedFile.FileName);
                try
                {
                    return PdfHelper.GetTextFromPdfBytes(PdfHelper.ReadFully(httpPostedFile.InputStream));
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to convert file to text using PdfHelper.");
                    Trace.TraceError(exc.ToString());
                }
            }
            return string.Empty;
        }

        [Route("api/Document/Recategorize/{docId}")]
        [HttpGet]
        public SearchResult[] Recategorize(string docId)
        {
            // Recategorize doc
            return AzureSearchHelper.CategorizeDocument(docId);
        }

        [Route("api/Document/RemoveCategory/{docId}")]
        [HttpPost]
        public void RemoveCategory(string docId, [FromBody]string categoryName)
        {
            AzureSearchHelper.RemoveDocumentCategory(docId, categoryName);
        }

        [Route("api/Document/AddCategory/{docId}")]
        [HttpPost]
        public void AddCategory(string docId, [FromBody]string categoryName)
        {
            AzureSearchHelper.AddDocumentCategory(docId, categoryName);
        }
    }
}
