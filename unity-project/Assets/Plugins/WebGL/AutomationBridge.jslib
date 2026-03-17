mergeInto(LibraryManager.library, {
    JS_NotifyScreenChanged: function(namePtr) {
        var name = UTF8ToString(namePtr);
        window.__unityScreenName = name;
        console.log("[AutomationBridge] Screen: " + name);
    },
    JS_RegisterButton: function(idPtr, x, y, w, h) {
        var id = UTF8ToString(idPtr);
        if (!window.__unityButtons) window.__unityButtons = {};
        window.__unityButtons[id] = { x: x, y: y, w: w, h: h };
    },
    JS_ClearButtons: function() {
        window.__unityButtons = {};
    }
});
