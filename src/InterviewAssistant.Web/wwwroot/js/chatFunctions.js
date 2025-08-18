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

    // 테스트 환경에서 스크롤 고정 유지
    if (navigator.webdriver) return;

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

window.createChart = (canvasId, chartData) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    new Chart(ctx, {
        type: "pie", // 'bar'로 바꾸면 막대그래프가 됩니다.
        data: {
            labels: chartData.labels,
            datasets: [
                {
                    label: "질문 유형 분석",
                    data: chartData.values,
                    backgroundColor: [
                        "rgba(255, 99, 132, 0.7)",
                        "rgba(54, 162, 235, 0.7)",
                        "rgba(255, 206, 86, 0.7)",
                        "rgba(75, 192, 192, 0.7)",
                        "rgba(153, 102, 255, 0.7)",
                    ],
                    borderColor: "rgba(255, 255, 255, 1)",
                    borderWidth: 1,
                },
            ],
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
        },
    });
};

// body 태그의 overflow 스타일을 설정하는 함수
window.setBodyOverflow = (style) => {
    document.body.style.overflow = style;
};

window.isMessageSendInProgress = function () {
    return window.isSend || false;
};

// PDF 다운로드 함수
window.downloadFile = function (base64Data, fileName, contentType) {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType });
    
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};