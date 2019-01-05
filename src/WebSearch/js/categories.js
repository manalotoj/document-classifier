AZ.Categories = (function (ko) {
    "use strict";

    var catViewModel = {
        categories: ko.observableArray(),
        catNameInput: ko.observable(),
        catTermInput: ko.observable(),
        catNameOriginal: ko.observable(),
        catTermOriginal: ko.observable(),
        catIdInput: ko.observable()
    };

    // Do this on start
    $(document).ready(function () {

        // Get list of categories
        AZ.Ajax.MakeAjaxCall("GET",
           "Category",
           null,
           function (data) {

               for (var i = 0; i < data.length; i++) {
                   catViewModel.categories = ko.observableArray(data);
               }

               // Set up binding
               ko.applyBindings(catViewModel);
           });
    });

    return {
        model: catViewModel,

        addEdit: function (cat) {
            if (cat) {
                catViewModel.catNameInput(cat.CategoryName);
                catViewModel.catNameOriginal(cat.CategoryName);
                catViewModel.catTermInput(cat.SearchTerm);
                catViewModel.catTermOriginal(cat.SearchTerm);
            } else {
                catViewModel.catNameInput("");
                catViewModel.catTermInput("");
                catViewModel.catTermOriginal("");
                catViewModel.catNameOriginal("");
            }
            $("#addEditCatModal").modal("show");
        },

        delCat: function (cat) {
            if (confirm("Delete entry for category " + cat.CategoryName + " term " + cat.SearchTerm + "?")) {
                AZ.Ajax.MakeAjaxCall("DELETE",
                "Category",
                JSON.stringify({ 'CategoryName': cat.CategoryName, "SearchTerm": cat.SearchTerm }),
                function () {
                    // just reload the page to get the changes
                    location.reload();
                });
            }
        },

        saveCat: function () {
            AZ.Ajax.MakeAjaxCall("POST",
            "Category",
            JSON.stringify({ 'CategoryName': catViewModel.catNameInput(), "SearchTerm": catViewModel.catTermInput(), 'OriginalName': catViewModel.catNameOriginal(), 'OriginalTerm': catViewModel.catTermOriginal() }),
            function () {
               // just reload the page to get the new category
               location.reload();
           });
        }
    }
}(ko));