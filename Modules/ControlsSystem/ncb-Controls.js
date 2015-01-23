(function () {

    function addIcon(element) {

        var icon = $('<i></i>');
        var iconName = element.attr("ncb-icon");

        if (iconName.indexOf("glyphicon") == 0) {

            icon.addClass("glyphicon");
            icon.addClass(iconName);
        }

        if (element.is["ncb-right"]) {

            element.append(icon);
        } else {

            element.prepend(icon);
        }
    };

    var module = angular.module('ncb-controls', []);

    // A Shorter, leaner Input boxes
    module.directive('ncbTextbox', function ($document, $timeout) {

        function link(scope, element, attrs) {

            // Bootstrap Setup
            element.addClass("form-control");
            element.wrap('<div class="form-group"></div>');

            if (element.is("[ncb-lg]")) {
                element.parent().addClass("form-group-lg");
            }

            if (element.is("[ncb-col]")) {
                element.parent().addClass(element.attr("ncb-col"));
            }

            // Label            
            if (element.is("[title]")) {

                var label = $('<label class="control-label"></label>');
                label.attr("for", element.attr("name"));
                label.text(element.attr("title"));
                element.before(label);
            }

            // Final touch ups
            if (element.is("[placeholder]") == false) {
                element.attr("placeholder", element.attr("title"));
            }
        }

        return {
            restrict: 'A',
            link: link
        };
    });

    // A Shorter, leaner form-control-static
    module.directive('ncbFormstatic', function () {

        function link(scope, element, attrs) {

            // Bootstrap Setup
            element.addClass("form-control-static");
            element.wrap('<div class="form-group"></div>');

            if (element.is("[ncb-lg]")) {
                element.parent().addClass("form-group-lg");
            }

            if (element.is("[ncb-col]")) {
                element.parent().addClass(element.attr("ncb-col"));
            }

            // Label            
            if (element.is("[title]")) {

                var label = $('<label class="control-label"></label>');
                label.attr("for", element.attr("name"));
                label.text(element.attr("title"));
                element.before(label);
            }

        }

        return {
            restrict: 'A',
            link: link
        };
    });

    // A Shorter, leaner bootstrap and font-awesome icons
    module.directive('ncbIcon', function () {

        function link(scope, element, attrs) {
            addIcon(element);
        }

        return {
            restrict: 'A',
            link: link
        };
    });

    // Delete Button
    module.directive('ncbDel', function () {

        function link(scope, element, attrs) {

            element.removeAttr("ncb-del");
            element.attr("ncb-icon", "glyphicon-trash");
            element.addClass("btn");
            element.addClass("btn-danger");

            addIcon(element);
        }

        return {
            restrict: 'A',
            link: link
        };
    });

    // Add Button
    module.directive('ncbAdd', function () {

        function link(scope, element, attrs) {

            element.removeAttr("ncb-add");

            element.attr("ncb-icon", "glyphicon-plus-sign");
            element.addClass("btn");
            element.addClass("btn-info");

            addIcon(element);

        }

        return {
            restrict: 'A',
            link: link
        };
    });
    
    // Add Button
    module.directive('ncbTab', function () {

        function link(scope, element, attrs) {
            
            var child = element.children().first();
            child.unwrap();

            child.attr("id", element.attr("id"));
            child.addClass(element.attr("class"));
            child.attr("style", element.attr("style"));
        }

        return {
            restrict: 'E',
            link: link,
            transclude: true,
            template: '<div class="tab-pane fade" ng-transclude></div>'
        };
    });
    
    // Modal Dialog
    module.directive('ncbModal', ['$compile', function ($compile) {

        function link(scope, element, attrs) {
            
            if (element.is("[closebutton]") == false) {
                element.find("button.close").remove();
            }

            if (element.is("[deletebutton]") == false) {
                element.find("button.ncb-modal-delete").remove();
            }

            if (element.is("[title]") == false) {
                element.find("h2.modal-title").remove();
            }

            if (element.find(".modal-header").children().length == 0) {
                element.find(".modal-header").remove();
            }

            var footer = element.find("ncb-footer").remove();
            element.find(".modal-footer").append(footer);

            if (element.find(".modal-footer").children().length == 0) {
                element.find(".modal-footer").remove();
            }
        }

        return {
            restrict: 'E',
            transclude: true,
            replace: true,
            templateUrl: '/Modules/ControlsSystem/Templates/ncbModal.html',
            link: link
        };
    }]);

    // List Editor
    module.directive('ncbListedit', ['$compile', function ($compile) {

        function link(scope, element, attrs) {
            
            scope.list = [{ name: 'a' }];

            // alter ncb-repeat into ng-repeat
            element.find(".ncb-listarea").find("[ncb-repeat]").each(function ( i, item ) {

                $(item).attr("ng-repeat", $(item).attr("ncb-repeat"));
                $(item).removeAttr("ncb-repeat");

            });

            // remove transclude, as the html is already put into place
            element.find(".ncb-listarea").removeAttr("ng-transclude");

            // compile the template to make ng-repeat works
            $compile(element.find('.ncb-listarea'))(scope);

            var myScope = scope;
            scope.add = function () {

                myScope.list.push({});

            };
        }

        return {
            restrict: 'E',
            transclude: true,
            templateUrl: '/Modules/ControlsSystem/Templates/ncbListEdit.html',
            scope: {
                list: '=list',
            },
            link: link
        };
    }]);
    
    // Pictures List
    module.directive('ncbPicturelist', ['$http', function ( $http ) {
        
        function link(scope, element, attrs) {
            
            var myScope = scope;

            scope.remove = function (item) {

                var sure = confirm("Delete?");
                if (!sure) {
                    return;
                }

                var filename = item.Url.substring(item.Url.lastIndexOf('/') + 1);
                var targetUrl = '/tables/' + myScope.$parent.tableName +
                                    "/" + myScope.object.id + "/files/" + filename;

                $http.delete(targetUrl).
                  success(function (data, status, headers, config) {

                      var index = myScope.object.Pictures.indexOf(item);
                      myScope.object.Pictures.splice(index, 1);
                  }).
                  error(function (data, status, headers, config) {
                      
                      myScope.$parent.error = { message: status };
                  });

            };
        }

        return {
            restrict: 'E',
            scope: {
                object: '=ngModel',
            },
            templateUrl: '/Modules/ControlsSystem/Templates/ncbPictureList.html',
            link: link
        };
    }]);

    // Uploader
    module.directive('ncbUploader', ['$document', '$timeout', function ($document, $timeout) {
        return function (scope, element, attr) {

            var me = this;
            me.initialized = false;
            me.targetUrl = null;

            scope.$watch("object", function () {

                if (scope.object == null) {
                    me.targetUrl = null;
                    return;
                }

                me.targetUrl = '/tables/' + scope.tableName +
                                    "/" + scope.object.id + "/files";
            });

            var input = $('<input type="file" />');
            var div = element.wrap("<div class='ncb-uploader'></div>");
            element.before(input);

            var progress = $('<div class="progress">' +
                              '<div class="progress-bar progress-bar-striped">' +
                              '</div>' +
                            '</div>');

            element.after(progress);

            me.progressValue = element.parent().find(".progress-bar");

            var uploadFile = function (files) {

                if (files == null || files.length == 0 || me.targetUrl == null) {
                    return;
                }

                var fd = new FormData();
                fd.append("fileToUpload", files[0]);

                me.progressValue.addClass("active");
                me.progressValue.removeClass("progress-bar-success");
                me.progressValue.removeClass("progress-bar-danger");

                var req = $.ajax({
                    url: me.targetUrl,
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

                                    $timeout(function () {
                                        me.progressValue.width(percent + "%");
                                    }, 100);

                                }
                            }, false);
                        }
                        return req;
                    },
                });


                req.done(function (msg) {

                    $timeout(function () {

                        me.progressValue.removeClass("active");
                        me.progressValue.width("0%");
                    }, 1500);

                    $timeout(function () {

                        me.progressValue.removeClass("active");
                        me.progressValue.addClass("progress-bar-success");
                        me.progressValue.width("100%");

                        if (scope.object.Pictures == null) {
                            scope.object.Pictures = [];
                        }

                        msg.forEach(function (item) {

                            scope.object.Pictures.push({

                                Url: item,
                                DisplayOrder: 0,
                            });
                        });

                    }, 1000);

                });

                req.fail(function (jqXHR, jqXHR, textStatus) {

                    $timeout(function () {

                        me.progressValue.addClass("progress-bar-danger");
                        me.progressValue.width("100%");

                        scope.error = { message: 'Upload failed:' + textStatus };

                    }, 100);

                    $timeout(function () {

                        me.progressValue.removeClass("active");
                        me.progressValue.width("0%");
                    }, 1500);

                });
            };

            input.on("change", function (evt) {

                uploadFile(evt.target.files);
                input.val("");
            });


        };
    }]);


})();

