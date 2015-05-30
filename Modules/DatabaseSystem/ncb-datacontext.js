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

    var ncb = angular.module("ncb-datacontext", []);
    

    // Data Context provides neccessary functions  to access nancyblack database
    // by leveraging azure mobile service api
    ncb.directive('ncbDatacontext', ['$http', function ($http) {

        function link($scope, element, attrs) {
            
            if (attrs.table == null) {

                throw "DataContext requries table attribute"
            }

            var $me = this;
            
            $scope.data = {};
            $scope.object = null;
            $scope.isBusy = false;
            $scope.alerts = [];
            $scope.list = [];
            $scope.timestamp = (new Date()).getTime();
            $scope.table = client.getTable( attrs.table );
            $scope.paging = {

                page: 1,
                size: 10,
                total: 20,
            };

            //#region Shared Function

            $me.handleError = function (err) {

                $scope.$apply(function () {
                    err.type = "danger";
                    $scope.alerts.push(err);

                    $scope.isBusy = false;
                });
            };

            //#endregion

            $me.processServerObject = function (item) {

                if (item.Id != null) {

                    item.id = item.Id;
                    delete item.Id;
                }

                return item;
            };

            if ($scope.model == null) {

                // in model
                $scope.data.refresh = function (filter) {

                    var source = $scope.table;

                    if (filter != null) {

                        source = source.filter(source);
                    } else {

                        source = source.orderByDescending("Id")
                    }

                    if ($scope.paging.page > 0) {

                        source = source.skip(($scope.paging.page - 1) * $scope.paging.size);
                    }

                    if ($scope.paging.size > 0) {

                        source = source.take($scope.paging.size);
                    }

                    source.read().done(function (results) {

                        $scope.$apply(function () {

                            $scope.isBusy = false;

                            $scope.list = results;
                            $scope.list.forEach($me.processServerObject);

                            if (results.length < $scope.paging.size) {

                                $scope.paging.total = $scope.paging.page * $scope.paging.size;

                            } else {

                                $scope.paging.total = ($scope.paging.page * $scope.paging.size) + 1;
                            }

                            if ($scope.onrefreshed != null) {

                                $scope.onrefreshed($scope, results);
                            }
                        });

                    }, $me.handleError);

                };

                // and have reload function
                $scope.data.reload = function ( object ) {

                    if ($scope.object.id == null) {
                        $scope.object.id = $scope.object.Id;
                    }

                    $scope.table.lookup($scope.object.id).done(function (result) {

                        $scope.object = result;

                        if ($scope.onrefreshed != null) {

                            $scope.onrefreshed($scope, result);
                        }

                    }, $me.handleError);
                };
                
            } else {

                $scope.object = $me.processServerObject($scope.model);

                $scope.isModelDeleted = false;
                $scope.originalModel = JSON.stringify($scope.model);

                // refresh with model will refresh the object
                $scope.data.reload = function () {

                    if (object.id == null) {
                        object.id = object.Id;
                    }

                    $scope.table.lookup(object.id).done(function (result) {

                        object = result;

                        if ($scope.onrefreshed != null) {

                            $scope.onrefreshed($scope, result);
                        }

                    }, $me.handleError);
                };

                // restore the model to original value
                $scope.data.restore = function () {

                    $scope.object = JSON.parse($scope.originalModel);
                };
            }

            $scope.data.save = function ( object ) {

                // create a copy of object to save
                var toSave = JSON.parse(JSON.stringify(object));

                $scope.isBusy = true;
                $scope.timestamp = (new Date()).getTime();

                // fix the 'id' casing
                if (toSave.id == null) {

                    toSave.id = toSave.Id;
                    delete toSave.Id;
                }

                // delete extra info
                delete toSave.$type;
                delete toSave.$$hashKey;

                if (toSave.id != null) {

                    $scope.table.update(toSave).done(
                        function (result) {

                            $scope.$apply(function () {

                                $scope.isBusy = false;
                                object = $me.processServerObject(result);

                                if ($scope.onupdated != null) {

                                    $scope.onupdated($scope, object);
                                }
                            });

                        }, $me.handleError
                    );

                } else {

                    $scope.table.insert(toSave).done(
                        function (result) {

                            $scope.$apply(function () {

                                object = $me.processServerObject(result);
                                $scope.data.refresh();

                                if ($scope.oninserted != null) {

                                    $scope.oninserted($scope, object);
                                }

                            });

                        }, $me.handleError
                    );

                }

            };

            $scope.data.delete = function ( object ) {

                if ($scope.object != null) {

                    // use model if specified
                    object = $scope.object;
                }

                if (object.id == null) {                    
                    object.id = object.Id;
                }

                if (confirm("are you completely sure about this?") == false) {
                    return;
                }

                $scope.table.del(object)
                    .done(function () {

                        $scope.$apply(function () {

                            $scope.isBusy = false;

                            if ($scope.list != null) {

                                var index = $scope.list.indexOf(toDelete);
                                $scope.list.splice(index, 1);
                            }

                            if ($scope.ondeleted != null) {

                                $scope.ondeleted($scope, object);
                            }

                            if ($scope.object != null) {

                                $scope.isModelDeleted = true;
                            }
                        });
                        

                    }, $me.handleError);
            };
            
            $scope.closeAlert = function (index) {
                $scope.alerts.splice(index, 1);
            };

        }

        return {
            restrict: 'A',
            link: link
        };
    }]);

    // Data table is designed to be attached to list or table
    ncb.directive('ncbDatatable', ['$http', function ($http) {

        function link($scope, element, attrs) {

            var $me = this;

            var stopWatch = $scope.$watch("data", function () {

                $me.initialize();
            });

            $me.initialize = function () {

                stopWatch();

                if ($scope.data != null) {

                    $scope.data.refresh();

                    // focus on the given object
                    $scope.data.view = function (item) {
                        $scope.object = item;
                    };
                }
            };
        }

        return {
            restrict: 'A',
            link: link
        };
    }]);


})();