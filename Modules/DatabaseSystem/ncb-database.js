(function () {

    if (!window.location.origin) {
        window.location.origin = window.location.protocol + "//" + window.location.hostname + (window.location.port ? ':' + window.location.port : '');
    }

    var mobileService = WindowsAzure.MobileServiceClient;
    var path = window.location.origin;

    if (window.databasepath) {
        path = path + "/" + window.databasepath;

    } else {

        if (window.location.pathname.indexOf("/SuperAdmin") == 0) {
            path = path + "/system";
        }
    }

    var client = new mobileService(
                    path,
                    '-');

    var ncb = angular.module("ncb-database", []);
    
    ncb.factory('ncbDatabaseClient', function ($http) {
        return function (controller, $scope, tableName, disableJsonify, $angularScope) {

            $scope.tableName = tableName;

            // allow this client to work with plain object
            if (controller == null) {

                controller = {};
            }

            // allow this client to work with $scope that is not angular object
            if ($scope.$apply == null) {

                if ($angularScope != null) {

                    $scope.$apply = function (a) {

                        $angularScope.$apply(a);
                    };

                } else {
                    $scope.$apply = function (a) {

                        if (typeof (a) == "function") {
                            a();
                        }
                    };
                }
            }

            if ($scope.$watch == null) {

                if ($angularScope != null) {

                    $scope.$watch = function (s, a) {

                        $angularScope.$watch(s, a);
                    };

                } else {
                    $scope.$watch = function () { }; // cannot actually do anything
                }
            }

            var $me = this;
            $me.$scope = $scope;
            $me.$controller = controller;
            $me.$table = client.getTable(tableName);
            $me.disableJsonify = disableJsonify;

            $me.handleError = function (err) {

                $scope.$apply(function () {
                    err.type = "danger";
                    $scope.alerts.push(err);;

                    $scope.isBusy = false;
                });
            };

            $me.processServerObject = function (item) {

                if (item.Id != null) {

                    item.id = item.Id;
                    delete item.Id;
                }

                for (var key in item) {

                    // fields that has word 'JSONText' will be de-serialized
                    // into the field without 'JSONText' word
                    // text is added to designate that SiSoDB should use ntext type
                    if (key.indexOf("JSONText") > 0) {

                        var value = JSON.parse(item[key]);
                        item[key.replace("JSONText", "")] = value;

                    }
                }

                for (var key in item) {

                    // fields that has word 'JSONText' will be de-serialized
                    // into the field without 'JSONText' word
                    // text is added to designate that SiSoDB should use ntext type
                    if (key.indexOf("JSONText") > 0) {

                        var value = JSON.parse(item[key]);
                        item[key.replace("JSONText", "")] = value;

                    }
                }

                return item;
            };

            this.listFilter = function () {

                if (controller.listFilter != null) {
                    return controller.listFilter();
                }

                return $me.$table;
            };

            this.list = function () {


                $scope.isBusy = true;

                var source = $me.listFilter();

                if ($scope.paging.page > 0) {

                    source = source.skip(($scope.paging.page - 1) * $scope.paging.size);
                }

                if ($scope.paging.size > 0 ) {

                    source = source.take($scope.paging.size);
                }

                source.read().done(function (results) {

                    $scope.$apply(function () {

                        $scope.list = results;
                        $scope.isBusy = false;

                        if (results.length == 0) {

                            $scope.paging.total = $scope.paging.page * $scope.paging.size;
                        }

                        $scope.list.forEach($me.processServerObject);

                        $.event.trigger({
                            type: "ncb-database",
                            action: "listed",
                            list: $scope.list,
                            sender: $me
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

                if (toSave.Id == null) {
                    delete toSave.Id;
                }

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

                                $scope.object = $me.processServerObject( result );

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

                            $.event.trigger({
                                type: "ncb-database",
                                action: "save",
                                object: $scope.object,
                                sender: $me,
                            });


                        }, $me.handleError
                    );

                } else {

                    toSave.__createdAt = new Date();

                    $me.$table.insert(toSave).done(
                        function (result) {

                            $scope.$apply(function () {

                                $scope.object = $me.processServerObject( result );

                                $scope.list.push($scope.object);
                                $scope.isBusy = false;
                            });

                            $.event.trigger({
                                type: "ncb-database",
                                action: "insert",
                                object: $scope.object,
                                sender: $me,
                            });
                            
                            $.event.trigger({
                                type: "ncb-database",
                                action: "save",
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

            // refreshlookup for UI-select
            this.refreshLookup = function (tableName, search, fieldName, done) {

                if (done == null) {
                    done = function () { };
                }

                if ($scope.lookup == null) {
                    $scope.lookup = {};
                }

                var targetFieldName = fieldName;
                if (targetFieldName == null) {
                    targetFieldName = "Title";
                }

                var targetLookupName = tableName + "By" + targetFieldName;
                if (targetFieldName == "Title") {
                    targetLookupName = tableName;
                }

                var buildLookupTable = function (data) {

                    var lookupTable = [];

                    data.forEach(function (item) {
                        lookupTable[item.Id] = item;
                    });

                    return lookupTable;
                };

                var url = '/tables/' + tableName + "?$orderby=" + targetFieldName;
                if (search != null && search != "") {

                    url = '/tables/' + tableName + "?$top=10&$orderby=" + targetFieldName + "&$filter=startswith(" + targetFieldName + ",'" + search + "')";
                }

                $scope.timestamp = (new Date()).getTime();
                url += "&ts=" + $scope.timestamp;

                $http.get(url).
                  success(function (data, status, headers, config) {
                      $scope.lookup[targetLookupName] = buildLookupTable(data);
                      done();
                  }).
                  error(function (data, status, headers, config) {
                      $scope.alerts.push({
                          type: 'warning',
                          message: 'Lookup Error'
                      });
                  });


            };

            /// get an item from database
            this.get = function (id, callback) {

                $me.$table.lookup(id).done(function (result) {
                    
                    callback(result);

                }, $me.handleError);
            };

            $scope.$watch("paging.page", function (newValue, oldValue) {

                if (oldValue != newValue) {
                    // refresh the data due to paging change
                    $me.list();
                }

            });

            // Initialize standard controller properties
            $scope.isBusy = false;
            $scope.alerts = [];
            $scope.list = [];
            $scope.timestamp = (new Date()).getTime();
            $scope.paging = {

                page: 1,
                size: 25,
                total: 250,
            };

            controller.$table = $me.$table;
            controller.save = $me.save;
            controller.list = $me.list;
            controller.del = $me.del;
            controller.delete = $me.del;
            controller.refreshLookup = $me.refreshLookup;
            
            controller.closeAlert = function (index) {
                $scope.alerts.splice(index, 1);
            };

        };
    });

})();