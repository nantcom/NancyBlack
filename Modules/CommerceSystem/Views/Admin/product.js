(function () {
    'use strict';

    angular
        .module('app', ['ngTable', 'ui.tree'])
        .controller('product_controller', product_controller);

    product_controller.$inject = ['ngTableParams', '$location', '$log', '$scope', '$window', '$http', '$rootScope'];

    function product_controller(ngTableParams, $location, $log, $scope, $window, $http, $rootScope) {
        /* jshint validthis:true */

        $scope.object = {};

        $scope.defaultAttributes = ['Color', 'Gender', 'Size', 'BirthMonth'];
        
        var vm = this;        
        vm.view_data = _viewData;
        vm.filterbyurl = _FilterByUrl;
        vm.UpdateMultipleProducts = _UpdateMultipleProducts;

        vm.UpdateStock = 0;
        vm.UpdatePrice = 0;
        vm.IsCollapse = true;
        vm.list = [];

        $scope.$on("ncb-datacontext.loaded", function () {
            _LoadTreeDataLeftMenu();
        });                            

        function _LoadTreeDataLeftMenu() {

            $http.get('/__commerce/api/productstructure').
                  then(function (response) {                      
                      vm.list = response.data                      
                  }, function (response) {                      
                      $log.error(response)
                  });
        };

        function _UpdateMultipleProducts() {

            $scope.tableParams.data.forEach(function (Object) {
                var checked = ($scope.checkboxes.items[Object.id]) || 0;
                if (checked == true) {                    

                    Object.Stock = vm.UpdateStock;
                    Object.Price = vm.UpdatePrice;
                    
                    $scope.data.save(Object)                    
                }
                
            });

        };

        // Start Checkbox Zone
        $scope.checkboxes = { checked: false, items: {} };

        $scope.$watch('checkboxes.checked', function (value) {
            angular.forEach($scope.tableParams.data, function (item) {
                if (angular.isDefined(item.id)) {
                    $scope.checkboxes.items[item.id] = value;
                }
            });
        });

        $scope.$watch('checkboxes.items', function (values) {
            if (!$scope.tableParams.data) {
                return;
            }
            var checked = 0, unchecked = 0,
                    total = $scope.tableParams.data.length;
            angular.forEach($scope.tableParams.data, function (item) {
                checked += ($scope.checkboxes.items[item.id]) || 0;
                unchecked += (!$scope.checkboxes.items[item.id]) || 0;
            });
            if ((unchecked == 0) || (checked == 0)) {
                //$scope.checkboxes.checked = (checked == total);
            }
            // grayed checkbox
            //angular.element(document.getElementById("select_all")).prop("indeterminate", (checked != 0 && unchecked != 0));
        }, true);

        // End Checkbox Zone

        $scope.filters = {};
        $scope.tableParams = new ngTableParams({
            page: 1,            // show first page
            count: 10,          // count per page
            sorting: {
                Id: 'asc'       // initial sorting
            },
            filter: $scope.filters // initial filters
        }, {
            total: 0, // length of data
            getData: function ($defer, params) {

                $scope.isBusy = true;
                var oDataQueryParams = _oDataAddFilterAndOrder(params);
                
                $scope.data.inlinecount(oDataQueryParams, function (data) {

                    $scope.isBusy = false;
                    $defer.resolve(data.Results);
                    params.total(data.Count);
                });                
    
            }
        });

        //#region Reload on data change
        {
            var reload = function () {

                $("#ProductModal").modal('hide');

                vm.IsCollapse = true;

                $scope.tableParams.reload();
                _LoadTreeDataLeftMenu();
            };
            
            $rootScope.$on("inserted", reload);
            $rootScope.$on("deleted", reload);
            $rootScope.$on("ncb-datacontext.deleted", reload);

        }

        function _oDataAddFilterAndOrder(params) {

            var oDataQueryParams = '';
            
            var _filter = params.filter();
            var _sorting = params.sorting();
            var _pageNum = params.page();
            var _pageSize = params.count();

            var _takeV = 10;
            var _skipV = 0;

            // OrderBy
            for (var property in _sorting) {
                var _sortOrder = _sorting[property];
                oDataQueryParams += '$orderby=' + property + ' ' + _sortOrder;
            }

            // Filters
            var _arrFilters = [];
            for (var property in _filter) {
                var _filterAttr = _filter[property];

                if (_filterAttr == null || _filterAttr == "") { continue; }

                var _strFilter = "";

                _strFilter = "contains(" + property + ",'" + _filterAttr + "')";
                                              
                _arrFilters.push(_strFilter);
            }

            if (_arrFilters.length > 0) {
                var _baseQuery = "&$filter=";
                var _joinFilters = _arrFilters.join(" and ");
                oDataQueryParams += _baseQuery + _joinFilters;                
            }

            // Skip & Take
            _takeV = _pageSize;
            _skipV = (_pageNum * _pageSize) - _pageSize;

            oDataQueryParams += "&$skip=" + _skipV + "&$top=" + _takeV;

            // InlineCount
            oDataQueryParams += '&$inlinecount=allpages';

            return oDataQueryParams;

        };        

        function _viewData(Product) {            
            vm.IsCollapse = false;

            $scope.object = Product;
            $scope.carouseltemplate = null;

            window.setTimeout(function () {

                $scope.$apply(function () {

                    $scope.carouseltemplate = "carousel.html";
                });
            }, 100);
        };

        function _FilterByUrl(CollectionName) {
            var _url = CollectionName.fullPath;
            $scope.filters.Url = _url;            
        };

    }
})();
