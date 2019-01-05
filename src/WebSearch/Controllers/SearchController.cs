using System.Web.Http;
using AzureSearchDocumentClassiferLib;

namespace WebSearch.Controllers
{
    public class SearchController : ApiController
    {
        // POST api/<controller>
        public SearchResult[] Post(SearchRequest request)
        {
            var result = AzureSearchHelper.DoSearch(request);
            return result;
        }

        [Route("api/Search/MoreLikeThis/{docId}")]
        [HttpGet]
        public SearchResult[] MoreLikeThis(string docId)
        {
            return AzureSearchHelper.MoreLikeThis(docId);
        }

    }
}
