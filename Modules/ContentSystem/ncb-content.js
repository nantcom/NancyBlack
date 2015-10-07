(function () {

    var ncb = angular.module("ncb-content", []);

    ncb.directive('ncbPagecontext', function ($http, $datacontext) {

        function link($scope, element, attrs) {

            if (window.model.Content == null) {

                throw "require window.model.Content";
            }

            var datacontext = new $datacontext($scope, window.model.Content.TableName);

            $scope.object = window.model.Content;
            if ($scope.object.ContentParts == null) {

                $scope.object.ContentParts = {};
            }
        }

        return {
            restrict: 'A',
            link: link,
            priority: 9999, // make sure we got compiled first
            scope: true
        };
    });

    ncb.directive('ncbEditable', function ($timeout) {

        function link($scope, element, attrs) {

            var $me = {};

            if ($("script[src='/Content/Scripts/ckeditor/ckeditor.js']").length == 0) {

                $('<script src="/Content/Scripts/ckeditor/ckeditor.js"></script>').appendTo($("head"));
            };

            if ($("#editable-toolbar").length == 0) {

                var toolbar = $('<div id="editable-toolbar"></div>');
                $('<div id="editable-toolbar-tools"></div>').appendTo(toolbar);
                $('<div id="editable-toolbar-breadcrumbs"></div>').appendTo(toolbar);

                toolbar.appendTo($("body"));

                $("html").css("margin-top", "75px");

            }

            var property = attrs.itemprop;
            if (property == null) {

                throw "require itemprop to specify the content part to edit";
            }

            var original = element.html();
            $timeout(function () {

                element.attr("contenteditable", "true");
                element.addClass("ncb-editable");

                element.on("blur", function () {

                    var newContent = element.html();
                    if (original == newContent) {
                        return;
                    }

                    if ($scope.object == null) {
                        throw "require $scope.object, make sure pagecontext or datacontext is available.";
                    }
                    $scope.object.ContentParts[property] = element.html();
                    $scope.$apply();
                });

                $me.editor = window.CKEDITOR.inline(element[0], {
                    extraPlugins: 'sharedspace',
                    removePlugins: 'floatingspace,resize',
                    sharedSpaces: {
                        top: 'editable-toolbar-tools',
                        bottom: 'editable-toolbar-breadcrumbs'
                    }
                });


            }, 400);
        };

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });

    ncb.directive('ncbEditablePicture', function ($datacontext) {

        var findAttachment = function (source, type) {

            var found = {};
            if (source.Attachments == null) {

                return {};
            }

            source.Attachments.forEach(function (element) {

                if (element.AttachmentType == type) {

                    found = element;
                }
            });

            return found;
        };

        function link($scope, element, attrs) {

            var $me = {};

            if (attrs.type == null) {

                throw "Require type attribute";
            }

            //// The chanage able option for display only mode.
            //var changeAble = true;
            //if (attrs.changeAble != null) {
            //    changeAble = attrs.changeAble;
            //}            

            if (parent == null ||
                parent.location.pathname != "/__editor" ) {

                if (attrs.always != "true") {

                    return;
                }
            }

            // set css/href to ensure that picture is shown
            var updateAttachment = function (newObject) {

                var attachment = findAttachment(newObject, attrs.type).Url

                if (element[0].tagName != "IMG") {

                    element.css("background-image", "url('" + attachment + "')");
                } else {

                    element.attr("src", attachment);
                }
            };

            var button = $('<a class="changepicturebutton"><i class="ion-android-camera"></i></a>');
            button.css("opacity", 0);
            button.css("position", "absolute");
            button.appendTo($("body"));
       

            element.on("mouseenter", function () {
                
                var offset = element.offset();
                button.css("left", offset.left);
                button.css("top", offset.top);
                button.css("opacity", 0.9);
                button.css("z-index", 99999);
            });

            element.on("mouseleave", function () {
                button.css("opacity", 0);
            });

            button.on("mouseenter", function () {
                
                button.css("opacity", 0.9);
                
            });

            button.on("mouseleave", function () {
                button.css("opacity", 0);
            });

            button.on("click", function () {
                
                if ($me.input == null) {

                    $me.input = $(document.createElement('input'));
                    $me.input.attr("type", "file");
                    $me.input.on("change", function (e) {

                        var files = $me.input[0].files;
                        datacontext.upload(files[0], updateAttachment, null, attrs.type, true);
                    });
                }

                $me.input.trigger('click');
            });            

            var datacontext = null;
            if (attrs.itemscope != null) {

                datacontext = new $datacontext($scope, attrs.itemtype);
                datacontext.getById(attrs.itemid, function (data) {

                    $scope.object = data;
                    updateAttachment($scope.object);
                });
            } else {

                datacontext = $scope.data;
                if (datacontext == null) {

                    datacontext = new $datacontext($scope, window.model.Content.TableName);
                    $scope.object = window.model.Content;
                    updateAttachment(window.model.Content);
                }
            }


        };

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });


})();