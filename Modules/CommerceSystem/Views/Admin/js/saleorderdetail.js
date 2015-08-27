﻿(function () {
    'use strict';

    var app = angular.module('saleorderdetail', ['xeditable']);    

    app.controller('saleorderdetailview', saleorderdetailview);

    app.run(function (editableOptions) {
        editableOptions.theme = 'bs3'; // bootstrap3 theme. Can be also 'bs2', 'default'
    });

    saleorderdetailview.$inject = ['$location', '$scope', '$window'];
   
    function saleorderdetailview($location, $scope, $window) {
        
        var vm = this;        
        
        $scope.productResolverTmpId = "";        
        
        var _stopWatchData = $scope.$watch('data', function (newVal, oldVal) {
            if (newVal != undefined) {
                _stopWatchData();
                _loadOrderDetail();
            }
        });

        $scope.genderList = [
            { id: 1, title: "Male"},
            { id: 2, title: "FeMale" },
            { id: 3, title: "Other" },
        ];

        $scope.loadOrderDetail = _loadOrderDetail;
        $scope.saveSaleOrderDetail = _saveSaleOrderDetail;
        $scope.printDocument = _printDocument;
        //$scope.onFormShow = _onFormShow;

        $scope.copyAddressShippingToBilling = _copyAddressShippingToBilling;
        
        function _getSOIdFromAbsUrl() {

            var _absUrl = $location.absUrl();
            var _appUrl = _absUrl.split("/");
            var _soId = _appUrl.pop();
            
            return _soId;

        };

        function _loadOrderDetail() {
            
            var _soId = _getSOIdFromAbsUrl();

            $scope.data.getById(_soId, function (data) {

                $scope.object = data;

                // To avoid that object in shipping cart is null
                $scope.productResolverTmpId = "saleorder_detail_productresolver.html";

            });
            
        };

        function _saveSaleOrderDetail() {
            $scope.data.save($scope.object, function (response) {
                console.log(response);
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
})();
