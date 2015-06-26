(function () {

    if (!window.location.origin) {
        window.location.origin = window.location.protocol + "//" + window.location.hostname + (window.location.port ? ':' + window.location.port : '');
    }

    var mobileService = WindowsAzure.MobileServiceClient;
    var path = window.location.origin;
    var emittedEvents = {
        refreshed: "refreshed",
        updated: "updated",
        inserted: "inserted",
        deleted: "deleted"
    };

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
    
    var dataContext = function link($scope, element, attrs) {

        if (attrs.table == null) {

            throw "DataContext requries table attribute"
        }

        var $me = this;

        $scope.emittedEvents = emittedEvents;

        $scope.data = {};
        $scope.object = null;
        $scope.isBusy = false;
        $scope.alerts = [];
        $scope.list = [];
        $scope.timestamp = (new Date()).getTime();
        $scope.table = client.getTable(attrs.table);
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

            // no model
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

                        $scope.$emit(emittedEvents.refreshed, { sender: $scope, args: results });
                    });

                }, $me.handleError);

            };

            // reload is 
            $scope.data.reload = function () {

                throw "Reload is not available unless model is specified";
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
                    $scope.$emit(emittedEvents.refreshed, { sender: $scope, args: results });

                }, $me.handleError);
            };

            // restore the model to original value
            $scope.data.restore = function () {

                $scope.object = JSON.parse($scope.originalModel);
            };
        }

        // Insert is, unlike save, always create new object in the backend
        $scope.data.insert = function (object) {
            
            delete object.id;
            delete object.Id;
                
            $scope.data.save(object, function () {
                
                // clears the object after inserted
                for (var k in object) {
                    object[k] = null;
                }

            });

        };

        $scope.data.save = function (object, callback) {

            if (object == null) {

                throw "object cannot be null";
            }

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

                            $scope.$emit(emittedEvents.updated, { sender: $scope, args: object });

                            if (callback != null) {
                                callback();
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

                            $scope.$emit(emittedEvents.inserted, { sender: $scope, args: object });

                            if (callback != null) {
                                callback();
                            }
                        });

                    }, $me.handleError
                );

            }

        };

        $scope.data.delete = function (object) {

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

                        $scope.$emit(emittedEvents.deleted, { sender: $scope, args: object });

                        if ($scope.object != null) {

                            $scope.isModelDeleted = true;
                        }
                    });


                }, $me.handleError);
        };

        $scope.closeAlert = function (index) {
            $scope.alerts.splice(index, 1);
        };

    };

    // Data Context provides neccessary functions  to access nancyblack database
    // by leveraging azure mobile service api
    ncb.directive('ncbDatacontext', ['$http', function ($http) {

        return {
            restrict: 'A',
            link: dataContext,
            scope: true
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

    ncb.directive('ncbSavebutton', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            var $me = this;

            element.attr("ng-disabled", "isBusy");
            element.attr("ng-click", "data.save(object)");

            element.prepend( '<i class="fa fa-spin fa-circle-o-notch" ng-show="isBusy"></i>')
            element.removeAttr("ncb-savebutton"); // prevent infinite loop

            var template = $compile(element);
            template($scope);
        }

        return {
            restrict: 'A',
            link: link,
            terminal: true, // we will use $compile - so we want to stop all other directives
            priority: 9999, // make sure we got compiled first
        };
    }]);

    ncb.directive('ncbInsertbutton', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            var $me = this;

            element.attr("ng-disabled", "isBusy");
            element.attr("ng-click", "data.insert(object)");

            element.prepend('<i class="fa fa-spin fa-circle-o-notch" ng-show="isBusy"></i>')
            element.removeAttr("ncb-insertbutton"); // prevent infinite loop

            var template = $compile(element);
            template($scope);
        }

        return {
            restrict: 'A',
            link: link,
            terminal: true, // we will use $compile - so we want to stop all other directives
            priority: 9999, // make sure we got compiled first
        };
    }]);

})();