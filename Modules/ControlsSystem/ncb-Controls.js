(function () {

    function addIcon(element, iconName) {

        var icon = $('<i></i>');

        if (iconName == null) {
            iconName = element.attr("ncb-icon");
        }

        if (iconName.indexOf("glyphicon-") == 0) {

            icon.addClass("glyphicon");
            icon.addClass(iconName);
        }

        if (iconName.indexOf("fa-") == 0) {

            icon.addClass("fa");
            icon.addClass(iconName);
        }

        if (element.is["ncb-right"]) {

            element.append(icon);
        } else {

            element.prepend(icon);
        }
    };

    function processFormElement(element)
    {
        // Bootstrap Setup
        element.addClass("form-control");

        // if parent is form-horizontal, do things differently
        if (element.closest("form").hasClass("form-horizontal")) {

            var group = $('<div class="form-group"></div>');

            if (element.is("[ncb-lg]")) {
                group.addClass("form-group-lg");
            }

            var labelCol = 3;
            if (element.is("[labelcol]")) {

                labelCol = element.attr("labelcol");
            }

            var inputCol = 12 - labelCol;

            // Label            
            if (element.is("[title]")) {

                var label = $('<label class="control-label col-xs-' + labelCol + '"></label>');
                label.attr("for", element.attr("name"));
                label.text(element.attr("title"));

                group.append(label);
            }

            var inputColumn = $('<div class="col-xs-' + inputCol + '"></div>')
            group.append(inputColumn);

            // no title, put offset
            if (element.is("[title]") == false) {

                inputColumn.addClass("col-xs-offset-" + labelCol);
            }

            element.before(group); // add group at the element's position
            element.remove(); // detach element from DOM

            inputColumn.append(element); // add it into input column instead


        } else {

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


    }

    var module = angular.module('ncb-controls', []);

    // Take Picture and upload
    module.directive('ncbCameraOpen', function ($document, $timeout) {

        function link(scope, element, attrs) {

            var previewTarget = null;
            var previewModal = null;

            if (element.is("[previewtarget]") == false) {
                throw "previewtarget attribute is required";
                return;
            } else {

                previewTarget = element.attr("previewtarget");
            }

            if (element.is("[previewmodal]")) {

                previewModal = element.attr("previewmodal");
            }

            navigator.getUserMedia = (navigator.getUserMedia ||
                       navigator.webkitGetUserMedia ||
                       navigator.mozGetUserMedia ||
                       navigator.msGetUserMedia);

            if (navigator.getUserMedia) {
                navigator.getUserMedia(

                   // constraints
                   {
                       video: true,
                       audio: false
                   },

                   // successCallback
                   function (localMediaStream) {

                       cameraStream = localMediaStream;

                       if (previewModal != null) {
                           $(previewModal).modal('show');
                       }

                       var video = $("#photoPreview")[0];
                       video.src = window.URL.createObjectURL(localMediaStream);
                       video.onloadedmetadata = function (e) {

                           video.play();
                       };
                   },

                   // errorCallback
                   function (err) {

                       alert("ไม่สามารถเปิดกล้องได้ กรุณาใช้ Chrome หรือ Firefox" + err);
                   }
                );
            } else {

                alert("ไม่สามารถเปิดกล้องได้ กรุณาใช้ Chrome หรือ Firefox");
            }


        }

        return {
            restrict: 'A',
            link: link
        };
    });

    module.factory("ncbForm", function () {

        return function (controller, $scope) {

            $scope.datepickeropen = {};
            controller.opendatepicker = function ($event, target) {
                $event.preventDefault();
                $event.stopPropagation();

                $scope.datepickeropen[target] = !$scope.datepickeropen[target];
            }
        };
    });

    // more readable select
    module.directive('ncbSelect', function ($document, $timeout) {

        function link(scope, element, attrs) {
            
            processFormElement(element);
        }

        return {
            restrict: 'A',
            link: link
        };
    });
    
    // A Shorter, leaner Input boxes
    module.directive('ncbTextbox', function ($document, $timeout) {

        function link(scope, element, attrs) {

            processFormElement(element);
            
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
    
    // A Shorter, leaner checkbox
    module.directive('ncbCheckbox', function ($document, $compile) {

        function link(scope, element, attrs) {

            var inputColumn = null;

            if (element.parent().is("[form-horizontal]")) {

                var group = $('<div class="form-group"></div>');

                if (element.is("[ncb-lg]")) {
                    group.addClass("form-group-lg");
                }

                var labelCol = 3;
                if (element.is("[labelcol]")) {

                    labelCol = element.attr("labelcol");
                }

                // Label            
                if (element.is("[title]")) {

                    var label = $('<label class="control-label col-xs-' + labelCol + '"></label>');
                    label.attr("for", element.attr("name"));
                    label.text(element.attr("title"));

                    group.append(label);
                }

                var inputCol = 12 - labelCol;
                inputColumn = $('<div class="col-xs-' + inputCol + '"></div>')
                group.append(inputColumn);

                // no title, put offset
                if (element.is("[title]") == false) {

                    inputColumn.addClass("col-xs-offset-" + labelCol);
                }
            }

            var parent = $('<div class="checkbox"></div>');
            var label = $('<label></label>');

            parent.append(label);

            element.before(parent);
            element.remove();

            label.append(element);
            
            if (element.is("[text]")) {

                var text = $('<span>' + element.attr("text") + '</span>');
                var compiled = $compile(text);

                compiled(scope);

                element.after( text );
            }

            if (inputColumn != null) {
                
                inputColumn.append(parent);
            }
        }

        return {
            restrict: 'A',
            link: link
        };
    });


    // add button into input box
    module.directive('ncbInputgroup', function ($document, $timeout, $compile) {

        function link(scope, element, attrs) {

            // Bootstrap Setup
            var inputGroup = $('<div class="input-group"></div>');
            var inputBtn = $('<span class="input-group-btn"></span>');
            element.wrap( inputGroup );

            var button = $('<button class="btn btn-default" type="button"></button>');
            inputBtn.append(button);

            if (element.is("[buttontype]")) {
                button.removeClass("btn-default");
                button.addClass(element.attr("buttontype"));
            }

            // Label            
            if (element.is("[before]")) {
                button.text(element.attr("before"));
                element.before(inputBtn);
            }

            if (element.is("[after]")) {
                button.text(element.attr("after"));
                element.after(inputBtn);
            }

            if (element.is("[icon]")) {
                addIcon(button, element.attr("icon"));
            }

            if (element.is("[btnclick]")) {
                button.attr("ng-click", element.attr("btnclick"));
            }

            var tpl = $compile(button);
            tpl(scope);
        }

        return {
            restrict: 'A',
            link: link
        };
    });

    // A Shorter, leaner form-control-static
    module.directive('ncbFormstatic', function () {

        function link(scope, element, attrs) {

            if (element.parent().hasClass("form-horizontal") == false) {

                element.addClass("form-control-static");
            }

            // Bootstrap Setup
            processFormElement(element);

            element.removeClass("form-control");

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
    
    // Date Picker Control
    module.directive('ncbDatepicker', function ($compile) {

        function link(scope, element, attrs) {
            
            if (element.is("[title]")) {

                element.find(".control-label").text(element.attr("title"));
            } else {

                element.find(".control-label").remove();
            }

            if (element.is("[placeholder]")) {

                element.find("input").attr("placeholder", element.attr("placeholder"));
            }

            scope.isopen = false;
            scope.opendatepicker = function ($event) {

                $event.preventDefault();
                $event.stopPropagation();

                scope.isopen = !scope.isopen;
            };
        }

        return {
            restrict: 'E',
            link: link,
            scope: {
                model: '=model',
                format: '=format'
            },
            templateUrl: '/Modules/ControlsSystem/Templates/ncbDatePicker.html',
        };
    });

    // Date Picker Control
    module.directive('ncbSimpledatepicker', function ($compile) {

        function link($scope, element, attrs) {

            if (element.is("[title]")) {

                element.find("label").text(element.attr("title"));
            }

            $scope.object = {};
            $scope.object.BirthDate = new Date();
            $scope.object.Interests = {};

            $scope.oneTo31 = [];
            for (var i = 1; i <= 31; i++) {
                $scope.oneTo31.push(i);
            }

            $scope.oneTo12 = [];
            for (var i = 1; i <= 12; i++) {
                $scope.oneTo12.push(i);
            }

            $scope.years = [];

            var minYear = "-100";
            var maxYear = "-13";
            var startYear = (new Date()).getFullYear();

            if (element.is("[minYear]")) {

                minYear = element.attr("minYear");
            }

            if (element.is("[maxYear]")) {

                maxYear = element.attr("maxYear");
            }

            if (element.is("[startYear]")) {

                startYear = element.attr("startYear");
            }

            minYear = eval(startYear + minYear);
            maxYear = eval(startYear + maxYear);

            for (var i = maxYear; i >= minYear; i--) {
                $scope.years.push(i);
            }

            $scope.tempDate = (new Date()).getDate();
            $scope.tempMonth = (new Date()).getMonth();
            $scope.tempYear = startYear;
        }

        return {
            restrict: 'E',
            link: link,
            scope: {
                model: '=model',
            },
            templateUrl: '/Modules/ControlsSystem/Templates/ncbSimpleDatePicker.html',
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
            } else {
                element.find("h2.modal-title").text(element.attr("title"));
            }

            if (element.find(".modal-header").children().length == 0) {
                element.find(".modal-header").remove();
            }

            var footer = element.find("ncb-footer").remove();
            if (footer.length > 0) {
                var footerTpl = $compile(footer);
                footerTpl(scope);
            }
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

