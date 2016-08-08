/**
@ngdoc directive
@name umbraco.directives.directive:umbMediaGrid
@restrict E
@scope

@description
Use this directive to generate a thumbnail grid of media items.

<h3>Markup example</h3>
<pre>
    <div ng-controller="My.Controller as vm">

        <umb-media-grid
           items="vm.mediaItems"
           on-click="vm.clickItem"
           on-click-name="vm.clickItemName">
        </umb-media-grid>

    </div>
</pre>

<h3>Controller example</h3>
<pre>
    (function () {
        "use strict";

        function Controller() {

            var vm = this;
            vm.mediaItems = [];

            vm.clickItem = clickItem;
            vm.clickItemName = clickItemName;

            myService.getMediaItems().then(function (mediaItems) {
                vm.mediaItems = mediaItems;
            });

            function clickItem(item, $event, $index){
                // do magic here
            }

            function clickItemName(item, $event, $index) {
                // set item.selected = true; to select the item
                // do magic here
            }

        }

        angular.module("umbraco").controller("My.Controller", Controller);
    })();
</pre>

@param {array} items (<code>binding</code>): Array of media items.
@param {callback=} onDetailsHover (<code>binding</code>): Callback method when the details icon is hovered.
    <h3>The callback returns:</h3>
    <ul>
        <li><code>item</code>: The hovered item</li>
        <li><code>$event</code>: The hover event</li>
        <li><code>hover</code>: Boolean to tell if the item is hovered or not</li>
    </ul>
@param {callback=} onClick (<code>binding</code>): Callback method to handle click events on the media item.
    <h3>The callback returns:</h3>
    <ul>
        <li><code>item</code>: The clicked item</li>
        <li><code>$event</code>: The click event</li>
        <li><code>$index</code>: The item index</li>
    </ul>
@param {callback=} onClickName (<code>binding</code>): Callback method to handle click events on the media item name.
    <h3>The callback returns:</h3>
    <ul>
        <li><code>item</code>: The clicked item</li>
        <li><code>$event</code>: The click event</li>
        <li><code>$index</code>: The item index</li>
    </ul>
@param {string=} filterBy (<code>binding</code>): String to filter media items by
@param {string=} itemMaxWidth (<code>attribute</code>): Sets a max width on the media item thumbnails.
@param {string=} itemMaxHeight (<code>attribute</code>): Sets a max height on the media item thumbnails.
@param {string=} itemMinWidth (<code>attribute</code>): Sets a min width on the media item thumbnails.
@param {string=} itemMinHeight (<code>attribute</code>): Sets a min height on the media item thumbnails.

**/

