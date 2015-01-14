(function () {

    var module = angular.module('ncb-controls', []);
    
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
            templateUrl: '/Content/ncbControls/ncbPictureList.html',
            link: link
        };
    }]);

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