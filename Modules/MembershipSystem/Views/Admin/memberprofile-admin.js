(function () {
    'use strict';

    var app = angular.module('Membership', []);

    app.controller('MembershipView', function ($scope, $http) {

        var me = this;

        $scope.member = window.allData.Member;
        $scope.purchaseHistory = window.allData.PurchaseHistory
    });

})();