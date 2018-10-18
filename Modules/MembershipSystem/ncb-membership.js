(function () {

    var utils = {};

    //#region Utils

    // Querystring extraction function
    (function (n) { "use strict"; n.QueryStringParser = function (n) { var r = [], t, u, e, i, f; try { for (t = n.split("?")[1].split("#"), n = t[0], t[1] && (r["#"] = decodeURIComponent(t[1])), u = n.split("&"), e = u.length, i = 0; i < e; i++) f = u[i].split("="), r[f[0]] = decodeURIComponent(f[1]) } catch (o) { } return r }; var t = n.QueryStringParser(location.search); n.Querystring = function (n) { if (n === "#") { if (location.hash) return location.hash.substr(1) } else return t[n] } })(utils);

    //MD5
    (function (n) { "use strict"; function f(n, t) { var i = (n & 65535) + (t & 65535), r = (n >> 16) + (t >> 16) + (i >> 16); return r << 16 | i & 65535 } function p(n, t) { return n << t | n >>> 32 - t } function e(n, t, i, r, u, e) { return f(p(f(f(t, n), f(r, e)), u), i) } function t(n, t, i, r, u, f, o) { return e(t & i | ~t & r, n, t, u, f, o) } function i(n, t, i, r, u, f, o) { return e(t & r | i & ~r, n, t, u, f, o) } function r(n, t, i, r, u, f, o) { return e(t ^ i ^ r, n, t, u, f, o) } function u(n, t, i, r, u, f, o) { return e(i ^ (t | ~r), n, t, u, f, o) } function o(n, e) { n[e >> 5] |= 128 << e % 32; n[(e + 64 >>> 9 << 4) + 14] = e; for (var a, v, y, p, o = 1732584193, s = -271733879, h = -1732584194, c = 271733878, l = 0; l < n.length; l += 16) a = o, v = s, y = h, p = c, o = t(o, s, h, c, n[l], 7, -680876936), c = t(c, o, s, h, n[l + 1], 12, -389564586), h = t(h, c, o, s, n[l + 2], 17, 606105819), s = t(s, h, c, o, n[l + 3], 22, -1044525330), o = t(o, s, h, c, n[l + 4], 7, -176418897), c = t(c, o, s, h, n[l + 5], 12, 1200080426), h = t(h, c, o, s, n[l + 6], 17, -1473231341), s = t(s, h, c, o, n[l + 7], 22, -45705983), o = t(o, s, h, c, n[l + 8], 7, 1770035416), c = t(c, o, s, h, n[l + 9], 12, -1958414417), h = t(h, c, o, s, n[l + 10], 17, -42063), s = t(s, h, c, o, n[l + 11], 22, -1990404162), o = t(o, s, h, c, n[l + 12], 7, 1804603682), c = t(c, o, s, h, n[l + 13], 12, -40341101), h = t(h, c, o, s, n[l + 14], 17, -1502002290), s = t(s, h, c, o, n[l + 15], 22, 1236535329), o = i(o, s, h, c, n[l + 1], 5, -165796510), c = i(c, o, s, h, n[l + 6], 9, -1069501632), h = i(h, c, o, s, n[l + 11], 14, 643717713), s = i(s, h, c, o, n[l], 20, -373897302), o = i(o, s, h, c, n[l + 5], 5, -701558691), c = i(c, o, s, h, n[l + 10], 9, 38016083), h = i(h, c, o, s, n[l + 15], 14, -660478335), s = i(s, h, c, o, n[l + 4], 20, -405537848), o = i(o, s, h, c, n[l + 9], 5, 568446438), c = i(c, o, s, h, n[l + 14], 9, -1019803690), h = i(h, c, o, s, n[l + 3], 14, -187363961), s = i(s, h, c, o, n[l + 8], 20, 1163531501), o = i(o, s, h, c, n[l + 13], 5, -1444681467), c = i(c, o, s, h, n[l + 2], 9, -51403784), h = i(h, c, o, s, n[l + 7], 14, 1735328473), s = i(s, h, c, o, n[l + 12], 20, -1926607734), o = r(o, s, h, c, n[l + 5], 4, -378558), c = r(c, o, s, h, n[l + 8], 11, -2022574463), h = r(h, c, o, s, n[l + 11], 16, 1839030562), s = r(s, h, c, o, n[l + 14], 23, -35309556), o = r(o, s, h, c, n[l + 1], 4, -1530992060), c = r(c, o, s, h, n[l + 4], 11, 1272893353), h = r(h, c, o, s, n[l + 7], 16, -155497632), s = r(s, h, c, o, n[l + 10], 23, -1094730640), o = r(o, s, h, c, n[l + 13], 4, 681279174), c = r(c, o, s, h, n[l], 11, -358537222), h = r(h, c, o, s, n[l + 3], 16, -722521979), s = r(s, h, c, o, n[l + 6], 23, 76029189), o = r(o, s, h, c, n[l + 9], 4, -640364487), c = r(c, o, s, h, n[l + 12], 11, -421815835), h = r(h, c, o, s, n[l + 15], 16, 530742520), s = r(s, h, c, o, n[l + 2], 23, -995338651), o = u(o, s, h, c, n[l], 6, -198630844), c = u(c, o, s, h, n[l + 7], 10, 1126891415), h = u(h, c, o, s, n[l + 14], 15, -1416354905), s = u(s, h, c, o, n[l + 5], 21, -57434055), o = u(o, s, h, c, n[l + 12], 6, 1700485571), c = u(c, o, s, h, n[l + 3], 10, -1894986606), h = u(h, c, o, s, n[l + 10], 15, -1051523), s = u(s, h, c, o, n[l + 1], 21, -2054922799), o = u(o, s, h, c, n[l + 8], 6, 1873313359), c = u(c, o, s, h, n[l + 15], 10, -30611744), h = u(h, c, o, s, n[l + 6], 15, -1560198380), s = u(s, h, c, o, n[l + 13], 21, 1309151649), o = u(o, s, h, c, n[l + 4], 6, -145523070), c = u(c, o, s, h, n[l + 11], 10, -1120210379), h = u(h, c, o, s, n[l + 2], 15, 718787259), s = u(s, h, c, o, n[l + 9], 21, -343485551), o = f(o, a), s = f(s, v), h = f(h, y), c = f(c, p); return [o, s, h, c] } function c(n) { for (var i = "", t = 0; t < n.length * 32; t += 8) i += String.fromCharCode(n[t >> 5] >>> t % 32 & 255); return i } function s(n) { var t, i = []; for (i[(n.length >> 2) - 1] = undefined, t = 0; t < i.length; t += 1) i[t] = 0; for (t = 0; t < n.length * 8; t += 8) i[t >> 5] |= (n.charCodeAt(t / 8) & 255) << t % 32; return i } function w(n) { return c(o(s(n), n.length * 8)) } function b(n, t) { var i, r = s(n), u = [], f = [], e; for (u[15] = f[15] = undefined, r.length > 16 && (r = o(r, n.length * 8)), i = 0; i < 16; i += 1) u[i] = r[i] ^ 909522486, f[i] = r[i] ^ 1549556828; return e = o(u.concat(s(t)), 512 + t.length * 8), c(o(f.concat(e), 640)) } function l(n) { for (var r = "0123456789abcdef", u = "", i, t = 0; t < n.length; t += 1) i = n.charCodeAt(t), u += r.charAt(i >>> 4 & 15) + r.charAt(i & 15); return u } function h(n) { return unescape(encodeURIComponent(n)) } function a(n) { return w(h(n)) } function k(n) { return l(a(n)) } function v(n, t) { return b(h(n), h(t)) } function d(n, t) { return l(v(n, t)) } function y(n, t, i) { return t ? i ? v(t, n) : d(t, n) : i ? a(n) : k(n) } typeof define == "function" && define.amd ? define(function () { return y }) : n.md5 = y })(utils);

    // Cookie (One that we use does not work with removing cookie)
    !function (a) { var b = !1; if ("function" == typeof define && define.amd && (define(a), b = !0), "object" == typeof exports && (module.exports = a(), b = !0), !b) { var c = window.Cookies, d = window.Cookies = a(); d.noConflict = function () { return window.Cookies = c, d } } }(function () { function a() { for (var a = 0, b = {}; a < arguments.length; a++) { var c = arguments[a]; for (var d in c) b[d] = c[d] } return b } function b(c) { function d(b, e, f) { var g; if ("undefined" != typeof document) { if (arguments.length > 1) { if (f = a({ path: "/" }, d.defaults, f), "number" == typeof f.expires) { var h = new Date; h.setMilliseconds(h.getMilliseconds() + 864e5 * f.expires), f.expires = h } f.expires = f.expires ? f.expires.toUTCString() : ""; try { g = JSON.stringify(e), /^[\{\[]/.test(g) && (e = g) } catch (a) { } e = c.write ? c.write(e, b) : encodeURIComponent(String(e)).replace(/%(23|24|26|2B|3A|3C|3E|3D|2F|3F|40|5B|5D|5E|60|7B|7D|7C)/g, decodeURIComponent), b = encodeURIComponent(String(b)), b = b.replace(/%(23|24|26|2B|5E|60|7C)/g, decodeURIComponent), b = b.replace(/[\(\)]/g, escape); var i = ""; for (var j in f) f[j] && (i += "; " + j, f[j] !== !0 && (i += "=" + f[j])); return document.cookie = b + "=" + e + i } b || (g = {}); for (var k = document.cookie ? document.cookie.split("; ") : [], l = /(%[0-9A-Z]{2})+/g, m = 0; m < k.length; m++) { var n = k[m].split("="), o = n.slice(1).join("="); '"' === o.charAt(0) && (o = o.slice(1, -1)); try { var p = n[0].replace(l, decodeURIComponent); if (o = c.read ? c.read(o, p) : c(o, p) || o.replace(l, decodeURIComponent), this.json) try { o = JSON.parse(o) } catch (a) { } if (b === p) { g = o; break } b || (g[p] = o) } catch (a) { } } return g } } return d.set = d, d.get = function (a) { return d.call(d, a) }, d.getJSON = function () { return d.apply({ json: !0 }, [].slice.call(arguments)) }, d.defaults = {}, d.remove = function (b, c) { d(b, "", a(c, { expires: -1 })) }, d.withConverter = b, d } return b(function () { }) });

    //#endregion

    var membership = angular.module('ncb-membership', []);

    membership.directive('ncbMembership', function ($http, $compile, $window) {

        function link($scope, element, attrs) {

            if ($scope.membership != null) {

                throw "Membership was already used in this scope";
            }


            $scope.membership = {};
            $scope.membership.alerts = [];

            var $me = $scope.membership;

            $me.currentUser = $window.currentUser;

            if ($me.currentUser == null) {

                $me.currentUser = {
                    Id: 0,
                    Guid: "00000000-0000-0000-0000-000000000000",
                    UserName: "Anonymous",
                };
            }

            $me.currentUser.IsFacebookUser = $me.currentUser.UserName.indexOf('fb_') == 0;
            if ($me.currentUser.IsFacebookUser) {
                $me.currentUser.Picture = "//graph.facebook.com/" + $me.currentUser.UserName.substr(3) + "/picture";
            }

            var loginTimeout = null;
            loginTimeout = window.setInterval(function () {

                var tried = $me.loginfacebook(null, null, null, true);

                if (tried == true) {
                    window.clearInterval(loginTimeout);
                }

            }, 1000);



            var processLogin = function (response, callback) {

                $me.currentUser = response;
                $window.currentUser = $me.currentUser;

                $me.currentUser.IsFacebookUser = $me.currentUser.UserName.indexOf('fb_') == 0;
                $me.currentUser.Picture = "//graph.facebook.com/" + $me.currentUser.UserName.substr(3) + "/picture";

                $scope.$broadcast("ncb-membership.login", {
                    sender: $scope,
                    user: response,
                });

                $scope.$emit("ncb-membership.login", {
                    sender: $scope,
                    user: response,
                });

                if (callback != null) {

                    callback();
                }
            };

            // Login user using given email, password
            $me.login = function (email, password, callback) {

                if (email == null || password == null) {

                    return;
                }

                $http.post('/__membership/login', { Email: email, Password: utils.md5(password) }).
                    success(function (data, status, headers, config) {

                        processLogin(data, callback);
                    }).
                    error(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'danger', msg: 'Invalid Credentials' });
                    });

            };

            $me.register = function (email, password, callback) {

                $http.post('/__membership/register', { Email: email, Password: utils.md5(password) }).
                    success(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'success', msg: 'Registration Completed.' });
                        processLogin(data, callback);

                    }).
                    error(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'danger', msg: 'This email was used.' });

                    });

            };

            $me.loginfacebook = function (callback, popupless, state, isAutologin) {

                if (typeof (FB) == "undefined") {
                    return false;
                }

                var processFacebookLogin = function () {

                    if (isAutologin && Cookies.get("_ncbfbuser") != null) {

                        // _ncbfbuser will be added at the same time as facebook login 
                        // so if this cookie is present then there is no need to login again
                        return;
                    }

                    FB.api('/me?fields=email,first_name,last_name,birthday', function (resultMe) {

                        $http.post('/__membership/loginfacebook', { me: resultMe }).
                            success(function (data, status, headers, config) {

                                processLogin(data, callback);

                                if ((new Date(data.__createdAt)).toDateString() == (new Date()).toDateString()) {

                                    fbq('track', 'CompleteRegistration');
                                    ga('send', 'event', 'Register via Facebook');

                                    if ($scope.socialprove != null) {

                                        $scope.socialprove.getprovewithdata('CompleteRegistration', JSON.stringify( resultMe ));
                                    }

                                }

                                if ($scope.socialprove != null) {

                                    $scope.socialprove.getprovewithdata('Login', JSON.stringify(resultMe));
                                }
                                ga('send', 'event', 'Login Facebook');


                            }).
                            error(function (data, status, headers, config) {

                                $me.alerts.push({ type: 'danger', msg: 'Cannot Login' });

                            });
                    });
                };

                FB.getLoginStatus(function (response) {

                    if (response.status == 'unknown' || response.status == 'not_authorized') {

                        if (isAutologin) {
                            return;
                        }

                        // will redirect and user has to do the action again
                        if (popupless == true) {

                            var uri = encodeURI(window.location.href);
                            window.location = encodeURI("https://www.facebook.com/dialog/oauth?" +
                                "client_id=" + window.facebookAppId +
                                "&redirect_uri=" + uri +
                                "&state=" + state +
                                "&response_type=token&scope=email,public_profile,user_birthday,user_friends");

                        } else {

                            FB.login(function (loginResponse) {

                                if (loginResponse.authResponse) {

                                    processFacebookLogin();

                                } else {
                                    console.log('User cancelled login or did not fully authorize.');
                                }

                            }, { scope: 'email,public_profile,user_birthday,user_friends' });
                        }
                    }

                    if (response.status == "connected") {

                        processFacebookLogin();
                    }
                });

                return true;
            };

            $me.reset = function (code, password, callback) {

                $http.post('/__membership/reset', { Code: code, Password: utils.md5(password) }).
                    success(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'success', msg: 'Password Reset Completed.' });
                        processLogin(data, callback);

                    }).
                    error(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'danger', msg: 'This email was used.' });

                    });

            };

            $me.resetrequest = function (email, callback) {

                $http.post('/__membership/resetrequest', { Email: email }).
                    success(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'success', msg: 'Password reset instructions were sent to your email.' });
                        processLogin(data, callback);

                    }).
                    error(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'danger', msg: 'Please try again.' });

                    });

            };

            $me.logout = function () {

                $me.currentUser = {
                    Id: 0,
                    Guid: '',
                    UserName: "Anonymous",
                };

                Cookies.remove('_ncfa', { path: '' }); // removed!
                Cookies.remove('_ncbfbuser', { path: '' }); // removed!
            };

            $me.isLoggedIn = function () {

                var result = $me.currentUser.Guid != null &&
                    $me.currentUser.Guid != "" &&
                    $me.currentUser.Guid != '00000000-0000-0000-0000-000000000000';

                return result;
            };

            $me.isAnonymous = function () {

                var login = $me.isLoggedIn();
                return login == false;
            };

            $me.updateProfile = function () {

                if ($me.currentUser.Id == 0) {
                    return;
                }

                // save user profile
                $http.post('/__membership/api/updateprofile', $scope.membership.currentUser.Profile)
                    .success(function (data) {

                        $me.alerts.push({ type: 'success', msg: 'Profile was saved.' });

                    })
                    .error(function (data, status, headers, config) {

                        $me.alerts.push({ type: 'danger', msg: 'Cannot save data.' });

                    });
            };
            
            $me.closeAlert = function (index) {

                $me.alerts.splice(index, 1);
            };

            $scope.$broadcast("ncb-membership.ready", {
                sender: $scope,
                user: $me,
            });

            $scope.$emit("ncb-membership.ready", {
                sender: $scope,
                user: $me,
            });

        }

        return {
            restrict: 'A',
            link: link,
            scope: false
        };
    });

    membership.controller('MemberShip-LoginController', function ($scope, $timeout) {

        var $me = this;

        $scope.login = {
            email: null,
            password: null,
            passwordConfirm: '',
        };
        //$scope.mode = 'login';
        $scope.mode = 'register';

        $me.afterLogin = function () {

            $("#loginDialog").modal('hide');

            // redirect if login from membership page
            if (window.location.pathname == "/__membership/login") {

                var target = utils.Querystring("returnUrl");
                var isAdmin = $scope.membership.currentUser.Claims.indexOf("admin") > -1;

                if (target == null || target == undefined) {
                    target = "/";
                }

                if (target == "/" && isAdmin) {
                    target = "/admin";
                }

                window.location.href = target;
            }
        };

        this.login = function () {

            if ($scope.login.email == null || $scope.login.password == null) {

                return;
            }

            $scope.membership.login($scope.login.email, $scope.login.password, $me.afterLogin);
        };

        this.loginfacebook = function () {


        };


        this.register = function () {

            if ($scope.login.email == null ||
                $scope.login.password == null ||
                ($scope.login.password != $scope.login.passwordConfirm)) {

                return;
            }

            $scope.membership.register($scope.login.email, $scope.login.password, function () {

                $("#loginDialog").modal('hide');

                // redirect if login from membership page
                if (window.location.pathname == "/__membership/login") {

                    var target = utils.Querystring("returnUrl");

                    if (target == null || target == undefined) {
                        target = "/";
                    }

                    window.location.href = target;
                }

                $scope.login.email = null;
                $scope.login.password = null;
                $scope.login.passwordConfirm = null;
            });
        };

        this.view = function () {
            $('#loginDialog').modal('show');
        };

        this.forget = function () {

            if ($scope.login.email == null) {

                return;
            }

            $scope.membership.resetrequest($scope.login.email, function () {

                $scope.login.email = null;
            });
        };

        $timeout(function () {

            // show login modal if from login page
            if (window.location.pathname.toLowerCase() == "/__membership/login") {

                $('#loginDialog').modal('show');
            }

        }, 1000);
    });

    membership.controller('MemberShip-ProfileController', function ($scope, $http) {

    });

    membership.controller('MemberShip-ResetPasswordController', function ($scope, $http, $timeout) {

        var $me = this;

        $scope.login = {
            password: null,
            passwordConfirm: '',
        };

        this.reset = function () {

            if (
                $scope.login.password == null ||
                ($scope.login.password != $scope.login.passwordConfirm)) {

                return;
            }

            $scope.membership.register($scope.login.email, $scope.login.password, function () {

                $("#loginDialog").modal('hide');

                // redirect if login from membership page
                if (window.location.pathname == "/__membership/login") {

                    var target = utils.Querystring("returnUrl");

                    if (target == null || target == undefined) {
                        target = "/";
                    }

                    window.location.href = target;
                }

                $scope.login.email = null;
                $scope.login.password = null;
                $scope.login.passwordConfirm = null;
            });
        };

        $timeout(function () {

            if (utils.Querystring("code") == null ||
                utils.Querystring("code") == undefined) {

                window.location.href = "/";
                return;
            }

            // show login modal if from login page
            if (window.location.pathname == "/__membership/resetpassword") {

                $('#forgetDialog').modal('show');
            }

        }, 1000);

    });

    membership.directive('ncbLoginbutton', ['$http', '$compile', function ($http, $compile) {

        function link(scope, element, attrs) {

            var myScope = scope;

            element.on("click", function () {

                if (scope.membership.currentUser.Id == 0) {

                    if (typeof (FB) != undefined) {

                        // initialize facebook login
                        FB.getLoginStatus(function (response) {

                            if (response.status == "connected") {

                                FB.api('/me', function (me) {

                                    // get information about user and register
                                    scope.membership.loginfacebook(me, null);
                                });
                            } else {

                                window.location.href = "/__membership/login";
                            }
                        });

                    } else {

                        window.location.href = "/__membership/login";
                    }


                } else {

                    // already logged in
                }

            });
        }

        return {
            restrict: 'A',
            link: link
        };
    }]);


    membership.directive('ncbLoginFacebookbutton', ['$http', '$compile', function ($http, $compile) {

        function link(scope, element, attrs) {

            var myScope = scope;

            element.on("click", function () {

                if (typeof (FB) != undefined) {

                    // initialize facebook login
                    FB.getLoginStatus(function (response) {

                        if (response.status == "connected") {

                            FB.api('/me', function (me) {

                                // get information about user and register
                                scope.membership.loginfacebook(me, null);
                            });
                        } else {

                        }
                    });

                }

            });
        }

        return {
            restrict: 'A',
            link: link
        };
    }]);

})();