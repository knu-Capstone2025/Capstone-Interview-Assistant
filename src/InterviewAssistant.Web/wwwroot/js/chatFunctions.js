// chatFunctions.js
window.isAutoScrollEnabled = true;

window.scrollToBottom = function (elementId) {
    if (!window.isAutoScrollEnabled) return;

    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.setupAutoScrollDetection = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) return;

    element.addEventListener(
        "wheel",
        () => {
            window.isAutoScrollEnabled = false;
        },
        { passive: true }
    );
    element.addEventListener(
        "touchmove",
        () => {
            window.isAutoScrollEnabled = false;
        },
        { passive: true }
    );
};

window.resetAutoScroll = function () {
    window.isAutoScrollEnabled = true;
};

window.forceScrollToBottom = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
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

window.autoResizeTextArea = function (elementId) {
    const textarea = document.getElementById(elementId);
    if (textarea) {
        // 최소 높이와 최대 높이 설정
        const minHeight = 20;
        const maxHeight = 200;

        // 높이 재설정
        textarea.style.height = "auto";

        // 새 높이 계산 (최소, 최대 높이 제한 적용)
        const newHeight = Math.min(
            Math.max(textarea.scrollHeight, minHeight),
            maxHeight
        );
        textarea.style.height = newHeight + "px";

        // 스크롤 표시 여부 결정
        textarea.style.overflowY =
            textarea.scrollHeight > maxHeight ? "auto" : "hidden";
    }
};

window.setupTextAreaResize = function (elementId) {
    const textarea = document.getElementById(elementId);
    if (textarea) {
        // 초기 높이 설정
        window.autoResizeTextArea(elementId);

        // 입력 이벤트에 리사이징 함수 연결
        textarea.addEventListener("input", function () {
            window.autoResizeTextArea(elementId);
        });
    }
};

// 텍스트 영역 높이 리셋 함수
window.resetTextAreaHeight = function (elementId) {
    const textarea = document.getElementById(elementId);
    if (textarea) {
        // 기본 높이로 리셋 (CSS에서 지정한 min-height 값이 적용됨)
        textarea.style.height = "";
        textarea.style.overflowY = "hidden";
    }
};
