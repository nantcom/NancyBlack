
(function () {

    var mod = angular.module('Page', []);
    mod.controller('PageController', function ($scope, $http, $sce, $window) {

        var me = this;
        $scope.trackingUrl = "about:blank";
        $scope.inboundTrackingUrl = "about:blank";

        $scope.object = window.allData.SaleOrder;
        $scope.branding = window.branding;
        $scope.isAdmin = window.isAdmin == "True" ? true : false;
        $scope.allStatus = ["New", "Confirmed", "WaitingForOrder", "Delay", "OrderProcessing", "InTransit", "CustomsClearance", "Inbound", "WaitingForParts", "Building", "PartialBuilding", "Testing", "ReadyToShip", "Shipped", "Delivered", "Cancel"];
        $scope.allPaymentStatus = window.allData.PaymentStatusList;
        $scope.paymentLogs = window.allData.PaymentLogs;
        $scope.window = $window;

        for (var i = 0; i < $scope.allStatus.length; i++) {
            if ($scope.object.Status == $scope.allStatus[i]) {
                $scope.StatusState = i;
            }
        }

        var receiptIndex = -1;
        for (var i = 0; i < $scope.paymentLogs.length; i++) {
            var log = $scope.paymentLogs[i];
            if (log.IsPaymentSuccess) {
                receiptIndex++;

                log.receiptIndex = receiptIndex;
            }
        }

        console.log("WARNING: Using Status list from client side");

        if ($scope.object.DHLTrackingNumber != null) {
            $scope.trackingUrl = $sce.trustAsResourceUrl('http://www.dhl.com/cgi-bin/tracking.pl?AWB=' + $scope.object.DHLTrackingNumber);
        }
        
        if ($scope.object.InboundDHLTrackingNumber != null) {
            $scope.inboundTrackingUrl = $sce.trustAsResourceUrl('http://www.dhl.com/cgi-bin/tracking.pl?AWB=' + $scope.object.InboundDHLTrackingNumber);
        }

        if ($scope.object.InboundTrackingNumber != null) {

            if ($scope.object.InboundShippingMethod == 'DHL') {
                $scope.inboundTrackingUrl = $sce.trustAsResourceUrl('https://track.aftership.com/dhl/' + $scope.object.InboundTrackingNumber);
            }

            if ($scope.object.InboundShippingMethod == 'FedEx') {
                $scope.inboundTrackingUrl = $sce.trustAsResourceUrl('https://track.aftership.com/fedex/' + $scope.object.InboundTrackingNumber);
            }
        }

        if ($scope.object.OutboundTrackingNumber != null) {

            if ($scope.object.ShippingDetails.provider == 'DHL') {
                $scope.trackingUrl = $sce.trustAsResourceUrl('http://www.dhl.com/cgi-bin/tracking.pl?AWB=' + $scope.object.OutboundTrackingNumber);
            }

            if ($scope.object.ShippingDetails.provider == 'Kerry') {
                $scope.trackingUrl = $sce.trustAsResourceUrl('https://th.kerryexpress.com/th/track/?track=' + $scope.object.OutboundTrackingNumber);
            }

            if ($scope.object.ShippingDetails.method == 'deliveree') {
                $scope.trackingUrl = $sce.trustAsResourceUrl($scope.object.OutboundTrackingNumber);
            }
        }

        $scope.laptopStatus = [];
        var status = ["Deploy Keyword", "Deadpicel checking"];

        me.getAtteByType = function (type) {

            if ($scope.object.Attachments == null) {

                $scope.object.Attachments = [];
            }

            for (var i = 0; i < $scope.object.Attachments.length; i++) {
                if ($scope.object.Attachments[i].AttachmentType == type) {
                    return $scope.object.Attachments[i];
                }
            }

            var newAttr = { CreateDate: (new Date()).toISOString(), AttachmentType: type, Url: "", DisplayOrder: 0 };
            $scope.object.Attachments.push(newAttr);
            return newAttr;
        }


        // Create Deploy Step for first time
        for (var i = 0; i < status.length; i++) {
            $scope.laptopStatus.push(me.getAtteByType(status[i]));
        }

        var lookup = [];
        for (var i = 0; i < $scope.object.ItemsDetail.length; i++) {
            var item = $scope.object.ItemsDetail[i];

            lookup[item.Id] = item;
        }

        if ($scope.object.CustomData == null) {
            $scope.object.CustomData = {};
        }

        // Delete Unneccessary data from Custom Data
        delete $scope.object.CustomData.ram1;
        delete $scope.object.CustomData.ram2;
        delete $scope.object.CustomData.ram3;
        delete $scope.object.CustomData.ram4;
        delete $scope.object.CustomData.m21;
        delete $scope.object.CustomData.m22;
        delete $scope.object.CustomData.hdd1;
        delete $scope.object.CustomData.hdd2;
        delete $scope.object.CustomData.cpu1;
        delete $scope.object.CustomData.gpu1;
        delete $scope.object.CustomData.body1;

        if ($scope.object.CustomData.SerialNumbers == null) {
            $scope.object.CustomData.SerialNumbers = [];
        }

        for (var i = 0; i < $scope.object.Items.length; i++) {
            var id = $scope.object.Items[i];
            var product = lookup[id];

            if (product == null) {
                continue;
            }


            if (product.Url.indexOf("/products/parts/os") == 0 ||
                product.Url.indexOf("/promotions") == 0) {

                continue;
            }

            // Add only the missing parts
            var dupes = false;
            for (var j = 0; j < $scope.object.CustomData.SerialNumbers.length; j++) {

                var existing = $scope.object.CustomData.SerialNumbers[j];
                if (existing.index == i && existing.Title == product.Title) {
                    dupes = true;
                    break;
                }
            }

            if (dupes) {
                continue;
            }
            $scope.object.CustomData.SerialNumbers.push({ index: i, Title: product.Title, Serial: "" });
        }

        me.saveAttachmentMessage = function (url, message) {

            $http.post("/support/" + $scope.object.SaleOrderIdentifier + "/save/attachment/message", { Url: url, Message: message })
                .then(function (success) {
                    alert("Sucess!");
                }, function (error) {
                    alert(error.message);
                });

        }

        me.notifyForCheckingPayment = function () {

            fbq('track', 'Purchase', {
                value: $scope.object.TotalAmount,
                currency: 'THB'
            });

            $http.post("/support/notify/payment", { SaleOrderIdentifier: $scope.object.SaleOrderIdentifier })
                .then(function (success) {

                    swal(window.notifyMessage);

                }, function (error) {
                    alert(error.message);
                });

        }

        me.showTransferInfo = function () {

            swal(window.bankInfo);

        };
        
        $scope.cryptoRate = {
            OMG: 300,
            BTC: 100000,
            ETC: 9000
        };
        me.updateCryptoRate = function () {
            
            $http.get("/__commerce/cryptoquote").
                success(function (data, status, headers, config) {

                    $scope.cryptoRate = data;
                    console.log($scope.cryptoRate);

                }).
                error(function (data, status, headers, config) {

                });
        };
        me.updateCryptoRate();
        
        me.showAddress = function ( currency ) {

            $http.get("/__commerce/" + $scope.object.Id + "/cryptodeposit/" + currency).
                success(function (data, status, headers, config) {

                    swal({
                        title: "ชำระเงินด้วย " + currency,
                        text: '<p>Address:</p>' +

                        '<b>' + data.address + '</b>' +
                        '<p> จำนวน </p>' +
                        '<b>' + data.amount + ' ' + currency + '</b>' +
                        '<p><img src="' + data.qrcode + '" /></p>' +

                        '<p style="margin-top: 20px">หลังการโอนกรุณาอัพโหลดภาพ Screenshot เข้ามายังระบบ โดยใช้ปุ่ม <b>แจ้งตรวจสอบการชำระเงิน</b> ค่ะ</p>',
                        html: true
                    });

                }).
                error(function (data, status, headers, config) {

                });

        };
        
        
        if (window.location.href.indexOf("?paymentsuccess") > 0) {

            var trackpayment = (function () {

                if (typeof(fbq) == "undefined") {
                    window.setTimeout(trackpayment, 1000);
                    return;
                }

                fbq('track', 'Purchase', {
                    value: $scope.object.TotalAmount,
                    currency: 'THB'
                });
            });

            trackpayment();

        }
    });

    mod.controller('CountDownTimerController', function ($scope, $http) {

        $scope.days = 0;
        $scope.hours = 0;
        $scope.minutes = 0;
        $scope.seconds = 0;

        if (window.allData.SaleOrder.Status == "Confirmed") {
            var createdAt = new Date(window.allData.SaleOrder.__createdAt);
            // 14 * 24 * 60 * 60 * 1000 = 1209600000 (2 weeks), 86400000 = 1 วัน
            var deadline = new Date(createdAt.setUTCSeconds(0) + 432000000);
            //var deadline = new Date(2016, 4, 16, 23, 59, 59);

            function update() {
                var t = deadline - Date.parse(new Date());

                $scope.$apply(function () {

                    $scope.seconds = Math.floor((t / 1000) % 60);
                    $scope.minutes = Math.floor((t / 1000 / 60) % 60);
                    $scope.hours = Math.floor((t / (1000 * 60 * 60)) % 24);
                    $scope.days = Math.floor(t / (1000 * 60 * 60 * 24));
                });

            }

            window.setInterval(update, 1000);
        }

    })

    mod.filter('newline', function ($sce) {

        return function (input) {
            return $sce.trustAsHtml(input.replace(/\n/g, "<br/>"));
        }
    });


})();