(function () {

    var socialprove = angular.module('ncb-socialprove', []);

    socialprove.directive('ncbSocialprove', function ($http, $compile, $window) {

        function link($scope, element, attrs) {

            if ($scope.socialprove != null) {

                throw "Social Prove was already used in this scope";
            }

            var $me = {};
            $scope.socialprove = $me;

            $me.proves = [];
            $me.getprove = function (eventName, callback) {

                $http.get('/__socialprove?e=' + eventName).
                    success(function (data, status, headers, config) {
                        $me.proves[eventName] = data;

                        if (callback != null) {
                            callback(data);
                        }
                    });
            }

            $me.getprovewithdata = function (eventName, data, callback) {

                $http.get('/__socialprove?e=' + eventName + '&d=' + data).
                    success(function (data, status, headers, config) {
                        $me.proves[eventName] = data;

                        if (callback != null) {
                            callback(data);
                        }
                    });
            }

            var currentIndex = 0;
            var queue = [];

            $me.current = null;
            $me.showproof = function (item, permanent) {

                var notification = function () {
                    var time = -1;
                    var myItem = item;
                    var interval = window.setInterval(function () {

                        time++;

                        if (time == 0) {
                            // hide and apply the item
                            $(".socialprove").addClass("noprove");
                            return;
                        }

                        if (time == 8) {
                            // hide and apply the item
                            $scope.$apply(function () {
                                $me.current = myItem;
                            });
                            return;
                        }

                        if (time == 10) {
                            $(".socialprove").removeClass("noprove");
                            return; // show after 1 second
                        }

                        if (time >= 70) {
                            // time for us to go
                            $(".socialprove").addClass("noprove");
                            window.clearInterval(interval); // clear this interval

                            currentIndex++;
                            if (currentIndex < queue.length) {

                                queue[currentIndex](); // run the next one
                                return;
                            }

                            // we are the last
                            queue = queue.filter(i => i.permanent == true);
                            currentIndex = 0;

                            if (queue.length > 0) {
                                queue[0](); // loop the permanent notification
                            }
                        }


                    }, 100);
                };

                notification.item = item;
                notification.permanent = permanent;

                for (var i = 0; i < queue.length; i++) {
                    if (queue[i].item.title == item.title &&
                        queue[i].item.message == item.message &&
                        queue[i].item.footer == item.footer &&
                        queue[i].item.img == item.img) {

                        return;
                    }
                }

                queue.splice(currentIndex + 1, 0, notification);

                if (queue.length == 1) {
                    notification(); // start the notification queue
                }
            };

            $me.getpageview = function (callback) {

                $me.getprove(encodeURIComponent(window.location.pathname), callback);
            };

            $me.getprove('TotalVisitor', function () {

                $scope.$broadcast("ncb-socialprove.ready", {
                    sender: $scope,
                    user: $me,
                });

                $scope.$emit("ncb-socialprove.ready", {
                    sender: $scope,
                    user: $me,
                });
            });

        }

        return {
            restrict: 'A',
            link: link,
            scope: false
        };
    });


})();