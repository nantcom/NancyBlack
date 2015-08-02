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
    ncg.directive('ncgCart', function ($rootScope, $http, localStorageService) {

        function link(scope, element, attrs) {

            if (scope.shoppingcart != null) {

                throw "Cart system was already intialized in this scope"
            }

            scope.shoppingcart = cartSystem;

            // ensures that there is an initialized shopping cart
            cartSystem.ensureCartAvailable = function () {

                if (cartSystem.cart == null) {


                    //TODO: if logged in, load the current cart of the user

                    // shopping cart was not initialized for this scope, create it
                    cartSystem.cart = {
                        uuid: generateUUID(),
                        items: [],
                        owner: 'Anonymous'
                    };
                }

                if (cartSystem.cart.customer == null) {

                    cartSystem.cart.customer = {};
                }

                if (scope.currentUser != null) {

                    // user has logged in - update the cart info
                    cartSystem.cart.customer.email = scope.currentUser.Email;
                    cartSystem.cart.owner = scope.currentUser.Id;

                    cartSystem.saveCart();
                }
            };

            // Add item to Cart, itemId
            cartSystem.add = function (productId) {

                cartSystem.ensureCartAvailable();
                cartSystem.cart.items.push(parseInt(productId));
                cartSystem.saveCart();
            };

            // remove given product id from cart
            cartSystem.remove = function (productId) {

                cartSystem.ensureCartAvailable();

                productId = parseInt(productId);

                var partitions = _.partition(cartSystem.cart.items, function (item) { return item == productId; });

                var toRemove = partitions[0];
                var remainder = partitions[1];

                if (toRemove.length == 1) {

                    if (confirm("Do you want to remove this item?") == false) {
                        return;
                    }
                }

                toRemove.pop();
                cartSystem.cart.items = toRemove.concat(remainder);

                cartSystem.saveCart();
            };

            // save cart to storage, automatically called by add/remove
            cartSystem.saveCart = function () {

                localStorageService.set('cart', cartSystem.cart);
                cartSystem.totalitems = cartSystem.cart.items.length;
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

                if (cartSystem.cart == null || cartSystem.cart.items == null) {
                    return;
                }

                var getCount = function () {

                    var productId = scope.$eval(attrs.productid);
                    var result = _.filter(cartSystem.cart.items, function (item) { return item == productId; });

                    scope.count = result.length;
                };

                scope.$watchCollection(function () { return cartSystem.cart.items; }, getCount);
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

                if (cartSystem.cart == null || cartSystem.cart.items == null) {
                    return;
                }

                scope.$watchCollection(function () { return cartSystem.cart.items; }, function () {

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

        if ($scope.shoppingcart == null) {

            throw "require ncg-Cart in current scope";
        }

        var $me = this;
        $scope.products = {};
        $scope.cartView = {};

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

        var updateView = function () {
            $scope.cartView = _.groupBy($scope.shoppingcart.cart.items, function (item) { return item; });
            updateProductInfo();
        };

        $scope.$watchCollection(function () { return cartSystem.cart.items; }, updateView);

        $me.copytobilling = function () {


            cartSystem.cart.billto = JSON.parse(JSON.stringify(cartSystem.cart.shipto));
        };

        $me.savecart = function (datacontext, next) {

            datacontext.save(cartSystem.cart, next);
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
        }, 1000);

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

            $scope.cartView = _.groupBy($scope.saleorder.items, function (item) { return item; });
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
})();