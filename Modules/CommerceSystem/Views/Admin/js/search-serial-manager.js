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
        $scope.isBusy = false;

        vm.searchBySerial = function () {


            $scope.isBusy = true;

            $http.get('/admin/search/by/serial?key=' + $scope.keyword, {})
                .then(function (response) {
                    $scope.results = response.data;
                    $scope.isBusy = false;
                }, function (error) {
                    alert(error.data.Message);
                    $scope.isBusy = false;
                });
        };

        vm.entersearch = function (keyEvent) {

            if (keyEvent.which === 13) {
                vm.searchBySerial();
            }
        };

    }
})();
