(function () {
    'use strict';

    angular
        .module('saleorder', ["chart.js"])
        .controller('saleorder_list', saleorder_list);

    saleorder_list.$inject = ['$location', '$window', '$scope', '$http', '$log'];

    function saleorder_list($location, $window, $scope, $http, $log) {
        /* jshint validthis:true */
        var vm = this;

        vm.createSaleOrder = _createSaleOrder;

        $scope.object = {};

        function _createSaleOrder() {

            $http.post('/admin/tables/saleorder/new', { Items: [], CustomData: {}, ShippingDetails: {} })
                .then(function (response) {
                $window.location = "/Admin/tables/saleorder/" + response.data.Id;
            }, function (error) {
                alert(error.data.Message);
            });
        };

		//only work in level51 web
        $scope.selectedSaleOrders = [];
        vm.checkedSaleOrder = function (id) {
            var index = $scope.selectedSaleOrders.indexOf(id);
            if (index > -1) {
                $scope.selectedSaleOrders.splice(index, 1);
            }
            else {
                $scope.selectedSaleOrders.push(id);
            }
        }

        vm.showMergeItems = function () {
            //$window.location = "/Admin/inventoryitem/byselectedsaleorder?selectedos=" + $scope.selectedSaleOrder.join();
            window.open("/Admin/inventoryitem/byselectedsaleorder?selectedos=" + $scope.selectedSaleOrders.join(), '_blank');
        }

        
        
    }
})();
