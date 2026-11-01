/**
 * HTML特殊文字のエスケープ処理
 * 文字列内のHTML特殊文字を安全な実体参照に変換します。
 * 
 * @param {string} str - エスケープ対象の文字列
 * @returns {string} エスケープ済みの文字列
 */
function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

/**
 * コメントへの返信準備処理
 * 返信対象のコメントIDや通知表示、入力欄へのフォーカス等を設定します。
 * 
 * @param {string|number} commentId - 返信先コメントのID
 * @param {string|number} commentNumber - コメントの番号
 * @param {string} userName - 返信先ユーザー名
 */
export function replyToComment(commentId, commentNumber, userName) {
    const parentInput = document.getElementById('replyParentId');
    if (parentInput) {
        parentInput.value = commentId;
    }

    const fmt = (window.MeguriI18n && window.MeguriI18n.replyNoticeFormat) || '<strong>>>{0} ({1})</strong>';
    const cancelLabel = (window.MeguriI18n && window.MeguriI18n.cancelLabel) || 'Cancel';
    const safeUser = escapeHtml(userName || '');
    const noticeText = fmt.replace('{0}', commentNumber).replace('{1}', safeUser);

    const replyNotice = document.getElementById('replyNotice');
    if (replyNotice) {
        replyNotice.innerHTML = `${noticeText} <button type="button" class="btn-sm" aria-label="${escapeHtml(cancelLabel)}" data-cancel-reply>×</button>`;
        replyNotice.classList.remove('d-none');
    }

    const commentForm = document.getElementById('commentForm');
    if (commentForm) {
        const top = commentForm.getBoundingClientRect().top + window.scrollY - 100;
        window.scrollTo({ top: top, behavior: 'smooth' });
    }

    const commentTextInput = document.getElementById('commentTextInput');
    if (commentTextInput) {
        commentTextInput.focus();
    }
}

/**
 * コメント返信のキャンセル処理
 * 返信対象IDをクリアし、返信通知エリアを非表示にします。
 */
export function cancelReply() {
    const parentInput = document.getElementById('replyParentId');
    if (parentInput) {
        parentInput.value = '';
    }
    const replyNotice = document.getElementById('replyNotice');
    if (replyNotice) {
        replyNotice.classList.add('d-none');
        replyNotice.innerHTML = '';
    }
}

function handleCommentReplyClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const replyCommentBtn = target.closest('[data-reply-comment-id]');
    if (replyCommentBtn) {
        const commentId = replyCommentBtn.getAttribute('data-reply-comment-id');
        const commentNumber = replyCommentBtn.getAttribute('data-reply-comment-number');
        const userName = replyCommentBtn.getAttribute('data-reply-user');
        replyToComment(commentId, commentNumber, userName);
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
