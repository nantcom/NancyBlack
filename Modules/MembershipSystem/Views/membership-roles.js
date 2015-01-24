(function () {

    //http://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
    function generateUUID() {
        var d = new Date().getTime();
        var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (d + Math.random() * 16) % 16 | 0;
            d = Math.floor(d / 16);
            return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
        });
        return uuid;
    };

    var module = angular.module('RolesModule', ['ui.bootstrap', 'ncb-database']);

    module.controller("RoleController", function ($scope, $rootScope, $http, ncbDatabaseClient) {

        var $me = this;
        $scope.newitem = {};
        
        var ncbClient = new ncbDatabaseClient($me, $scope, "__Enrollment");
        ncbClient.list();

        $(document).on("ncb-database", function (e) {
            
            if (e.sender == ncbClient && e.action == "insert") {
                $scope.$apply(function () {
                    $scope.newitem = {};
                });
            }
        });

        this.removeEmail = function (email) {

            $scope.isEmailValid = null;

            delete $scope.newitem.UserId;
            delete $scope.newitem.UserGuid;
            delete $scope.newitem.User;
        };

        this.checkEmail = function (email) {

            $scope.isBusy = true;

            $http.get("/tables/User?$filter=(Email eq '" + email + "')").
              success(function (data, status, headers, config) {
                  
                  $scope.isEmailValid = data.length > 0;

                  if (data.length > 0) {

                      $scope.newitem.UserId = data[0].Id;
                      $scope.newitem.UserGuid = data[0].Guid;
                      $scope.newitem.User = data[0];
                  } else {

                      $scope.newitem.email = null;
                      delete $scope.newitem.UserId;
                      delete $scope.newitem.UserGuid;
                      delete $scope.newitem.User;
                  }

                  $scope.isBusy = false;
              }).
              error(function (data, status, headers, config) {

                  $scope.isBusy = false;
              });
        };

        this.create = function () {

            $scope.object = $scope.newitem;
            delete $scope.object.email;

            if ($scope.object.Code == null || $scope.object.Code.trim() == "") {

                if ($scope.object.User == null) {
                    $scope.object.Code = generateUUID();
                }
            }

            ncbClient.save();
        };
    });

})();