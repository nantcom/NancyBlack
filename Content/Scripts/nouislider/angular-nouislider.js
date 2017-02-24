'use strict';
angular.module('nouislider', []).directive('slider', function () {
    return {
        restrict: 'A',
        scope: {
            start: '@',
            step: '@',
            end: '@',
            callback: '@',
            margin: '@',
            ngModel: '=',
            ngFrom: '=',
            ngTo: '=',
            ngChange: '=',
            postfix: '@',
            prefix: '@',
            decimals: '@'
        },
        link: function (scope, element, attrs) {
            var callback, fromParsed, parsedValue, slider, toParsed;
            slider = $(element);
            callback = scope.callback ? scope.callback : 'slide';

            scope.postfix = scope.postfix ? ' ' + scope.postfix : ' ';
            scope.prefix = scope.prefix ? scope.prefix + ' ' : ' ';

            if (scope.ngFrom != null && scope.ngTo != null) {
                fromParsed = null;
                toParsed = null;
                slider.noUiSlider({
                    start: [
                      scope.ngFrom || scope.start,
                      scope.ngTo || scope.end
                    ],
                    step: parseFloat(scope.step || 1),
                    connect: true,
                    margin: parseFloat(scope.margin || 0),
                    range: {
                        min: [parseFloat(scope.start)],
                        max: [parseFloat(scope.end)]
                    },
                    format: wNumb({
                        decimals: parseFloat(scope.decimals || 0),
                    })
                });
                slider.on(callback, function () {
                    var from, to, _ref;
                    _ref = slider.val(), from = _ref[0], to = _ref[1];
                    fromParsed = parseFloat(from);
                    toParsed = parseFloat(to);
                    return scope.$apply(function () {
                        scope.ngFrom = fromParsed;
                        return scope.ngTo = toParsed;
                    });
                });
                scope.$watch('ngFrom', function (newVal, oldVal) {
                    if (newVal !== fromParsed) {
                        return slider.val([
                          newVal,
                          null
                        ]);
                    }
                });

                return scope.$watch('ngTo', function (newVal, oldVal) {
                    if (newVal !== toParsed) {
                        return slider.val([
                          null,
                          newVal
                        ]);
                    }
                });
            } else {
                parsedValue = null;

                slider.noUiSlider({
                    start: [scope.ngModel || scope.start],
                    step: parseFloat(scope.step || 1),
                    range: {
                        min: [parseFloat(scope.start)],
                        max: [parseFloat(scope.end)]
                    },
                    format: wNumb({
                        decimals: parseFloat(scope.decimals || 0),
                    })
                });

                slider.on(callback, function () {
                    parsedValue = parseFloat(slider.val());
                    return scope.$apply(function () {

                        if (scope.ngChange != null && typeof(scope.ngChange) == "function") {
                            scope.ngChange(parsedValue);
                        }

                        return scope.ngModel = parsedValue;
                    });
                });

                slider.Link('lower').to('-inline-<div class="tool-tip"></div>', function (value) {
                    // The tooltip HTML is 'this', so additional
                    // markup can be inserted here.
                    $(this).html(
                        '<span>' + scope.prefix + value + scope.postfix + '</span>'
                    );
                });
                return scope.$watch('ngModel', function (newVal, oldVal) {
                    if (newVal !== parsedValue) {
                        return slider.val(newVal);
                    }

                });
            }

        }
    };
});
