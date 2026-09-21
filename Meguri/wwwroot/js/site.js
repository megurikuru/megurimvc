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
            alert('タグは最大' + this.maxTags + '個まで設定できます。');
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

        this.selectedTags.forEach((tag, idx) => {
            const badge = $(`
                <span class="badge bg-primary text-white me-1 mb-1 p-2">
                    #${tag}
                    <button type="button" class="btn-close btn-close-white btn-sm ms-1" style="font-size:0.6rem;" aria-label="Remove"></button>
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
                            const opt = $(`<button type="button" class="list-group-item list-group-item-action py-1 px-2 small">${item.text}</button>`);
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
    $('#replyNotice').html(`<strong>>>${commentNumber} (${userName})</strong> への返信 <button type="button" class="btn-close btn-sm ms-2" onclick="cancelReply()"></button>`).removeClass('d-none');
    $('html, body').animate({
        scrollTop: $('#commentForm').offset().top - 100
    }, 300);
    $('#commentTextInput').focus();
}

function cancelReply() {
    $('#replyParentId').val('');
    $('#replyNotice').addClass('d-none').empty();
}

// カラーモード切替
function applyTheme(theme) {
    if (theme === 'auto') {
        const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        document.documentElement.setAttribute('data-bs-theme', isDark ? 'dark' : 'light');
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
}
window.applyTheme = applyTheme;

// OSのダークモード変更監視
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    const themeCookie = document.cookie.split('; ').find(row => row.startsWith('meguri_theme='));
    const theme = themeCookie ? themeCookie.split('=')[1] : 'auto';
    if (theme === 'auto') {
        applyTheme('auto');
    }
});