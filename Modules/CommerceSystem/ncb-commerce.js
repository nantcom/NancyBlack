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

            cartSystem.cart = localStorageService.get('cart');

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

            };

            // Add item to Cart, itemId
            cartSystem.add = function (productId) {

                cartSystem.ensureCartAvailable();
                cartSystem.cart.items.push(productId);
                cartSystem.saveCart();
            };

            // remove given product id from cart
            cartSystem.remove = function (productId) {

                cartSystem.ensureCartAvailable();

                cartSystem.cart.items = _.without(cartSystem.cart.items, productId);
                cartSystem.saveCart();
            };

            // save cart to storage, automatically called by add/remove
            cartSystem.saveCart = function () {

                localStorageService.set('cart', cartSystem.cart);
                cartSystem.totalitems = cartSystem.cart.items.length;
            };

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

})();