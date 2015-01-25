(function () {

    var ncbEditor = angular.module("editor-ui", ['ui.bootstrap', 'ncb-database']);

    
    ncbEditor.controller("PanelController", function ($scope, $rootScope, ncbDatabaseClient ) {

        var $me = this;
        var ncbClient = new ncbDatabaseClient($me, $scope, "Content");

        $scope.object = GLOBAL_CONTENT;
        $scope.isDirty = false;

        $(document).on("ncb-database", function (e) {

            if (e.action == "update" && e.sender == ncbClient) {

                $scope.$apply(function () {

                    $scope.isDirty = false;
                });
            }

        });

        window.onbeforeunload = confirmExit;
        function confirmExit()
        {
            if ($scope.isDirty) {
                return "You did not upload the changes yet";
            }
        }

        this.save = function()
        {
            ncbClient.save();
        }

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
                $scope.isDirty = true;
                $scope.object[$scope.activeEditor.data("propertyname")] = html;
            }
            
            $scope.activeEditor = null;
            return html;
        };

        this.leaveEditMode = function () {

            if (confirm("leave edit mode?")) {
                
                $("#leaveEditMode").submit();
            }

        };
    });


})();