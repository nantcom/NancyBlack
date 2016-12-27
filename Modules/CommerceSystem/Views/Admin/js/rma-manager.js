(function () {
    'use strict';

    angular
        .module('Page', [])
        .controller('PageController', PageController);

    PageController.$inject = ['$location', '$window', '$scope', '$http', '$log'];

    function PageController($location, $window, $scope, $http, $log) {
        /* jshint validthis:true */
        var vm = this;
        vm.createRMA = function () {

            $http.post('/admin/tables/rma/new', { Items: [], CustomData: {}, ShippingDetails: {} })
                .then(function (response) {
                    $window.location = "/rma/" + response.data.RMAIdentifier;
                }, function (error) {
                    alert(error.data.Message);
                });
        };

        $scope.object = {}; 

        
        
    }
})();
