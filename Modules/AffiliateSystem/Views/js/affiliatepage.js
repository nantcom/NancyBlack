
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


        $scope.isBusy = false;
        $scope.object = {};
        $scope.baseUrl = window.location.origin;

        if (window.data != null) { 

            $scope.data = window.data;

            $scope.data.altTotalCommission = 0;
            $scope.data.totalCommission = 0;
            $scope.data.totalApprovePending = 0;
            $scope.data.totalPaid = 0;
            $scope.data.totalCanWithdraw = 0;
            $scope.data.AffiliateTransaction.forEach(function (item) {

                $scope.data.totalCommission += parseFloat(item.CommissionAmount);

                if (item.IsCommissionPaid) {
                    $scope.data.totalPaid += parseFloat(item.CommissionAmount);
                }

                if (item.IsPendingApprove) {
                    $scope.data.totalApprovePending += parseFloat(item.CommissionAmount);
                }

                if (item.IsPendingApprove == false && item.IsCommissionPaid == false) {
                    $scope.data.totalCanWithdraw += parseFloat(item.CommissionAmount);
                }
            });

            $scope.so = null;

            var paid = $scope.data.SaleOrders.filter(so => so.PaymentStatus == "PaymentReceived" || so.PaymentStatus == "Deposit");
            if (paid.length > 0) {

                paid.sort(function (a, b) {

                    return a.Id - b.Id;

                });

                $scope.so = paid[paid.length - 1];
            }

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


        $me.updateProfile = function (object) {

            $scope.isBusy = true;
            object.UserId = $scope.data.Registration.NcbUserId;

            $http.post("/__affiliate/updateprofile", object).success(function (data) {

                $scope.isBusy = false;
                swal("ทุกอย่างดูดี!".translate("Looks Good!"), "ข้อมูลอัพเดทแล้ว".translate("Your Profile was Updated."), "success");
                

            }).error(function (data) {

                $scope.isBusy = false;
                swal("เกิดข้อผิดพลาด".translate("Something is not right!"), "เป็นอะไรไม่รู้อะ กรุณาลองใหม่อีกครั้งนะ".translate("Please try again"), "error");

            });

        };

        $me.requestPayment = function () {

            if (confirm("กรุณากด OK เพื่อยืนยันว่าคุณต้องการดำเนินการเบิกเงินส่วนแบ่ง เราจะติดต่อไปเพื่อขอข้อมูลเพิ่มเติม และดำเนินการโอนเงินทาง PromptPay ที่ผูกกับหมายเลขโทรศัพท์ที่คุณระบุไว้เท่านั้น") == false) {

                return;
            }

            $scope.isBusy = true;

            $http.post("/__affiliate/requestpayment", { btcaddress: data.Registration.BTCAddress }).success(function (data) {

                $scope.isBusy = false;
                swal("ทุกอย่างดูดี!".translate("Looks Good!"), "ขอรีเฟรชหน้านี้แป๊บนะ...".translate("Refreshing this page..."), "success");

                window.setTimeout(function () {
                    window.location.reload();
                }, 1000);

            }).error(function (data) {

                $scope.isBusy = false;
                swal("เกิดข้อผิดพลาด", "กรุณาลองใหม่อีกครั้ง", "error");

            });
        };

        $me.getRewards = function ( name ) {

            $scope.isBusy = true;

            $http.post("/__affiliate/getrewards", { rewardsName: name }).success(function (data) {

                $scope.isBusy = false;
                swal(data);
                

            }).error(function (data) {

                $scope.isBusy = false;
                swal("เกิดข้อผิดพลาด", "กรุณาลองใหม่อีกครั้ง", "error");

            });
        };

    });

})();