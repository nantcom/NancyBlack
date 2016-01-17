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

        function _createSaleOrder(object) {

            $http.post('/__commerce/api/checkout', object).then(function (response) {
                alert("สร้าง SO สำเร็จ");
            }, function () {

            });
        };
        
    }
})();
