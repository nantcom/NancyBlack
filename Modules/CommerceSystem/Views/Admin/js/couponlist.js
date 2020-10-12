﻿(function () {
    'use strict';

    angular
        .module('coupon', ["ngTable", 'ui.tree'])
        .controller('coupon_list', coupon_list);

    coupon_list.$inject = ['ngTableParams', '$location', '$log', '$scope', '$window', '$http', '$rootScope'];

    function coupon_list(ngTableParams, $location, $log, $scope, $window, $http, $rootScope) {
        /* jshint validthis:true */
        var vm = this;

        vm.createCoupon = function () {
            $scope.object = {};
        };

        $scope.object = {};

        $scope.filters = {};

        $scope.tableParams = new ngTableParams({
            page: 1,            // show first page
            count: 10,          // count per page
            sorting: {      // initial sorting
            },
            filter: $scope.filters // initial filters
        }, {
            total: 0, // length of data
            getData: function ($defer, params) {

                $scope.isBusy = true;
                var oDataQueryParams = _oDataAddFilterAndOrder(params);

                $scope.data.inlinecount(oDataQueryParams, function (data) {

                    $scope.isBusy = false;
                    $defer.resolve(data.Results);
                    params.total(data.Count);
                });

            }
        });
    }
})();
