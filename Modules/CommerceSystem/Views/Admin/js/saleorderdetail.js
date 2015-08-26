(function () {
    'use strict';

    var app = angular.module('saleorderdetail', ['xeditable']);    

    app.controller('saleorderdetailview', saleorderdetailview);

    app.run(function (editableOptions) {
        editableOptions.theme = 'bs3'; // bootstrap3 theme. Can be also 'bs2', 'default'
    });

    saleorderdetailview.$inject = ['$location', '$scope'];
   
    function saleorderdetailview($location, $scope) {
        
        var vm = this;
        
        var _stopWatchData = $scope.$watch('data', function (newVal, oldVal) {
            if (newVal != undefined) {
                _stopWatchData();
                _loadOrderDetail();
            }
        });        
            
        $scope.user = {
            name: 'awesome user'
        };

        $scope.genderList = [
            { id: 1, title: "Male"},
            { id: 2, title: "FeMale" },
            { id: 3, title: "Other" },
        ];

        $scope.loadOrderDetail = _loadOrderDetail;
        $scope.saveOrderDetail = _saveOrderDetail;
        $scope.saveCustomerDetail = _saveCustomerDetail;
        $scope.saveShippingDetail = _saveShippingDetail;

        $scope.saveSaleOrderDetail = _saveSaleOrderDetail;
        

        function _loadOrderDetail() {
            console.log($scope)
            $scope.data.getById(15, function (data) {
                $scope.object = data;
            });
            
        };

        function _saveCustomerDetail() {
            $scope.data.save($scope.object, function (response) {
                console.log(response);
            });
        };

        function _saveShippingDetail() {
            $scope.data.save($scope.object, function (response) {
                console.log(response);
            });
        };

        function _saveSaleOrderDetail() {
            $scope.data.save($scope.object, function (response) {
                console.log(response);
            });
        };

        function _saveOrderDetail() {
            console.log("Save")
        };


    }
})();
