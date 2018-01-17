(function () {
    'use strict';

    var app = angular.module('saleorderdetail', ['xeditable']);    

    app.controller('saleorderdetailview', saleorderdetailview);

    app.run(function (editableOptions) {
        editableOptions.theme = 'bs3'; // bootstrap3 theme. Can be also 'bs2', 'default'
    });

    saleorderdetailview.$inject = ['$location', '$scope', '$window', '$http'];
   
    function saleorderdetailview($location, $scope, $window, $http) {
        
        var vm = this;        

        $scope.window = $window;
        $scope.productResolverTmpId = "";        
        
        var _stopWatchData = $scope.$watch('data', function (newVal, oldVal) {
            if (newVal != undefined) {
                _stopWatchData();
                _initData();
                //_loadOrderDetail();
            }
        });

        $scope.genderList = [
            { id: 1, title: "Male"},
            { id: 2, title: "FeMale" },
            { id: 3, title: "Other" },
        ];

        $scope.soStatusList = [];

        $scope.loadOrderDetail = _loadOrderDetail;
        $scope.saveSaleOrderDetail = _saveSaleOrderDetail;
        $scope.printDocument = _printDocument;
        //$scope.onFormShow = _onFormShow;

        $scope.copyAddressShippingToBilling = _copyAddressShippingToBilling;
        
        function _initData() {
            _getSOStatusList();
            _getPaymentStatusList();
            _loadOrderDetail();
        };

        function _getSOStatusList() {
            $http.get("/admin/commerce/api/sostatus")
                .then(function (success) {
                    $scope.soStatusList = success.data;                    
                }, function (error) {
                    console.error("_getSOStatusList:", error);
                });
        };

        function _getPaymentStatusList() {
            $http.get("/admin/commerce/api/paymentstatus")
                .then(function (success) {
                    $scope.paymentStatusList = success.data;
                }, function (error) {
                    console.error("_getPaymentStatusList:", error);
                });
        };

        function _getSOIdFromAbsUrl() {

            var _absUrl = $location.absUrl();
            var _appUrl = _absUrl.split("/");
            var _soId = _appUrl.pop();
            
            return _soId;

        };

        function _loadOrderDetail() {
            
            var _soId = _getSOIdFromAbsUrl();

            $scope.object = window.allData.SaleOrder;
            $scope.rowVerions = window.allData.RowVerions;

            
        };

        function _saveSaleOrderDetail() {            
            $scope.data.save($scope.object, function (response) {
                console.log("success");
                //console.log(response.Status);
            });
        };

        function _copyAddressShippingToBilling(arg) {            
            // TODO - Its does not work when form is opened.
            $scope.object.BillTo = angular.copy($scope.object.ShipTo);
            
            console.log("Save", $scope)            
        };

        function _printDocument(soIdentifier, docType) {            
            var _path = "/__commerce/saleorder/" + soIdentifier + "/" + docType;
            var _absUrl = $location.protocol() + "://" + $location.host() + ":" + $location.port() + _path;            
            $window.open(_absUrl);
        };

        function _onFormShow() {
        
        };
    }

    app.controller('PaymentController', function ($scope, $http) {

        var me = this;

        var so = window.allData.SaleOrder;
        $scope.paymentLogs = window.allData.PaymentLogs;
        $scope.paymentMethods = window.allData.PaymentMethods;
        $scope.paymentDetail = {};

        me.resetPaymentDetail = function () {
            $scope.paymentDetail.paidWhen = new Date();
            // TransferringMoney is the default
            $scope.paymentDetail.paymentMethod = $scope.paymentMethods[1];
            $scope.paymentDetail.amount = 0;
            $scope.paymentTime = { Hour: "0", Min: "0" };
            $scope.paymentDetail.apCode = '';
        }

        $scope.paymentDetail.saleOrderIdentifier = so.SaleOrderIdentifier;
        me.resetPaymentDetail();

        me.saveNewPayment = function () {

            $scope.paymentDetail.paidWhen.setHours($scope.paymentTime.Hour, $scope.paymentTime.Min, 0, 0);

            $http.post("/admin/commerce/api/pay", $scope.paymentDetail)
                .then(function (success) {
                    $scope.paymentLogs.push(success.data);
                    me.resetPaymentDetail();
                    alert("Payment Sucess!");
                    location.reload();
                }, function (error) {
                    alert(error.message);
                    location.reload();
                });

            return;
        }

    });
})();
