/*
 * String.translate
 * 
 * Localization makes easy
 */

(function (window) {
    'use strict';

    var translate = window.angular.module("translate", []);

    var englishTranslations = new Array();

    String.prototype.translate = function (english) {
    
        var me = this.toString();

        if (english == null) {

            // use previous translation
            if (englishTranslations[me] != null) {
                return englishTranslations[me];
            }

            // no previous translation, use current one
            return me;
        }

        if (typeof english == "string") {

            if (window.language == "") {
                return me;
            }

            // save the translation
            englishTranslations[me] = english.toString();

            return englishTranslations[me];
        }

    };

})(window);