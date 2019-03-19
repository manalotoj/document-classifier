using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace AzureSearchDocumentClassiferLib
{
    public static class AzureSearchHelper
    {
        private const string doc_index = "categorizeddocs";

        public static void ResetIndexes()
        {
            SearchServiceClient serviceClient = GetServiceClient();
            if (serviceClient.Indexes.Exists(doc_index))
            {
                serviceClient.Indexes.Delete(doc_index);
            }

            var definition = new Index()
            {
                Name = doc_index,
                Fields = new[]
                {
                    new Field("docId", DataType.String) {IsKey = true, IsFilterable = true},
                    new Field("clientId", DataType.String) {IsFilterable = true, IsSortable = true, IsFacetable = true},
                    new Field("docFileName", DataType.String)
                    {
                        IsFilterable = true,
                        IsSearchable = true,
                        IsSortable = true
                    },
                    new Field("docUrl", DataType.String) {IsFilterable = true, IsSortable = true},
                    new Field("content", DataType.String) {IsSearchable = true, Analyzer = AnalyzerName.EnMicrosoft},
                    new Field("categories", DataType.Collection(DataType.String))
                    {
                        IsSearchable = true,
                        IsFilterable = true,
                        IsFacetable = true
                    },
                    new Field("insertDate", DataType.DateTimeOffset)
                    {
                        IsFilterable = true,
                        IsSortable = true,
                        IsFacetable = true
                    }
                }
            };

            serviceClient.Indexes.Create(definition);
        }

        public static SearchResult[] DoSearch(SearchRequest request)
        {
            var indexClient = GetSearchIndexClient();

            // Set up search parameters
            var sp = new SearchParameters();

            // Set up highlights
            var hlFields = new List<string> {"content"};
            sp.HighlightFields = hlFields;
            sp.HighlightPreTag = "<b>";
            sp.HighlightPostTag = "</b>";

            // Set mode to 'all' (must match all terms/clauses). Default is 'any'
            sp.SearchMode = SearchMode.All;

            // Use the lucerne query engine
            sp.QueryType = QueryType.Full;

            // Filter by docid if requested
            if (!string.IsNullOrEmpty(request.DocId))
            {
                sp.Filter = "docId eq '" + request.DocId + "'";
            }

            // Filter by clientid if requested
            if (!string.IsNullOrEmpty(request.ClientId))
            {
                // add the and clause if needed
                if (!string.IsNullOrEmpty(sp.Filter))
                {
                    sp.Filter += " and ";
                }
                sp.Filter = "clientId eq '" + request.ClientId + "'";
            }

            // Filter by category(s) if requested
            if (request.Categories != null)
            {
                for (int i = 0; i < request.Categories.Length; i++)
                {
                    // start the filter expression
                    if (i == 0)
                    {
                        // add the and clause if needed
                        if (!string.IsNullOrEmpty(sp.Filter))
                        {
                            sp.Filter += " and ";
                        }
                        sp.Filter += "(";
                    }

                    // Add category filter
                    sp.Filter += "category eq '" + request.Categories[i];

                    // end expression or add 'and'
                    if (i == request.Categories.Length - 1)
                    {
                        // end expression
                        sp.Filter += ")";
                    }
                    else
                    {
                        sp.Filter += " and ";
                    }
                }
            }

            // Do not return the content
            sp.Select = new List<string> {"docId", "docFileName", "docUrl", "categories", "insertDate"};

            // Perform the search 
            DocumentSearchResult<Document> response = null;
            try
            {
                response = indexClient.Documents.Search<Document>(request.SearchTerm, sp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error doing search. " + ex.Message);
            }

            var ret = new List<SearchResult>();

            if (response != null)
                foreach (SearchResult<Document> result in response.Results)
                {
                    var sResult = new SearchResult
                    {
                        Score = result.Score,
                        DocId = (string) result.Document["docId"],
                        FileName = (string) result.Document["docFileName"],
                        FileUrl = (string) result.Document["docUrl"],
                        Categories = (IEnumerable<string>) result.Document["categories"],
                        LastUpdate = ((DateTimeOffset) result.Document["insertDate"]).DateTime
                    };
                    if (result.Highlights != null && result.Highlights.ContainsKey("content"))
                    {
                        sResult.Highlights = result.Highlights["content"].ToArray();
                    }

                    sResult.FileUrl += AzureStorageHelper.GenerateSasUrl(sResult.FileName);
                    ret.Add(sResult);
                }

            return ret.ToArray();
        }

        private static SearchIndexClient GetSearchIndexClient()
        {
            SearchServiceClient serviceClient = GetServiceClient();
            SearchIndexClient indexClient = serviceClient.Indexes.GetClient(doc_index);
            return indexClient;
        }

        private static SearchServiceClient GetServiceClient()
        {
            string searchServiceName = ConfigurationManager.AppSettings["AzureSearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["AzureSearchServiceApiKey"];
            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
            return serviceClient;
        }

        public static bool InsertDocument(SearchDocument document)
        {
            var batch = IndexBatch.Upload(new[] {document});
            return PerformIndexOperation(batch);
        }

        public static bool UpdateDocument(SearchDocument document)
        {
            var batch = IndexBatch.Merge(new[] {document});
            return PerformIndexOperation(batch);
        }

        public static void DeleteDocById(string id)
        {
            // Get the doc first
            var indexClient = GetSearchIndexClient();

            SearchDocument document = indexClient.Documents.Get<SearchDocument>(id);
            if (document != null)
            {
                // Remove file from azure
                AzureStorageHelper.DeleteBlob(document.DocFileName);

                // Now remove from index
                var batch = IndexBatch.Delete(new[] {document});
                PerformIndexOperation(batch);
            }
        }

        private static bool PerformIndexOperation(IndexBatch<SearchDocument> batch)
        {
            var indexClient = GetSearchIndexClient();

            try
            {
                indexClient.Documents.Index(batch);
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Debug.WriteLine("Failed to index the document: " + e.Message);
                Trace.TraceError(e.ToString());
                return false;
            }
            catch (Microsoft.Rest.Azure.CloudException ex)
            {
                // Could be that the index has not been created!!
                Trace.TraceError(ex.ToString());
                return false;
            }

            // Wait a while for indexing to complete.
            Thread.Sleep(2000);
            return true;
        }

        public static SearchResult[] CategorizeDocument(string docId)
        {
            string catScoreThreshold = ConfigurationManager.AppSettings["CategorizationScoreThreshold"];
            double catTheshold;
            if (!double.TryParse(catScoreThreshold, out catTheshold))
            {
                throw new Exception("Bad or missing Categorization Score Threshold value.");
            }

            var categoryList = AzureStorageHelper.GetTableRows();
            if (categoryList != null && categoryList.Length > 0)
            {
                // Get the doc 
                var indexClient = GetSearchIndexClient();
                SearchDocument document = indexClient.Documents.Get<SearchDocument>(docId);

                if (document != null)
                {
                    // Create list of terms per category
                    Dictionary<string, List<string>> catTerms = new Dictionary<string, List<string>>();
                    foreach (var categoryPoco in categoryList)
                    {
                        if (catTerms.ContainsKey(categoryPoco.CategoryName))
                        {
                            catTerms[categoryPoco.CategoryName].Add(categoryPoco.SearchTerm);
                        }
                        else
                        {
                            catTerms.Add(categoryPoco.CategoryName, new List<string> {categoryPoco.SearchTerm});
                        }
                    }

                    List<SearchResult> resultList = new List<SearchResult>();
                    List<string> documentCategoryList = new List<string>();

                    // Do the search for each category
                    foreach (var catTerm in catTerms)
                    {
                        // create each phrase and separate by an OR clause
                        SearchRequest request = new SearchRequest
                        {
                            DocId = docId,
                            SearchTerm = "(" + string.Join(")|(", catTerm.Value) + ")"
                        };

                        // Do the search
                        var results = DoSearch(request);
                        if (results != null && results.Length > 0)
                        {
                            // Found a match, but we should only get one result b/c we filtered on the unique docId
                            if (results.Length > 1)
                            {
                                throw new Exception("More than one result found for document id: " + docId);
                            }

                            var firstResult = results[0];
                            
                            // add the category to the item if the score is high enough
                            bool addedToCategory = false;
                            if (firstResult.Score >= catTheshold)
                            {
                                documentCategoryList.Add(catTerm.Key);
                                addedToCategory = true;
                            }

                            // Put the category and it's search term in the categories field (a bit of a hack)
                            firstResult.Categories = new[] {catTerm.Key, request.SearchTerm, addedToCategory.ToString().ToLower()};
                            resultList.Add(firstResult);
                        }
                    }

                    // Set category for item
                    document.Categories = documentCategoryList.ToArray();

                    // Resave item
                    UpdateDocument(document);

                    // return results
                    return resultList.OrderByDescending(r => r.Score).ToArray();
                }
            }
            return null;
        }

        public static void RemoveDocumentCategory(string docId, string categoryName)
        {
            if (!string.IsNullOrEmpty(categoryName))
            {
                // Get the doc 
                var indexClient = GetSearchIndexClient();
                SearchDocument document = indexClient.Documents.Get<SearchDocument>(docId);

                if (document != null)
                {
                    // select all the categories except the one to remove
                    document.Categories = document.Categories.Where(c => c != categoryName).ToArray();

                    // Resave item
                    UpdateDocument(document);
                }
            }
        }

        public static void AddDocumentCategory(string docId, string categoryName)
        {
            if (!string.IsNullOrEmpty(categoryName))
            {
                // Get the doc 
                var indexClient = GetSearchIndexClient();
                SearchDocument document = indexClient.Documents.Get<SearchDocument>(docId);

                if (document != null && document.Categories.All(c => c != categoryName))
                {
                    // Add the category
                    var newCatList = new List<string>(document.Categories);
                    newCatList.Add(categoryName);
                    document.Categories = newCatList.ToArray();

                    // Resave item
                    UpdateDocument(document);
                }
            }
        }

        public static SearchResult[] MoreLikeThis(string docId)
        {
            string searchServiceName = ConfigurationManager.AppSettings["AzureSearchServiceName"];

            string url = "https://" + searchServiceName + ".search.windows.net/indexes/" + doc_index + "/docs";
            url += "?api-version=2015-02-28-Preview&moreLikeThis=" + docId + "&searchFields=content&highlight=content&$select=docId,docFileName,docUrl,categories,insertDate";
            url += "&highlightPreTag=" + WebUtility.UrlEncode("<b>");
            url += "&highlightPostTag=" + WebUtility.UrlEncode("</b>");

            var request = WebRequest.Create(url) as HttpWebRequest;

            if (request != null)
            {
                string apiKey = ConfigurationManager.AppSettings["AzureSearchServiceApiKey"];
                request.Accept = "application/json";
                request.Headers.Add("api-key", apiKey);
                request.ContentType = "application/json";
                request.Method = "GET";
                request.KeepAlive = true;

                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (response != null && response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var stream = response.GetResponseStream())
                            {
                                if (stream != null)
                                {
                                    StreamReader sr = new StreamReader(stream);
                                    string outputJson = sr.ReadToEnd();
                                    ODataSearchResponse resp = JsonConvert.DeserializeObject<ODataSearchResponse>(outputJson);
                                    return resp.Value.Select(v => v.ToEySearchResult()).ToArray();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error with MoreLikeThis request: " + ex.Message);
                    Trace.TraceError(ex.ToString());
                    throw;
                }
            }
            return null;
        }
    }

    public class SearchRequest
    {
        public string SearchTerm;
        public string[] Categories;
        public string ClientId;
        public string DocId; // used to search within a specific document - e.g. for categorization
    }

    public class SearchResult
    {
        public double Score;
        public string DocId;
        public string FileName;
        public string FileUrl;
        public DateTime LastUpdate;
        public IEnumerable<string> Categories;
        public string[] Highlights;
    }

    public class ODataSearchValue
    {
        [JsonProperty("@search.score")]
        public double Score { set; get; }
        public string docId { get; set; }
        public string docFileName { get; set; }
        public string docUrl { get; set; }
        public string[] Categories { get; set; }
        public DateTime InsertDate { get; set; }

        [JsonProperty("@search.highlights")]
        public ODataSearchHighlight Highlights { get; set; }

        public SearchResult ToEySearchResult()
        {
            return new SearchResult
            {
                DocId = docId,
                FileName = docFileName,
                FileUrl = docUrl + AzureStorageHelper.GenerateSasUrl(docFileName),
                Categories = Categories,
                LastUpdate = InsertDate,
                Score = Score,
                Highlights = Highlights.content.ToArray()
            };
        }
    }

    public class ODataSearchHighlight
    {
        [JsonProperty("content@odata.type")]
        public string Type { get; set; }
        public List<string> content { get; set; }
    }

    public class ODataSearchResponse
    {
        [JsonProperty("odata.context")]
        public string Metadata { get; set; }
        public List<ODataSearchValue> Value { get; set; }
    }
}