(function () {

    // Cookie (One that we use does not work with removing cookie)
    !function (a) { var b = !1; if ("function" == typeof define && define.amd && (define(a), b = !0), "object" == typeof exports && (module.exports = a(), b = !0), !b) { var c = window.Cookies, d = window.Cookies = a(); d.noConflict = function () { return window.Cookies = c, d } } }(function () { function a() { for (var a = 0, b = {}; a < arguments.length; a++) { var c = arguments[a]; for (var d in c) b[d] = c[d] } return b } function b(c) { function d(b, e, f) { var g; if ("undefined" != typeof document) { if (arguments.length > 1) { if (f = a({ path: "/" }, d.defaults, f), "number" == typeof f.expires) { var h = new Date; h.setMilliseconds(h.getMilliseconds() + 864e5 * f.expires), f.expires = h } f.expires = f.expires ? f.expires.toUTCString() : ""; try { g = JSON.stringify(e), /^[\{\[]/.test(g) && (e = g) } catch (a) { } e = c.write ? c.write(e, b) : encodeURIComponent(String(e)).replace(/%(23|24|26|2B|3A|3C|3E|3D|2F|3F|40|5B|5D|5E|60|7B|7D|7C)/g, decodeURIComponent), b = encodeURIComponent(String(b)), b = b.replace(/%(23|24|26|2B|5E|60|7C)/g, decodeURIComponent), b = b.replace(/[\(\)]/g, escape); var i = ""; for (var j in f) f[j] && (i += "; " + j, f[j] !== !0 && (i += "=" + f[j])); return document.cookie = b + "=" + e + i } b || (g = {}); for (var k = document.cookie ? document.cookie.split("; ") : [], l = /(%[0-9A-Z]{2})+/g, m = 0; m < k.length; m++) { var n = k[m].split("="), o = n.slice(1).join("="); '"' === o.charAt(0) && (o = o.slice(1, -1)); try { var p = n[0].replace(l, decodeURIComponent); if (o = c.read ? c.read(o, p) : c(o, p) || o.replace(l, decodeURIComponent), this.json) try { o = JSON.parse(o) } catch (a) { } if (b === p) { g = o; break } b || (g[p] = o) } catch (a) { } } return g } } return d.set = d, d.get = function (a) { return d.call(d, a) }, d.getJSON = function () { return d.apply({ json: !0 }, [].slice.call(arguments)) }, d.defaults = {}, d.remove = function (b, c) { d(b, "", a(c, { expires: -1 })) }, d.withConverter = b, d } return b(function () { }) });

    var ncb = angular.module("ncb-affiliate", []);
    window.mobilecheck = function () {
        var check = false;
        (function (a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) check = true; })(navigator.userAgent || navigator.vendor || window.opera);
        return check;
    };


    ncb.directive('ncbSubscribe', function ($http, $datacontext, $document) {

        function link($scope, element, attrs) {

            $(document).on("click", ".sweet-overlay", function () {

                swal.close();

            });

            var apply = function (email) {

                $http.post("/__affiliate/apply", { code: 'auto', email: email }).success(function (data) {

                    var code = data.AffiliateCode;
                    var url = "http://www.level51pc.com/?subscribe=1&source=" + code;

                    swal({
                        type: 'success',
                        title: 'หวัดดี SQUAD51 คนที่ ' + data.Id,
                        text:
                        'ชวนเพื่อนมาเป็น SQUAD51 เพิ่ม' +
                        '<p style="margin-top: 10px;">' +
                        '<a target="_blank" href="https://www.facebook.com/sharer/sharer.php?u=' + encodeURIComponent(url) + '" alt="share">' +
                        '<img src="http://www.level51pc.com/Site/images/affiliate/share-fb.png" alt="Share On Facebook" />' +
                        '</a> ' +
                        '<a ' +
                        'href="https://twitter.com/intent/tweet?text=' + encodeURIComponent("Check Out LEVEL51 Custom Laptop! " + url) + '"' +
                        'data-size="large">' +
                        '<img src="http://www.level51pc.com/Site/images/affiliate/share-tw.png" alt="Share On Twitter" />' +
                        '</a>' +
                        '</p>' +
                        '<p style="margin-top: 10px; font-weight: bold;">ครบตามจำนวนที่กำหนด ก็รับของรางวัลตามนี้เลย</p>' +
                        '<img src="/Site/images/promo/squad-2.png" />',
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

            $scope.affiliateProcessSubscribe = function () {

                $scope.membership.loginfacebook(function () {

                    var email = "";
                    if ($scope.membership.currentUser.Profile.email == "" || $scope.membership.currentUser.Profile.email == null) {

                        swal({
                            type: 'input',
                            title: 'เอ๊อะ!',
                            text: 'ในเฟสบุ้คคุณ' + $scope.membership.currentUser.Profile.first_name + ' ไม่ได้ใส่อีเมลล์ไว้น่ะ รบกวนขออีเมลล์ด้วยนะ',
                            html: true,
                            showCancelButton: true,
                            showLoaderOnConfirm: true,
                            closeOnConfirm: false,
                            confirmButtonText: "บันทึก",
                            cancelButtonText: "ไม่เอาละ",
                            animation: "slide-from-top"
                        }, function (text) {

                            if (text === false)
                                return false;

                            apply(text);

                        });

                    } else {

                        apply();
                    }


                }, true, "subscribe");
            };

            $scope.affiliateSubscribe = function () {

                var name = Cookies.get("affiliatename");
                var title = 'อยากได้โค๊ดส่วนลดไหม?';
                if (name != null)
                {
                    title = name + ' ชวนให้คุณมาสมัครรับโค๊ดส่วนลดด้วยกัน';
                }

                swal({
                    title: title,
                    text:
                    '<img src="/Site/images/promo/squad-1.png" />',
                    html: true,
                    showCancelButton: false,
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

                    $scope.affiliateProcessSubscribe();

                });

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

            }

            
        }

        return {
            restrict: 'A',
            link: link,
            scope: false
        };
    });


})();