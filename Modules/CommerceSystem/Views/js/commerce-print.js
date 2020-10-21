

(function () {

    var mod = angular.module('Page', []);
    mod.controller('PageController', function ($scope, $http) {


        var $me = this;

        $scope.so = window.data.SaleOrder;
        $scope.rc = window.data.Receipt;
        $scope.paymentDetail = window.data.PaymentDetail;
        $scope.billing = window.billing;
        $scope.branding = window.branding;

        $scope.discount = { Price: 0 };

        $scope.getPriceBeforeVat = function (Price) {
            return Price * 100 / 107;
        };

        $scope.getProductValue = function () {

            var grandTotal = $scope.getProductTotal() + $scope.discount.Price;
            var beforeVat = $scope.getPriceBeforeVat(grandTotal);

            return beforeVat;
        };

        $scope.getBathText = function (inputNumber) {

            var getText = function (input) {
                var toNumber = input.toString();
                var numbers = toNumber.split('').reverse();

                var numberText = "/หนึ่ง/สอง/สาม/สี่/ห้า/หก/เจ็ด/แปด/เก้า/สิบ".split('/');
                var unitText = "/สิบ/ร้อย/พ้น/หมื่น/แสน/ล้าน".split('/');

                var output = "";
                for (var i = 0; i < numbers.length; i++) {
                    var number = parseInt(numbers[i]);
                    var text = numberText[number];
                    var unit = unitText[i];

                    if (number == 0)
                        continue;

                    if (i == 1 && number == 2) {
                        output = "ยี่สิบ" + output;
                        continue;
                    }

                    if (i == 1 && number == 1) {
                        output = "สิบ" + output;
                        continue;
                    }

                    output = text + unit + output;
                }

                return output;
            }

            var fullNumber = Math.floor(inputNumber);
            var decimal = inputNumber - fullNumber;

            if (decimal == 0) {

                return getText(fullNumber) + "บาทถ้วน";

            } else {

                // convert decimal into full number, need only 2 digits
                decimal = decimal * 100;
                decimal = Math.round(decimal);

                return getText(fullNumber) + "บาท" + getText(decimal) + "สตางค์";
            }

        };

        $scope.getVat = function () {

            var grandTotal = $scope.getProductTotal() + $scope.discount.Price;
            var beforeVat = $scope.getPriceBeforeVat(grandTotal);

            return grandTotal - beforeVat;
        };

        $scope.getProductTotal = function () {

            var total = 0;
            for (var i = 0; i < $scope.ItemsDetail.length; i++) {

                total += $scope.ItemsDetail[i].Price * $scope.ItemsDetail[i].Attributes.Qty;
            }

            return total;
        };

        $scope.getTotalBeforeVat = function () {
            var total = $scope.getTotal();
            return total / 1.07;
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


        if ($scope.paymentDetail.PaymentRemaining != $scope.so.TotalAmount) {
            $scope.paymentType = 'Split';
        }
        else {
            $scope.paymentType = 'Pay All';
        }

        $scope.ItemsDetail = [];
        $scope.type = window.formType;

        if ($scope.type == 'checklist') {

            for (var i = 0; i < $scope.so.ItemsDetail.length; i++) {

                if ($scope.so.ItemsDetail[i].Attributes.Qty > 0) {
                    $scope.ItemsDetail.push($scope.so.ItemsDetail[i]);
                }
            }

            $scope.ItemsDetail.push({ Title: 'Check HDD/SSD initilization', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Driver SystemX, SoundBlaster and ControlCenter', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Sound, Mic, Camera and WIFI', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check All Port: USB, HTMI and Jack', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Device Manager', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Screw', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Grip Whole Body', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Scrach Whole Body', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Keyboard\'s Grip and Swollen', Url: '', Attributes: { Qty: 1 } });
            $scope.ItemsDetail.push({ Title: 'Check Dead Pixels', Url: '', Attributes: { Qty: 1 } });
        }
        else { // for reciept and non-checklist
            for (var i = 0; i < $scope.so.ItemsDetail.length; i++) {

                if ($scope.so.ItemsDetail[i].CurrentPrice == 0) {
                    continue;
                }

                if ($scope.so.ItemsDetail[i].CurrentPrice > 0) {
                    $scope.ItemsDetail.push($scope.so.ItemsDetail[i]);
                }

                if ($scope.so.ItemsDetail[i].CurrentPrice < 0) {
                    $scope.discount.Price += $scope.so.ItemsDetail[i].Price;
                }

            }
        }

        if ($scope.so.ShippingFee == 0 && $scope.so.ShippingInsuranceFee > 0) {
            $scope.ItemsDetail.push({
                Title: "Shipping Insurance",
                Attributes: { Qty: 1 },
                Price: $scope.so.ShippingFee + $scope.so.ShippingInsuranceFee,
            });
        }

        if ($scope.so.ShippingFee > 0 && $scope.so.ShippingInsuranceFee > 0) {
            $scope.ItemsDetail.push({
                Title: "Shipping (with Insurance)",
                Attributes: { Qty: 1 },
                Price: $scope.so.ShippingFee + $scope.so.ShippingInsuranceFee,
            });
        }

        if ($scope.so.ShippingFee > 0 && $scope.so.ShippingInsuranceFee == 0) {
            $scope.ItemsDetail.push({
                Title: "Shipping",
                Attributes: { Qty: 1 },
                Price: $scope.so.ShippingFee,
            });
        }

        if ($scope.so.PaymentFee > 0) {
            $scope.ItemsDetail.push({
                Title: "Payment Fee",
                Attributes: { Qty: 1 },
                Price: $scope.so.PaymentFee,
            });
        }

        $scope.toPay = $scope.paymentDetail.PaymentRemaining

        this.checkAmount = function (amount) {

            if (amount > $scope.paymentDetail.PaymentRemaining) {

                return false;
            }

            return true;
        };

        $me.filterZero = function (item) {

            if (item.Attributes != null && item.Attributes.Qty == 0) {
                return false;
            }

            return true;
        };

        $me.getOrder = function (item) {

            if (item.Url.indexOf("/laptops") >= 0) {
                return 0;
            }

            if (item.Url.indexOf("/cpu") >= 0) {
                return 100;
            }

            if (item.Url.indexOf("/thermal") >= 0) {
                return 110;
            }

            if (item.Url.indexOf("/gpu") >= 0) {
                return 200;
            }

            if (item.Url.indexOf("/monitor") >= 0) {
                return 300;
            }

            if (item.Url.indexOf("/calibrate") >= 0) {
                return 310;
            }

            if (item.Url.indexOf("/ram") >= 0) {
                return 400;
            }

            if (item.Url.indexOf("/m2") >= 0) {
                return 500;
            }

            if (item.Url.indexOf("/hdd") >= 0) {
                return 510;
            }

            if (item.Url.indexOf("/keyboard") >= 0) {
                return 600;
            }


            return 999;
        };

    });

    mod.filter('newline', function ($sce) {

        return function (input) {
            return $sce.trustAsHtml(input.replace(/\n/g, "<br/>"));
        }
    });


})();