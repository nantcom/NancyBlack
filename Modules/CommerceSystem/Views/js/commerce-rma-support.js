
(function () {

    var mod = angular.module('Page', []);
    mod.controller('PageController', function ($scope, $http, $sce) {

        var me = this;

        $scope.object = window.allData.RMA;
        $scope.rmaItems = window.allData.RMAItems;
        $scope.branding = window.branding;
        $scope.isAdmin = window.isAdmin == "True" ? true : false;
        $scope.allStatus = ["InQueue", "CheckingIssues", "ResearchOnSolution", "WaitingForParts", "ReassembleAndTesting", "ReadyToShip", "Shipped", "Delivered"];
        $scope.methods = ["DHL", "Kerry", "Carrying"];
        $scope.isSerialSaving = false;
        $scope.newRMAItem = {};

        for (var i = 0; i < $scope.allStatus.length; i++) {
            if ($scope.object.Status == $scope.allStatus[i]) {
                $scope.StatusState = i;
            }
        }

        me.addRMAItem = function () {
            $scope.isSerialSaving = true;

            $http.post("/rma/" + $scope.object.RMAIdentifier + "/add", $scope.newRMAItem)
                .then(function (success) {
                    alert("Sucess! Please refresh view");
                    $scope.isSerialSaving = false;
                }, function (error) {
                    alert(error.message);
                    $scope.isSerialSaving = false;
                });
        }

        console.log("WARNING: Using Status list from client side");

        me.getTrackUrl = function (trackingNumber, method) {
            if (trackingNumber == null) {
                return null;
            }

            if (method == $scope.methods[0]) {
                return $sce.trustAsResourceUrl('http://www.dhl.com/cgi-bin/tracking.pl?AWB=' + trackingNumber);
            }

            if (method == $scope.methods[1]) {
                return $sce.trustAsResourceUrl('https://th.kerryexpress.com/th/track/?track=' + trackingNumber);
            }
        }

        me.showTransferInfo = function () {

            swal(window.bankInfo);

        };
    });

    mod.filter('newline', function ($sce) {

        return function (input) {
            return $sce.trustAsHtml(input.replace(/\n/g, "<br/>"));
        }
    });


})();