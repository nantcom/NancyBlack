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

        
        $scope.modes = [
            { Title: "Customer View", table: "tablecustomertemplate.html", sort: { SaleOrderIdentifier : "desc" }, filter: "(PaymentStatus eq 'PaymentReceived') or (PaymentStatus eq 'Deposit')" },
            { Title: "Follow up", table: "tablecustomertemplate.html", sort: { SaleOrderIdentifier : "desc" }, filter: "(Status eq 'Confirmed') and (PaymentStatus eq 'WaitingForPayment')" },
            { Title: "Waiting to Order", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate: 'asc' }, filter: "(Status eq 'WaitingForOrder')" },
            { Title: "Delay", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate: 'asc' }, filter: "(Status eq 'Delay')" },
            { Title: "Order Processing", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate: 'asc' }, filter: "(Status eq 'OrderProcessing')" },
            { Title: "Incoming", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate: 'asc' }, filter: "(Status eq 'InTransit') or (Status eq 'CustomsClearance')" },
            { Title: "Working", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate: 'asc' }, filter: "(Status eq 'Inbound') or (Status eq 'AtLEVEL51') or (Status eq 'WaitingForParts') or (Status eq 'PartialBuilding') or (Status eq 'Building') or (Status eq 'Testing')" },
            { Title: "Waiting for Parts", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate: 'asc' }, filter: "(Status eq 'WaitingForParts') or (Status eq 'PartialBuilding')" },
            { Title: "Ready To Ship", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate : 'asc' }, filter: "(Status eq 'ReadyToShip')" },
            { Title: "Shipped", table: "tablecustomtemplate.html", sort: { PaymentReceivedDate : 'asc' }, filter: "(Status eq 'Shipped')" }
        ];

        $scope.modeView = "";
        vm.changeView = function (view) {
            $scope.currentMode = view;
            $scope.modeView = "";

            window.setTimeout(function () {

                $scope.$apply(function () {
                    $scope.modeView = "tableview";
                });

            }, 400);
        };
        vm.changeView($scope.modes[0]);
    }
})();
