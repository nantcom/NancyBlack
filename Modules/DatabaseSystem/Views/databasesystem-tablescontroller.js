
(function () {

    var module = angular.module('TablesModule', ['ui.bootstrap']);

    module.controller("TablesController", function ($scope, $rootScope, $http) {

        var $me = this;
        $scope.dataTypes = ["int", "double", "Boolean", "String", "DateTime", "Guid"];

        this.view = function (object) {

            $scope.object = object;

            $("#DataTypeModal").modal('show');
            $scope.object.Properties.forEach(function (e) {

                delete e.$$hashKey;
            });

            $scope.object.Properties.sort(function (p1, p2) {
                return p1.Name.charCodeAt(0) - p2.Name.charCodeAt(0)
            });

            $scope.sampleData = {};
            editorSample.set($scope.sampleData);
            editorSample.setName($scope.object.OriginalName);
        }

        this.create = function () {
            $me.view({

                OriginalName: "NewEntity",
                Properties: [
                    {
                        Name: "Property1",
                        Type: "String"
                    }
                ],

            });
        }

        $scope.newProperty = { Name: '', Type: 'string' };
        this.appendProperty = function (newProperty) {

            $scope.object.Properties.push(newProperty);
            $scope.newProperty = {};

        };

        this.removeProperty = function (p) {

            var index = $scope.object.Properties.indexOf(p);
            $scope.object.Properties.splice(index, 1);

        };

    });

})();