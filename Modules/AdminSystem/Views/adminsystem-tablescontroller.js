
(function () {

    var module = angular.module('TablesModule', ['ui.bootstrap', 'ncb-database']);

    module.controller("TablesController", function ($scope, $rootScope, $http, ncbDatabaseClient) {

        var editorSample = new JSONEditor(document.getElementById('sampledata'));
        var $me = this;

        var ncbClient = new ncbDatabaseClient($me, $scope, "DataType", true );
        ncbClient.list();

        $scope.$watch('object.OriginalName',
            function () {

                if ($scope.object == null) {
                    return;
                }
                editorSample.setName($scope.object.OriginalName);
            }
        );

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

        this.scaffold = function () {

            $scope.isBusy = true;
            $scope.sampleData = editorSample.get();
            $http.post('/tables/DataType/Scaffold', $scope.sampleData).success(function (value) {

                $scope.object.Properties = value.Properties;
                $scope.isBusy = false;

                $('#TablesFormTabs a:first').tab('show');
            });
        }

        this.create = function () {
            $me.view({

                OriginalName: "NewEntity",
                Properties: [
                    {
                        Name: "Property1",
                        Type: "string"
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