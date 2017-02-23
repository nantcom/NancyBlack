/* --------------------------------------
=========================================
ADMAG - Responsive Blog & Magazine HTML Template
Version: 1.0
Designed by: DigitalTheme.co
=========================================

1. Bootstrap tooltip
2. Bootstrap Select First Tab
3. Tabs hover effect
4. Menu hover effect
5. Fade effect on Menu and Tab
6. FLexslider
7. Image popup
8. Parallax Effect
9. Full width background image
10. Sticky Sidebar
11. Mobile Menu
12. Mobile Menu Scrollbar
13. Responsive video
14. Sticky Header
15. Fixed Sidebar Scrollbar
16. Go to Top Button
17. Count up share counter
18. Google Map
-----------------------------------------*/

$(function () {
	"use strict";

    $(document).ready( function(){

    	var $_html = $("html");
    	var $_body = $("body");
    	var $_flexslider = $(".flexslider");
    	var $_header_menu = $('#header');

    	// 1. Bootstrap tooltip
		$('[data-toggle="tooltip"]').tooltip();

		// 2. Bootstrap Select First Tab
		$('#widget-tab a.first').tab('show');
		$('.tab-hover .nav-tabs > li > a.first').tab('show');

		// 3. Tabs hover effect
		$('.tab-hover .nav-tabs > li > a').on("mouseover", function(){
      	$(this).tab('show');
   	});

		// 4. Menu hover effect
		$('.dropdown-toggle').dropdownHover();

    	// 5. Fade effect on Menu and Tab
		$('.nav .dropdown-menu').addClass('animated fadeIn');
		$('.tab-pane').addClass('animated fadeIn');

		if( $_html.hasClass("no-touch") ){
			$('.navbar .dropdown > a').on("click", function(){
	         location.href = this.href;
	      });
      }

		// 6. FLexslider
    	if( $_flexslider.length ){
    		$_flexslider.flexslider({
				selector: ".featured-slider > .slider-item",
				maxItems: 1,
				minItems: 1,
				startAt: 0,
				animation:"slide",
				slideshow: true,
				controlNav: false,
				nextText:'<i class="fa fa-angle-right"></i>',
				prevText:'<i class="fa fa-angle-left"></i>'
			});
    	}

		// 7. Image popup
		$('.post-content a.popup-image').magnificPopup({ 
		  	type: 'image',
			mainClass: 'mfp-with-zoom', // this class is for CSS animation below

			zoom: {
				enabled: true, // By default it's false, so don't forget to enable it

				duration: 200, // duration of the effect, in milliseconds
				easing: 'ease-in-out', // CSS transition easing function 

				opener: function(openerElement) {
				 	return openerElement.is('img') ? openerElement : openerElement.find('img');
				}
			}
		});

		// 8. Parallax Effect
	   $.stellar({
			horizontalScrolling: false,
			verticalOffset: 60
		});

	   // 9. Full width background image
	   var $_parallax = $( "#parallax-image" );

	   if ( $_parallax.length ) {
			$_parallax.backstretch( $_parallax.data('image') );
		}

		// 10. Sticky Sidebar
		var $_datacolumn = $("[data-stickycolumn]");

		if ( $( "[data-stickyparent]" ).length ) {
			// Sticky Sidebar start when images loaded
			imagesLoaded( $_body ).on( 'always', function( instance ) {
			  $_datacolumn.stick_in_parent({
					parent: "[data-stickyparent]"
				}).on('sticky_kit:bottom', function(e) {
				    $(this).parent().css('position', 'static');
				})
				.on('sticky_kit:unbottom', function(e) {
				    $(this).parent().css('position', 'relative');
				});

				// destroy it if mobile or tablet 
				destroy_sticky();
			});
		}

		var $_windowwidth = $(window).width();

		// Destroy sticky Sidebar on Mobile and Tablet
		function destroy_sticky(){
			var $_windowwidth = $(window).width();
			if( $_windowwidth < 992){
				$_datacolumn.trigger("sticky_kit:detach");
			}
		}

		if($_windowwidth < 992){
			FastClick.attach(document.body);
		}

		// 11. Mobile Menu
		$("#mobile-nav").mmenu({
         // options
         searchfield: true,
         slidingSubmenus: false,
         extensions: ["theme-dark", "effect-slide", "border-full"]
      },{
         // configuration
      });

		// 12. Mobile Menu Scrollbar
		$('.mobile-wrapper').perfectScrollbar({
			wheelSpeed: 1,
			suppressScrollX: true
		});

		$('#fixed-button').on("click", function(event){
			event.preventDefault();
			$_html.toggleClass("ad-opened");
		});

		$('#mobile-overlay').on("click", function(event){
			event.preventDefault();
			$_html.toggleClass("ad-opened");
		});

		// 13. Responsive video
		$(".image-overlay").fitVids();

		// 14. Sticky Header
		if( !$("#main").hasClass('fixed-sidebar') ){
	    	// Sticky menu hide
	    	$_header_menu.headroom({
			  "offset": 330,
			  "tolerance": 0,
			  "classes": {
			    "initial": "animated",
			    "pinned": "slideDown",
			    "unpinned": "slideUp"
			  }
			});
		}
		
		var $_stickyScrollbar = $('.sticky-scroll');
		var $_sidebar = $("#sticky-sidebar");
		var $_goTop = $('#go-top-button');
		var $_header_topoffset = 0;
		var $_offset = 133;
		$_header_topoffset = $_header_menu.offset().top;


		// 15. Fixed Sidebar Scrollbar
		$_stickyScrollbar.perfectScrollbar({
			wheelSpeed: 1,
			suppressScrollX: true
		});

		// Change scrollbar height when load
		change_height();

		// Change scrollbar height when resize
		$(window).on("resize", function() {
			if ( $_datacolumn.length ) {
				$(document.body).trigger("sticky_kit:recalc");
				destroy_sticky();
			}

			change_height();
		});

		$(window).scroll(function(){
			// Go to top button
			if ($(this).scrollTop() > 100) {
				$_goTop.css({ bottom: '20px' });
			}
			else {
				$_goTop.css({ bottom: '-100px' });
			}

			// Fixed header
			if ($(this).scrollTop() > $_header_topoffset) {
				$_header_menu.addClass("set-fixed");
				$_sidebar.addClass("set-sidebar");
			}
			else {
				$_header_menu.removeClass("set-fixed");
				$_sidebar.removeClass("set-sidebar");
			}

			// Fixed sidebar
			if ($(this).scrollTop() > 50) {
				$_sidebar.addClass("get-sidebar");
				change_height();
			}
			else {
				$_sidebar.removeClass("get-sidebar");
				change_height();
			}
		});

		// Change scrollbar height
		function change_height(){
			var windowHeight = $(window).height();

			if( $_sidebar.length ){
				$_offset = $_sidebar.position().top + 80;

				var setHeight = windowHeight - $_offset;
				$_stickyScrollbar.height(setHeight);
				$_stickyScrollbar.perfectScrollbar('update');  // Update
			}

			$('.mobile-wrapper').height(windowHeight);
			$('.mobile-wrapper').perfectScrollbar('update');  // Update
		}
		
		// 16. Go to Top Button
		$_goTop.on("click", function(){
			$('html, body').animate({scrollTop : 0},700);
			return false;
		});

    });
    
});
/*
 * End Jquery
*/

