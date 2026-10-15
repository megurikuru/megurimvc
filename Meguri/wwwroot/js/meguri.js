// SNS Meguri 共通 JavaScript

// リアクション送信 (AJAX)
function toggleReaction(targetType, targetId, reactionType, btn) {
    const token = $('input[name="__RequestVerificationToken"]').first().val();
    $.ajax({
        url: '/Reaction/Toggle',
        type: 'POST',
        data: {
            targetType: targetType,
            targetId: targetId,
            reactionType: reactionType,
            __RequestVerificationToken: token
        },
        success: function (res) {
            if (res.success) {
                const countSpan = $(btn).find('.reaction-count');
                countSpan.text(res.count);
                if (res.hasReacted) {
                    $(btn).addClass('reacted');
                } else {
                    $(btn).removeClass('reacted');
                }
            }
        },
        error: function (xhr) {
            if (xhr.status === 401) {
                window.location.href = '/Account/Login';
            }
        }
    });
}

// 画像の並び替え (上へ / 下へ)
function moveImageOrder(btn, direction) {
    const item = $(btn).closest('.image-order-item');
    if (direction === 'up') {
        const prev = item.prev('.image-order-item');
        if (prev.length > 0) {
            item.insertBefore(prev);
        }
    } else if (direction === 'down') {
        const next = item.next('.image-order-item');
        if (next.length > 0) {
            item.insertAfter(next);
        }
    }
    updateImageOrderIndexes();
}

function removeImageOrderItem(btn) {
    $(btn).closest('.image-order-item').remove();
    updateImageOrderIndexes();
}

function updateImageOrderIndexes() {
    $('#imageOrderContainer .image-order-item').each(function (index) {
        $(this).find('.order-number').text(index + 1);
        $(this).find('.order-input').val(index);
    });
}

// タグ管理コンポーネント (最大10個)
const TagManager = {
    selectedTags: [],
    maxTags: 10,

    init: function (initialTags) {
        if (initialTags && Array.isArray(initialTags)) {
            this.selectedTags = [...initialTags];
        }
        this.render();
        this.setupAutocomplete();
    },

    addTag: function (tagText) {
        tagText = tagText.trim();
        if (!tagText) return;
        if (this.selectedTags.length >= this.maxTags) {
            const fmt = (window.MeguriI18n && window.MeguriI18n.maxTagsAlertFormat) || 'Up to {0} tags.';
            alert(fmt.replace('{0}', this.maxTags));
            return;
        }
        if (!this.selectedTags.includes(tagText)) {
            this.selectedTags.push(tagText);
            this.render();
        }
        $('#tagInput').val('');
        $('#tagSuggestions').empty().hide();
    },

    removeTag: function (tagText) {
        this.selectedTags = this.selectedTags.filter(t => t !== tagText);
        this.render();
    },

    render: function () {
        const container = $('#selectedTagsContainer');
        const hiddenInputsContainer = $('#hiddenTagsInputs');
        container.empty();
        hiddenInputsContainer.empty();

        const removeLabel = (window.MeguriI18n && window.MeguriI18n.removeLabel) || 'Remove';
        this.selectedTags.forEach((tag, idx) => {
            const badge = $(`
                <span>
                    #${tag}
                    <button type="button"  style="font-size:0.6rem;" aria-label="${removeLabel}"></button>
                </span>
            `);
            badge.find('button').on('click', () => this.removeTag(tag));
            container.append(badge);

            hiddenInputsContainer.append(`<input type="hidden" name="Tags[${idx}]" value="${tag}" />`);
        });

        $('#tagCountDisplay').text(`${this.selectedTags.length}/${this.maxTags}`);
    },

    setupAutocomplete: function () {
        const input = $('#tagInput');
        const suggestions = $('#tagSuggestions');

        let timeout = null;
        input.on('input', function () {
            clearTimeout(timeout);
            const query = $(this).val().trim();
            if (!query) {
                suggestions.empty().hide();
                return;
            }
            timeout = setTimeout(() => {
                $.getJSON('/Tag/Search?q=' + encodeURIComponent(query), function (data) {
                    suggestions.empty();
                    if (data && data.length > 0) {
                        data.forEach(item => {
                            const opt = $(`<button type="button" >${item.text}</button>`);
                            opt.on('click', function () {
                                TagManager.addTag(item.text);
                            });
                            suggestions.append(opt);
                        });
                        suggestions.show();
                    } else {
                        suggestions.hide();
                    }
                });
            }, 250);
        });

        input.on('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                TagManager.addTag($(this).val());
            }
        });
    }
};

// コメント返信機能
function replyToComment(commentId, commentNumber, userName) {
    $('#replyParentId').val(commentId);
    const fmt = (window.MeguriI18n && window.MeguriI18n.replyNoticeFormat) || '<strong>>>{0} ({1})</strong>';
    const cancelLabel = (window.MeguriI18n && window.MeguriI18n.cancelLabel) || 'Cancel';
    const safeUser = $('<div>').text(userName).html();
    const noticeText = fmt.replace('{0}', commentNumber).replace('{1}', safeUser);
    $('#replyNotice').html(`${noticeText} <button type="button" aria-label="${cancelLabel}" onclick="cancelReply()"></button>`).removeClass('d-none');
    $('html, body').animate({
        scrollTop: $('#commentForm').offset().top - 100
    }, 300);
    $('#commentTextInput').focus();
}

function cancelReply() {
    $('#replyParentId').val('');
    $('#replyNotice').addClass('d-none').empty();
}

// UTC の <time> 要素をブラウザのタイムゾーン/ロケールで表示
function localizeTimes() {
    document.querySelectorAll('time.local-time').forEach(function (el) {
        const d = new Date(el.getAttribute('datetime'));
        if (isNaN(d)) return;
        const opts = el.dataset.format === 'date'
            ? { year: 'numeric', month: '2-digit', day: '2-digit' }
            : { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };
        el.textContent = d.toLocaleString(navigator.language, opts);
        el.title = d.toISOString();
    });
}

document.addEventListener('DOMContentLoaded', localizeTimes);
