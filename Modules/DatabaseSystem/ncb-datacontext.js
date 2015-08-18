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
        deleted: "deleted",
        loaded: "ncb-datacontext.loaded",
        uploaded: "ncb-datacontext.uploaded",
        deleted: "ncb-datacontext.deleted",
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

    var dataContext = function ($scope, element, attrs, $http) {

        if (attrs.table == null) {

            throw "DataContext requries table attribute"
        }

        //if ($scope.data != null) {

        //    throw "DataContext was already initialized in current scope"
        //}

        var $me = this;

        $scope.emittedEvents = emittedEvents;

        $scope.data = {};

        if ($scope.object == undefined) {
            $scope.object = null;
        } else {

            $scope.originalObject = JSON.parse(JSON.stringify($scope.object));
        }

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

                var detail;
                try {
                    detail = JSON.parse(err.request.response);
                    detail.type = "danger";
                    detail.msg = detail.Message;
                } catch (e) {

                    detail = {
                        type: 'danger',
                        msg: err
                    };
                }

                $scope.alerts.push(detail);

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

        $scope.data.refresh = function () {

            var source = $scope.table;
            source = source.orderByDescending("Id")

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

        // Insert is, unlike save, always create new object in the backend
        $scope.data.insert = function (object, callback) {

            delete object.id;
            delete object.Id;

            $scope.data.save(object, function () {

                // clears the object after inserted
                for (var k in object) {
                    object[k] = null;
                }

                if (callback != null) {

                    callback();
                }

                object.inserted = true;

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

                        object = $me.processServerObject(result);
                        $scope.$emit(emittedEvents.updated, { sender: $scope, args: object });

                        if (callback != null) {

                            try {
                                callback(object);
                            } catch (e) {

                            }
                        }

                        $scope.$apply(function () {

                            $scope.isBusy = false;
                            $scope.alerts.push({

                                type: 'success',
                                msg: 'Item was saved.'
                            });
                        });

                    }, $me.handleError
                );

            } else {

                $scope.table.insert(toSave).done(
                    function (result) {

                        object = $me.processServerObject(result);

                        if (callback != null) {

                            try {
                                callback(object);
                            } catch (e) {

                            }
                        }

                        $scope.$emit(emittedEvents.inserted, { sender: $scope, args: object });

                        $scope.$apply(function () {

                            $scope.data.refresh();
                            $scope.alerts.push({

                                type: 'success',
                                msg: 'Item ID:' + object.id + ' was created.'
                            });
                        });

                    }, $me.handleError
                );

            }

        };

        $scope.data.copy = function (object, callback) {

            if (confirm("Copy?") == false) {

                return;
            }

            // create a copy of object and remove key properties
            var toSave = JSON.parse(JSON.stringify(object));
            delete toSave.Id;
            delete toSave.id;
            delete toSave.__createdAt;
            delete toSave.__updatedAt;

            $scope.data.save(toSave, callback);
        };

        // query the database using odata
        $scope.data.query = function (oDataQuery, callback) {

            $scope.isBusy = true;

            $scope.table.read(oDataQuery).done(function (results) {
                
                results.forEach($me.processServerObject);
                if (callback != null) {

                    callback(results);
                }

                $scope.$apply(function () {

                    $scope.isBusy = false;
                });

            }, $me.handleError);
        };

        $scope.data.count = function (oDataQuery, callback) {

            $scope.isBusy = true;

            $scope.table.read(oDataQuery).done(function (results) {
                
                if (callback != null) {

                    callback(results);
                }

                $scope.$apply(function () {

                    $scope.isBusy = false;
                });

            }, $me.handleError);
        };

        $scope.data.inlinecount = function (oDataQuery, callback) {

            $scope.isBusy = true;

            $scope.table.read(oDataQuery).done(function (results) {

                results.Results.forEach($me.processServerObject);
                if (callback != null) {

                    callback(results);
                }

                $scope.$apply(function () {

                    $scope.isBusy = false;
                });

            }, $me.handleError);

        };

        // get specific item using id
        $scope.data.getById = function (id, callback) {

            $scope.isBusy = true;

            $scope.table.lookup(id).done(function (result) {

                $me.processServerObject( result );
                if (callback != null) {

                    callback(result);
                }

                $scope.$apply(function () {

                    $scope.isBusy = false;
                });

            }, $me.handleError);
        };


        $scope.data.delete = function (object, callback) {

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

                            var index = $scope.list.indexOf(object);
                            $scope.list.splice(index, 1);
                        }

                        $scope.$emit(emittedEvents.deleted, { sender: $scope, args: object });

                        if ($scope.object != null) {

                            $scope.isModelDeleted = true;
                            $scope.object = null;
                        }

                        if (callback != null) {

                            callback(object);
                        }

                        $scope.alerts.push({

                            type: 'success',
                            msg: 'Delete Successful.'
                        });
                    });


                }, $me.handleError);
        };

        $scope.closeAlert = function (index) {
            $scope.alerts.splice(index, 1);
        };

        // file upload
        $scope.data.uploadProgress = 0;
        $scope.data.uploading = false;
        $scope.data.upload = function (file, callback, id) {

            if (id == null) {

                id = $scope.object.id;
                if (id == null) {

                    id = $scope.object.Id;
                }
            }

            if (id == null) {
                throw "Id parameter is required or $scope.object must be set";
            }

            var targetUrl = String.format("/tables/{0}/{1}/files", attrs.table, id);

            var fd = new FormData();
            fd.append("fileToUpload", file);

            $scope.data.uploadProgress = 0;
            $scope.data.uploadStatus = "uploading";

            var req = $.ajax({
                url: targetUrl,
                type: "POST",
                data: fd,
                processData: false,
                contentType: false,
                xhr: function () {
                    var req = $.ajaxSettings.xhr();
                    if (req) {
                        req.upload.addEventListener('progress', function (event) {
                            if (event.lengthComputable) {
                                var percent = event.loaded / event.total * 100;
                                if (percent % 10 > 5) {

                                    $scope.$apply(function () {
                                        $scope.data.uploadProgress = percent;
                                        $scope.data.uploading = true;
                                    });
                                }
                            }
                        }, false);
                    }
                    return req;
                },
            });

            req.done(function (result) {

                var object = $me.processServerObject(result);

                if (callback != null) {

                    callback(object);
                }
                
                $scope.$apply(function () {

                    $scope.object = object;
                    $scope.data.uploadProgress = 100;
                    $scope.data.uploadStatus = "success";

                    $scope.alerts.push({

                        type: 'success',
                        msg: 'File was uploaded for item:' + result.id
                    });

                    $scope.$emit(emittedEvents.uploaded, { sender: $scope, args: result });
                });
            });

            req.fail(function (jqXHR, jqXHR, textStatus) {

                if (callback != null) {

                    callback();
                }

                $scope.$apply(function () {
                    $scope.data.uploadProgress = 0;
                    $scope.data.uploadStatus = "fail";
                    $scope.alerts.push({

                        type: 'danger',
                        msg: 'Upload failed:' + textStatus

                    });
                });
            });
        };

        $scope.data.removefile = function (attachment, callback) {

            if ($scope.object == null) {

                throw "Object cannot be null";
            }

            $me.processServerObject($scope.object);

            var targetUrl = String.format("/tables/{0}/{1}/files/{2}",
                attrs.table,
                $scope.object.id,
                attachment.Url.substring(attachment.Url.lastIndexOf("/") + 1));

            var req = $.ajax({
                url: targetUrl,
                type: "DELETE",
                data: attachment,
            });

            req.done(function (result) {

                if (callback != null) {

                    callback(true);
                }

                var object = $me.processServerObject(result);

                $scope.$apply(function () {

                    $scope.object = object;

                    $scope.alerts.push({
                        type: 'warning',
                        msg: 'File was deleted for item:' + result.id
                    });

                    $scope.$emit(emittedEvents.updated, { sender: $scope, args: result });
                });
            });

            req.fail(function (jqXHR, jqXHR, textStatus) {

                if (callback != null) {

                    callback(false);
                }

                $scope.$apply(function () {
                    $scope.alerts.push({

                        type: 'danger',
                        msg: 'Delete Failed:' + textStatus

                    });
                });
            });
        };

        $scope.$emit(emittedEvents.loaded, { sender: $scope });
    };

    // Data Context provides neccessary functions  to access nancyblack database
    // by leveraging azure mobile service api
    ncb.directive('ncbDatacontext', ['$http', function ($http) {

        function link($scope, element, attrs) {
            return new dataContext($scope, element, attrs, $http);
        }

        return {
            restrict: 'A',
            link: link,
            priority: 9999, // make sure we got compiled first
            scope: true,
        };
    }]);

    // DataContext which integrated into current scope instead of creating new
    // child sopce
    ncb.directive('ncbDatacontextIntegrated', ['$http', function ($http) {

        function link($scope, element, attrs) {
            return new dataContext($scope, element, attrs, $http);
        }

        return {
            restrict: 'A',
            link: link,
            priority: 9999, // make sure we got compiled first
            scope: false // integrate into current scope
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

    var saveinsertButton = function ($scope, element, attrs, $compile) {

        var $me = this;

        element.attr("ng-disabled", "isBusy");

        var parentForm = element.parents("form");
        if (parentForm.length == 0) {

            // not in a form, find a form in data context
            var ctx = element.parents("[ncb-datacontext]");
            parentForm = ctx.find("form");
        }

        if (parentForm.length == 0) {
            console.log("save button: No form found, cannot bind to validation.");
        }

        if (parentForm.length > 1) {
            console.log("save button: Multiple form found, cannot bind to validation.");
        }

        if (parentForm.length == 1) {

            element.attr("ng-disabled", parentForm.attr("name") + ".$valid == false || isBusy");
        }

        element.prepend('<i class="fa fa-spin fa-circle-o-notch" ng-show="isBusy == true"></i>')

        if (element.is("[ncb-insertbutton]")) {

            element.attr("mode", "insert");
        }

        element.removeAttr("ncb-savebutton"); // prevent infinite loop
        element.removeAttr("ncb-insertbutton"); // prevent infinite loop
        var template = $compile(element);
        template($scope);

        element[0].onclick = function (e) {

            e.preventDefault();

            if (attrs.beforesave != null) {

                var result = $scope.$eval(attrs.beforesave);
                if (result == false) {

                    return;
                }
            }

            if (element.attr("mode") == "insert") {

                $scope.$eval("data.insert(object, aftersave)");
            } else {

                $scope.$eval("data.save(object, aftersave)");
            }
        };
    };
    ncb.directive('ncbSavebutton', ['$compile', function ($compile) {

        function link($scope, element, attrs) {
            return new saveinsertButton($scope, element, attrs, $compile);
        }

        return {
            restrict: 'A',
            link: link,
            scope: true,
            terminal: true, // we will use $compile - so we want to stop all other directives
            priority: 9999, // make sure we got compiled first
        };
    }]);

    ncb.directive('ncbInsertbutton', ['$compile', function ($compile) {

        function link($scope, element, attrs) {
            return new saveinsertButton($scope, element, attrs, $compile);
        }

        return {
            restrict: 'A',
            link: link,
            terminal: true, // we will use $compile - so we want to stop all other directives
            priority: 9999, // make sure we got compiled first
        };
    }]);

    ncb.directive('ncbListeditor', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            var $me = this;

            $scope.newItem = {};
            $scope.target = $scope.$parent.$eval(attrs.target);

            $scope.$watch(attrs.target, function () {

                $scope.target = $scope.$parent.$eval(attrs.target);
            });

            $scope.remove = function (item) {

                var target = $scope.$parent.$eval(attrs.target);

                if (target == null) {

                    return;
                }

                var index = target.indexOf(item);
                if (index < 0) {

                    $scope.$parent.alerts.push({
                        msg: "Item not found: " + item
                    });
                    return;
                }

                target.splice(index, 1);
                $scope.newItem = item;
            };

            $scope.add = function () {

                var target = $scope.$parent.$eval(attrs.target);

                if (target == null) {

                    $scope.$parent.$eval(attrs.target + "=[]");
                    target = $scope.$parent.$eval(attrs.target);
                }

                if (target.indexOf($scope.newItem) >= 0) {

                    $scope.$parent.alerts.push({
                        msg: "Duplicate Item: " + $scope.newItem
                    });
                    return;
                }

                target.push($scope.newItem);
                $scope.target = target;

                $scope.newItem = {};
            };
        }

        return {
            restrict: 'A',
            link: link,
            scope: true
        };
    }]);

    ncb.directive('ncbLookupscope', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            var $me = this;
            $me.handleError = function (err) {

                $scope.$apply(function () {

                    $scope.isBusy = false;
                });
            };

            $scope.table = client.getTable(attrs.table);
            $scope.lookup = [];
            $scope.isBusy = false;

            $scope.refreshLookup = function (key) {

                var oDataQuery = attrs.filter.replace(/\$key/g, key);

                if (key == null || key == '') {

                    oDataQuery = "";
                } else {

                    oDataQuery += "&";
                }

                oDataQuery += "$top=10";

                $scope.isBusy = true;
                $scope.table.read(oDataQuery).done(function (results) {

                    $scope.$apply(function () {

                        $scope.isBusy = false;

                        if (attrs.labelpath != null) {
                            console.log("RefreshLookup", results)
                            //results.forEach(function (item) {
                            //    item.label = item[attrs.labelpath];
                            //});
                        }

                        $scope.lookup = results;
                    });

                }, $me.handleError);

            };

        }

        return {
            restrict: 'A',
            link: link,
            scope: true,
        };
    }]);

    ncb.directive('ncbLookup', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            var $me = this;
        }

        return {
            restrict: 'E',
            replace: true,
            link: link,
            templateUrl: '/Modules/DatabaseSystem/template/ncbLookupbox.html'
        };
    }]);

    ncb.directive('ncbAttachmentmanager', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            if ($scope.attachmentManager == null) {
                $scope.attachmentManager = {};
            } else {

                throw "there is already attachment manager in this scope";
            }

            var $me = $scope.attachmentManager;

            if (element.find(".uploader").length == 0) {

                var uploader = $("<div class='uploader'>Drop Files Here</div>");
                element.append()
            }

            //#region Drag Upload

            var uploader = element.find(".uploader");
            var handleEnter = function (e) {
                e.stopPropagation();
                e.preventDefault();
                uploader.addClass("hintdrop");
            };
            var cancel = function (e) {
                e.stopPropagation();
                e.preventDefault();
            };

            uploader.on('dragenter', handleEnter);
            uploader.on('dragover', cancel);
            $(document).on('dragenter', cancel);
            $(document).on('dragover', handleEnter);
            $(document).on('drop', cancel);

            uploader.on('drop', function (e) {

                e.preventDefault();
                var files = e.originalEvent.dataTransfer.files;

                $scope.data.upload(files[0]);
            });
            uploader.on('dragleave', function (e) {

                cancel(e);
                uploader.removeClass("hintdrop");

            });

            $scope.$watch("data.uploadProgress", function () {

                uploader.find(".uploadprogress").css("width", $scope.data.uploadProgress + "%");
            });

            //#endregion

            //#region Click Upload

            uploader.click(function () {

                if (uploader.input == null) {

                    uploader.input = $(document.createElement('input'));
                    uploader.input.attr("type", "file");
                    uploader.input.on("change", function (e) {

                        var files = uploader.input[0].files;
                        $scope.data.upload(files[0]);
                    });
                }

                uploader.input.trigger('click');
                return false;
            });

            //#endregion

            $scope.viewing = null;
            $me.view = function (item) {

                $scope.viewing = item;
                element.find(".attachmentView").modal("show");
            };

            $me.delete = function (item) {

                if (confirm("Are you sure to delete? This cannot be undone and your file is gone forever.") == false) {

                    return;
                }

                $scope.data.removefile(item, function (result) {

                    if (result == true) {

                        element.find(".attachmentView").modal("hide");
                    }
                });
            };

        }

        return {
            restrict: 'E',
            replace: true,
            link: link,
            scope: false,
            templateUrl: '/Modules/DatabaseSystem/template/ncbAttachmentManager.html'
        };
    }]);

})();