// 17. Count up share counter
if(document.getElementById("countUp")){
	var count = document.getElementById("countUp");

	var number = 0;

	if(count.hasAttribute("data-count")){
		var number = count.getAttribute("data-count");
	}

	var options = {
	  useEasing : true, 
	  useGrouping : true, 
	  separator : '',
	  decimal : '.',
	  prefix : '',
	  suffix : ''
	};

	var countUp = new countUp("countUp", 0, number, 0, 2.5, options);

	var waypoint = new Waypoint({
	  element: document.getElementById('countUp'),
	  handler: function(direction) {
	    countUp.start();
	  },
	  offset: window.innerHeight-70
	});
}

// 18. Header Search Button
new UISearch( document.getElementById( 'sb-search' ) );

// 19. Google Map
var map_canvas =  document.getElementById('map-canvas');
if (typeof(map_canvas) != 'undefined' && map_canvas != null)
{
	// Contact page Google map
	function initialize() {
	  var myLatlng = new google.maps.LatLng(-25.363882,131.044922);
	  var mapOptions = {
	    zoom: 4,
	    center: myLatlng
	  }
	  var map = new google.maps.Map(document.getElementById('map-canvas'), mapOptions);

	  var marker = new google.maps.Marker({
	      position: myLatlng,
	      map: map,
	      title: 'Hello World!'
	  });
	}

	google.maps.event.addDomListener(window, 'load', initialize); 
}