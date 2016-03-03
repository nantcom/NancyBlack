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
        vm.createSaleOrder = _createSaleOrder;

        function _viewOrderDetail(id) {                           
            $window.location = "/Admin/tables/saleorder/" + id;
        };

        $scope.object = {}; 

        function _createSaleOrder() {

            $http.post('/admin/tables/saleorder/new', { Items: [], CustomData: {}, ShippingDetails: {} })
                .then(function (response) {
                $window.location = "/Admin/tables/saleorder/" + response.data.Id;
            }, function (error) {
                alert(error.data.Message);
            });
        };
        
    }
})();
