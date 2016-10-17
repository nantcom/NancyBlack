(function () {
    'use strict';

    var module = angular.module('membershipadmin', []);

    module.controller('MembershipController', function ($location, $log, $scope, $window, $http, $rootScope) {
        


    });


    module.controller('RoleController', function ($location, $log, $scope, $window, $http, $rootScope) {



    });
    
    module.controller('EnrollController', function ($location, $log, $scope, $window, $http, $rootScope) {

        var $me = this;

        $me.createEnrollment = function () {

            //http://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
            var generateUUID = function () {
                var d = new Date().getTime();
                if (window.performance && typeof window.performance.now === "function") {
                    d += performance.now(); //use high-precision timer if available
                }
                var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                    var r = (d + Math.random() * 16) % 16 | 0;
                    d = Math.floor(d / 16);
                    return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
                });
                return uuid;
            };

            $scope.object = {};
            $scope.object.EnrollCode = generateUUID();

        };

    });
})();