function handleChatTypeChange(e) {
    const target = e.target;
    if (!target) return;

    if (target.matches('input[name="IsGroup"]')) {
        const container = document.getElementById('groupTitleContainer');
        if (container) {
            container.hidden = target.value !== 'true';
        }
    }
}

function handleMessageUiClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const cancelMessageReplyBtn = target.closest('[data-cancel-message-reply]');
    if (cancelMessageReplyBtn) {
        const parentInput = document.getElementById('parentMessageId');
        if (parentInput) parentInput.value = '';
        const notice = document.getElementById('chatReplyNotice');
        if (notice) notice.hidden = true;
        const label = document.getElementById('chatReplyLabel');
        if (label) label.textContent = '';
        const preview = document.getElementById('chatReplyPreview');
        if (preview) preview.textContent = '';
        const textInput = document.getElementById('messageText');
        if (textInput) textInput.focus();
        return;
    }

    // メッセージ返信ボタン
    const replyMsgBtn = target.closest('[data-reply-id]');
    if (replyMsgBtn) {
        const parentInput = document.getElementById('parentMessageId');
        if (parentInput) {
            parentInput.value = replyMsgBtn.dataset.replyId || '';
        }
        const label = document.getElementById('chatReplyLabel');
        if (label) label.textContent = replyMsgBtn.dataset.replyLabel || '';
        const preview = document.getElementById('chatReplyPreview');
        if (preview) preview.textContent = replyMsgBtn.dataset.replyPreview || '';
        const notice = document.getElementById('chatReplyNotice');
        if (notice) notice.hidden = false;
        const textInput = document.getElementById('messageText');
        if (textInput) {
            textInput.focus();
        }
    }
}

export function initMessageUi() {
    // メッセージ新規作成のチャット種別切り替え（ダイレクト / グループ）
    document.addEventListener('change', handleChatTypeChange);

    document.addEventListener('click', handleMessageUiClick);
}
