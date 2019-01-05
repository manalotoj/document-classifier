using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Configuration;
using System.Linq;

namespace AzureSearchDocumentClassiferLib
{
    public static class AzureStorageHelper
    {
        private static string CategoryTableName
        {
            get { return ConfigurationManager.AppSettings["CategoryTableName"]; }
        }

        public static string UploadBlob(byte[] fileBytes, string inputFileName)
        {
            var blockBlob = GetBlobReference(inputFileName);

            // Create or overwrite the "myblob" blob with contents from the byte array
            blockBlob.UploadFromByteArray(fileBytes, 0, fileBytes.Length);

            return blockBlob.StorageUri.PrimaryUri.AbsoluteUri;
        }

        public static void DeleteBlob(string blobFileName)
        {
            var blockBlob = GetBlobReference(blobFileName);
            if (blockBlob != null)
            {
                blockBlob.Delete();
            }
        }

        private static CloudBlockBlob GetBlobReference(string inputFileName)
        {
            string containerName = ConfigurationManager.AppSettings["AzureContainerName"];

            CloudStorageAccount storageAccount = GetConnection();

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(inputFileName);
            return blockBlob;
        }

        public static DocumentCategoryPoco[] GetTableRows()
        {
            CloudStorageAccount storageAccount = GetConnection();

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(CategoryTableName);

            // Create the table if it doesn't exist.
            table.CreateIfNotExists();

            // query the table
            TableQuery<DocumentCategory> query = new TableQuery<DocumentCategory>();
            var results = table.ExecuteQuery(query);

            return (results == null) ? null : results.Select(e => e.GetPoco()).ToArray();
        }

        public static void AddRowToTable(DocumentCategoryPoco value)
        {
            CloudStorageAccount storageAccount = GetConnection();

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(CategoryTableName);

            // Create the TableOperation object that inserts the customer entity.
            var insertOperation = TableOperation.Insert(value.GetEntity());

            // Execute the insert operation (no duplicates allowed)
            try
            {
                table.Execute(insertOperation);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Conflict"))
                {
                    throw new Exception("Error adding category: " + ex.Message);
                }
            }

            // If we are updating, delete the original
            if ((!string.IsNullOrEmpty(value.OriginalTerm) && (value.SearchTerm != value.OriginalTerm)) || (!string.IsNullOrEmpty(value.OriginalName) && (value.CategoryName != value.OriginalName)))
            {
                RemoveRowFromTable(new DocumentCategoryPoco
                {
                    CategoryName = value.OriginalName,
                    SearchTerm = value.OriginalTerm
                });
            }
        }

        public static string GenerateSasUrl(string filename)
        {
            var toDateTime = DateTime.Now.AddMinutes(10);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = null,
                SharedAccessExpiryTime = new DateTimeOffset(toDateTime)
            };

            var blobRef = GetBlobReference(filename);            
            return blobRef.GetSharedAccessSignature(policy);
        }

        public static void RemoveRowFromTable(DocumentCategoryPoco value)
        {
            CloudStorageAccount storageAccount = GetConnection();

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(CategoryTableName);

            // Create a retrieve operation that expects a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<DocumentCategory>(value.CategoryName, value.SearchTerm);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity.
            DocumentCategory deleteEntity = (DocumentCategory)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);
            }
        }

        private static CloudStorageAccount GetConnection()
        {
            var storageConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"];

            // Retrieve storage account from connection string.
            return CloudStorageAccount.Parse(storageConnectionString.ConnectionString);
        }
    }
}
