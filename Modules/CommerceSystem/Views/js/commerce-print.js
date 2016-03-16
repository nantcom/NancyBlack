

(function () {

    var mod = angular.module('Page', []);
    mod.controller('PageController', function ($scope, $http) {

        $scope.so = window.data.SaleOrder;
        $scope.paymentDetail = window.data.PaymentDetail;
        $scope.billing = window.billing;
        $scope.branding = window.branding;

        //#region copy from ncb-commerce

        $scope.getTotal = function () {
            return $scope.so.TotalAmount - $scope.so.ShippingFee - $scope.so.ShippingInsuranceFee - $scope.so.PaymentFee;
        };

        $scope.getPriceBeforeVat = function (Price) {
            return Price * 0.9345794392523364485981308411215;
        };

        $scope.getPriceBeforeVatWithGap = function (Price) {
            var result = $scope.getPriceBeforeVat(Price);
            var total = $scope.getTotal();
            var beforeVat = $scope.getPriceBeforeVat(total);
            var vatPirce = total - beforeVat;
            var margin = total - (beforeVat + vatPirce);

            return result + margin;
        };

        $scope.getVatNew = function () {
            var total = $scope.getTotal();
            return total - $scope.getPriceBeforeVat(total);
        };

        $scope.getVatFromSplitedPayment = function () {
            var price = $scope.getTotalFromSplitedPayment();
            return price - $scope.getPriceBeforeVat(price);
        };

        $scope.getTotalFromSplitedPayment = function () {
            return $scope.paymentDetail.TransactionLog[$scope.paymentDetail.SplitedPaymentIndex].Amount;
        };

        //#endregion

        var Round = function (price) {
            return Math.round(price * 100) / 100;
        }

        if ($scope.paymentDetail.PaymentRemaining != $scope.so.TotalAmount) {
            $scope.paymentType = 'Split';
        }
        else {
            $scope.paymentType = 'Pay All';
        }

        $scope.ItemsDetail = [];
        $scope.type = window.formType;
        if (window.formType == 'receipt') {
            for (var i = 0; i < $scope.so.ItemsDetail.length; i++) {
                if ($scope.so.ItemsDetail[i].Price != 0) {
                    $scope.ItemsDetail.push($scope.so.ItemsDetail[i]);
                }
            }
        }
        else {
            $scope.ItemsDetail = $scope.so.ItemsDetail;
        }

        var totalWithoutVat = 0;
        for (var i = 0; i < $scope.ItemsDetail.length; i++) {
            $scope.ItemsDetail[i].LineTotal = Round($scope.getPriceBeforeVat($scope.ItemsDetail[i].Price * $scope.ItemsDetail[i].Attributes.Qty));
            $scope.ItemsDetail[i].Price = Round($scope.getPriceBeforeVat($scope.ItemsDetail[i].Price));
            totalWithoutVat += $scope.ItemsDetail[i].LineTotal;
        }

        totalWithoutVat = Round(totalWithoutVat);
        var gap = Round($scope.getTotal() - (totalWithoutVat + Round($scope.getVatNew())));
        $scope.ItemsDetail[0].LineTotal = Round($scope.ItemsDetail[0].LineTotal + gap);
        $scope.ItemsDetail[0].Price = Round($scope.ItemsDetail[0].Price + (gap / $scope.ItemsDetail[0].Attributes.Qty));

        //alert(totalWithoutVat);
        //alert(gap);

        $scope.toPay = $scope.paymentDetail.PaymentRemaining

        this.checkAmount = function (amount) {

            if (amount> $scope.paymentDetail.PaymentRemaining) {

                return false;
            }

            return true;
        };
        
    });

    mod.filter('newline', function ($sce) {

        return function (input) {
            return $sce.trustAsHtml(input.replace(/\n/g, "<br/>"));
        }
    });


})();