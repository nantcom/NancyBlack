(function () {

    var module = angular.module('InventoryAdminModule', ['ui.bootstrap', 'angular.filter']);

    module.controller("InventoryNotFullfilled", function ($scope, $rootScope, $http) {

        $me = this;
        $scope.totalBuying = 0;
        $scope.totalSelling = 0;

        $http.get("/admin/tables/inventoryitem/__notfullfilled").success(function (data) {

            $scope.data = data;

            var totalSellingPrice = 0;
            var totalAveragePrice = 0;
            for (var i = 0; i < data.length; i++) {

                totalAveragePrice += window.averagePrices[data[i].ProductId];
                totalSellingPrice += data[i].InventoryItem.SellingPrice;
            }

            $scope.totalSelling = totalSellingPrice;
            $scope.totalBuying = totalAveragePrice;
        })
    });
    
    module.controller("InventoryInstock", function ($scope, $rootScope, $http) {

        $me = this;
        $scope.totalValue = 0;
        $scope.data = window.instockData;

        for (var i = 0; i < $scope.data.length; i++) {
            $scope.totalValue += $scope.data[i].Price;
        }
    });
    
    module.controller("InboundController", function ($scope, $rootScope, $http) {

        $me = this;
        $scope.object = {};
        $scope.totalToDistribute = 0;

        $me.getTotal = function (obj) {

            if (obj == null) {
                return;
            }

            var total = 0;
            for (var i = 0; i < obj.Items.length; i++) {
                total += obj.Items[i].Price;
            }
            
            var totalTax = 0;
            for (var i = 0; i < obj.Items.length; i++) {
                totalTax += obj.Items[i].Tax;
            }

            obj.TotalAmountWithoutTax = total;
            obj.TotalTax = totalTax;
            obj.TotalAmount = total + totalTax;

            return total;

        };

        $me.distributeCost = function (obj, totalToDistribute) {

            if (obj == null) {
                return;
            }

            var toDistribute = (totalToDistribute - this.getTotal(obj)) / obj.Items.length;
            for (var i = 0; i < obj.Items.length; i++) {
                obj.Items[i].Price += toDistribute;
                obj.Items[i].Tax = obj.Items[i].Price * (window.commerceSettings.billing.vatpercent / 100);
            }

            $scope.totalToDistribute = 0;
        };

        $me.includeTax = function (item) {

            var priceWithoutTax = (item.Price * 100) / (window.commerceSettings.billing.vatpercent + 100);
            item.Tax = item.Price - priceWithoutTax;
            item.Price = priceWithoutTax;
        };

        $me.addTax = function (item) {

            item.Tax = item.Price * (window.commerceSettings.billing.vatpercent / 100);
        };

        $me.save = function (obj) {

            $scope.data.save(obj, function (newData) {

                $scope.object = newData;
            });

        };
    });

})();