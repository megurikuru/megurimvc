export function replyToComment(commentId, replyLabel, replyPreview) {
    const parentInput = document.getElementById('replyParentId');
    if (parentInput) {
        parentInput.value = commentId || '';
    }

    const label = document.getElementById('commentReplyLabel');
    if (label) label.textContent = replyLabel || '';

    const preview = document.getElementById('commentReplyPreview');
    if (preview) preview.textContent = replyPreview || '';

    const replyNotice = document.getElementById('replyNotice');
    if (replyNotice) {
        replyNotice.hidden = false;
    }

    const commentTextInput = document.getElementById('commentTextInput');
    if (commentTextInput) {
        commentTextInput.focus();
    }
}

export function cancelReply() {
    const parentInput = document.getElementById('replyParentId');
    if (parentInput) {
        parentInput.value = '';
    }
    const replyNotice = document.getElementById('replyNotice');
    if (replyNotice) {
        replyNotice.hidden = true;
    }
    const label = document.getElementById('commentReplyLabel');
    if (label) label.textContent = '';

    const preview = document.getElementById('commentReplyPreview');
    if (preview) preview.textContent = '';

    const commentTextInput = document.getElementById('commentTextInput');
    if (commentTextInput) {
        commentTextInput.focus();
    }
}

function handleCommentReplyClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const replyCommentBtn = target.closest('[data-reply-comment-id]');
    if (replyCommentBtn) {
        const commentId = replyCommentBtn.dataset.replyCommentId;
        const replyLabel = replyCommentBtn.dataset.replyLabel;
        const replyPreview = replyCommentBtn.dataset.replyPreview;
        replyToComment(commentId, replyLabel, replyPreview);
        return;
    }

    const cancelReplyBtn = target.closest('[data-cancel-reply]');
    if (cancelReplyBtn) {
        cancelReply();
    }
}

export function initCommentReply() {
    document.addEventListener('click', handleCommentReplyClick);
}
