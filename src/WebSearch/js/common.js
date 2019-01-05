// Global Namespace
var AZ = {};

AZ.Ajax = (function () {
    "use strict";

    $(document).ready(function () {
        // Add please wait to body and attach to ajax function
        var loadingDiv = "<div id='ajax_loader' style='width: 100%;height: 100%;top: 0;left: 0;position: fixed;opacity: 0.7;background-color: #fff;z-index: 9999;text-align: center;display: none;'><h1 style='margin-top: 300px;'>Loading...</h1></div>";
        $("body").append(loadingDiv);

        $(document).ajaxStart(function () {
            $("#ajax_loader").show();
        });
        $(document).ajaxComplete(function () {
            $("#ajax_loader").hide();
        });
    });

    function handleBasicError(xhr, status, error) {
        alert("Error: " + error);
    }

    return {

        MakeAjaxCall: function (ajaxType, ajaxUrl, data, successFunc, isFileUpload) {
            $.ajax({
                type: ajaxType,
                url: "api/" + ajaxUrl,
                data: data,
                contentType: (isFileUpload) ? false : "application/json; charset=utf-8",
                dataType: (isFileUpload) ? "" : "json",
                processData: (isFileUpload) ? false : "",
                success: successFunc,
                error: handleBasicError
            });
        },

        UploadFile: function uploadFile(picker, url, success) {
            var files = picker.files;
            if (files.length > 0) {

                // Only can do one file
                var formData = new FormData();
                formData.append("file", files[0]);

                AZ.Ajax.MakeAjaxCall("POST", url, formData, success, true);
            } else {
                alert("Must choose a file first.");
            }
        }
    };
}());
