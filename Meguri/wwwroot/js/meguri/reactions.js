function isButtonDisabled(button) {
    return button.disabled;
}

function disableButton(button) {
    button.disabled = true;
}

function enableButton(button) {
    button.disabled = false;
}

function isMatchingReactionTarget(button, targetType, targetId, reactionType) {
    return button.dataset.reactionTargetType === targetType
        && button.dataset.reactionTargetId === String(targetId)
        && button.dataset.reactionType === String(reactionType);
}

/**
 * リアクション送信処理 (Fetch API)
 * 指定されたターゲットに対してリアクションの追加・削除（トグル）を非同期で行います。
 * 
 * @param {string} targetType - ターゲットの種類
 * @param {number|string} targetId - ターゲットのID
 * @param {number|string} reactionType - リアクションの種類
 * @param {HTMLElement} btn - 操作されたボタン要素
 */
export async function toggleReaction(targetType, targetId, reactionType, btn) {
    function isMatchingButton(button) {
        return isMatchingReactionTarget(button, targetType, targetId, reactionType);
    }

    const matchingButtons = Array.from(document.querySelectorAll('[data-reaction-target-type]')).filter(isMatchingButton);
    if (matchingButtons.some(isButtonDisabled)) return;
    matchingButtons.forEach(disableButton);
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    const body = new URLSearchParams({
        targetType: String(targetType),
        targetId: String(targetId),
        reactionType: String(reactionType),
        __RequestVerificationToken: token
    });

    try {
        const response = await fetch('/Reaction/Toggle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: body.toString(),
            credentials: 'same-origin'
        });

        if (response.status === 401) {
            window.location.href = '/Account/Login';
            return;
        }

        if (!response.ok) return;

        const res = await response.json();
        if (res && res.success) {
            function updateReactionButtonState(button) {
                const countSpan = button.querySelector('.reaction-count');
                if (countSpan) countSpan.textContent = res.count;
                button.classList.toggle('reacted', Boolean(res.hasReacted));
                button.setAttribute('aria-pressed', String(Boolean(res.hasReacted)));
            }

            matchingButtons.forEach(updateReactionButtonState);
        }
    } catch (err) {
        console.error('Reaction error:', err);
    } finally {
        matchingButtons.forEach(enableButton);
    }
}

function handleReactionClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const reactionBtn = target.closest('[data-reaction-target-type]');
    if (reactionBtn) {
        const targetType = reactionBtn.getAttribute('data-reaction-target-type');
        const targetId = Number(reactionBtn.getAttribute('data-reaction-target-id'));
        const reactionType = Number(reactionBtn.getAttribute('data-reaction-type'));
        toggleReaction(targetType, targetId, reactionType, reactionBtn);
    }
}

export function initReactions() {
    document.addEventListener('click', handleReactionClick);
}
