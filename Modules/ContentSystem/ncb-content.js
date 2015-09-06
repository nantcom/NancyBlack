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

            var property = element.data("propertyname");
            if (property == null) {

                throw "require 'data-propertyname' to be defined. Use @this.MakeEditable to do so";
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

    ncb.directive('ncbEditablePicture', function () {

        var findAttachment = function(source, type) {

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

            // set css/href to ensure that picture is shown
            var updateAttachment = function (newObject) {

                var attachment = findAttachment(newObject, attrs.type).Url

                if (element[0].tagName != "IMG") {

                    element.css("background-image", "url('" + attachment + "')");
                } else {

                    element.attr("href", attachment);
                }
            };

            updateAttachment(window.model.Content);

            var button = $('<a class="changepicturebutton"><i class="ion-android-camera"></i></a>');
            button.css("opacity", 0);
            button.css("pointer-events", "none");
            button.appendTo($("body"));
            
            element.on("mouseenter", function () {

                var offset = element.offset();
                button.css("left", offset.left);
                button.css("top", offset.top);
                button.css("opacity", 0.9);
            });

            element.on("mouseleave", function () {

                button.css("opacity", 0);
            });

            element.on("click", function () {

                if ($me.input == null) {

                    $me.input = $(document.createElement('input'));
                    $me.input.attr("type", "file");
                    $me.input.on("change", function (e) {

                        var files = $me.input[0].files;
                        $scope.data.upload(files[0], updateAttachment, null, attrs.type, true);
                    });
                }

                $me.input.trigger('click');
            });

            return false;
        };

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });


})();