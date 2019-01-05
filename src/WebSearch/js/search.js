AZ.Search = (function (ko) {
    "use strict";

    var searchViewModel = {
        searchText: ko.observable(),
        searchResults: ko.observableArray(),
        isSearchResult: ko.observable(false)
    }

    // Do this on start
    $(document).ready(function () {

        // Enter in search box will submit search
        $("#searchTerm").keyup(function (event) {
            if (event.keyCode === 13) {
                AZ.Search.doSearch();
            }
        });

        // Get number of docs in index
        ko.applyBindings(searchViewModel);
    });

    return {
        model: searchViewModel,

        doSearch: function () {
            var text = searchViewModel.searchText();
            
            if (!text) {
                alert("Must enter a search term.");
            } else {
                AZ.Ajax.MakeAjaxCall("POST",
                    "Search",
                    JSON.stringify({ 'SearchTerm': text }),
                    function(data) {
                        // hide search help
                        searchViewModel.isSearchResult(true);
                        searchViewModel.searchResults(data);
                    });
            }
        },

        moreLikeThis: function(result) {
            searchViewModel.searchText("[Documents similar to " + result.FileName + "]");
            AZ.Ajax.MakeAjaxCall("GET",
                "Search/MoreLikeThis/" + result.DocId,
                null,
                function(data) {
                    // hide search help
                    searchViewModel.isSearchResult(true);
                    searchViewModel.searchResults(data);
                });

        }
    }
}(ko));