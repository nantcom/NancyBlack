(function () {
    
    // Cookie (One that we use does not work with removing cookie)
    !function (a) { var b = !1; if ("function" == typeof define && define.amd && (define(a), b = !0), "object" == typeof exports && (module.exports = a(), b = !0), !b) { var c = window.Cookies, d = window.Cookies = a(); d.noConflict = function () { return window.Cookies = c, d } } }(function () { function a() { for (var a = 0, b = {}; a < arguments.length; a++) { var c = arguments[a]; for (var d in c) b[d] = c[d] } return b } function b(c) { function d(b, e, f) { var g; if ("undefined" != typeof document) { if (arguments.length > 1) { if (f = a({ path: "/" }, d.defaults, f), "number" == typeof f.expires) { var h = new Date; h.setMilliseconds(h.getMilliseconds() + 864e5 * f.expires), f.expires = h } f.expires = f.expires ? f.expires.toUTCString() : ""; try { g = JSON.stringify(e), /^[\{\[]/.test(g) && (e = g) } catch (a) { } e = c.write ? c.write(e, b) : encodeURIComponent(String(e)).replace(/%(23|24|26|2B|3A|3C|3E|3D|2F|3F|40|5B|5D|5E|60|7B|7D|7C)/g, decodeURIComponent), b = encodeURIComponent(String(b)), b = b.replace(/%(23|24|26|2B|5E|60|7C)/g, decodeURIComponent), b = b.replace(/[\(\)]/g, escape); var i = ""; for (var j in f) f[j] && (i += "; " + j, f[j] !== !0 && (i += "=" + f[j])); return document.cookie = b + "=" + e + i } b || (g = {}); for (var k = document.cookie ? document.cookie.split("; ") : [], l = /(%[0-9A-Z]{2})+/g, m = 0; m < k.length; m++) { var n = k[m].split("="), o = n.slice(1).join("="); '"' === o.charAt(0) && (o = o.slice(1, -1)); try { var p = n[0].replace(l, decodeURIComponent); if (o = c.read ? c.read(o, p) : c(o, p) || o.replace(l, decodeURIComponent), this.json) try { o = JSON.parse(o) } catch (a) { } if (b === p) { g = o; break } b || (g[p] = o) } catch (a) { } } return g } } return d.set = d, d.get = function (a) { return d.call(d, a) }, d.getJSON = function () { return d.apply({ json: !0 }, [].slice.call(arguments)) }, d.defaults = {}, d.remove = function (b, c) { d(b, "", a(c, { expires: -1 })) }, d.withConverter = b, d } return b(function () { }) });

    var ncb = angular.module("ncb-affiliate", []);
    
    ncb.directive('ncbSubscribe', function ($http, $datacontext) {

        function link($scope, element, attrs) {

            var apply = function ( email ) {
                
                $http.post("/__affiliate/apply", { code: 'auto', email: email }).success(function (data) {

                    var code = data.AffiliateCode;
                    var url = "http://www.level51pc.com/?subscribe=1&source=" + code;

                    swal({
                        type: 'success',
                        title: 'เยี่ยมกู๊ด หวัดดี SQUAD51 คนที่ ' + data.Id,
                        text: 'แล้วก็ถ้าอยากได้โค๊ดส่วนลด <b>2,000 บาท</b> เลยไม่ต้องรอโปรโมชั่น แค่ชวนเพื่อน 5 คนมาลงทะเบียนรับข่าวโปรโมชั่นกะเรา</p>' +
                        '<p>และถ้าเป็นคนเพื่อนเยอะ มีคนมาลงทะเบียนถึง 10 คนขึ้นไปรับกระเป๋าเป้ SWISSGEAR ลาย LEVEL51 ไปใส่คอมพ์(ยี่ห้ออื่นก็ได้) <b>มูลค่า 2,790 บาท</b> ส่งฟรีถึงบ้านเลยจ้า</p>' +
                        '<p>ติดตามจำนวนคนคลิก และ' +
                        '<a href="http://www.level51pc.com/squad51/dashboard" target="_blank">ก็อปปี้ลิงค์เอาไว้ชวนเพื่อนได้จากหน้านี้เลยนะ</a></p>',
                        html: true,
                        closeOnConfirm: true,
                        confirmButtonText: "โอเคร!!",
                        animation: "slide-from-top"
                    });

                    Cookies.set('subscribecheck', 'already', { expires: 60 });

                }).error(function (data) {

                    $scope.isBusy = false

                    swal("เกิดข้อผิดพลาด".translate(), "กรุณาลองใหม่อีกครั้งค่ะ".translate("Please try sending the code again."), "error");

                });


            };

            $scope.$on('ncb-membership.login', function (a, e) {
                
                fbq('track', 'CompleteRegistration');
                ga('send', 'event', 'Subscribe');

            });
                       
            swal({
                type: 'info',
                title: 'อยากได้โค๊ดส่วนลดมั๊ย?',
                text: 'สวัสดีจ้า ถ้าสนใจเครื่องของเรา ขอชวนมาเป็น <b>SQUAD51</b> และลงทะเบียนรับข่าวส่วนลดของเราได้ที่นี่นะ ถ้าเรามีโปรโมชั่นใหม่ เราจะแจ้งคุณก่อนทางอีเมลล์เลย',
                html: true,
                showCancelButton: true,
                showLoaderOnConfirm: true,
                closeOnConfirm: false,
                confirmButtonText: "ลงทะเบียนด้วยเฟสบุ้ค",
                cancelButtonText: "ไม่เอา เราบ้านรวย",
                animation: "slide-from-top"
            }, function (isConfirm) {

                if (isConfirm == false) {

                    Cookies.set('subscribecheck', 'nope', { expires: 7 });
                    return;
                }

                $scope.membership.loginfacebook(function () {
                    
                    swal.close();

                    var email = "";
                    if ($scope.membership.currentUser.Profile.email == "" || $scope.membership.currentUser.Profile.email == null) {

                        swal({
                            type: 'input',
                            title: 'เอ๊อะ!',
                            text: 'ในเฟสบุ้คคุณ' + membership.currentUser.Profile.first_name + ' ไม่ได้ใส่อีเมลล์ไว้น่ะ รบกวนขออีเมลล์ด้วยนะ',
                            html: true,
                            showCancelButton: true,
                            showLoaderOnConfirm: true,
                            closeOnConfirm: false,
                            confirmButtonText: "บันทึก",
                            cancelButtonText: "ไม่เอาละ",
                            animation: "slide-from-top"
                        }, function (text) {

                            if (inputValue === false)
                                return false;

                            apply(email);

                        });

                    } else {

                        apply();
                    }


                });

            });
        }

        return {
            restrict: 'A',
            link: link,
            scope: false
        };
    });


})();