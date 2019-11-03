(function () {
    'use strict';

    angular
        .module('Page', [])
        .controller('PageController', PageController);

    PageController.$inject = ['$location', '$window', '$scope', '$http', '$log'];

    function PageController($location, $window, $scope, $http, $log) {
        /* jshint validthis:true */
        var vm = this;

        $scope.results = [];

        vm.searchBySerial = function () {

            $http.get('/admin/search/by/serial?key=' + $scope.keyword, { })
                .then(function (response) {
                    $scope.results = response.data;
                }, function (error) {
                    alert(error.data.Message);
                });
        };
        
    }
})();
