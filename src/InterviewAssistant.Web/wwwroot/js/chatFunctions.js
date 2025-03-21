// chatFunctions.js
window.scrollToBottomWithOffset = function (elementId, offset) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight - offset;
    }
};

window.focusTextArea = function (elementId) {
    setTimeout(function () {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
        }
    }, 0);
};

window.getTextAreaValue = function (elementId) {
    const element = document.getElementById(elementId);
    return element ? element.value : "";
};
