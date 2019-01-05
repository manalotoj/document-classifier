using System;
using Microsoft.Azure.Search.Models;

namespace AzureSearchDocumentClassiferLib
{
    [SerializePropertyNamesAsCamelCase]
    public class SearchDocument
    {
        public string DocId { get; set; }
        public string ClientId { get; set; }
        public string DocFileName { get; set; }
        public string DocUrl { get; set; }
        public string Content { get; set; }
        public string[] Categories { get; set; }
        public DateTimeOffset InsertDate { get; set; }
    }
}
