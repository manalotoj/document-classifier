using Microsoft.WindowsAzure.Storage.Table;

namespace AzureSearchDocumentClassiferLib
{
    public class DocumentCategory : TableEntity
    {
        public DocumentCategory(string categoryName, string searchTerm)
        {
            PartitionKey = categoryName;
            RowKey = searchTerm;
        }

        public DocumentCategory() { }

        public DocumentCategoryPoco GetPoco()
        {
            return new DocumentCategoryPoco()
            {
                CategoryName = PartitionKey,
                SearchTerm = RowKey
            };
        }
    }

    public class DocumentCategoryPoco
    {
        public string CategoryName;
        public string SearchTerm;
        public string OriginalName;  // used for edits
        public string OriginalTerm;  // used for edits
        public DocumentCategory GetEntity()
        {
            return new DocumentCategory(CategoryName, SearchTerm);
        }
    }
}
