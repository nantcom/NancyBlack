
(function () {

    var module = angular.module('DynamicQueryModule', ['ui.bootstrap']);

    module.controller("DynamicQueryController", function ($scope, $rootScope, $http) {

        var $me = this;

        $scope.dataTypes = {};
        $scope.isBusy = true;

        $me.initialize = function () {

        $http.get("/admin/dynamicquery/alldatatype")
            .success(function (data, status, headers, config) {
                
                $scope.dataTypes = data;
                $scope.isBusy = false;
            }).
            error(function(data, status, headers, config) {
                // called asynchronously if an error occurs
                // or server returns response with an error status.
            });

        };

        $me.setDataType = function (t) {

            window.location.href = "/Admin/dynamicquery?t=" + t.NormalizedName;
        };

        $me.initialize();

    });

})();