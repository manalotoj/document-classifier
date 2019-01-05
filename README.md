This repo has been cloned and minimally modified from https://scottse.visualstudio.com/ (thanks Scott!). Updates/modifications include:
1. Exposing publicly in order to comply with iTextSharp GNU Affero General Public License version 3
2. .NET Framework and nuget references
3. Addition of Resource Group project containing ARM template
4. dev.azure.com CICD pipelines

# AzureSearchDocumentClassifier
Use Azure Search and the Cognitive Vision API to categorize and search across uploaded PDF documents including scanned images. 

# Problem Space
Azure Search allows for rich search across indexed documents. We can use this capability to let users define search terms that identify a document and then categorize them on upload. For example, documents with the phrase "Germany" might be German documents. However, some PDF documents are simply scanned images. We can use the Azure Cognitive Vision APIs to do OCR on these documents so they can be indexed like other documents.  

# Setup
To use this demo, you must first create the following in Azure:

1. Azure Search Service (the free tier should suffice for prototyping)
2. Azure Storage (for holding the uploaded files and the category table)
3. Cognitive Services API - Computer Vision (for doing OCR, free tier available for testing)
4. Azure Web App (optional, for hosting the UI if you want a publicly accessible site, again shared should be enough for testing)

Once the above are created, add the following to the web.config file to point to your resources (keys are found in the Keys tab of the resource blade):

1. Search Service API key and service name 
2. In the storage account, create a new blob container with any name and put the name you used in the web.config file.
3. Also in the storage account, look up the access key and use it along with the account name to update the AzureStorage connection string. 
4. Cognitive API key

When you run for the first time, you need to click the 'Delete all documents and rebuild index button' on the documents tab. This will create the index. 

# Functionality

In the UI, there are three tabs: 
* The “Search” tab allows free-form text search across all the documents previously uploaded. The “More Like This” button creates a term map of the document and tries to find other documents with similar terms (with the score showing the quality of the match).
* The “Category Management” tab allows you to create categories and associated search terms. You can create more than one term for each category (all the terms must be found to consider it a ‘hit’ for the category). Terms can be quite complex – see the links under the term input box for more information.
* The “Document Management” tab is where you can upload new documents for categorization, or just to add them to the index. Documents must be PDF format but they can be either text (normal) or scanned documents and we should be able to parse them. During categorization, I set a score threshold of 0.10 to consider it part of a category but this can be changed in the web.config
 
This app is a Single Page Application style (e.g. not MVC – no server-rendered HTML) calling back to WebAPI which does the real work. Files are stored in Azure Blob storage. Category terms are stored in Azure Table Storage. Azure Cognitive Vision API is used to do OCR on scanned PDFs (i.e. PDFs that can’t be parsed normally). 

# Categorization Process

1. Users enter categories and associated search phrases that will identify documents belonging to that category.
2. When users upload documents they are parsed and then an attempt is made to categorize the documents based on the previously uploaded phrases. If the phrase is found, then the search score is used to determine if the match a suggested categorization. Matches that fall below the score are still shown to the user but they must click on the categorize button to mark the document as belonging to that category.
3. On the search screen, a user can do free-form search. The "more like this" button allows the user to find similar documents. 

