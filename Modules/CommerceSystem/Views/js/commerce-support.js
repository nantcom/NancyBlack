
(function () {

    var mod = angular.module('Page', []);
    mod.controller('PageController', function ($scope, $http, $sce) {

        var me = this;

        $scope.object = window.allData.SaleOrder;
        $scope.branding = window.branding;
        $scope.isAdmin = window.isAdmin == "True" ? true : false;
        $scope.allStatus = ["New", "Confirmed", "WaitingForOrder", "Delay", "AtLEVEL51", "Building", "Testing", "ReadyToShip", "Shipped", "Delivered", "Cancel"];
        $scope.allPaymentStatus = window.allData.PaymentStatusList;
        $scope.paymentLogs = window.allData.PaymentLogs;

        for (var i = 0; i < $scope.allStatus.length; i++) {
            if ($scope.object.Status == $scope.allStatus[i]) {
                $scope.StatusState = i;
            }
        }

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

                    swal(window.notifyMessage);

                }, function (error) {
                    alert(error.message);
                });

        }

        me.showTransferInfo = function () {

            swal(window.bankInfo);

        };
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