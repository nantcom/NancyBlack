(function () {

    var mobileService = WindowsAzure.MobileServiceClient;
    var path = window.location.origin;

    if (window.location.pathname.indexOf("/SuperAdmin") == 0) {
        path = path + "/system";
    }

    var client = new mobileService(
                    path,
                    '-');

    var ncb = angular.module("ncb-database", []);
    
    ncb.factory('ncbDatabaseClient', function () {
        return function (controller, $scope, tableName, disableJsonify) {

            $scope.tableName = tableName;

            var $me = this;
            $me.$scope = $scope;
            $me.$controller = controller;
            $me.$table = client.getTable(tableName);
            $me.disableJsonify = disableJsonify;

            $me.handleError = function (err) {

                $scope.$apply(function () {
                    $scope.alerts.push(err);;
                    $scope.isBusy = false;
                });
            };

            this.listFilter = function () {

                if (controller.listFilter != null) {
                    return controller.listFilter;
                }

                return $me.$table;
            };

            this.list = function () {


                $scope.isBusy = true;

                $me.listFilter().read().done(function (results) {

                    $scope.$apply(function () {

                        $scope.list = results;
                        $scope.isBusy = false;

                        $scope.list.forEach(function (item) {

                            item.id = item.Id;
                            delete item.Id;

                            for (var key in item) {

                                // fields that has word 'JSONText' will be de-serialized
                                // into the field without 'JSONText' word
                                // text is added to designate that SiSoDB should use ntext type
                                if (key.indexOf("JSONText") > 0) {

                                    var value = JSON.parse(item[key]);
                                    item[key.replace("JSONText", "")] = value;

                                }
                            }

                        });

                        $.event.trigger({
                            type: "ncb-database",
                            action: "listed",
                            list: $scope.list
                        });

                    });

                }, $me.handleError);

            };

            this.del = function (toDelete) {

                $scope.error = null;
                $scope.timestamp = (new Date()).getTime();

                if (toDelete.id == null) {
                    return;
                }

                if (confirm("are you completely sure about this?") == false) {
                    return;
                }

                $me.$table.del(toDelete)
                    .done(function () {

                        $scope.$apply(function () {

                            var index = $scope.list.indexOf(toDelete);

                            $scope.list.splice(index, 1);
                            $scope.isBusy = false;
                            $scope.isDeleted = true;
                        });

                        $.event.trigger({
                            type: "ncb-database",
                            action: "deleted",
                            object: $scope.object,
                            sender: $me,
                        });

                        $("#" + tableName + "Modal").modal('hide');

                    }, $me.handleError);
            };

            this.save = function () {

                if ($scope.object == null) {
                    return;
                }

                // create a copy of object to save
                var toSave = JSON.parse(JSON.stringify($scope.object));

                $scope.error = null;
                $scope.isBusy = true;
                $scope.timestamp = (new Date()).getTime();

                delete toSave.$$hashKey;

                // fix the 'id' casing
                if (toSave.Id != null && $scope.id == null) {
                    toSave.id = toSave.Id;
                    delete toSave.Id;
                }

                // find the properties where type is Object or Array
                // and JSON stringify it
                if ($me.disableJsonify == false || $me.disableJsonify == null) {

                    for (var key in toSave) {
                        var type = ({}).toString.call(toSave[key]).match(/\s([a-zA-Z]+)/)[1].toLowerCase();

                        if (type == "object" || type == "array") {

                            var value = JSON.stringify(toSave[key]);
                            toSave[key + "JSONText"] = value;
                            delete toSave[key];
                        }
                    }

                }

                if (toSave.id != null) {

                    $me.$table.update(toSave).done(
                        function (result) {

                            $scope.$apply(function () {

                                $scope.isBusy = false;
                                $scope.timestamp = (new Date()).getTime();
                                $scope.files = null;
                            });

                            $.event.trigger({
                                type: "ncb-database",
                                action: "update",
                                object: $scope.object,
                                sender: $me,
                            });


                        }, $me.handleError
                    );

                } else {

                    $me.$table.insert(toSave).done(
                        function (result) {

                            $scope.$apply(function () {
                                $scope.object.id = result.Id;

                                $scope.list.push($scope.object);
                                $scope.isBusy = false;
                            });

                            $.event.trigger({
                                type: "ncb-database",
                                action: "insert",
                                object: $scope.object,
                                sender: $me,
                            });

                        }, $me.handleError
                    );

                }

            };
            
            this.lookup = function (tableName) {
                
                if ($scope.lookup == null) {
                    $scope.lookup = [];
                }

                if ($scope.lookup[tableName] != null) {

                    return $scope.lookup[tableName];
                }

                $scope.lookup[tableName] = [];

                var table = zumo.getTable(tableName);
                table.read().done(function (results) {

                    $scope.$apply(function () {

                        $scope.isBusy = false;

                        var lookupTable = [];

                        results.forEach(function (item) {
                            lookupTable[item.Id] = item;
                        });

                        $scope.lookup[tableName] = lookupTable;
                    });

                }, $me.handleError );


                return []; // return empty array first and update later

            };

            // Initialize standard controller properties
            $scope.object = null;
            $scope.isBusy = false;
            $scope.alerts = [];
            $scope.list = [];
            $scope.timestamp = (new Date()).getTime();

            controller.$table = $me.$table;
            controller.save = $me.save;
            controller.list = $me.list;
            controller.del = $me.del;
            
            controller.closeAlert = function (index) {
                $scope.alerts.splice(index, 1);
            };

        };
    });

})();