/**
 @ngdoc service
 * @name umbraco.services.overlayService
 *
 * @description
 * <b>Added in Umbraco 8.0</b>. Application-wide service for handling overlays.
 */
(function () {
    "use strict";

    function overlayService(eventsService, backdropService) {

        var currentOverlay = null;

        function getCurrent() {
            return currentOverlay;
        }

        function refresh(overlay) {
            //extend the passed overlay on top of the current overlay and then re-assign to currentOverlay
            var merged = angular.extend({}, currentOverlay, overlay);
            currentOverlay = merged;

            eventsService.emit("appState.overlay", currentOverlay);
        }

        function open(newOverlay) {

            // prevent two open overlays at the same time
            if(currentOverlay) {
                close();
            }

            var backdropOptions = {};
            var overlay = newOverlay;

            // set the default overlay position to center
            if(!overlay.position) {
                overlay.position = "center";
            }

            // set the default overlay size to small
            if(!overlay.size) {
                overlay.size = "small";
            }

            // use a default empty view if nothing is set
            if(!overlay.view) {
                overlay.view = "views/common/overlays/default/default.html";
            }

            // option to disable backdrop clicks
            if(overlay.disableBackdropClick) {
                backdropOptions.disableEventsOnClick = true;
            }

            overlay.show = true;
            backdropService.open(backdropOptions);
            currentOverlay = overlay;
            eventsService.emit("appState.overlay", currentOverlay);
        }

        function close() {
            backdropService.close();
            currentOverlay = null;
            eventsService.emit("appState.overlay", null);
        }

        function ysod(error) {
            const overlay = {
                view: "views/common/overlays/ysod/ysod.html",
                error: error,
                close: function() {
                    close();
                }
            };
            open(overlay);
        }

        var service = {
            open: open,
            close: close,
            ysod: ysod,
            refresh: refresh,
            getCurrent: getCurrent
        };

        return service;

    }

    angular.module("umbraco.services").factory("overlayService", overlayService);


})();
