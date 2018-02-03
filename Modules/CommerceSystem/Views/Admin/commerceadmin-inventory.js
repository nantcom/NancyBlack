(function () {

    var module = angular.module('InventoryAdminModule', ['ui.bootstrap', 'angular.filter']);

    //module.controller("InventoryNotFullfilled", function ($scope, $rootScope, $http) {

    //    $me = this;
    //    $scope.totalBuying = 0;
    //    $scope.totalSelling = 0;
    //    $scope.averagePrices = [];

    //    $http.get("/admin/tables/inventoryitem/__averageprice").success(function (data) {

    //        for (var i = 0; i < data.length; i++) {
    //            var key = data[i].ProductId;
    //            $scope.averagePrices[key] = data[i].Price;
    //        }

    //        $http.get("/admin/tables/inventoryitem/__notfullfilled").success(function (data) {

    //            $scope.data = data;

    //            var totalSellingPrice = 0;
    //            var totalAveragePrice = 0;
    //            for (var i = 0; i < data.length; i++) {

    //                var avgPrice = $scope.averagePrices[data[i].ProductId]

    //                totalAveragePrice += (avgPrice == null ? 0 : avgPrice);
    //                totalSellingPrice += data[i].InventoryItem.SellingPrice;
    //            }

    //            $scope.totalSelling = totalSellingPrice;
    //            $scope.totalBuying = totalAveragePrice;
    //        });

    //    });

    //});
    
    module.controller("InventoryNotFullfilled2", function ($scope, $rootScope, $http, $window) {

        $me = this;
        $scope.totalBuying = 0;
        $scope.totalSelling = 0;
        $scope.averagePrices = [];
        $scope.data = $window.data.InventoryRequests;
        
        var instockLookup = [];
        var instock = $window.data.Instock;
        for (var i = 0; i < instock.length; i++) {
            var key = instock[i].ProductId;
            instockLookup[key] = instock[i].Qty;
        }

        $http.get("/admin/tables/inventoryitem/__averageprice").success(function (data) {

            for (var i = 0; i < data.length; i++) {
                var key = data[i].ProductId;
                $scope.averagePrices[key] = data[i].Price;
            }
            
            var total = 0;
            for (var i = 0; i < $scope.data.length; i++) {

                var id = $scope.data[i].ProductId;
                $scope.data[i].Url = $window.productUrlLookup[id]

                var qtyStock = instockLookup[id];
                if (qtyStock == null) {
                    qtyStock = 0;
                }
                $scope.data[i].QtyStock = qtyStock;

                var diff = $scope.data[i].QtyStock - $scope.data[i].Qty;
                if (diff < 0) {

                    var price = $scope.averagePrices[id];
                    if (price == null) {
                        total += $scope.data[i].SoldPrice * (diff * -1);
                    } else {

                        total += price * $scope.data[i].Qty;
                    }

                }

            }

            $scope.totalValue = total;
        });
        
    });

    module.controller("InventoryNotInbound", function ($scope, $rootScope, $http) {

        $me = this;
        $scope.totalBuying = 0;

        $http.get("/admin/tables/inventoryitem/__waitingforinbound").success(function (data) {

            $scope.data = data;

            var totalBuying = 0;
            for (var i = 0; i < data.length; i++) {

                totalBuying += data[i].InventoryItem.BuyingCost;
            }

            $scope.totalBuying = totalBuying;
        })
    });

    module.controller("InventoryInstock", function ($scope, $rootScope, $window) {

        $me = this;
        $scope.totalValue = 0;
        $scope.data = $window.data.Instock;

        var waitingLookup = [];
        var waiting = $window.data.WaitingForInbound;
        for (var i = 0; i < waiting.length; i++) {
            var key = waiting[i].ProductId;
            waitingLookup[key] = waiting[i].Qty;
        }

        var total = 0;
        for (var i = 0; i < $scope.data.length; i++) {
            
            var id = $scope.data[i].ProductId;
            $scope.data[i].Url = $window.productUrlLookup[id]

            var qtyWaiting = waitingLookup[id];
            if (qtyWaiting == null) {
                qtyWaiting = 0;
            }

            $scope.data[i].QtyWaiting = qtyWaiting;

            total += $scope.data[i].Price;
        }

        $scope.totalValue = total;
    });
    
    module.controller("InboundController", function ($scope, $rootScope, $http, $window) {

        $me = this;
        $me.IsPriceIncludeVat = 1;
        $scope.object = {};
        $scope.object.Items = [];
        $scope.object.Shipping = 0;
        $scope.object.Additional = 0;
        $scope.object.Tax = 0;
        $scope.object.Total = 0;
        $scope.newItem = {};

        $scope.autocomplete = {};
        $scope.totalToDistribute = 0;

                  

        $http.get("/admin/tables/accountingentry/__autocompletes").success(function (data) {

            $scope.autocomplete = data;

        });

        $me.getTotal = function (obj) {

            if (obj == null) {
                return;
            }

            if (obj.Items == null) {
                return;
            }

            var total = 0;
            for (var i = 0; i < obj.Items.length; i++) {
                total += obj.Items[i].BuyingPrice * obj.Items[i].Qty;
            }
            
            if ( $me.IsPriceIncludeVat == 1 ) 
            {
                var withoutVat = total * 100 / ($window.commerceSettings.billing.vatpercent + 100);
                obj.Tax = total - withoutVat;                
                obj.Total = total + obj.Shipping + obj.Additional; 
            }
            else
            {   
                obj.Tax = ($window.commerceSettings.billing.vatpercent / 100) * total;
                obj.Total = total + obj.Tax + obj.Shipping + obj.Additional; 
            }


            return obj.Total;

        };
        
        $me.submit = function (obj) {

            $scope.isBusy = true;
            $scope.object.IsPriceIncludeVat = $me.IsPriceIncludeVat == 1;

            $http.post("/admin/tables/inventorypurchase/__submitinvoice", $scope.object).
                success(function (data, status, headers, config) {

                    $scope.isBusy = false;
                    $scope.alerts.push({
                        type: "success",
                        msg: "Submitted Successfully"
                    });
                }).
                error(function (data, status, headers, config) {

                    $scope.isBusy = false;
                    $scope.alerts.push({
                        type: "danger",
                        msg: status + " : " + data.Message
                    });
                }
            );

        };
        
    });


    module.controller("InventoryInboundController2", function ($scope, $rootScope, $http) {

        $me = this;
        $scope.inbound = {};
        $scope.invoicenumbers = [];
        $scope.status = [{ type: 'info', msg: 'Select Invoice and Scan Barcode' }];

        $me.refreshInvoice = function () {
            
            $http.get("/admin/tables/inventorypurchase/__waitinginvoices").success(function (data) {

                $scope.invoicenumbers = data;
            });
        };

        $me.submit = function () {

            $http.post("/admin/tables/inventorypurchase/__inbound", $scope.inbound).success(function (data) {

                $scope.invoicenumbers = data.updatedinvoices;

                $scope.status.push({
                    type: 'success',
                    message: data.message
                });


            }).error(function (data, status) {

                $scope.status.push({
                    type: 'danger',
                    message: status + data
                });
            });
        };

        $me.refreshInvoice();

    });


})();