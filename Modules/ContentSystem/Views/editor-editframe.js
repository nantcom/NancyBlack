(function () {

    var ncbEditor = angular.module("editor-frame", ['ngAnimate', 'angular-sortable-view']);

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

            if (window.language != "") {
                area.name = area.name + "_" + window.language;
            }

            areas.push(area);
            areas[area.name] = area;
        });

        return areas;
    };

    util.listthemeeditable = function (siteView) {

        if (siteView == null) {

            siteView = $("#siteview");
        }

        var areas = [];

        siteView.contents().find("[data-themeeditable]").each(function (index, item) {

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
                name: collection.attr("name") == null ? collection.attr("table") : collection.attr("name"),
                table: collection.attr("table") == null ? "" : collection.attr("table").toLowerCase(),
                layout: collection.attr("layout") == null ? "" : collection.attr("layout").toLowerCase(),
            };

            if (collectionItem.table == null || collectionItem.table == "") {

                collectionItem.url = collection.attr("rooturl");
                collectionItem.name = collectionItem.url.substring(1);
                collectionItem.table = "page";

            } else {

                if (collection.attr("rooturl") != null) {

                    collectionItem.url = collection.attr("rooturl");

                } else {
                    collectionItem.url = "/" + collection.attr("table").toLowerCase() + "s";
                }

            }

            collections.push(collectionItem);
            collections[collectionItem.name] = collectionItem;
        });

        return collections;
    };

    util.endswith = function (str, suffix) {
        return str.indexOf(suffix, str.length - suffix.length) !== -1;
    };

    ncbEditor.controller("NancyWhite", function ($scope, $rootScope, $http, $timeout) {

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

        $scope.reloadSiteView = function (url) {

            if (typeof ( url ) == "string" ) {

                siteView.contents()[0].location.href = url;
                return;
            }
            
            siteView.contents()[0].location.reload();
        };

        $rootScope.$on("working", function () {

            $("#working").addClass("working");
        });

        $rootScope.$on("working-done", function () {

            $("#working").removeClass("working");
        });

        var siteView = $("#siteview");
        var menu = $("#menu");

        // list editable areas on page load
        siteView.on("load", function () {

            document.getElementById("siteview").contentWindow.addEventListener("unload", function () {

                menu.addClass("loading");
            });

            $timeout(function () {

                menu.removeClass("loading");
            }, 1500);


            // Link to CSS for edit area
            siteView.contents()
                .find("head")
                .append('<link rel="stylesheet" href="/NancyBlack/Modules/ContentSystem/Views/editor-editarea.css"></style>');

            $scope.$apply(function () {

                $scope.currentUrl = document.getElementById("siteview").contentWindow.location.pathname;
                $scope.currentContent = document.getElementById("siteview").contentWindow.model.Content;

                $scope.siteView.areas = util.listeditable(siteView);
                $scope.siteView.themeareas = util.listthemeeditable(siteView);
                $scope.siteView.collections = util.listcollections(siteView);

            });

            $rootScope.$broadcast("siteView-reloaded", $scope.siteView);
        });

        siteView.attr("src", "/");
    });

    ncbEditor.controller("NcbPageContent", function ($scope, $rootScope, $timeout, $location) {

        var $me = this;
        var siteView = $("#siteview");

        // hilight editable areas of the page
        $me.hoverarea = function (item) {

            item.element.toggleClass("editable-hover");
        };

        $me.edit = function (e, item) {

            item.id = item.element.data("id");
            item.table = item.element.data("table");

            $scope.globals.editing = item;
            $scope.switchMenu(e, "editframe-editcontent.html");
        };

    });

    ncbEditor.controller("NcbThemeContent", function ($scope, $rootScope, $timeout, $location) {

        var $me = this;
        var siteView = $("#siteview");

        $scope.menu.goback = function () {

            // restore the currentContent back to original when going back
            $scope.reloadSiteView();
        };

        // hilight editable areas of the page
        $me.hoverarea = function (item) {

            item.element.toggleClass("editable-hover");
        };

        $me.edit = function (e, item) {

            item.IsTheme = true;

            $scope.globals.editing = item;
            $scope.switchMenu(e, "editframe-editcontent.html");
        };

    });

    ncbEditor.controller("NcbContentEditor", function ($scope, $rootScope, $timeout, $location, $datacontext, $http) {

        var $me = this;
        var siteView = $("#siteview");
        var datacontext = null;

        $scope.editing = {};

        $me.getContent = function (callback) {

            var id = $scope.currentContent.Id;
            if (id == null) {
                id = $scope.currentContent.id;
            }

            var query = String.format("$filter=Id eq {0}", id);
            datacontext.query(query,
            function (results) {

                var content = results[0];
                if (content == null) {

                    throw "cannot find content with query: " + query;
                }

                if (content.ContentParts == null) {
                    content.ContentParts = {};
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

                if ($scope.currentContent.ContentParts == null) {
                    $scope.currentContent.ContentParts = {};
                }

                $scope.editing.content = $scope.currentContent;

                if ($scope.currentContent.ContentParts[$scope.editing.name] == null) {

                    $scope.editing.original = $scope.editing.element.html();
                } else {
                    $scope.editing.original = $scope.currentContent.ContentParts[$scope.editing.name].trim();
                }


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

                    var fromServer = content.ContentParts[$scope.editing.name];
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


                // remove owl-carousel
                if (item.element.is(".owl-loaded")) {

                    var child = item.element.find(".owl-item").not(".cloned");
                    child.each(function (index, element) {

                        $(element).children().unwrap();
                    });

                    item.element.find(".owl-item").remove();
                    item.element.find(".owl-controls").remove();

                    item.element.find(".owl-stage").unwrap();                    
                    item.element.find(".owl-stage").children().unwrap();
                }

                    content.ContentParts[$scope.editing.name] = $scope.editing.element.html();

                    // replace
                    datacontext.save(content, function () {

                        resetEditable();

                        // after save is completed
                        $rootScope.$broadcast("working-done");

                        $scope.menu.goback = null;
                        $scope.goback();

                        $scope.reloadSiteView();
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

        $me.reset = function () {

            if (confirm("Do you want to reset this content block to it's original value? (Usually provided with Template)") == false) {
                return;
            }

            $rootScope.$broadcast("working");

            $me.getContent(function (content) {

                delete content.ContentParts[$scope.editing.name];

                // replace
                datacontext.save(content, function () {

                    // after save is completed
                    $rootScope.$broadcast("working-done");

                    $scope.menu.goback = null;
                    $scope.goback();

                    $scope.reloadSiteView();
                });
            });

            return false; // we will handle going back ourselves
        };

        if ($scope.globals.editing.id != null) {

            $rootScope.$broadcast("working");

            // hijack the currentContent - change it into '/' content of Page Table
            datacontext = new $datacontext($scope, "Page");
            datacontext.getById($scope.globals.editing.id, function (data) {

                $rootScope.$broadcast("working-done");
                $scope.currentContent = data;
                $scope.object = data;

                $me.initializeEditor($scope.globals.editing);
            });

        } else {

            datacontext = new $datacontext($scope, $scope.currentContent.TableName);
            $me.initializeEditor($scope.globals.editing);
        }

    });

    ncbEditor.controller("NcbPagePropertyEdit", function ($scope, $rootScope, $timeout, $http) {

        var $me = this;
        var siteView = $("#siteview");
        var model = document.getElementById("siteview").contentWindow.model;

        if (model == null || model.Content.Id == null) {

            alert("Cannot get information about page's model");
            return;
        }

        $scope.currentTable = model.Content.TableName;

        $scope.object = JSON.parse(JSON.stringify(model.Content));
        $scope.menu.backbuttonText = "save";
        $scope.menu.altbuttonText = "discard";

        /* Fix SEO translations*/
        if ($scope.object.SEOTranslations == null) {
            $scope.object.SEOTranslations = {};
        }

        var suffix = '';
        if (window.language != "" && window.language != null) {
            suffix = "_" & window.language
        }

        for (var key in { 'Title' :'', 'MetaKeywords' : '', 'MetaDescription' : '' } ) {
            if ($scope.object.SEOTranslations[key + suffix] == null ||
                $scope.object.SEOTranslations[key + suffix] == '') {

                $scope.object.SEOTranslations[key + suffix] = $scope.object[key];
            }
        }
        /**/

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

                $scope.reloadSiteView();
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

        $scope.currentTable = model.Content.TableName;

        $scope.object = JSON.parse(JSON.stringify(model.Content));
        $scope.menu.backbuttonText = "back";

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

        $me.initializeScope = function () {

            var model = document.getElementById("siteview").contentWindow.model;

            if (model == null || model.Content.Id == null) {

                alert("Cannot get information about page's model");
                return;
            }

            $scope.currentTable = model.Content.TableName;

            $scope.object = JSON.parse(JSON.stringify(model.Content));
        };

        if ($scope.globals.editing != null) {

            $scope.selecttext = "Insert";
            $scope.onattachmentselect = function (item) {

                var img = $("<img />");
                img.attr("src", item.Url);
                $scope.globals.editing.element.append(img);
            };
        }

        $me.initializeScope();

        $scope.$on("siteView-reloaded", function () {

            $scope.$apply($me.initializeScope);
        });

        $scope.$on("ncb-datacontext.uploaded", function () {

            if ($scope.globals.editing == null) {

                $scope.$apply(function () {
                    $scope.reloadSiteView();
                });
            }
        });

        $scope.$on("ncb-datacontext.deleted", function () {

            if ($scope.globals.editing == null) {

                $scope.$apply(function () {
                    $scope.reloadSiteView();
                });
            }
        });
    });

    ncbEditor.controller("NcbSiteSettingsEdit", function ($scope, $rootScope, $timeout, $http) {

        var $me = this;

        $scope.currentTable = model.Content.TableName;

        $scope.object = JSON.parse(JSON.stringify(window.sitesettings));
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

                window.sitesettings = $scope.object;
                $rootScope.$broadcast("working-done");

                $scope.menu.goback = null;
                $scope.goback();
            });

            return false; // handle going back
        };

    });

    ncbEditor.controller("NcbPageEdit", function ($scope, $rootScope, $http) {

        var $me = this;
        var siteView = $("#siteview");

        $me.hoverarea = function (item) {

            if (item == null) {

                return;
            }

            var element = $scope.collection.element.find("[data-itemid=" + item.id + "]");
            element.toggleClass("editable-hover");
        };

        $me.view = function (e, item) {

            siteView.attr("src", item.Url);
        };

        $me.update = function () {

            // create id list from the data
            var id = [];
            $scope.pages.forEach(function (element) {

                id.push(element.id);
            });

            $http.post("/__editor/updateorder", { ids: id, table: $scope.collection.table })
            .success(function () {

                siteView.contents()[0].location.reload();
            })
            .error(function (msg) {

                alert("Cannot update order at this time, please try again");
            });
        };

        $me.delete = function (e, item) {

            e.preventDefault();

            if (item.Url == "/") {

                alert("You cannot delete home page");
                return;
            }

            $scope.data.delete(item, function (result) {

                if (item.Url == $scope.currentContent.Url) {

                    siteView.contents()[0].location.href = "/";
                } else {

                    siteView.contents()[0].location.reload();
                }

                $me.refreshCollection();
            });

        };

        var initialize = function () {

            $scope.rootUrl = $scope.currentUrl;
            if ($scope.globals.activecollection == null) {

                throw "$scope.globals.activecollection cannot be null";
            }

            $scope.rootUrl = $scope.globals.activecollection.url;
            $scope.collection = $scope.globals.activecollection;
            $scope.globals.activecollection = null;

            $scope.itemwording = "page";
            if ($scope.collection.table.toLowerCase() != "page") {

                $scope.itemwording = "item";
            }
        };

        var refreshCollection = function () {

            var query = ""
            if ($scope.rootUrl == "/") {

                //special query for root case
                query = "$filter=startswith(Url,'/') and (indexof(substring(Url,2),'/') eq 0 )";

            } else if ($scope.rootUrl == "/__/") {

                //special query for system pages
                query = "$filter=startswith(Url,'/__/')";
            }
            else {

                // url already starts with rootUrl
                if ($scope.rootUrl.indexOf("/" + $scope.collection.table + "s") == 0) {

                    // TEST:
                    /* Idea: if the url is sub of given url ('/collections') it must
                     * 1) starts with '/collectoins'
                     * 2) if /collections/ was removed - there must be no '/' 
                     * SELECT
                            Instr( Url, '/collections' ),
                            Substr( Url, Length('/collections') + 2),
                            Instr( Substr( Url, Length('/collections') + 2), '/' )
                         FROM Collection
                     */

                    query = String.format(
                        "$filter=startswith(Url,'{0}/') and (indexof(substring(Url,{1}),'/') eq 0 )",
                        $scope.rootUrl,
                        $scope.rootUrl.length + 2);
                }
                if ($scope.collection.table.toLowerCase() == "page") {

                    query = String.format(
                        "$filter=startswith(Url,'{0}/') and (indexof(substring(Url,{1}),'/') eq 0 )",
                        $scope.rootUrl,
                        $scope.rootUrl.length + 2);

                } else {

                    query = String.format(
                        "$filter=startswith(Url,'/{0}s{1}/') and (indexof(substring(Url,{2}),'/') eq 0 )",
                        $scope.collection.table,
                        $scope.rootUrl,
                        $scope.rootUrl.length + 2);
                }

                if ($scope.collection.table.toLowerCase() == "product") {

                    // product must also filter out variations
                    query += " and (IsVariation eq 0 )";
                };
            }

            query += "&$orderby=DisplayOrder";

            $scope.data.query(query, function (results) {

                $scope.$apply(function () {

                    $scope.pages = results;
                });
            });
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

            if ($scope.collection != null) {

                $scope.newItem.Title = "New " + $scope.collection.name + " " + ($scope.pages.length + 1);
            } else {

                $scope.newItem.Title = "New Page " + ($scope.pages.length + 1);
            }

            $scope.alerts = [];
            $("#newitemmodal").modal("show");
            e.preventDefault();
        };

        $me.commitAdd = function (callback) {

            var slug = $me.convertToSlug($scope.newItem.Title);
            var finalUrl = $scope.rootUrl + "/" + slug;

            if ($scope.rootUrl == "/") {

                finalUrl = "/" + slug;
            }

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
                toSave.DisplayOrder = 0;
                toSave.Layout = $scope.collection.layout;

                $scope.newItem = {};
                $scope.data.save(toSave, function (item) {

                    $scope.pages.push(item);
                    $("#newitemmodal").modal("hide");

                    if (callback == null) {

                        siteView.contents()[0].location.reload();
                    } else {

                        callback(item);
                    }
                });

            });

        };

        //#endregion

        //#region Mass Adding Item

        $me.massadd = function (e) {

            $scope.alerts = [];
            $("#massnewitemmodal").modal("show");
            e.preventDefault();
        };

        $me.massAddUsingFiles = function (files) {

            if (files.length == 0) {
                return;
            }

            var index = 0;
            var fileList = files;

            var continueNextFile = function (updated) {

                $scope.pages[$scope.pages.length - 1] = updated;

                index++;
                if (index >= fileList.length) {

                    $scope.reloadSiteView();
                    return;
                }

                createNewItem();
            };

            var uploadFile = function (newItem) {

                $scope.object = newItem;
                var file = fileList[index];

                $scope.data.upload(file, continueNextFile);
            };

            var createNewItem = function () {

                $scope.newItem.Title = "" + ($scope.pages.length + 1); //use numerical name
                $me.commitAdd(uploadFile); // commit add then upload file
            };

            createNewItem();
        };

        //#endregion

        $scope.$on("ncb-datacontext.loaded", function (e, args) {

            if (args.sender == $scope) {

                refreshCollection();
            }
        });

        $scope.$on("siteView-reloaded", function () {

            // check that this page still contains the same collection
            var sameCol = null;
            $scope.siteView.collections.forEach(function (col) {

                if (col.table == $scope.collection.table) {

                    sameCol = col;
                }
            });

            if (sameCol == null) {

                $scope.$apply($scope.goback);
            } else {

                $scope.$apply(function () {

                    $scope.globals.activecollection = sameCol;
                    initialize();
                    refreshCollection();
                });
            }
        });

        initialize()
    });

})();