(function () {
    'use strict';

    angular
        .module('saleorder', ["chart.js"])
        .controller('saleorder_list', saleorder_list);

    saleorder_list.$inject = ['$location', '$window', '$scope', '$http', '$log'];

    function saleorder_list($location, $window, $scope, $http, $log) {
        /* jshint validthis:true */
        var vm = this;

        vm.viewOrderDetail = _viewOrderDetail;

        function _viewOrderDetail(id) {                           
            $window.location = "/Admin/saleorder/" + id;
        };
        
    }
})();
