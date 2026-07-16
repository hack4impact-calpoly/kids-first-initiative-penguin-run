mergeInto(LibraryManager.library, {
  KFI_PostUnityProgress: function (jsonPtr) {
    try {
      var payload = JSON.parse(UTF8ToString(jsonPtr));

      if (typeof window.postUnityProgress !== "function") {
        console.error("The active WebGL template does not define window.postUnityProgress.");
        return;
      }

      window.postUnityProgress(payload);
    } catch (error) {
      console.error("Failed to post Unity progress.", error);
    }
  },
});
