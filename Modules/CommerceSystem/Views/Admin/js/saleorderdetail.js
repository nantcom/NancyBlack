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
        var $me = this;

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

        $scope.newItem = { Qty: 1};
        $scope.alerts = [];

        $me.addItem = function (parameter) {

            $scope.isBusy = true;

            $http.post("/admin/saleorder/" +
                    $scope.object.Id + "/add", parameter)
                .then(function (success) {

                    $scope.isBusy = false;
                    $scope.object = success.data.SaleOrder;
                    $scope.window.allData.InventoryRequests = success.data.InventoryRequests;

                    $scope.alerts.push({ type: 'success', msg: 'Item Added Successfully.' });
                    $scope.newItem = { Qty: 1 };

                }, function (error) {

                    $scope.isBusy = false;
                    $scope.alerts.push({ type: 'danger', msg: 'Cannot Add: ' + error });

                });

        };

        $me.setPrice = function (so, item, $index) {

            item.EditPrice = false;

            // this is not saved to database but will be used for showing
            item.CurrentPrice = item.ShowPrice;

            // we set into discount price so that the original price is intact
            item.DiscountPrice = item.ShowPrice;

            // this will ensure the price we set is used
            item.PromotionStartDate = new Date(2000, 1, 1);
            item.PromotionEndDate = new Date(2010, 1, 1);
            item.PromotionReferenceDate = new Date(2005, 1, 1);

            $me.previewTotal(so, item, $index);
        }

        var oldPrice = 0;
        var lastChange = -1;
        $me.previewTotal = function (so, item, $index) {

            $scope.isBusy = true;

            if (oldPrice == 0) {
                oldPrice = so.TotalAmount;
            }
            
            $http.post("/admin/saleorder/" +
                $scope.object.Id + "/previewtotal", so)
                .then(function (success) {

                    $scope.isBusy = false;
                    var newSo = success.data.SaleOrder;
                    $scope.object = newSo;
                    
                    var change = newSo.TotalAmount - oldPrice;
                    if (change == 0) {
                        return;
                    }

                    if (change != lastChange) {
                        lastChange = change;
                        $scope.alerts.push({
                            type: 'warning',
                            msg: 'Change: ' + change + ', Initial Total: ' + oldPrice
                        });
                    }

                    
                }, function (error) {

                    $scope.isBusy = false;

                });

        };
        
        $me.updateQty = function (object) {

            $scope.isBusy = true;

            $http.post("/admin/saleorder/" +
                $scope.object.Id + "/updateqty", object)
                .then(function (success) {

                    $scope.object = success.data.SaleOrder;
                    $scope.window.allData.InventoryRequests = success.data.InventoryRequests;

                    oldPrice = 0;

                    $scope.alerts.length = 0;
                    $scope.alerts.push({ type: 'success', msg: 'Qty and Prices Updated Successfully.' });

                }, function (error) {
                    $scope.alerts.push({ type: 'danger', msg: 'Cannot Update: ' + error });
                });

        };

        $me.updateSerial = function (data, itemline) {

            if (!itemline.SerialNumber) {
                return;
            }

            data.save(itemline, function (updated) {

                itemline.BuyingCost = updated.BuyingCost;
                itemline.BuyingTax = updated.BuyingTax;
                itemline.FulfilledDate = updated.FulfilledDate;

            });
        };

        $me.getGPU = function (item) {

            var correctUrl = item.Url.indexOf('/parts/gpu') > 0;
            var qty = item.Attributes.Qty > 0;

            return correctUrl && qty;
        };
    }

    app.controller('PaymentController', function ($scope, $http) {

        var me = this;

        var so = window.allData.SaleOrder;
        $scope.paymentLogs = window.allData.PaymentLogs;
        $scope.paymentMethods = window.allData.PaymentMethods;
        $scope.paymentDetail = {};


        var receiptIndex = -1;
        for (var i = 0; i < $scope.paymentLogs.length; i++) {
            var log = $scope.paymentLogs[i];
            if (log.IsPaymentSuccess) {
                receiptIndex++;

                log.receiptIndex = receiptIndex;
            }
        }


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
