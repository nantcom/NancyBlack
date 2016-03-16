
(function () {

    var mod = angular.module('Page', []);
    mod.controller('PageController', function ($scope, $http) {

        var me = this;

        $scope.object = window.allData.SaleOrder;
        $scope.branding = window.branding;
        $scope.isAdmin = window.isAdmin == "True" ? true : false;
        $scope.allStatus = ["New", "Confirmed", "WaitingForOrder", "AtLEVEL51", "Building", "Testing", "ReadyToShip", "Shipped", "Delivered", "Cancel"];
        $scope.allPaymentStatus = window.allData.PaymentStatusList;
        $scope.paymentLogs = window.allData.PaymentLogs;

        console.log("WARNING: Using Status list from client side");

        if ($scope.object.DHLTrackingNumber != null) {
            $scope.trackingUrl = $sce.trustAsResourceUrl('http://www.dhl.com/cgi-bin/tracking.pl?AWB=' + $scope.object.DHLTrackingNumber);
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

            $http.post("/support/notify/payment", { SaleOrderIdentifier: $scope.object.SaleOrderIdentifier })
                .then(function (success) {

                    swal({
                        title: "ตรวจสอบการชำระเงิน",
                        text: 'เราจะทำการตรวจสอบให้ท่านโดยเร็ว โดยคุณจะได้รับอีเมลล์แจ้งถึงการเปลี่ยนแปลงสถานะ ขอบคุณที่ใช้บริการค่ะ',
                        html: true
                    });

                }, function (error) {
                    alert(error.message);
                });

        }

        me.showTransferInfo = function () {

            swal({   
                title: "ชำระเงินโดยการโอนเงิน",   
                text: '<p>กรุณาโอนเงินมายัง:</p>' +

                      '<p>ธนาคารกสิกรไทย สาขาเสาชิงช้า <br/>' +
                      'เลขที่บัญชี: 004-2-45167-3<br/>' +
                      'ชื่อบัญชี: บจก. นันคอม</p>' +
                      
                      '<p style="margin-top: 20px">หลังการโอนเงิน กรุณาอัพโหลดสลิปการโอนเงินเข้ามายังระบบ โดยใช้แท็บ <b>แจ้งการชำระเงิน</b> ค่ะ</p>',  
                html: true
            });

        };
    });

    mod.controller('CountDownTimerController', function ($scope, $http) {

        if (window.allData.SaleOrder.Status == "Confirmed") {
            var createdAt = new Date(window.allData.SaleOrder.__createdAt);
            // 14 * 24 * 60 * 60 * 1000 = 1209600000 (2 weeks), 86400000 = 1 วัน
            var deadline = new Date(createdAt.setUTCHours(7) + 86400000);

            var daysSpan = null;
            var hoursSpan = null;
            var minutesSpan = null;
            var secondsSpan = null;

            function getTimeRemaining(endtime) {
                var t = endtime - Date.parse(new Date());
                var seconds = Math.floor((t / 1000) % 60);
                var minutes = Math.floor((t / 1000 / 60) % 60);
                var hours = Math.floor((t / (1000 * 60 * 60)) % 24);
                var days = Math.floor(t / (1000 * 60 * 60 * 24));
                return {
                    'total': t,
                    'days': days,
                    'hours': hours,
                    'minutes': minutes,
                    'seconds': seconds
                };
            }

            function initializeClock(id, endtime) {

                function updateClock() {
                    var t = getTimeRemaining(endtime);

                    daysSpan.innerHTML = t.days;
                    hoursSpan.innerHTML = ('0' + t.hours).slice(-2);
                    minutesSpan.innerHTML = ('0' + t.minutes).slice(-2);
                    secondsSpan.innerHTML = ('0' + t.seconds).slice(-2);
                    if (t.total <= 0) {
                        clearInterval(timeinterval);
                    }
                }

                var clock = document.getElementById(id);
                daysSpan = clock.querySelector('.days');
                hoursSpan = clock.querySelector('.hours');
                minutesSpan = clock.querySelector('.minutes');
                secondsSpan = clock.querySelector('.seconds');
                updateClock(); // run function once at first to avoid delay
                var timeinterval = setInterval(updateClock, 1000);
            }

            initializeClock('clockdiv', deadline);
        }

    })

    mod.filter('newline', function ($sce) {

        return function (input) {
            return $sce.trustAsHtml(input.replace(/\n/g, "<br/>"));
        }
    });


})();