(function() {
    'use strict';

    function MediaGridDirective($filter, mediaHelper) {

        function link(scope, el, attr, ctrl) {

            var itemDefaultHeight = 200;
            var itemDefaultWidth = 200;
            var itemMaxWidth = 200;
            var itemMaxHeight = 200;
            var itemMinWidth = 125;
            var itemMinHeight = 125;

            function activate() {

                if (scope.itemMaxWidth) {
                    itemMaxWidth = scope.itemMaxWidth;
                }

                if (scope.itemMaxHeight) {
                    itemMaxHeight = scope.itemMaxHeight;
                }

                if (scope.itemMinWidth) {
                    itemMinWidth = scope.itemMinWidth;
                }

                if (scope.itemMinWidth) {
                    itemMinHeight = scope.itemMinHeight;
                }

                for (var i = 0; scope.items.length > i; i++) {
                    var item = scope.items[i];
                    setItemData(item);
                    setOriginalSize(item, itemMaxHeight);

                    // remove non images when onlyImages is set to true
                    if(scope.onlyImages === "true" && !item.isFolder && !item.thumbnail){
                        scope.items.splice(i, 1);
                        i--;
                    }

                }

                if (scope.items.length > 0) {
                    setFlexValues(scope.items);
                }

            }

            function setItemData(item) {
                item.isFolder = !mediaHelper.hasFilePropertyType(item);
                if (!item.isFolder) {
                    item.thumbnail = mediaHelper.resolveFile(item, true);
                    item.image = mediaHelper.resolveFile(item, false);

                    var fileProp = _.find(item.properties, function (v) {
                        return (v.alias === "umbracoFile");
                    });

                    if (fileProp && fileProp.value) {
                        item.file = fileProp.value;
                    }

                    var extensionProp = _.find(item.properties, function (v) {
                        return (v.alias === "umbracoExtension");
                    });

                    if (extensionProp && extensionProp.value) {
                        item.extension = extensionProp.value;
                    }
                }
            }

            function setOriginalSize(item, maxHeight) {

                //set to a square by default
                item.width = itemDefaultWidth;
                item.height = itemDefaultHeight;
                item.aspectRatio = 1;

                var widthProp = _.find(item.properties, function(v) {
                    return (v.alias === "umbracoWidth");
                });

                if (widthProp && widthProp.value) {
                    item.width = parseInt(widthProp.value, 10);
                    if (isNaN(item.width)) {
                        item.width = itemDefaultWidth;
                    }
                }

                var heightProp = _.find(item.properties, function(v) {
                    return (v.alias === "umbracoHeight");
                });

                if (heightProp && heightProp.value) {
                    item.height = parseInt(heightProp.value, 10);
                    if (isNaN(item.height)) {
                        item.height = itemDefaultWidth;
                    }
                }

                item.aspectRatio = item.width / item.height;

                // set max width and height
                // landscape
                if (item.aspectRatio >= 1) {
                    if (item.width > itemMaxWidth) {
                        item.width = itemMaxWidth;
                        item.height = itemMaxWidth / item.aspectRatio;
                    }
                    // portrait
                } else {
                    if (item.height > itemMaxHeight) {
                        item.height = itemMaxHeight;
                        item.width = itemMaxHeight * item.aspectRatio;
                    }
                }

            }

            function setFlexValues(mediaItems) {

                var flexSortArray = mediaItems;
                var smallestImageWidth = null;
                var widestImageAspectRatio = null;

                // sort array after image width with the widest image first
                flexSortArray = $filter('orderBy')(flexSortArray, 'width', true);

                // find widest image aspect ratio
                widestImageAspectRatio = flexSortArray[0].aspectRatio;

                // find smallest image width
                smallestImageWidth = flexSortArray[flexSortArray.length - 1].width;

                for (var i = 0; flexSortArray.length > i; i++) {

                    var mediaItem = flexSortArray[i];
                    var flex = 1 / (widestImageAspectRatio / mediaItem.aspectRatio);

                    if (flex === 0) {
                        flex = 1;
                    }

                    var imageMinFlexWidth = smallestImageWidth * flex;

                    var flexStyle = {
                        "flex": flex + " 1 " + imageMinFlexWidth + "px",
                        "max-width": mediaItem.width + "px",
                        "min-width": itemMinWidth + "px",
                        "min-height": itemMinHeight + "px"
                    };

                    mediaItem.flexStyle = flexStyle;

                }

            }

            scope.clickItem = function(item, $event, $index) {
                if (scope.onClick) {
                    scope.onClick(item, $event, $index);
                }
            };

            scope.clickItemName = function(item, $event, $index) {
                if (scope.onClickName) {
                    scope.onClickName(item, $event, $index);
                    $event.stopPropagation();
                }
            };

            scope.hoverItemDetails = function(item, $event, hover) {
                if (scope.onDetailsHover) {
                    scope.onDetailsHover(item, $event, hover);
                }
            };

            var unbindItemsWatcher = scope.$watch('items', function(newValue, oldValue) {
                if (angular.isArray(newValue)) {
                    activate();
                }
            });

            scope.$on('$destroy', function() {
                unbindItemsWatcher();
            });

        }

        var directive = {
            restrict: 'E',
            replace: true,
            templateUrl: 'views/components/umb-media-grid.html',
            scope: {
                items: '=',
                onDetailsHover: "=",
                onClick: '=',
                onClickName: "=",
                filterBy: "=",
                itemMaxWidth: "@",
                itemMaxHeight: "@",
                itemMinWidth: "@",
                itemMinHeight: "@",
                onlyImages: "@"
            },
            link: link
        };

        return directive;
    }

    angular.module('umbraco.directives').directive('umbMediaGrid', MediaGridDirective);

})();
