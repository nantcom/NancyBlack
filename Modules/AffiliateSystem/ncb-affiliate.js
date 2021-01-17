(function () {

    // Cookie (One that we use does not work with removing cookie)
    !function (a) { var b = !1; if ("function" == typeof define && define.amd && (define(a), b = !0), "object" == typeof exports && (module.exports = a(), b = !0), !b) { var c = window.Cookies, d = window.Cookies = a(); d.noConflict = function () { return window.Cookies = c, d } } }(function () { function a() { for (var a = 0, b = {}; a < arguments.length; a++) { var c = arguments[a]; for (var d in c) b[d] = c[d] } return b } function b(c) { function d(b, e, f) { var g; if ("undefined" != typeof document) { if (arguments.length > 1) { if (f = a({ path: "/" }, d.defaults, f), "number" == typeof f.expires) { var h = new Date; h.setMilliseconds(h.getMilliseconds() + 864e5 * f.expires), f.expires = h } f.expires = f.expires ? f.expires.toUTCString() : ""; try { g = JSON.stringify(e), /^[\{\[]/.test(g) && (e = g) } catch (a) { } e = c.write ? c.write(e, b) : encodeURIComponent(String(e)).replace(/%(23|24|26|2B|3A|3C|3E|3D|2F|3F|40|5B|5D|5E|60|7B|7D|7C)/g, decodeURIComponent), b = encodeURIComponent(String(b)), b = b.replace(/%(23|24|26|2B|5E|60|7C)/g, decodeURIComponent), b = b.replace(/[\(\)]/g, escape); var i = ""; for (var j in f) f[j] && (i += "; " + j, f[j] !== !0 && (i += "=" + f[j])); return document.cookie = b + "=" + e + i } b || (g = {}); for (var k = document.cookie ? document.cookie.split("; ") : [], l = /(%[0-9A-Z]{2})+/g, m = 0; m < k.length; m++) { var n = k[m].split("="), o = n.slice(1).join("="); '"' === o.charAt(0) && (o = o.slice(1, -1)); try { var p = n[0].replace(l, decodeURIComponent); if (o = c.read ? c.read(o, p) : c(o, p) || o.replace(l, decodeURIComponent), this.json) try { o = JSON.parse(o) } catch (a) { } if (b === p) { g = o; break } b || (g[p] = o) } catch (a) { } } return g } } return d.set = d, d.get = function (a) { return d.call(d, a) }, d.getJSON = function () { return d.apply({ json: !0 }, [].slice.call(arguments)) }, d.defaults = {}, d.remove = function (b, c) { d(b, "", a(c, { expires: -1 })) }, d.withConverter = b, d } return b(function () { }) });

    var ncb = angular.module("ncb-affiliate", []);
    window.mobilecheck = function () {
        var check = false;
        (function (a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) check = true; })(navigator.userAgent || navigator.vendor || window.opera);
        return check;
    };


    ncb.directive('ncbSubscribe', function ($http, $datacontext, $document, $window) {

        function link($scope, element, attrs) {

            $(document).on("click", ".sweet-overlay", function () {

                swal.close();

            });

            var apply = function (email) {

                var sharedCoupon = window.localStorage.getItem("sharedcoupon");
                if (sharedCoupon != null) {
                    sharedCoupon = JSON.parse(sharedCoupon);
                }

                var sharedReward = window.localStorage.getItem("sharedreward");
                if (sharedReward != null) {
                    sharedReward = JSON.parse(sharedReward);
                }

                var source = Cookies.get("source");
                var name = Cookies.get("affiliatename");

                $http.post("/__affiliate/apply", { code: 'auto', email: email, sharedCoupon: sharedCoupon, sharedReward: sharedReward, source: source }).success(function (data) {

                    var code = data.AffiliateCode;
                    var url = "http://www.level51pc.com/?subscribe=1&source=" + code;

                    window.setTimeout(function () {
                        FB.XFBML.parse(document.getElementById('subscribebutton'));
                    }, 1000);

                    var title = "ยินดีต้อนรับ SQUAD51 ท่านที่ " + data.Id;
                    var preText = "";

                    if (sharedCoupon != null) {

                        if (data.CouponSaved == true) {

                            preText =
                                '<div><img src="/__c' + sharedCoupon.CouponId + '.jpg" style="margin-bottom: 5px"/></div>' +
                                '<div style="margin-bottom: 5px" >คูปองได้รับการบันทึกในโปรไฟล์ของคุณแล้ว</div>';

                            nonAdminAction(function () {
                                fbq('track', 'AddToWishlist', {
                                    value: 30000,
                                    currency: 'THB'
                                });
                            });
                        }
                        else {

                            var messages = [];
                            messages["SAME_TREE"] = "ระบบพบว่าเจ้าของคูปองนี้ ผู้ที่กดรับคูปองครั้งแรกคือคุณ:" + data.Owner + " ซึ่งถูกแนะนำโดยคุณ";
                            messages["SAME_USER"] = "ระบบพบว่าเจ้าของคูปองนี้ ผู้ที่กดรับคูปองครั้งแรกคือคุณ:" + data.Owner + " ซึ่งเป็นคุณเอง";

                            preText = '<div style="margin-bottom: 5px; color: red" >คุณไม่สามารถบันทึกคูปองนี้ได้ เนื่องจาก' + messages[data.Message] + '</div>';

                        }

                    }

                    if (sharedReward != null && data.RewardClaimed == true) {
                        preText =
                            '<div><img src="/__lv51/couponsimg/' + sharedReward.Id + '.jpg" style="margin-bottom: 5px"/></div>' +
                            '<div style="margin-bottom: 5px">คูปองได้รับการบันทึกในโปรไฟล์ของคุณแล้ว</div>';

                        nonAdminAction(function () {
                            fbq('track', 'AddToWishlist', {
                                value: 30000,
                                currency: 'THB'
                            });
                        });

                    }


                    Cookies.set("coupon", "");
                    window.localStorage.removeItem("sharedcoupon");

                    Cookies.set("reward", "");
                    window.localStorage.removeItem("sharedreward");

                    swal({
                        type: 'success',
                        title: title,
                        text:
                            preText
                        ,
                        html: true,
                        closeOnConfirm: true,
                        showConfirmButton: true,
                        confirmButtonText: "เกี่ยวกับ SQUAD51"
                    },
                        function(answer) {

                            if (answer == true) {
                                window.location.href = window.location.origin + "/squad51/dashboard";
                            }
                        }
                    );

                    Cookies.set('subscribecheck', 'already', { expires: 60 });

                }).error(function (data) {

                    $scope.isBusy = false

                    swal("เกิดข้อผิดพลาด".translate(), "กรุณาลองใหม่อีกครั้ง".translate("Please try again."), "error");

                });


            };

            $scope.affiliate = {};
            $scope.affiliate.claimreward = function (rewardId) {

                if (rewardId == "" || rewardId == null) {
                    return;
                }

                Cookies.set("reward", rewardId);
                $http.get("/__affiliate/getreward").success(function (data) {

                    if (data.IsValid == false) {

                        Cookies.set("reward", "");

                        swal({
                            html: true,
                            type: 'warning',
                            title: 'ขออภัยเป็นอย่างยิ่ง',
                            showConfirmButton: true,
                            showCancelButton: false,
                            confirmButtonText: "เรียนรู้เพิ่มเติม",
                            text:
                                'คูปองส่วนลดนี้ไม่สามารถใช้งานได้แล้ว<br/>' +
                                'แต่เรายังมีโปรโมชั่นต่างๆ อีกมากมายสำหรับสมาชิกเว็บไซต์ของเรา'
                        }, function (answer) {

                            if (answer == true) {
                                window.location.href = window.location.origin + "/squad51";
                            }

                        });
                        return;
                    }

                    if (data.MinimumPurchaseAmount > 0) {

                        nonAdminAction(function () {
                            fbq('track', 'Lead', {
                                value: data.MinimumPurchaseAmount,
                                currency: 'THB'
                            });
                        });
                    }
                    else
                    {
                        nonAdminAction(function () {
                            fbq('track', 'Lead', {
                                value: 30000,
                                currency: 'THB'
                            });
                        });
                    }


                    swal({
                        type: '',
                        title: 'ยินดีด้วย คุณได้รับคูปองส่วนลด',
                        text:
                            '<img src="/__lv51/couponsimg/' + data.Id + '.jpg" style="margin-bottom: 5px"/><br/>'
                        ,
                        html: true,
                        closeOnConfirm: true,
                        showConfirmButton: true,
                        showCancelButton: true,
                        confirmButtonText: "บันทึกไว้ในโปรไฟล์",
                        cancelButtonText: "ไม่บันทึก"

                    }, function (answer) {


                        if (answer == true) {

                            window.localStorage.setItem("sharedreward", JSON.stringify(data));
                            $scope.affiliateProcessSubscribe();

                        } else {

                            Cookies.set("reward", "");
                            window.localStorage.removeItem("sharedreward");
                        }

                    });

                });

            };

            $scope.affiliateProcessSubscribe = function () {

                if (location.hostname == "localhost") {

                    window.setTimeout(apply, 200);

                    return;
                }

                $scope.membership.loginfacebook(function () {

                    // no longer require email - we will be as frictionless as possible
                    apply();


                }, true, "subscribe");
            };

            $scope.affiliateSubscribe = function () {

                $scope.affiliateProcessSubscribe();

            };

            // this is redirection back from facebook
            if (window.location.hash.indexOf("state=subscribe") > 0) {

                var tryRegister = function () {

                    if (typeof (FB) == undefined) {

                        window.setTimeout(tryRegister, 2000);
                        return;
                    }

                    $scope.affiliateProcessSubscribe();
                };

                window.setTimeout(tryRegister, 2000);


            } else {

                if (window.location.search.indexOf( 'subscribe' ) >= 0) {

                    $scope.affiliateSubscribe();
                }

                if (Cookies.get("coupon") != "" && Cookies.get("coupon") != null) {

                    var couponId = Cookies.get("coupon");
                    $http.get("/__affiliate/getsharedcoupon").success(function (data) {

                        if (data.IsValid == false) {

                            var messages = [];
                            messages["USED"] = "คูปองส่วนลดนี้ได้ถูกใช้งานไปแล้ว";
                            messages["SAME_TREE"] = "คุณไม่สามารถรับคูปองส่วนลดนี้ได้";

                            window.localStorage.removeItem("sharedcoupon");
                            Cookies.set("coupon", "");

                            swal({
                                html: true,
                                type: 'warning',
                                title: 'ขออภัยเป็นอย่างยิ่ง',
                                showConfirmButton: true,
                                showCancelButton: false,
                                confirmButtonText: "เรียนรู้เพิ่มเติม",
                                text:
                                    messages[data.Message] + '<br/>' +
                                    'แต่เรายังมีโปรโมชั่นต่างๆ อีกมากมายสำหรับสมาชิกเว็บไซต์ของเรา'
                            }, function (answer) {

                                if (answer == true) {
                                    window.location.href = window.location.origin + "/squad51";
                                }

                            });
                            return;
                        }

                        swal({
                            type: '',
                            title: '',
                            text:
                                '<img src="/__c' + couponId + '.jpg" style="margin-bottom: 5px"/><br/>' +
                                '<b>' + data.AffiliateName + '</b> มอบคูปองนี้ให้คุณ'
                            ,
                            html: true,
                            closeOnConfirm: true,
                            showConfirmButton: true,
                            showCancelButton: true,
                            confirmButtonText: "บันทึกไว้ในโปรไฟล์",
                            cancelButtonText: "ไม่บันทึก",

                        }, function (answer) {


                            if (answer == true) {

                                window.localStorage.setItem("sharedcoupon", JSON.stringify(data));
                                $scope.affiliateProcessSubscribe();

                            } else {

                                window.localStorage.removeItem("sharedcoupon");
                                Cookies.set("coupon", "");
                            }

                        });

                    });

                    return;
                }

                if (Cookies.get("reward") != "" && Cookies.get("reward") != null) {

                    var rewardId = Cookies.get("reward");
                    $scope.affiliate.claimreward(rewardId);
                }
            }

            $scope.$watch("membership", function () {

                if ($scope.membership == null) {
                    return;
                }

                if ($scope.membership.currentUser.Id == 0) {

                    // currently anonymous user, if sign in will auto apply affiliate program
                    window.googleSigninCallback = function () {

                        apply();
                        window.googleSigninCallback = null;
                    };
                }

            });


        }

        return {
            restrict: 'A',
            link: link,
            priority: 99997,
            scope: false
        };
    });


})();