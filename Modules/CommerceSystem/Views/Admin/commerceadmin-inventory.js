(function () {

    var module = angular.module('InventoryAdminModule', ['ui.bootstrap', 'angular.filter']);

    module.controller("InventoryDashboard2", function ($scope, $rootScope, $http, $window) {

        var $me = this;

        $scope.object = {};
        $scope.WeekOutlook = [];
        $scope.WeekOutlookView = [];
        $scope.WeekOutlookBySO = [];
        $scope.SaleOrderById = [];
        $scope.ProductInfo = [];

        window.data.ProductInfo.forEach(function (value, index) {

            $scope.ProductInfo[value.Id] = value;
        });

        window.data.PriceList.forEach(function (price, index) {

            if ($scope.ProductInfo[price.ProductId] != null) {
                $scope.ProductInfo[price.ProductId].BuyingPrice = price.PriceExVat;
                $scope.ProductInfo[price.ProductId].BuyingCurrency = price.Currency;
            }
        });

        window.data.PendingSaleOrders.forEach(function (so, index) {

            var date;

            if (so.DueDate < so.__createdAt) {
                date = moment(so.__createdAt).add(2, 'isoWeek');
            }
            else {
                date = moment(so.DueDate);
            }

            var key = date.startOf('isoWeek').format('D MMMM YYYY');

            so.RequiredItems = [];
            so.HasRequest = false;

            if ($scope.WeekOutlook[key] == null) {

                $scope.WeekOutlook[key] = {
                    SaleOrders: [],
                    Key: key,
                    Order: 0,
                    MinDate: date,
                    MaxDate: date,
                    RequiredItems: {},
                    RequiredItemsView: []
                };

                $scope.WeekOutlookView.push($scope.WeekOutlook[key]);
            }

            $scope.SaleOrderById[so.Id] = so;
            $scope.WeekOutlookBySO[so.Id] = $scope.WeekOutlook[key];

            $scope.WeekOutlook[key].SaleOrders.push(so);
            $scope.WeekOutlook[key].MinDate = $scope.WeekOutlook[key].MinDate.isAfter(date) ? date : $scope.WeekOutlook[key].MinDate; 
            $scope.WeekOutlook[key].MaxDate = $scope.WeekOutlook[key].MaxDate.isBefore(date) ? date : $scope.WeekOutlook[key].MaxDate; 

            $scope.WeekOutlook[key].Order = $scope.WeekOutlook[key].MinDate.unix();
        });

        window.data.InventoryRequests.forEach(function (value, index) {

            var soId = value.SaleOrderId;
            var pId = value.ProductId;
            if ($scope.SaleOrderById[soId] == null) {

                console.log("Orphaned InventoryItem:" + value.Id);
                return;
            }

            if ($scope.WeekOutlookBySO[soId].RequiredItems[pId + ''] == null) {
                $scope.WeekOutlookBySO[soId].RequiredItems[pId + ''] = {
                    ProductId: pId,
                    SupplierId: $scope.ProductInfo[pId] == null ? -1 : $scope.ProductInfo[pId].SupplierId,
                    UseBy: new Set(),
                    UseByArray: [],
                    Qty: 0,
                    RequestList: [],
                };
                $scope.WeekOutlookBySO[soId].RequiredItemsView.push($scope.WeekOutlookBySO[soId].RequiredItems[pId + '']);
            }

            $scope.SaleOrderById[soId].HasRequest = true;

            $scope.WeekOutlookBySO[soId].RequiredItems[pId + ''].RequestList.push(value);
            $scope.WeekOutlookBySO[soId].RequiredItems[pId + ''].Qty++;
            $scope.WeekOutlookBySO[soId].RequiredItems[pId + ''].UseBy.add(soId);
            $scope.WeekOutlookBySO[soId].RequiredItems[pId + ''].UseByArray = [...$scope.WeekOutlookBySO[soId].RequiredItems[pId + ''].UseBy];
        });

        $me.getTotalGroup = function (group) {

            var total = {};
            window.multicurrency.available.forEach(function (currency) {

                total[currency] = 0;
            });

            group.RequiredItemsView.forEach(function (product) {

                var productInfo = $scope.ProductInfo[product.ProductId];
                if (productInfo == null) {
                    return;
                }

                if (productInfo.BuyingCurrency == null) {
                    return;
                }

                var current = total[productInfo.BuyingCurrency];
                current += product.Qty * productInfo.BuyingPrice;

                total[productInfo.BuyingCurrency] = current;
            });

            return total;
        };

        $me.getTotalAllCurrency = function (totalGroup) {

            var total = 0;

            for (key in totalGroup) {

                if (totalGroup[key] == 0) {
                    continue;
                }

                total += $scope.multicurrency.toCurrentCurrency(totalGroup[key], key);

            }

            return total;
        };

        $me.hasSaleOrderWithoutInventoryRequest = function (group) {

            var has = true;
            group.SaleOrders.forEach(function (value, index) {

                has &= value.HasRequest;
            });

            return !has;
        };

        $me.setPriceDialog = function ( productId ) {

            $scope.object = {
                Id: 0,
                ProductId: productId,
                Currency: $scope.localization.Currency
            };
            $("#PriceDialog").modal("show");
        };

        $me.updatePrice = function (object) {

            $scope.object = {};
            $scope.ProductInfo[object.ProductId].BuyingCurrency = object.Currency;
            $scope.ProductInfo[object.ProductId].BuyingPrice = object.PriceExVat;

            $("#PriceDialog").modal("hide");

        };

        $me.modifyInventoryRequest = function (request, list) {

            $scope.currentList = list;
            $scope.object = request;
            $("#InventoryItemDialog").modal("show");
        };

        $me.deletedRequest = function (object) {

            $("#InventoryItemDialog").modal("hide");
            window.location.reload();

        };

        $me.updatedRequest = function (object) {

            $("#InventoryItemDialog").modal("hide");
            window.location.reload();

        };

    });

    module.controller("InventoryNotFullfilled2", function ($scope, $rootScope, $http, $window) {

        var $me = this;
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
        
        $scope.saleOrderStatus = [];
        $window.data.PendingSaleOrders.forEach(function (item) {

            $scope.saleOrderStatus[item.Id] = item.Status;
        });

        $scope.inventoryRequestsByProduct = [];
        $window.data.InventoryRequestRaw.forEach(function (item) {

            if ( item.Id == 2806 )
            {
                console.log( item );
            }

            var existing = $scope.inventoryRequestsByProduct[item.ProductId];
            if (existing == null ) {
                existing = [];
                $scope.inventoryRequestsByProduct[item.ProductId] = existing;
            }
            existing.push( item );
        });

        if ($window.commerceSettings.inventory == null) {
            $window.commerceSettings.inventory = {};
        }

        if ($window.commerceSettings.inventory.priceOverrides == null) {
            $window.commerceSettings.inventory.priceOverrides = [];
        }

        $me.updateRequiredDashboard = function () {

            var total = 0;
            for (var i = 0; i < $scope.data.length; i++) {

                var pId = $scope.data[i].ProductId;
                $scope.data[i].Url = $window.productUrlLookup[pId];
                $scope.data[i].SupplierId = $window.productSupplierLookup[pId];

                var qtyStock = instockLookup[pId];
                if (qtyStock == null) {
                    qtyStock = 0;
                }
                $scope.data[i].QtyStock = qtyStock;

                var diff = $scope.data[i].QtyStock - $scope.data[i].Qty;
                diff = diff * -1;

                var price = $scope.averagePrices[pId];
                if (price == null) {
                    total += $scope.data[i].SoldPrice * diff;
                    $scope.data[i].Price = $scope.data[i].SoldPrice;
                } else {

                    total += price * diff;
                    $scope.data[i].Price = price;
                }

                $scope.data[i].Requests = $scope.inventoryRequestsByProduct[pId];
                $scope.data[i].SaleOrderWaiting = [];
                $scope.data[i].SaleOrderIncoming = [];
                $scope.data[i].Requests.forEach(function (item) {

                    var waitingForOrder = $scope.saleOrderStatus[item.SaleOrderId] == "WaitingForOrder";
                    if (waitingForOrder) {
                        $scope.data[i].SaleOrderWaiting.push(item.SaleOrderId);
                    }
                    else {
                        $scope.data[i].SaleOrderIncoming.push(item.SaleOrderId);
                    }

                });

            }

            $scope.totalValue = total;
        }

        $http.get("/admin/tables/inventoryitem/__averageprice").success(function (data) {
            
            for (var i = 0; i < data.length; i++) {
                var key = data[i].ProductId;
                $scope.averagePrices[key] = data[i].Price;

            };

            for( var key in $window.commerceSettings.inventory.priceOverrides) {
                
                if ($window.commerceSettings.inventory.priceOverrides[key] != null) {
                    $scope.averagePrices[parseInt(key)] = $window.commerceSettings.inventory.priceOverrides[key];
                }
            }
        


            $me.updateRequiredDashboard();

        });
        
        $me.overridePrice = function (item) {

            var newPrice = prompt("Enter Override Price", item.Price);
            if (newPrice == false) {
                return;
            }
            $scope.averagePrices[item.ProductId] = parseFloat(newPrice);

            $window.commerceSettings.inventory.priceOverrides[item.ProductId + ""] = $scope.averagePrices[item.ProductId];
            $http.post('/Admin/sitesettings/commerce', $window.commerceSettings)
                .success(function (data) {
                    
                })
                .error(function (data) {
                    alert("Failed to Save Settings");
                });

            $me.updateRequiredDashboard();
        };
        
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

        var $me = this;
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

        $me.getTotal = function (obj, setVat) {

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

            if (setVat != null) {
                $me.IsPriceIncludeVat = 0;
            }

            if ( $me.IsPriceIncludeVat == 1 ) 
            {
                var withoutVat = total * 100 / ($window.commerceSettings.billing.vatpercent + 100);
                obj.Tax = total - withoutVat;                
                obj.Total = total + obj.Shipping + obj.Additional; 
                obj.TotalProductValue = withoutVat;
            }
            else
            {   
                if (setVat) {

                    obj.Tax = parseFloat( setVat );

                } else {

                    obj.Tax = ($window.commerceSettings.billing.vatpercent / 100) * total;
                }


                obj.Total = total + obj.Tax + obj.Shipping + obj.Additional; 
                obj.TotalProductValue = total;
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

        var $me = this;
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