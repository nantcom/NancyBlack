(function () {

    var ncbEditor = angular.module("editor-frame", ['ngRoute', 'ngAnimate']);

    var util = {};
    util.listeditable = function (siteView) {

        if (siteView == null) {

            siteView = $("#siteview");
        }

        var areas = [];

        siteView.contents().find("[data-editable]").each(function (index, item) {

            var editable = $(item);
            var area =
            {
                index: areas.length,
                element: editable,
                name: editable.data("propertyname"),
            };
            areas.push(area);
            areas[area.name] = area;
        });

        return areas;
    };

    util.listcollections = function (siteView) {

        if (siteView == null) {

            siteView = $("#siteview");
        }

        var collections = [];

        siteView.contents().find("[ncw-collection]").each(function (index, item) {

            var collection = $(item);
            var collectionItem =
            {
                element: collection,
                name: collection.attr("table"),
                table: collection.attr("table"),
                layout: collection.attr("layout")
            };
            collections.push(collectionItem);
            collections[collectionItem.name] = collectionItem;
        });

        return collections;
    };

    util.endswith = function (str, suffix) {
        return str.indexOf(suffix, str.length - suffix.length) !== -1;
    };

    ncbEditor.controller("NancyWhite", function ($scope, $rootScope, $http) {

        var $me = this;
        var siteView = $("#siteview");

        //#region Menu System, did not use ng-view because of confusion when navigate iframe

        $scope.menu = {};
        $scope.menu.stack = ['editframe-homemenu.html'];
        $scope.menu.content = $scope.menu.stack[0];

        $me.goback = function (e) {

            if ($scope.menu.goback != null) {

                var result = $scope.menu.goback();
                if (result == false) {

                    return;
                }
            }

            $scope.menu.stack.pop();
            $scope.menu.content = $scope.menu.stack[$scope.menu.stack.length - 1];

            $me.resetButtons();

            if (e != null) {
                e.preventDefault();
            }
        };

        $me.cancel = function (e) {

            if ($scope.menu.cancel != null) {

                var result = $scope.menu.cancel();
                if (result == false) {

                    return;
                }
            } else {

                if (confirm("Are you sure?") == false) {

                    return;
                }
            }



            $scope.menu.stack.pop();
            $scope.menu.content = $scope.menu.stack[$scope.menu.stack.length - 1];

            $me.resetButtons();

            if (e != null) {
                e.preventDefault();
            }
        };

        $me.resetButtons = function () {

            $scope.menu.backbuttonText = 'back';
            $scope.menu.altbuttonText = null;
            $scope.menu.goback = null;
            $scope.menu.cancel = null;
        };
        $me.resetButtons();

        $scope.switchMenu = function (e, url, args) {

            $scope.menu.stack.push(url);
            $scope.menu.content = url;

            if (args != null) {

                $scope.menu.backbuttonText = args.backbuttonText;
                $scope.menu.altbuttonText = args.altbuttonText;
                $scope.menu.goback = args.goback;
                $scope.menu.cancel = args.cancel;
            }

            if ($scope.menu.backbuttonText == null) {

                $scope.menu.backbuttonText = 'back';
            }

            e.preventDefault();
        };

        $scope.goback = $me.goback;
        $scope.cancel = $me.cancel;

        //#endregion

        $scope.width = "Full Width";
        $scope.siteView = siteView;
        $scope.currentUrl = siteView.attr("src");

        $me.view = function (width, e) {
            if (width != "none") {

                width = width + "px";
                $scope.width = width;
            }
            else {
                $scope.width = "Full Width";
            }

            siteView.css("max-width", width);
            e.preventDefault();
        };

        $scope.globals = {};

        $scope.reloadSiteView = function () {

            siteView.contents()[0].location.reload();
        };

        $rootScope.$on("working", function () {

            $("#working").addClass("working");
        });

        $rootScope.$on("working-done", function () {

            $("#working").removeClass("working");
        });

        var siteView = $("#siteview");
        // list editable areas on page load
        siteView.on("load", function () {

            // Link to CSS for edit area
            siteView.contents()
                .find("head")
                .append('<link rel="stylesheet" href="/Modules/ContentSystem/Views/editor-editarea.css"></style>');

            $scope.$apply(function () {

                $scope.currentUrl = document.getElementById("siteview").contentWindow.location.pathname;

                $scope.siteView.areas = util.listeditable(siteView);
                $scope.siteView.collections = util.listcollections(siteView);

            });

            $rootScope.$broadcast("siteView-reloaded", $scope.siteView);
        });

    });

    ncbEditor.controller("NcbPageContent", function ($scope, $rootScope, $timeout, $location) {

        var $me = this;
        var siteView = $("#siteview");

        // hilight editable areas of the page
        $me.hoverarea = function (item) {

            item.element.toggleClass("editable-hover");
        };

        $me.edit = function (e, item) {

            $scope.globals.editing = item;
            $scope.switchMenu(e, "editframe-editcontent.html");
        };

    });

    ncbEditor.controller("NcbContentEditor", function ($scope, $rootScope, $timeout, $routeParams, $location) {

        var $me = this;
        var siteView = $("#siteview");
        $scope.editing = {};

        $me.getContent = function (callback) {
            
            var query = String.format("$filter=Url eq '{0}'", $scope.currentUrl);
            $scope.data.query(query,
            function (results) {

                var content = results[0];
                if (content == null) {

                    throw "cannot find content with query: " + query;
                }

                callback(content);
            });
        };

        $me.initializeEditor = function (item) {

            item.element.attr("contenteditable", "true");
            item.element.removeClass("editable-hover");
            item.element.addClass("editable-edit");

            $scope.editing = item;
            $("#toolbarcontainer").css("height", "74px");

            $timeout(function () {

                $scope.editing.ckeditor = window.CKEDITOR.inline(item.element[0], {
                    extraPlugins: 'sharedspace',
                    removePlugins: 'floatingspace,resize',
                    sharedSpaces: {
                        top: 'toolbar1',
                        bottom: 'toolbar2'
                    }
                });

                $me.getContent(function (content) {

                    $scope.editing.content = content;
                    $scope.editing.original = content[$scope.editing.name];

                    if (content[$scope.editing.name] == null) {

                        // default value from database
                        $scope.editing.original = $scope.editing.element.html();
                    }
                });

            }, 400);

            var resetEditable = function () {

                $scope.editing.ckeditor.destroy();

                $("#toolbarcontainer").css("height", "0px");
                $scope.editing.element.removeAttr("contenteditable");
                $scope.editing.element.removeClass("editable-edit");

                $scope.globals.editing = null;
            };

            var gobackHandler = function () {

                $rootScope.$broadcast("working");

                $me.getContent(function (content) {

                    var fromServer = content[$scope.editing.name];
                    if (fromServer != null && fromServer != $scope.editing.original) {

                        // alert
                        if (confirm("Someone else already changed this content, Do you want to replace changes on the server with your changes?")) {

                            // replace

                        } else {

                            // load the changes into editable
                            $scope.editing.element.html(fromServer);

                            return;
                        }
                    }

                    content[$scope.editing.name] = $scope.editing.element.html();

                    // replace
                    $scope.data.save(content, function () {

                        resetEditable();

                        // after save is completed
                        $rootScope.$broadcast("working-done");

                        $scope.menu.goback = null;
                        $scope.goback();

                    });
                });

                return false; // we will handle going back ourselves
            };

            var cancelHandler = function () {

                if (confirm("Are you sure you want to discard the changes?")) {

                    $scope.editing.element.html($scope.editing.original);
                    resetEditable();
                    return true;
                }

                return false; // cancel the cancel
            };

            $scope.menu.backbuttonText = 'save';
            $scope.menu.altbuttonText = 'discard';
            $scope.menu.goback = gobackHandler;
            $scope.menu.cancel = cancelHandler;

        };

        $me.waitData = null;
        if ($scope.data == null) {

            $me.waitData = $scope.$watch("data", function () {

                $me.waitData(); //stops the watch
                $me.initializeEditor($scope.globals.editing);

            });

            return;
        } else {

            $me.initializeEditor($scope.globals.editing);
        }

    });

    ncbEditor.controller("NcbCollection", function ($scope, $rootScope, $timeout, $location) {

        var $me = this;
        var siteView = $("#siteview");

        if ($scope.siteView.collections == null) {

            return;
        }

        var loadedUrl = $scope.currentUrl;

        $scope.collection = $scope.globals.activecollection;
        $scope.collection.list = [];

        $me.hoverarea = function (item) {

            if (item == null) {

                return;
            }

            var element = $scope.collection.element.find("[data-itemid=" + item.id + "]");
            element.toggleClass("editable-hover");
        };

        $me.view = function (e, item) {

            siteView.attr("src", item.Url);
            $scope.goback();
        };

        //#region Adding Item

        $me.convertToSlug = function (Text) {

            if (Text == null) {

                return "";
            }

            return Text
                .toLowerCase()
                .replace(/[^\w ]+/g, '')
                .replace(/ +/g, '-')
            ;
        }

        $scope.newItem = {};
        $me.add = function (e) {

            $("#newitemmodal").modal("show");
            e.preventDefault();
        };

        $me.commitAdd = function () {

            var slug = $me.convertToSlug($scope.newItem.Title);
            var finalUrl = $scope.currentUrl + "/" + slug;

            var query = String.format("$filter=Url eq '{0}'", finalUrl);
            $scope.data.query(query, function (results) {

                if (results.length > 0) {

                    $scope.alerts.push({
                        msg: finalUrl + " was already used."
                    });
                    return;
                }

                var toSave = JSON.parse(JSON.stringify($scope.newItem));
                toSave.Url = finalUrl;
                toSave.Layout = $scope.globals.activecollection.layout;
                toSave.DisplayOrder = 0;

                $scope.newItem = {};
                $scope.data.save(toSave, function (item) {

                    $scope.collection.list.push(toSave);
                    $("#newitemmodal").modal("hide");

                    siteView.contents()[0].location.reload();
                });

            });

        };

        //#endregion

        $me.waitData = null;
        $me.refreshCollection = function () {

            if ($scope.data == null) {

                $me.waitData = $scope.$watch("data", function () {

                    $me.waitData(); //stops the watch
                    $me.refreshCollection();
                });

                return;
            }

            $scope.data.query("", function (results) {

                $scope.$apply(function () {

                    $scope.collection.list = results;
                });
            });
        };

        $me.refreshCollection();

    });

    ncbEditor.controller("NcbPagePropertyEdit", function ($scope, $rootScope, $timeout, $http) {

        var $me = this;
        var siteView = $("#siteview");
        var model = document.getElementById("siteview").contentWindow.model;

        if (model == null || model.Content.Id == null) {

            alert("Cannot get information about page's model");
            return;
        }

        $scope.currentTable = "content";
        if (model.Content.typeName != null) {
            $scope.currentTable = model.Content.typeName;
        }

        $scope.object = JSON.parse( JSON.stringify( model.Content ) );
        $scope.menu.backbuttonText = "save";
        $scope.menu.altbuttonText = "discard";

        $scope.menu.cancel = function () {

            if (confirm("Are you sure?")) {

                return true;
            }

            return false;

        };

        $scope.menu.goback = function () {

            $rootScope.$broadcast("working");
            $scope.data.save($scope.object, function () {

                model.Content = $scope.object;
                $rootScope.$broadcast("working-done");

                $scope.menu.goback = null;
                $scope.goback();
            });

            return false; // handle going back
        };

    });

    ncbEditor.controller("NcbPageLayoutEdit", function ($scope, $rootScope, $timeout, $http) {

        var $me = this;
        var siteView = $("#siteview");
        var model = document.getElementById("siteview").contentWindow.model;

        if (model == null || model.Content.Id == null) {

            alert("Cannot get information about page's model");
            return;
        }

        $scope.currentTable = "content";
        if (model.Content.typeName != null) {
            $scope.currentTable = model.Content.typeName;
        }

        $scope.object = JSON.parse(JSON.stringify(model.Content));
        $scope.menu.backbuttonText = "save";

        $http.get('/__editor/data/availablelayouts').
          success(function (data) {

              $scope.layouts = data;
          }).
          error(function (data) {


          });

        $me.changelayout = function (e, item) {

            $rootScope.$broadcast("working");

            $scope.object.Layout = item;
            $scope.data.save($scope.object, function () {

                $scope.reloadSiteView();
                $rootScope.$broadcast("working-done");
            });
        };
    });

    ncbEditor.controller("NcbAttachments", function ($scope, $rootScope, $timeout, $http) {

        var $me = this;
        var siteView = $("#siteview");
        var model = document.getElementById("siteview").contentWindow.model;

        if (model == null || model.Content.Id == null) {

            alert("Cannot get information about page's model");
            return;
        }

        $scope.currentTable = "content";
        if (model.Content.typeName != null) {
            $scope.currentTable = model.Content.typeName;
        }

        $scope.object = JSON.parse(JSON.stringify(model.Content));

        //#region Drag Upload

        var uploader = $(".uploader");
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

        $scope.viewing = null;
        $me.view = function (item) {

            $scope.viewing = item;
            $("#attachmentView").modal("show");
        };

        $me.addToContentBlock = function (item) {

            var img = $("<img />");
            img.attr("src", item.Url);
            $scope.globals.editing.element.append(img);
        };

        $me.delete = function (item) {

            if (confirm("Are you sure to delete? This cannot be undone and your file is gone forever.") == false) {

                return;
            }

            $scope.data.removefile(item, function (result) {
                
                if (result == true) {

                    $("#attachmentView").modal("hide");
                }
            });
        };
    });

})();