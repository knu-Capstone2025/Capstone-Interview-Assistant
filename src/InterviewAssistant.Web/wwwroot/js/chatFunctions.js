// chatFunctions.js
window.scrollToBottomWithOffset = function(elementId, offset) {
  const element = document.getElementById(elementId);
  if (element) {
      element.scrollTop = element.scrollHeight - offset;
  }
}

window.focusTextArea = function(elementId) {
  setTimeout(function() {
      const element = document.getElementById(elementId);
      if (element) {
          element.focus();
      }
  }, 0);
}