AZ.Documents = (function (ko) {
    "use strict";

    var docViewModel = {
        documents: ko.observableArray(),
        catNameInput: ko.observable(),
        catTermInput: ko.observable(),
        catIdInput: ko.observable(),
        docParsedText: ko.observable(),
        searchResults: ko.observableArray()
    };

    // Do this on start
    $(document).ready(function () {

        // Get list of Documents
        AZ.Ajax.MakeAjaxCall("GET",
           "Document",
           null,
           function (data) {

               for (var i = 0; i < data.length; i++) {
                   docViewModel.documents = ko.observableArray(data);
               }

               // Set up binding
               ko.applyBindings(docViewModel);
           });
    });

    return {
        model: docViewModel,

        resetIndexes: function () {
            if (confirm("This action will delete all current data and documents from the index. This action cannot be undone. Proceed?")) {
                AZ.Ajax.MakeAjaxCall("DELETE",
               "Document",
               null,
               function () {
                   // just reload the page to get the changes
                   location.reload();
               });
            }
        },
        
        addEdit: function () {
            docViewModel.catNameInput("");
            docViewModel.catTermInput("");
            // Hide any other modals
            $("#addEditDoc2Modal").modal("hide");
            $("#addEditDocModal").modal("show");
        },

        saveDoc: function() {
            var picker = $("#fileUpload")[0];

            AZ.Ajax.UploadFile(picker,
                "Index",
                function (data) {
                    // Load data
                    docViewModel.searchResults(data);
                    $("#addEditDoc2Modal").modal("hide");
                    $("#addEditDoc3Modal").modal({ backdrop: 'static', keyboard: false, show: true });
                });
        },

        delDoc: function(doc) {
            if (confirm("Delete document " + doc.FileName + " and remove it from the index?")) {
                AZ.Ajax.MakeAjaxCall("DELETE",
                    "Index/" + doc.DocId,
                    null,
                    function() {
                        // just reload the page to get the changes
                        location.reload();
                    });
            }
        },

        catDoc: function (doc) {
            if (confirm("Recategorize " + doc.FileName + " and remove any existing categorization?")) {
                AZ.Ajax.MakeAjaxCall("GET",
                    "Document/Recategorize/" + doc.DocId,
                    null,
                    function (data) {
                        docViewModel.searchResults(data);
                        $("#addEditDoc3Modal").modal({ backdrop: 'static', keyboard: false, show: true });
                    });
            }
        },

        removeCat: function(cat) {
            var catName = cat.Categories[0];
            if (confirm("Reject the categorization for category '" + catName + "'?")) {
                
                // make the call to remove the cat
                AZ.Ajax.MakeAjaxCall("POST",
                    "Document/RemoveCategory/" + cat.DocId,
                    JSON.stringify(catName),
                    function () {
                        // toggle
                        cat.Categories[2] = "false";
                        var changedIdx = docViewModel.searchResults.indexOf(cat);
                        docViewModel.searchResults.splice(changedIdx, 1); // removes the item from the array
                        docViewModel.searchResults.splice(changedIdx, 0, cat); // adds it back

                    });
            }
        },

        addCat: function (cat) {
            var catName = cat.Categories[0];
            if (confirm("Accept the categorization for category '" + catName + "'?")) {

                // make the call to remove the cat
                AZ.Ajax.MakeAjaxCall("POST",
                    "Document/AddCategory/" + cat.DocId,
                    JSON.stringify(catName),
                    function () {
                        cat.Categories[2] = "true";
                        var changedIdx = docViewModel.searchResults.indexOf(cat);
                        docViewModel.searchResults.splice(changedIdx, 1); // removes the item from the array
                        docViewModel.searchResults.splice(changedIdx, 0, cat); // adds it back
                    });
            }
        },

        uploadDoc: function () {
            var picker = $("#fileUpload")[0];

            AZ.Ajax.UploadFile(picker,
                "Document",
           function (data) {
               docViewModel.docParsedText(data);
               $("#addEditDocModal").modal("hide");
               $("#addEditDoc2Modal").modal("show");
           }
       );
        }
    }
}(ko));