mergeInto(LibraryManager.library, {
  MemeopolyImportBrowserSave: function(gameObjectPtr) {
    try {
      var name = UTF8ToString(gameObjectPtr);
      var json = window.localStorage.getItem('memeopoly-save-v2') ||
                 window.localStorage.getItem('memeopoly-save-v1');
      if (json) SendMessage(name, 'ReceiveLegacySave', json);
    } catch (error) {
      console.warn('Memeopoly: existing web save not available', error);
    }
  }
});
