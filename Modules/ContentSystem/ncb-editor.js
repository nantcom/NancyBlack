(function () {
    
    var ncb = angular.module("ncb-editor", []);

    ncb.directive('ncbContentlist', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            if ($("body").is("[editmode]") == false) {

                return;
            }

            if (attrs.itemlayout == null) {

                throw "attribute itemlayout is required";
            }

            var $me = this;

            $me.parentUrl = location.pathname;

            element.attr("ncb-datacontext-integrated", "");
            element.attr("table", "content");

            element.removeAttr("ncb-contentlist"); // prevent infinite loop
            element.addClass("ncb-contentlist");

            var template = $compile(element);
            template($scope);

            $scope.contentlist = {};

            $scope.contentlist.insertItem = function (displayOrder) {

                var newContent = {};

                newContent.Url = location.pathname + "/" + (new Date()).getTime();
                newContent.Url = newContent.Url.toLowerCase();
                newContent.DisplayOrder = displayOrder;

                newContent.Layout = attrs.itemlayout;

                if (displayOrder == null) {

                    // no display order specified, just add and reload
                    $scope.data.insert(newContent, function () {

                        location.reload();
                    });

                } else {

                    // add the new content among siblings
                    $scope.contentlist.siblings.splice(displayOrder, 0, newContent);

                    // set the index on siblings
                    $scope.contentlist.siblings.forEach(function (element, index) {

                        element.DisplayOrder = index;
                    });

                    // save all siblings
                    var saveIndex = -1;
                    var saver = function () {

                        saveIndex++;
                        if (saveIndex >= $scope.contentlist.siblings.length) {

                            location.reload();
                            return;
                        }

                        $scope.data.save($scope.contentlist.siblings[saveIndex], saver);
                    };
                    saver();
                }

            };
            
            // gets all contents under this url
            // insert anchor will only be added if siblings gets loaded correctl;y
            $scope.data.query("$filter=startswith(Url, '" + $me.parentUrl + "')", function (results) {

                $scope.contentlist.siblings = results;

                // add insert anchor
                element.find("> *").each(function (index, item) {

                    var $item = $(item);
                    $item.before('<div class="insert" data-index="' + index + '"><i class="fa fa-plus"></div>');
                });
                element.append('<div class="insert"><i class="fa fa-plus"></div>');

                element.find(" > div.insert").on("click", function () {

                    var insertItem = $(this);
                    $scope.contentlist.insertItem(insertItem.data("index"));
                });

            });

        }

        return {
            restrict: 'A',
            link: link,
            terminal: true, // we will use $compile - so we want to stop all other directives
            priority: 9999, // make sure we got compiled first
            scope: true, // create as child scope
        };
    }]);

    ncb.directive('ncbAddcontentbutton', ['$compile', function ($compile) {

        function link($scope, element, attrs) {

            var $me = this;

            element.attr("ng-click", "contentlist.addContent()");
            element.removeAttr("ncb-addcontentbutton"); // prevent infinite loop

            var template = $compile(element);
            template($scope);
        }

        return {
            restrict: 'A',
            link: link,
            terminal: true, // we will use $compile - so we want to stop all other directives
            priority: 9999, // make sure we got compiled first
        };
    }]);


})();