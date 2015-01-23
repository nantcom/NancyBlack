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

            this.$scope = $scope;
            this.$table = client.getTable(tableName);
            this.disableJsonify = disableJsonify;

            var $me = this;
            $me.handleError = function (err) {

                $scope.$apply(function () {
                    $scope.alerts.push(err);;
                    $scope.isBusy = false;
                });
            };

            $me.dispatchEvent = function (name) {

                var evt = document.createEvent("CustomEvent");
                evt.initCustomEvent(name, false, false, {
                    'data': $scope.object,
                    'type': $table,
                    'id': $scope.object.id,
                    'scope': $scope,
                });
                window.dispatchEvent(evt);

            };

            this.listFilter = function () {

                if (controller.listFilter != null) {
                    return controller.listFilter;
                }

                return $table;
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

                        if (controller.onListed) {
                            controller.onListed();
                        }


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

                $table.del(toDelete)
                    .done(function () {

                        $scope.$apply(function () {

                            var index = $scope.list.indexOf(toDelete);

                            $scope.list.splice(index, 1);
                            $scope.isBusy = false;
                            $scope.isDeleted = true;
                        });
                        
                        if (controller.onDeleted) {
                            controller.onDeleted();
                        }

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
                if (this.disableJsonify == false || this.disableJsonify == null) {

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

                    $table.update(toSave).done(
                        function (result) {

                            $scope.$apply(function () {

                                $scope.isBusy = false;
                                $scope.timestamp = (new Date()).getTime();
                                $scope.files = null;
                            });

                            $me.dispatchEvent("updated");

                        }, $me.handleError
                    );

                } else {

                    $table.insert(toSave).done(
                        function (result) {

                            $scope.$apply(function () {
                                $scope.object.id = result.Id;

                                if (result.AttachmentUrl) {
                                    $scope.object.AttachmentUrl = result.AttachmentUrl;
                                }

                                $scope.list.push($scope.object);
                                $scope.isBusy = false;
                            });
                            $me.dispatchEvent("inserted");

                        }, $me.handleError
                    );

                }

            };
            
            // Initialize standard controller properties
            $scope.object = null;
            $scope.isBusy = false;
            $scope.isDeleted = false;
            $scope.alerts = [];
            $scope.list = [];
            $scope.timestamp = (new Date()).getTime();

            controller.$table = this.$table;
            controller.save = this.save;
            controller.list = this.list;
            controller.del = this.del;

        };
    });

})();