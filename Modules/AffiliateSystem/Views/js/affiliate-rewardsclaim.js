(function () {
    'use strict';

    var app = angular.module('AffiliateRewardsApp', []);
    
    app.controller('AffiliateRewardsController', function ($scope, $http) {

        var me = this;

        $scope.rewardsClaims = window.allData.AffiliateRewardsClaims;
        $scope.logisticsCompanies = window.allData.LogisticsCompanies;

        me.openTrackingUrl = function (claim) {
            var logisticsCompanies = $.grep($scope.logisticsCompanies, function (e) { return e.Id == claim.ShipByLogisticsCompanyId; });
            if (logisticsCompanies.length == 0) {
                alert('selected logistics company is not found.');
            }
            else {
                window.open(logisticsCompanies[0].TrackingUrlFormat.replace("{0}", claim.TrackingCode));
            }
        }
    });

})();