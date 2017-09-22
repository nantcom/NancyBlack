
(function () {
    
    var module = angular.module('Page', ["ui.bootstrap"]);
    
    module.controller("PageController", function ($scope, $http, $cookies) {

        var $me = this;


        $scope.filterPath = function (url) {
            return function (item) {

                if (item.Path == null) {
                    return false;
                }
                return item.Path.indexOf(url) > 0;
            };
        };

        $scope.BTCRate = { "avg": 127787.54, "high": 129500.00, "low": 125003.00, "volume": "460.18767603", "open": "129100.00", "close": "129439.00" };
        $me.updateBTCRate = function () {

            var now = new Date();
            var date = now.getFullYear() + "-" + now.getMonth() + "-" + now.getDate();

            $http.get("https://bx.in.th/api/tradehistory/?pairing=1&date=" + date).
                success(function (data, status, headers, config) {

                    $scope.BTCRate = data.data;
                    $scope.BTCRate.avg = parseFloat($scope.BTCRate.avg);
                    $scope.BTCRate.high = parseFloat($scope.BTCRate.high);
                    $scope.BTCRate.low = parseFloat($scope.BTCRate.low);
                    $me.calculateAlternateRate();
                }).
                error(function (data, status, headers, config) {

                });
        };
        $me.updateBTCRate();

        $scope.isBusy = false;
        $scope.object = {};
        $scope.baseUrl = window.location.origin;

        if (window.data != null) { // dashboard

            $scope.data = window.data;

            $scope.data.alltraffic = 0;
            $scope.data.Traffic.forEach(function (item) {

                $scope.data.alltraffic += parseInt(item.Count);
            });

            $scope.data.altTotalCommission = 0;
            $scope.data.totalCommission = 0;
            $scope.data.averageBTCRate = 0;
            $scope.data.PendingCommission.forEach(function (item) {

                $scope.data.totalCommission += parseFloat(item.BTCAmount);
                $scope.data.averageBTCRate += parseFloat(item.BTCRate);
            });
            $scope.data.averageBTCRate = $scope.data.averageBTCRate / $scope.data.PendingCommission.length;

            $me.calculateAlternateRate = function () {

                $scope.data.PendingCommission.forEach(function (item) {

                    item.AlternateBTCAmount = item.BTCAmount * (item.BTCRate / $scope.BTCRate.low);
                    $scope.data.altTotalCommission += item.AlternateBTCAmount;
                });
            };
            $me.calculateAlternateRate();

        }

        $me.register = function (object) {

            $scope.isBusy = true;

            $http.post("/__affiliate/apply", object).success(function (data) {

                $scope.isBusy = false;
                swal("ทุกอย่างดูดี!".translate("Looks Good!"), "ขอรีเฟรชหน้านี้แป๊บนะ...".translate("Refreshing this page..."), "success");

                window.setTimeout(function () {
                    window.location.reload();
                }, 1000);

            }).error(function (data) {

                $scope.isBusy = false;
                swal("เกิดข้อผิดพลาด".translate("Something is not right!"), "โค๊ดนี้น่าจะมีคนจองไปแล้ว กรุณาลองใหม่อีกครั้งนะ".translate("Other Person might have used this code, Please try again"), "error");

            });

        };

        $me.requestPayment = function () {

            $scope.isBusy = true;

            $http.post("/__affiliate/requestpayment").success(function (data) {

                $scope.isBusy = false;
                swal("ทุกอย่างดูดี!".translate("Looks Good!"), "ขอรีเฟรชหน้านี้แป๊บนะ...".translate("Refreshing this page..."), "success");

                window.setTimeout(function () {
                    window.location.reload();
                }, 1000);

            }).error(function (data) {

                $scope.isBusy = false;
                swal("เกิดข้อผิดพลาด".translate("Something is not right!"), "โค๊ดนี้น่าจะมีคนจองไปแล้ว กรุณาลองใหม่อีกครั้งนะ".translate("Other Person might have used this code, Please try again"), "error");

            });

        };

        $scope.$on('ncb-membership.login', function (a, e) {

            $scope.object.Customer.User = e.user;
            $scope.object.Customer.FirstName = e.user.Profile.first_name;
            $scope.object.Customer.LastName = e.user.Profile.last_name;
            $scope.object.Customer.Email = e.user.Profile.email;

            fbq('track', 'CompleteRegistration');
            ga('send', 'event', 'Login Facebook');

        });
    });

})();