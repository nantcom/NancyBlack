(function () {

    var ncbEditor = angular.module("ncbEditor", ['ui.bootstrap', 'ncb']);

    
    ncbEditor.controller("PanelController", function ($scope, $rootScope, zumo, ncbOps ) {

        var table = zumo.getTable("Content");

        var standardOperations = new ncbOps($scope, table, '#ContentForm');
        standardOperations.startScope();

        this.delete = standardOperations.del;
        this.save = standardOperations.save;

        $scope.object = GLOBAL_CONTENT;

        $scope.activeEditor = null;
        this.enableEditArea = function () {

            $scope.show = 0;
            CKEDITOR.disableAutoInline = true;

            $("*[data-editable]").each(function () {

                var $me = $(this);
                if ($me.hasClass("editarea")) {
                    return;
                }

                $me.addClass("editarea");
                $me.attr("id", "editor" + $me.data("propertyName"));

                $me.one("click", function () {

                    if ($scope.activeEditor != null ) {
                        return;
                    }

                    // clears out all edit area
                    $("*[data-editable]").removeClass("editarea");
                    $me.addClass("editarea"); // except us

                    $me.data("oldContent", $me.html());
                    $me.attr("contenteditable", true);
                    $me.data("ckeditor", CKEDITOR.inline($me.attr("id")));
                    $scope.activeEditor = $me;

                });
            });

        };

        this.commitEditArea = function ( undo ) {

            if ($scope.activeEditor == null) {
                return;
            }

            $scope.activeEditor.removeAttr("contenteditable");
            $scope.activeEditor.removeClass("editarea");

            var editor = $scope.activeEditor.data("ckeditor");
            editor.destroy();

            var html = $scope.activeEditor.html();

            if (undo) {
                $scope.activeEditor.html($scope.activeEditor.data("oldContent"));
            } else {
                $scope.object[$scope.activeEditor.data("propertyname")] = html;
            }
            
            $scope.activeEditor = null;
            return html;
        };


    });


})();