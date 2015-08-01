(function () {

    // Resize Image to fit the specified width/height
    // result will be passed back with callback( dataUri )
    function ResizeImage(dataUri, maxWidth, maxHeight, contain, callback) {
        var incrementalResize = function (img, targetW) {
            // we have to generate new image which is incrementally smaller to get better quality scaling
            var scaleDown = 0.5;
            var source = document.createElement('canvas');
            var w = img.naturalWidth;
            var h = img.naturalHeight;

            source.width = w;
            source.height = h;

            var ctxSource = source.getContext('2d');
            ctxSource.drawImage(img, 0, 0, w, h);
            delete ctxSource;

            while (w * scaleDown > targetW) {

                w = w * scaleDown;
                h = h * scaleDown;

                // to support transparency - we cannot draw repeatedly into same canvas
                var intermediate = document.createElement('canvas');
                intermediate.width = Math.floor(w);
                intermediate.height = Math.floor(h);

                var ctx = intermediate.getContext('2d');
                ctx.drawImage(source, 0, 0, source.width, source.height, 0, 0, intermediate.width, intermediate.height);
                delete ctx;

                // flip buffer
                delete source;
                source = intermediate;
            }

            var output = document.createElement('canvas');
            output.width = w;
            output.height = h;

            var outputCtx = output.getContext('2d');
            outputCtx.drawImage(source, 0, 0, source.width, source.height, 0, 0, output.width, output.height); // crop image from temp into output
            delete outputCtx;

            delete source;

            return output;
        }

        var drawResized = function (img, w, h, output, quality) {
            quality = quality == null ? 0.9 : quality;
            output = output == null ? "image/jpeg" : output;

            var reducedImage = incrementalResize(img, w);

            var ratioH = img.naturalHeight / img.naturalWidth;
            var ratioW = img.naturalWidth / img.naturalHeight;

            var drawW = w;
            var drawH = ratioH * w;
            var offsetTop = 0;
            var offsetLeft = 0;

            if (h == null) {
                h = (img.naturalHeight / img.naturalWidth) * w;
            }

            var canvas = this.document.createElement("canvas");

            // canvas must be the size we want
            canvas.width = w;
            canvas.height = h;

            if (contain) {

                if (drawH > h) {
                    // reduce height first to match height
                    drawW = ratioW * h;
                    drawH = h;
                }

                offsetLeft = (w - drawW) / 2;
                offsetTop = (h - drawH) / 2;

            } else {

                if (drawH < canvas.height) {
                    console.log("height is less than dimension, fitting the height");

                    drawH = canvas.height;
                    drawW = (img.naturalWidth / img.naturalHeight) * drawH;
                }

                // calculate offset
                if (drawH > canvas.height) {
                    offsetTop = (drawH - canvas.height) / -2;
                }

                if (drawW > canvas.width) {
                    offsetLeft = (drawW - canvas.width) / -2;
                }

                if (isNaN(drawH)) {
                    debugger;
                }
            }

            console.log("ResizeImage> will resize to: " + drawW + "x" + drawH + " output: " + output + " quality: " + quality);

            var context = canvas.getContext("2d");
            context.drawImage(reducedImage, 0, 0, reducedImage.width, reducedImage.height, offsetLeft, offsetTop, drawW, drawH);

            var result = canvas.toDataURL(output, quality);

            delete context;
            delete canvas;
            delete reducedImage;

            return result;
        }

        // wait for image to load
        var img = new Image();
        img.onload = function () {

            var result = null;

            // don't resize to larger size or same size
            if (maxWidth >= img.naturalWidth && maxHeight >= img.naturalHeight) {
                console.log("ResizeImage> image is smaller than required dimension, does not resize")
                callback(dataUri);
                return;
            }

            if (dataUri.substring(0, 14) == "data:image/png") {

                if (img.naturalWidth > 256) { // big PNG image - offer to convert to PNG

                    if (confirm("Do you want to convert image to JPEG format? \r\n (Do not convert if you need transparency support")) {

                        result = drawResized(img, maxWidth, maxHeight);
                        delete img;
                        callback(result);
                        return;
                    }
                }

                // resize to smaller size using PNG
                result = drawResized(img, maxWidth, maxHeight, "image/png");
                delete img;
                callback(result);
                return;
            }

            // image is JPEG but it is large
            result = drawResized(img, maxWidth, maxHeight);
            delete img;
            callback(result);
            return;

        };

        try {
            img.src = dataUri;
        } catch (e) {
            return dataUri;
        }
    }

    var module = angular.module('ncb-imageresize', []);

    module.service('ImageResize', function () {

        // resize image to given dimension, image will fill
        this.fill = function (url, maxWidth, maxHeight, callback) {

        };

        this.resize = function (url, maxWidth, maxHeight, callback) {

        };

    });

})();