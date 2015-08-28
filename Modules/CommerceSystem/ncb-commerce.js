(function () {
    //Commerce is 'Green' or NCG

    function generateUUID() {
        var d = new Date().getTime();
        var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (d + Math.random() * 16) % 16 | 0;
            d = Math.floor(d / 16);
            return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
        });
        return uuid;
    };

    var ncg = angular.module("ncb-commerce", []);

    ncg.config(function (localStorageServiceProvider) {
        localStorageServiceProvider
          .setPrefix('ncg')
          .setNotify(false, false)
    });

    // cart system, define in module scope to let all directives access it
    var cartSystem = {};

    // create cart system in current scope
    ncg.directive('ncgCart', function ($rootScope, $http, $timeout, localStorageService) {

        function link(scope, element, attrs) {

            if (scope.shoppingcart != null) {

                throw "Cart system was already intialized in this scope"
            }

            scope.shoppingcart = cartSystem;
            
            // ensures that there is an initialized shopping cart
            cartSystem.ensureCartAvailable = function () {

                if (cartSystem.cart == null) {

                    // shopping cart was not initialized for this scope, create it
                    cartSystem.cart = {
                        Items: [],
                        NcbUserId: 0,
                    };
                }

                if (cartSystem.cart.Customer == null) {

                    cartSystem.cart.Customer = {};
                }

                if (scope.currentUser != null) {

                    // user has logged in - update the cart info
                    cartSystem.cart.Customer.Email = scope.currentUser.Email;
                    cartSystem.cart.NcbUserId = scope.currentUser.Id;

                    cartSystem.saveCart();
                }
            };

            cartSystem.add = function (productId, amount) {

                cartSystem.ensureCartAvailable();

                var productId = parseInt(productId);

                if (amount != null && typeof(amount) == "number") {

                    for (var i = 0; i < amount; i++) {
                        cartSystem.cart.Items.push(productId);
                    }
                } else {

                    cartSystem.cart.Items.push(productId);
                }

                cartSystem.saveCart();
            };

            // remove given product id from cart
            cartSystem.remove = function (productId) {

                cartSystem.ensureCartAvailable();

                productId = parseInt(productId);

                var partitions = _.partition(cartSystem.cart.Items, function (item) { return item == productId; });

                var toRemove = partitions[0];
                var remainder = partitions[1];

                if (toRemove.length == 1) {

                    if (confirm("Do you want to remove this item?") == false) {
                        return;
                    }
                }

                toRemove.pop();
                cartSystem.cart.Items = toRemove.concat(remainder);

                cartSystem.saveCart();
            };

            // save cart to storage, automatically called by add/remove
            cartSystem.saveCart = function () {

                localStorageService.set('cart', cartSystem.cart);
                cartSystem.totalitems = cartSystem.cart.Items.length;
            };

            cartSystem.checkout = function ( callback ) {

                $http.post("/__commerce/api/checkout", cartSystem.cart)
                    .success(function (data) {

                        // hide cart
                        $("#cartmodal").modal("hide");
                        $("#working").addClass("show");

                        $timeout(cartSystem.clearcart, 100);

                        callback(data);
                    })
                    .error(function (data) {

                        callback({
                            error: true,
                            type: 'danger',
                            data: data
                        });
                    });
            };

            cartSystem.clearcart = function () {

                // checked out, clear the local cart
                localStorageService.set('cart', null);
                cartSystem.cart = null;
                cartSystem.ensureCartAvailable();

            };

            cartSystem.cart = localStorageService.get('cart');
            cartSystem.ensureCartAvailable();

            $rootScope.$broadcast("ncg-cart.initialized", { sender: scope });
        }

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });

    // Add to cart button
    ncg.directive('ncgAddtocart', function ($http, localStorageService) {

        function link(scope, element, attrs) {

            element.on("click", function (e) {

                if (scope.shoppingcart == null) {

                    throw "require ncg-Cart in current scope";
                }

                if (attrs.productid == null) {

                    throw "require productid attribute";
                }


                e.preventDefault();

                var productid = scope.$eval(attrs.productid);
                if (productid == null) {

                    throw "'" + attrs.productid + "' evaluates to empty";
                }

                scope.$apply(function () {

                    scope.shoppingcart.add(productid);
                });
            });
        }

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });

    // gets the number of item currently in cart
    ncg.directive('ncgCurrentincart', function ($http, localStorageService) {

        function link(scope, element, attrs) {

            scope.count = null;

            var initialize = function () {

                if (scope.initialized == true) {

                    return;
                }

                if (cartSystem.cart == null || cartSystem.cart.Items == null) {
                    return;
                }

                var getCount = function () {

                    var productId = scope.$eval(attrs.productid);
                    var result = _.filter(cartSystem.cart.Items, function (item) { return item == productId; });

                    scope.count = result.length;
                };

                scope.$watchCollection(function () { return cartSystem.cart.Items; }, getCount);
                scope.$watch(attrs.productid, getCount);

                scope.initialized = true;
            };

            initialize();
            scope.$on("ncg-cart.initialized", initialize);
        }

        return {
            restrict: 'A',
            link: link,
            scope: true,
        };
    });

    // shows the link to open shopping cart and also hilight when content of the cart changed
    ncg.directive('ncgCartbutton', function ($compile, $timeout) {

        function link(scope, element, attrs) {

            scope.modalsource = null;
            var initialize = function () {

                if (scope.initialized == true) {

                    return;
                }

                if (cartSystem.cart == null || cartSystem.cart.Items == null) {
                    return;
                }

                scope.$watchCollection(function () { return cartSystem.cart.Items; }, function () {

                    // item was added/removed
                    element.find("#cartbutton").addClass("notify");
                });

                scope.showcartmodal = function () {

                    if ($("#cartmodal").length == 0) {

                        $("body").css("cursor", "progress");
                        $timeout(scope.showcartmodal, 400);
                        return;
                    }

                    $("body").css("cursor", "default");
                    $("#cartmodal").modal("show");
                };

                scope.viewCart = function (e) {

                    e.preventDefault();

                    if (scope.modalsource == null) {
                        scope.modalsource = "/modules/commercesystem/templates/ncg-cartmodal.html";
                    }

                    scope.showcartmodal();
                };

                scope.initialized = true;
            };

            initialize();
            scope.$on("ncg-cart.initialized", initialize);

        }

        return {
            restrict: 'E',
            templateUrl: '/Modules/CommerceSystem/templates/ncg-cartbutton.html',
            link: link,
            scope: true,
            replace: true,
        };
    });

    ncg.controller("CheckoutModal", function ($scope, $http, $timeout) {

        var $me = this;

        $scope.profileForm = null;
        $scope.shipToForm = null;
        $scope.billToForm = null;

        $scope.pages = [true, false, false, false, false]

        //#region Attach forms

        $scope.$on('formLocator', function (event, data) {

            if (event.targetScope.shippingAddress != null) {
                $scope.shipToForm = event.targetScope.shippingAddress;
            }

            if (event.targetScope.billingAddress != null) {
                $scope.billToForm = event.targetScope.billingAddress;
            }

            if (event.targetScope.profile != null) {
                $scope.profileForm = event.targetScope.profile;
            }
        });

        //#endregion

        //#region Handle Address Save

        {
            $scope.addresses = [];

            var cleanupAddress = function (input) {

                if (input == null) {

                    return null;
                }

                input.To = input.To.trim();
                input.Address = input.Address.trim();
                input.Country = input.Country.trim();
                input.State = input.State.trim();
                input.District = input.District.trim();
                input.SubDistrict = input.SubDistrict.trim();
                input.PostalCode = input.PostalCode.trim();

                return input;
            };

            var refreshAddresses = function () {

                // load address
                $http.get("/__commerce/api/addresses").success(function (data) {

                    $scope.addresses = data;
                });
            };

            $scope.$on("ncb-membership.login", function () {

                refreshAddresses();
            });

            $scope.$watch("pages[3]", function (newValue, oldValue) {

                // page activated, refresh address
                if (newValue == true && oldValue == false) {

                    if ($scope.addresses.length == 0) {

                        refreshAddresses();
                    }
                }

                // page 3 (addresses) was deactivated
                if (newValue == false && oldValue == true) {

                    if ($scope.shipToForm.$valid == false) {

                        return;
                    }

                    var toSave = [];
                    toSave[0] = cleanupAddress($scope.shoppingcart.cart.shipto);
                    toSave[1] = cleanupAddress($scope.shoppingcart.cart.billto);

                    $http.post("/__commerce/api/address", toSave)
                        .success(function (data) {
                            $scope.addresses = data;
                    });
                }
            });
        }

        //#endregion

        //#region Handling Profile save

        $scope.$watch("pages[2]", function (newValue, oldValue) {

            // page 2 was deactivated after activated
            if (newValue == false && oldValue == true) {

                $scope.membership.updateProfile();

            }
        });

        //#endregion

        // Disable/Enable Next Button
        $me.everythingOK = function () {

            if ($scope.pages[0] == true) {

                return true;
            }

            if ($scope.pages[1] == true) {

                return $scope.membership.isLoggedIn();
            }

            if ($scope.pages[2] == true) {

                if ($scope.profileForm == null) {
                    return false;
                }

                return $scope.profileForm.$valid == true;
            }

            if ($scope.pages[3] == true) {

                if ($scope.shipToForm == null || $scope.billToForm == null) {
                    return false;
                }

                if ($scope.shoppingcart.cart.useBillingAddress == true) {

                    return $scope.shipToForm.$valid == true &&
                           $scope.billToForm.$valid == true;
                }

                return $scope.shipToForm.$valid == true;
            }
            return false;
        };

        $me.copytobilling = function () {


            cartSystem.cart.billto = JSON.parse(JSON.stringify(cartSystem.cart.shipto));
        };

        $me.moneytransfer = function ( datacontext) {

            $me.savecart(datacontext, function ( item ) {

                window.location.href = "/__commerce/saleorder/" + item.uuid + "/notifytransfer";

            });
        };

        $timeout(function () {

            // view the cart directly
            if (window.location.pathname == "/__commerce/cart") {

                $scope.showcartmodal = function () {

                    if ($("#cartmodal").length == 0) {

                        $("body").css("cursor", "progress");
                        $timeout($scope.showcartmodal, 400);
                        return;
                    }

                    $("body").css("cursor", "default");
                    $("#cartmodal").modal("show");
                };

                $scope.showcartmodal();
            }
        }, 100);

    });

    ncg.controller("PaysbuyController", function ($scope, $http, $timeout) {

        if ($scope.shoppingcart == null) {

            throw "require ncg-Cart in current scope";
        }

        $scope.so = {};

        var $me = this;
        $me.pay = function () {

            $("#working").addClass("show");

            var submitForm = function () {

                $("#paysbuy_form").attr("action",
                    "https://www.paysbuy.com/paynow.aspx?lang=" + $scope.paysbuy.lang);

                $timeout(function () {

                    $("#paysbuy_form").submit();
                }, 1000);

            };

            var getPaysbuySettings = function () {

                $http.get("/__commerce/paysbuy/settings")
                    .success(function (data) {

                        $scope.paysbuy = data;
                        submitForm();
                    })
            };

            $scope.shoppingcart.checkout(function (data, arg) {

                if (data.error == true) {

                    $("#working").removeClass("show");
                    alert("Cannot Process your request, please try again.");
                    return;
                }

                $scope.so = data;

                getPaysbuySettings();
            });

        };

    });

    ncg.controller("NotifyMoneyTransfer", function ($scope, $timeout) {

        $scope.products = {};
        $scope.cartView = [];

        $scope.saleorder = window.model.Content;
        if ($scope.saleorder.notified == null) {

            $scope.saleorder.notified = false;
        }

        var getProdcutInfo = function (productid) {

            $scope.data.getById(productid, function (item) {

                $scope.$apply(function () {

                    $scope.products[productid] = item;
                });
            });
        };

        var updateProductInfo = function () {

            for (productid in $scope.cartView) {

                if ($scope.products[productid] == null) {

                    getProdcutInfo(productid);
                }
            }
        };

        $timeout(function () {

            // view the cart directly
            if (window.location.pathname.indexOf( "/__commerce/saleorder" ) == 0 &&
                window.location.pathname.indexOf( "/notifytransfer") > 0 ) {


                $("#notifypayment").modal("show");
            }
        }, 1000);

        $scope.$on("ncb-datacontext.loaded", function () {

            $scope.cartView = _.groupBy($scope.saleorder.Items, function (item) { return item; });
            updateProductInfo();
        });

        var $me = this;

        $me.sendnotify = function (datacontext, notify) {

            var files = $("input[type=file]")[0].files;

            if (files.length == 0) {

                alert("Please select a file");
                return;
            }

            var thenUpload = function (result) {

                datacontext.upload(files[0], function () {

                    $scope.saleorder.notified = true;
                }, result.id);
            };

            datacontext.save(notify, thenUpload);
        };
    });

    ncg.directive('ncgSotable', function ($http) {

        function link($scope, element, attrs) {

            $scope.products = {};
            $scope.cartView = {};

            var getProdcutInfo = function (productid) {

                $http.get("/tables/product/" + productid)
                    .success(function (data) {
                        $scope.products[productid] = data;
                    });
            };

            var updateProductInfo = function () {

                for (productid in $scope.cartView) {

                    if ($scope.products[productid] == null) {

                        getProdcutInfo(productid);
                    }
                }
            };

            var updateView = function () {
                $scope.cartView = _.groupBy($scope.shoppingcart.cart.Items, function (item) { return item; });
                updateProductInfo();
            };

            $scope.$watchCollection(function () { return cartSystem.cart.Items; }, updateView);

            $scope.getTotal = function () {

                var total = 0;
                $scope.shoppingcart.cart.Items.forEach(function (productid) {

                    if ($scope.products[productid] != null) {

                        total += $scope.products[productid].Price;
                    }
                });

                return total;
            };

        }

        return {
            restrict: 'E',
            templateUrl: '/Modules/CommerceSystem/templates/ncg-sotable.html',
            link: link,
            scope: false,
            replace: true,
        };
    });
    
    ncg.directive('ncgProductresolver', function ($http) {

        function link($scope, elmement, attrs) {

            $scope.so = $scope.$eval(attrs.saleorder);

            $scope.products = {};
            $scope.cartView = {};

            var getProdcutInfo = function (productid) {

                $http.get("/tables/product/" + productid)
                    .success(function (data) {
                        $scope.products[productid] = data;
                    });
            };

            var updateProductInfo = function () {

                for (productid in $scope.cartView) {

                    if ($scope.products[productid] == null) {

                        getProdcutInfo(productid);
                    }
                }
            };

            var updateView = function () {
                $scope.cartView = _.groupBy($scope.so.Items, function (item) { return item; });
                updateProductInfo();
            };

            $scope.$watchCollection(function () { return $scope.so.Items; }, updateView);

            $scope.getTotal = function () {

                var total = 0;
                if ($scope.so == null) {

                    return 0;
                }
                $scope.so.Items.forEach(function (productid) {

                    if ($scope.products[productid] != null) {

                        total += $scope.products[productid].Price;
                    }
                });

                return total;
            };

        }

        return {
            restrict: 'A',
            link: link,
            scope: true,
        };
    });

    ncg.directive('ncgChart', function ($http, $filter) {

        function link(scope, element, attrs) {                        

            /* Variable declarable */            

            scope.labels = [];
            scope.series = [];
            scope.data = [
              //[65, 59, 80, 81, 56, 55, 40],
              //[28, 48, 40, 19, 86, 27, 90]
            ];

            /* Function Mapping */
            scope.filterChanged = _filterChanged;
            scope.onClick = _chartOnClick;
            scope.getDataByPeriod = _getDataByPeriod;

            /* Function Calling */
            scope.getDataByPeriod("day");

            /* Events */
            // OnChart create
            scope.$on('create', function (event, chart) {                
                //console.log("Create", chart);
            });
            // OnChart update
            scope.$on('update', function (event, chart) {
                //console.log("Update", chart);
            });

            /* Function declarable */
            function _getDataByPeriod(period) {

                var criteria = "/tables/" + scope.table + "/summarize?period=" + period + "&fn=" + scope.fn + "&select=" + scope.select + "&time=__createdAt";
                $http.get(criteria).
                      then(function (response) {                          

                          _mapDataToGraph(response.data, period)

                      }, function (response) {
                          // TODO
                          // called asynchronously if an error occurs
                          // or server returns response with an error status.
                      });
            };

            function _chartOnClick(points, evt) {
                console.log(points, evt);
            };

            function _filterChanged(period) {                
                _getDataByPeriod(period);
            };

            function _FormatDate(timestamp, period) {

                var _displayValue = "";                

                switch (period) {
                    case "day":
                        _displayValue = $filter('date')(timestamp, 'EEEE');
                        break;

                    case "month":
                        _displayValue = $filter('date')(timestamp, 'MMMM, yyyy');
                        break;

                    case "year":
                        _displayValue = $filter('date')(timestamp, 'yyyy');;
                        break;

                    default:
                        _displayValue = timestamp;
                        break;
                }
                
                return _displayValue;
            };

            function _mapDataToGraph(data, period) {
                
                var arrKey = [], arrValue = [];
                if (period == "hour") {

                    data.forEach(function (item) {
                        var date = new Date(parseInt(item.Key));

                        arrKey.push(String.format("{0:HH}:00", date));
                        arrValue.push(item.Value);
                    });
                }
                if (period == "week") {

                    data.forEach(function (item) {
                        arrKey.push(item.Key);
                        arrValue.push(item.Value);
                    });
                }
                if (period == "day") {

                    data.forEach(function (item) {
                        var date = new Date(parseInt(item.Key));

                        arrKey.push(String.format("{0:dddd}", date));
                        arrValue.push(item.Value);
                    });
                }
                if (period == "month") {

                    data.forEach(function (item) {
                        var date = new Date(parseInt(item.Key));

                        arrKey.push(String.format("{0:MMMM}", date));
                        arrValue.push(item.Value);
                    });
                }
                if (period == "year") {

                    data.forEach(function (item) {
                        var date = new Date(parseInt(item.Key));

                        arrKey.push(String.format("{0:yyyy}", date));
                        arrValue.push(item.Value);
                    });
                }

                scope.data = [arrValue];                
                scope.labels = arrKey;

            };

        }

        return {
            restrict: 'A',
            templateUrl: '/Modules/CommerceSystem/templates/ncg-chart.html',
            link: link,
            scope: {
                title: "=title",
                table: "=table",
                fn: "=fn",
                select: "=select"
            },
            //replace: true,
        };
    });




})();