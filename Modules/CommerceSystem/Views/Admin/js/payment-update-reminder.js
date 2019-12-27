(function () {
    'use strict';

    angular
        .module('Page', [])
        .filter('PurStatusFilter', PurStatusFilter)
        .controller('PageController', PageController);

    PageController.$inject = ['$location', '$window', '$scope', '$http', '$log', '$filter'];

    function PageController($location, $window, $scope, $http, $log) {
        /* jshint validthis:true */
        var vm = this;

        //$filter('filtername');

        $scope.purStatuses = window.purStatuses;
        $scope.object = {};
        $scope.object.purStatuses = $scope.purStatuses;

        $scope.object.updateStatus = function (pur, statusId) {

            $http.post('/admin/pur/statusupdate', { purId: pur.id, status: statusId })
                .then(function (response) {
                    pur = response.data;
                    alert("updated");
                }, function (error) {
                    alert(error.data.Message);
                });
        };
    }

    function PurStatusFilter() {

        return function (input) {

            for (var i = 0; i < window.purStatuses.length; i++) {
                if (window.purStatuses[i].Item1 === input) {
                    return window.purStatuses[i].Item2;
                }
            }

            return 'not found';
        };
    }
})();
