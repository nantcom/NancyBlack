(function () {

    var mobileService = WindowsAzure.MobileServiceClient;
    var client = new mobileService(
                    window.location.origin,
                    '-');

    var ncb = angular.module("ncb", []);

    ncb.value("zumo", client);

    // Common Dialog code
    ncb.factory('ncbDialog', function () {

        return function (formId) {

            var form = $(formId);
            var scope = angular.element(form).scope();
            scope.timestamp = (new Date()).getTime();

            this.show = function (object, onDialogClosed) {

                scope.$apply(function () {

                    scope.files = [];
                    scope.object = object;
                });

                form.modal('show');

                var hiddenHandler = null;
                hiddenHandler = function () {

                    if (typeof (onDialogClosed) == "function") {

                        onDialogClosed();
                    }

                    form.off('hidden.bs.modal', hiddenHandler);
                };

                form.on('hidden.bs.modal', hiddenHandler);

                $(formId + ' a:first').tab('show');
                form.modal('show');
            };

        };
    });

    ncb.factory('ncbOps', function () {
        return function ($scope, $table, formId) {

            this.$table = $table;
            this.$scope = $scope;

            var $me = this;
            $me.handleError = function (err) {

                $scope.$apply(function () {
                    $scope.error = err;
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

            this.startScope = function () {

                $scope.object = null;
                $scope.files = [];
                $scope.isBusy = false;
                $scope.isDeleted = false;
                $scope.error = null;
                $scope.list = [];
                $scope.timestamp = (new Date()).getTime();

            };

            this.listFilter = function () {

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

                        });

                    });

                }, function (err) {

                    $scope.$apply(function () {

                        $scope.error = err;
                    });
                });

            };

            this.del = function (toDelete, afterDelete) {

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

                        $(formId).modal('hide');
                        $me.dispatchEvent("deleted");

                        if (afterDelete) {
                            afterDelete();
                        }

                    }, $me.handleError);
            };

            this.save = function () {

                if ($scope.object == null) {
                    return;
                }

                $scope.error = null;
                $scope.isBusy = true;
                $scope.timestamp = (new Date()).getTime();

                delete $scope.object.$$hashKey;

                if ($scope.files !== null && $scope.files.length === 1) {

                    $scope.object.AttachmentBase64 = $scope.files[0].dataRaw;
                }

                if ($scope.object.id != null) {

                    $table.update($scope.object).done(
                        function (result) {

                            $scope.$apply(function () {

                                if (result.AttachmentUrl) {
                                    $scope.object.AttachmentUrl = result.AttachmentUrl;
                                }

                                $scope.isBusy = false;
                                $scope.timestamp = (new Date()).getTime();
                                $scope.files = null;
                            });

                            $me.dispatchEvent("updated");

                        }, $me.handleError
                    );

                } else {

                    $table.insert($scope.object).done(
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

        };
    });

})();