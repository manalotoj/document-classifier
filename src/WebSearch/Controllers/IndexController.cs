using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using AzureSearchDocumentClassiferLib;

namespace WebSearch.Controllers
{
    public class IndexController : ApiController
    {
        // DELETE api/<controller>
        public void Delete(string id)
        {
            AzureSearchHelper.DeleteDocById(id);
        }

        // POST api/<controller>
        public SearchResult[] Post()
        {
            var httpPostedFile = HttpContext.Current.Request.Files["file"];

            if (httpPostedFile == null)
            {
                ThowError("No file uploaded.");
                return null;
            }

            string baseFileName = System.IO.Path.GetFileName(httpPostedFile.FileName);
            byte[] fileBytes = PdfHelper.ReadFully(httpPostedFile.InputStream);
            string content = PdfHelper.GetTextFromPdfBytes(fileBytes);
            if (string.IsNullOrEmpty(content))
            {
                ThowError("No content found for file: " + baseFileName);
                return null;
            }

            // Save original file
            string fileUrl = AzureStorageHelper.UploadBlob(fileBytes, baseFileName);
            if (string.IsNullOrEmpty(fileUrl))
            {
                ThowError("Could not upload file to azure.");
                return null;
            }

            // Add to index
            SearchDocument document = new SearchDocument
            {
                DocId = Guid.NewGuid().ToString(),
                Content = content,
                DocFileName = baseFileName,
                DocUrl = fileUrl,
                InsertDate = DateTime.UtcNow
            };

            if (!AzureSearchHelper.InsertDocument(document))
            {
                ThowError("Could not add document to the index. If this is the first time you are using the index you need to click on the 'Delete all documents and rebuild index button' first.");
            }

            return AzureSearchHelper.CategorizeDocument(document.DocId);
        }

        private static void ThowError(string errMsg)
        {
            HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(errMsg),
                ReasonPhrase = errMsg
            };
            throw new HttpResponseException(msg);
        }
    }
}
