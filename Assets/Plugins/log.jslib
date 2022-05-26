mergeInto(LibraryManager.library, 
{
  FBGameStart: function () 
  {
    analytics.logEvent("Game Start", {});
  },
});