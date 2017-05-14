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

    //#region Currency Codes

    var currencyList = [
        { 'code': 'AED', name: 'United Arab Emirates dirham' },
        { 'code': 'AFN', name: 'Afghan afghani' },
        { 'code': 'ALL', name: 'Albanian lek' },
        { 'code': 'AMD', name: 'Armenian dram' },
        { 'code': 'ANG', name: 'Netherlands Antillean guilder' },
        { 'code': 'AOA', name: 'Angolan kwanza' },
        { 'code': 'ARS', name: 'Argentine peso' },
        { 'code': 'AUD', name: 'Australian dollar' },
        { 'code': 'AWG', name: 'Aruban florin' },
        { 'code': 'AZN', name: 'Azerbaijani manat' },
        { 'code': 'BAM', name: 'Bosnia and Herzegovina convertible mark' },
        { 'code': 'BBD', name: 'Barbados dollar' },
        { 'code': 'BDT', name: 'Bangladeshi taka' },
        { 'code': 'BGN', name: 'Bulgarian lev' },
        { 'code': 'BHD', name: 'Bahraini dinar' },
        { 'code': 'BIF', name: 'Burundian franc' },
        { 'code': 'BMD', name: 'Bermudian dollar' },
        { 'code': 'BND', name: 'Brunei dollar' },
        { 'code': 'BOB', name: 'Boliviano' },
        { 'code': 'BRL', name: 'Brazilian real' },
        { 'code': 'BSD', name: 'Bahamian dollar' },
        { 'code': 'BTN', name: 'Bhutanese ngultrum' },
        { 'code': 'BWP', name: 'Botswana pula' },
        { 'code': 'BYR', name: 'Belarusian ruble' },
        { 'code': 'BZD', name: 'Belize dollar' },
        { 'code': 'CAD', name: 'Canadian dollar' },
        { 'code': 'CDF', name: 'Congolese franc' },
        { 'code': 'CHE', name: 'WIR Euro (complementary currency)' },
        { 'code': 'CHF', name: 'Swiss franc' },
        { 'code': 'CHW', name: 'WIR Franc (complementary currency)' },
        { 'code': 'CLP', name: 'Chilean peso' },
        { 'code': 'CNY', name: 'Chinese yuan' },
        { 'code': 'COP', name: 'Colombian peso' },
        { 'code': 'CRC', name: 'Costa Rican colon' },
        { 'code': 'CUC', name: 'Cuban convertible peso' },
        { 'code': 'CUP', name: 'Cuban peso' },
        { 'code': 'CVE', name: 'Cape Verde escudo' },
        { 'code': 'CZK', name: 'Czech koruna' },
        { 'code': 'DJF', name: 'Djiboutian franc' },
        { 'code': 'DKK', name: 'Danish krone' },
        { 'code': 'DOP', name: 'Dominican peso' },
        { 'code': 'DZD', name: 'Algerian dinar' },
        { 'code': 'EGP', name: 'Egyptian pound' },
        { 'code': 'ERN', name: 'Eritrean nakfa' },
        { 'code': 'ETB', name: 'Ethiopian birr' },
        { 'code': 'EUR', name: 'Euro' },
        { 'code': 'FJD', name: 'Fiji dollar' },
        { 'code': 'FKP', name: 'Falkland Islands pound' },
        { 'code': 'GBP', name: 'Pound sterling' },
        { 'code': 'GEL', name: 'Georgian lari' },
        { 'code': 'GHS', name: 'Ghanaian cedi' },
        { 'code': 'GIP', name: 'Gibraltar pound' },
        { 'code': 'GMD', name: 'Gambian dalasi' },
        { 'code': 'GNF', name: 'Guinean franc' },
        { 'code': 'GTQ', name: 'Guatemalan quetzal' },
        { 'code': 'GYD', name: 'Guyanese dollar' },
        { 'code': 'HKD', name: 'Hong Kong dollar' },
        { 'code': 'HNL', name: 'Honduran lempira' },
        { 'code': 'HRK', name: 'Croatian kuna' },
        { 'code': 'HTG', name: 'Haitian gourde' },
        { 'code': 'HUF', name: 'Hungarian forint' },
        { 'code': 'IDR', name: 'Indonesian rupiah' },
        { 'code': 'ILS', name: 'Israeli new shekel' },
        { 'code': 'INR', name: 'Indian rupee' },
        { 'code': 'IQD', name: 'Iraqi dinar' },
        { 'code': 'IRR', name: 'Iranian rial' },
        { 'code': 'ISK', name: 'Icelandic króna' },
        { 'code': 'JMD', name: 'Jamaican dollar' },
        { 'code': 'JOD', name: 'Jordanian dinar' },
        { 'code': 'JPY', name: 'Japanese yen' },
        { 'code': 'KES', name: 'Kenyan shilling' },
        { 'code': 'KGS', name: 'Kyrgyzstani som' },
        { 'code': 'KHR', name: 'Cambodian riel' },
        { 'code': 'KMF', name: 'Comoro franc' },
        { 'code': 'KPW', name: 'North Korean won' },
        { 'code': 'KRW', name: 'South Korean won' },
        { 'code': 'KWD', name: 'Kuwaiti dinar' },
        { 'code': 'KYD', name: 'Cayman Islands dollar' },
        { 'code': 'KZT', name: 'Kazakhstani tenge' },
        { 'code': 'LAK', name: 'Lao kip' },
        { 'code': 'LBP', name: 'Lebanese pound' },
        { 'code': 'LKR', name: 'Sri Lankan rupee' },
        { 'code': 'LRD', name: 'Liberian dollar' },
        { 'code': 'LSL', name: 'Lesotho loti' },
        { 'code': 'LYD', name: 'Libyan dinar' },
        { 'code': 'MAD', name: 'Moroccan dirham' },
        { 'code': 'MDL', name: 'Moldovan leu' },
        { 'code': 'MGA', name: 'Malagasy ariary' },
        { 'code': 'MKD', name: 'Macedonian denar' },
        { 'code': 'MMK', name: 'Myanmar kyat' },
        { 'code': 'MNT', name: 'Mongolian tugrik' },
        { 'code': 'MOP', name: 'Macanese pataca' },
        { 'code': 'MRO', name: 'Mauritanian ouguiya' },
        { 'code': 'MUR', name: 'Mauritian rupee' },
        { 'code': 'MVR', name: 'Maldivian rufiyaa' },
        { 'code': 'MWK', name: 'Malawian kwacha' },
        { 'code': 'MXN', name: 'Mexican peso' },
        { 'code': 'MYR', name: 'Malaysian ringgit' },
        { 'code': 'MZN', name: 'Mozambican metical' },
        { 'code': 'NAD', name: 'Namibian dollar' },
        { 'code': 'NGN', name: 'Nigerian naira' },
        { 'code': 'NIO', name: 'Nicaraguan córdoba' },
        { 'code': 'NOK', name: 'Norwegian krone' },
        { 'code': 'NPR', name: 'Nepalese rupee' },
        { 'code': 'NZD', name: 'New Zealand dollar' },
        { 'code': 'OMR', name: 'Omani rial' },
        { 'code': 'PAB', name: 'Panamanian balboa' },
        { 'code': 'PEN', name: 'Peruvian nuevo sol' },
        { 'code': 'PGK', name: 'Papua New Guinean kina' },
        { 'code': 'PHP', name: 'Philippine peso' },
        { 'code': 'PKR', name: 'Pakistani rupee' },
        { 'code': 'PLN', name: 'Polish złoty' },
        { 'code': 'PYG', name: 'Paraguayan guaraní' },
        { 'code': 'QAR', name: 'Qatari riyal' },
        { 'code': 'RON', name: 'Romanian leu' },
        { 'code': 'RSD', name: 'Serbian dinar' },
        { 'code': 'RUB', name: 'Russian ruble' },
        { 'code': 'RWF', name: 'Rwandan franc' },
        { 'code': 'SAR', name: 'Saudi riyal' },
        { 'code': 'SBD', name: 'Solomon Islands dollar' },
        { 'code': 'SCR', name: 'Seychelles rupee' },
        { 'code': 'SDG', name: 'Sudanese pound' },
        { 'code': 'SEK', name: 'Swedish krona/kronor' },
        { 'code': 'SGD', name: 'Singapore dollar' },
        { 'code': 'SHP', name: 'Saint Helena pound' },
        { 'code': 'SLL', name: 'Sierra Leonean leone' },
        { 'code': 'SOS', name: 'Somali shilling' },
        { 'code': 'SRD', name: 'Surinamese dollar' },
        { 'code': 'SSP', name: 'South Sudanese pound' },
        { 'code': 'STD', name: 'São Tomé and Príncipe dobra' },
        { 'code': 'SYP', name: 'Syrian pound' },
        { 'code': 'SZL', name: 'Swazi lilangeni' },
        { 'code': 'THB', name: 'Thai baht' },
        { 'code': 'TJS', name: 'Tajikistani somoni' },
        { 'code': 'TMT', name: 'Turkmenistani manat' },
        { 'code': 'TND', name: 'Tunisian dinar' },
        { 'code': 'TOP', name: 'Tongan paʻanga' },
        { 'code': 'TRY', name: 'Turkish lira' },
        { 'code': 'TTD', name: 'Trinidad and Tobago dollar' },
        { 'code': 'TWD', name: 'New Taiwan dollar' },
        { 'code': 'TZS', name: 'Tanzanian shilling' },
        { 'code': 'UAH', name: 'Ukrainian hryvnia' },
        { 'code': 'UGX', name: 'Ugandan shilling' },
        { 'code': 'USD', name: 'United States dollar' },
        { 'code': 'UYU', name: 'Uruguayan peso' },
        { 'code': 'UZS', name: 'Uzbekistan som' },
        { 'code': 'VEF', name: 'Venezuelan bolívar' },
        { 'code': 'VND', name: 'Vietnamese dong' },
        { 'code': 'VUV', name: 'Vanuatu vatu' },
        { 'code': 'WST', name: 'Samoan tala' },
        { 'code': 'XAF', name: 'CFA franc BEAC' },
        { 'code': 'XCD', name: 'East Caribbean dollar' },
        { 'code': 'XDR', name: 'Special drawing rights' },
        { 'code': 'XOF', name: 'CFA franc BCEAO' },
        { 'code': 'XPF', name: 'CFP franc (franc Pacifique)' },
        { 'code': 'XSU', name: 'SUCRE' },
        { 'code': 'XUA', name: 'ADB Unit of Account' },
        { 'code': 'YER', name: 'Yemeni rial' },
        { 'code': 'ZAR', name: 'South African rand' },
        { 'code': 'ZMW', name: 'Zambian kwacha' },

    ];

    var currencyRate = null;
    var reloadExhcangeRate = function ($http, localStorageService) {

        $http.get("/admin/commerce/api/exchangerate")
            .success(function (data) {
                currencyRate = data;
                localStorageService.set("currencyRate", data);
            });
    };
    var ensureRateAvailable = function ($http, localStorageService) {

        if (currencyRate == null) {

            currencyRate = localStorageService.get("currencyRate");

            if (currencyRate == null) {
                reloadExhcangeRate($http, localStorageService);
            } else {

                var now = (new Date()).getTime();
                var downloaded = currencyRate.timestamp * 1000;

                if (now - downloaded > 1000 * 60) {
                    reloadExhcangeRate($http, localStorageService);
                }
            }

        }

    };

    //#endregion

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

                if (amount != null && typeof (amount) == "number") {

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

            cartSystem.checkout = function (callback) {

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
                        scope.modalsource = "/NancyBlack/Modules/commercesystem/templates/ncg-cartmodal.html";
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
            templateUrl: '/NancyBlack/Modules/CommerceSystem/templates/ncg-cartbutton.html',
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
                    toSave[0] = cleanupAddress($scope.shoppingcart.cart.ShipTo);
                    toSave[1] = cleanupAddress($scope.shoppingcart.cart.BillTo);

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

                // set email into customer info
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

                if ($scope.shoppingcart.cart.ShippingDetails == null) {
                    $scope.shoppingcart.cart.ShippingDetails = {
                        method: 'parcel'
                    };
                }

                if ($scope.shoppingcart.cart.ShippingDetails.method == 'pickup') {

                    // nothing to do here
                    return true;
                }

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


            cartSystem.cart.BillTo = JSON.parse(JSON.stringify(cartSystem.cart.ShipTo));
        };

        $me.moneytransfer = function (datacontext) {

            $me.savecart(datacontext, function (item) {

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

    ncg.controller("TreePayBySaleOrderController", function ($scope, $http, $timeout) {
        
        var $me = this;

        $me.selectedAmout = null;
        $me.remainingAmount = 0;
        $me.saleOrder = {};
        $me.orderNo = "";
        $me.confirmedSelectedAmout = 0;

        var soPaymentLogs = {}

        $me.paymentMethods = [
            { code: "PACA", title: "Full Payment (ชำระยอดเต็ม)" },
            { code: "PAIN", title: "Installment (ผ่อนชำระ)" }
            //,            { code: "PABK", title: "Internet Banking" }
        ]

        function FormatNumberLength(num, length) {
            var r = "" + num;
            while (r.length < length) {
                r = "0" + r;
            }
            return r;
        }

        var setOrderNO = function () {
            if ($me.selectedAmout == $me.saleOrder.TotalAmount && soPaymentLogs.length == 0) {
                $me.orderNo = $me.saleOrder.SaleOrderIdentifier;
            }
            else {
                $me.orderNo = $me.saleOrder.SaleOrderIdentifier + "-" + FormatNumberLength(soPaymentLogs.length, 2);
            }
        }

        $me.init = function (saleorder, paymentLogs) {
            soPaymentLogs = paymentLogs;
            $me.saleOrder = saleorder;

            // set remaining payment
            $me.remainingAmount = $me.saleOrder.TotalAmount;
            $me.selectedAmout = $me.remainingAmount;
            if (paymentLogs != null && paymentLogs.length > 0) {
                $me.remainingAmount = $me.saleOrder.TotalAmount;
                for (var i = 0; i < paymentLogs.length; i++) {
                    if (paymentLogs[i].IsPaymentSuccess) {
                        $me.remainingAmount = $me.remainingAmount - paymentLogs[i].Amount;
                    }
                }
            }

            // set default method
            // CreditCart is a typo and we know it
            if ($me.saleOrder.IsPayWithCreditCart == 1) {
                $me.paymentMethod =  $me.paymentMethods[0];
            }
            else {
                $me.paymentMethods.splice(0, 2);
                $me.paymentMethod = $me.paymentMethods[0];
            }
        }

        $me.pay = function () {

            if ($me.paymentType == "AllRemaining") {
                $me.selectedAmout = $me.remainingAmount;
            }
            else if ($me.selectedAmout > $me.remainingAmount) {
                alert("ขอโทษค่ะ จำนวนเงินเกินยอดที่ต้องชำระค่ะ");
                return;
            }

            try {
                fbq('track', 'InitiateCheckout');
            } catch (e) {
            }

            // in treepay 230.50 need to convert to 23050
            $me.confirmedSelectedAmout = parseInt($me.selectedAmout * 100, 10);

            $("#working").addClass("show");
            setOrderNO();

            $http.post("/treepay/hashdata", { orderNo: $me.orderNo, soIdentifier: $me.saleOrder.SaleOrderIdentifier, trade_mony: $me.confirmedSelectedAmout, pay_type: $me.paymentMethod.code })
                .success(function (data) {

                    var hashData = data;

                    //get treepay settings
                    $http.post("/__commerce/treepay/settings")
                        .success(function (data) {

                            swal({
                                title: "Pending...",
                                text: "Navigating to TreePay",
                                timer: 2000,
                                showConfirmButton: false
                            });

                            setTimeout(function () {
                                $scope.$apply(function () {
                                    $me.settings = data;
                                    $me.settings.hash_data = hashData;
                                });

                                // need to include https://pay.treepay.co.th/js/plugin.tp script tag in using page
                                // Call TreePay payment window
                                TP_Pay_Execute(document.treepay_form);
                            }, 2000);

                        });
                });

        };

    });

    ncg.controller("PaysbuyBySaleOrderController", function ($scope, $http, $timeout) {


        var $me = this;

        $scope.splitValue = null;

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

            getPaysbuySettings();

        };

        $me.splitPay = function (amount) {

            $scope.splitValue = amount;
            if ($scope.splitValue > $scope.paymentDetail.PaymentRemaining) {
                alert("ขอโทษค่ะ จำนวนเงินเกินยอดที่ต้องชำระค่ะ");
                return;
            }

            $("#working").addClass("show");

            var submitForm = function () {

                $("#paysbuy_split_form").attr("action",
                    "https://www.paysbuy.com/paynow.aspx?lang=" + $scope.paysbuy.lang);

                $timeout(function () {

                    $("#paysbuy_split_form").submit();
                }, 1000);

            };

            var getPaysbuySettings = function () {

                $http.get("/__commerce/paysbuy/settings")
                    .success(function (data) {

                        $scope.paysbuy = data;
                        submitForm();
                    })
            };

            getPaysbuySettings();

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
            if (window.location.pathname.indexOf("/__commerce/saleorder") == 0 &&
                window.location.pathname.indexOf("/notifytransfer") > 0) {


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
            templateUrl: '/NancyBlack/Modules/CommerceSystem/templates/ncg-sotable.html',
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
                return $scope.so.TotalAmount;
            };

            $scope.getPriceBeforeVat = function (Price) {
                return Price * 0.9345794392523364485981308411215;
            };

            $scope.getPriceBeforeVatWithGap = function (Price) {
                var result = $scope.getPriceBeforeVat(Price);
                var total = $scope.getTotal();
                var beforeVat = $scope.getPriceBeforeVat(total);
                var vatPirce = total - beforeVat;
                var margin = total - (beforeVat + vatPirce);

                return result + margin;
            };

            $scope.getVat = function () {
                var total = $scope.getTotal();
                return total - $scope.getPriceBeforeVat(total);
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
            scope.data = [];

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

                var criteria = "/tables/" + scope.table + "/summarize?period=" + period +
                                                          "&fn=" + scope.fn +
                                                          "&select=" + scope.select +
                                                          "&time=__createdAt";

                if (attrs.filter != "") {

                    criteria += "&$filter=" + attrs.filter;
                }

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

                        arrKey.push(String.format("{0:dd/MM}", date));
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
            templateUrl: '/NancyBlack/Modules/CommerceSystem/templates/ncg-chart.html',
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

    // directive which injects currency list into the scope
    ncg.directive('ncgCurrencylist', function ($http) {

        function link($scope) {

            $scope.currencyList = currencyList;
        };

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });

    // directive which injects currency functions into scope
    ncg.directive('ncgMulticurrency', function ($http) {

        function link($scope) {

            $scope.currencyList = currencyList;

            if ($scope.multicurrency == null) {
                $scope.multicurrency = {};
                $scope.multicurrency.home = 'THB';
            }

            var $me = $scope.multicurrency;

            $me.convertHome = function (input, wantCurrency) {

                return $me.convert(input, $me.home, wantCurrency);
            };

            $me.fairRatio = function (homeValue, wantValue, wantCurrency)
            {
                var fairValue = $me.convert(homeValue, $me.home, wantCurrency);
                return wantValue / fairValue
            }

            $me.convert = function (input, inputCurrency, wantCurrency) {

                var want = currencyRate.rates[wantCurrency];
                var home = currencyRate.rates[inputCurrency];

                var usd = input / home;
                var result = usd * want;

                return result;
            };

            $me.getRate = function (wantCurrency) {

                return $me.convert(1, $me.home, wantCurrency);
            };
        };

        return {
            restrict: 'A',
            link: link,
            scope: false,
        };
    });

    ncg.filter('xchg', function ($http, localStorageService) {

        ensureRateAvailable($http, localStorageService);

        return function (input, wantCurrency) {

            if (wantCurrency == undefined) {
                wantCurrency = window.currency;
            }

            if (wantCurrency == undefined || wantCurrency == null || wantCurrency == "") {
                return input;
            }

            var want = currencyRate.rates[wantCurrency];
            var home = currencyRate.rates['THB'];

            var usd = input / home;
            var result = usd * want;

            // add 3% buffer
            result = result * 1.03;

            return result;
        };
    });
    
    ncg.filter('xchgrate', function ($http, localStorageService) {

        ensureRateAvailable($http, localStorageService);
        return function (input, want) {

            var home = currencyRate.rates['THB'];
            var want = currencyRate.rates[want];

            var perUsd = 1 / home;
            var result = want * perUsd;
            return result;
        };
    });

    ncg.filter('xchgdate', function ($http, localStorageService) {

        ensureRateAvailable($http, localStorageService);

        return function (input) {

            return new Date( currencyRate.timestamp * 1000 );
        };
    });

})();