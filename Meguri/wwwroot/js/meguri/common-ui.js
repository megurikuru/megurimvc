/**
 * 日時要素（<time>）のローカライズ処理
 * UTCで記述された日時を、ブラウザの言語設定やタイムゾーンに合わせてフォーマット・表示を変換します。
 */
export function localizeTimes() {
    function formatLocalTime(el) {
        const d = new Date(el.getAttribute('datetime'));
        if (isNaN(d)) return;
        const opts = el.dataset.format === 'date'
            ? { year: 'numeric', month: '2-digit', day: '2-digit' }
            : { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };
        el.textContent = d.toLocaleString(navigator.language, opts);
        el.title = d.toLocaleString(navigator.language);
        el.setAttribute('aria-label', el.title);
    }

    document.querySelectorAll('time.local-time').forEach(formatLocalTime);
}

function handleConfirmSubmit(e) {
    const form = e.target.closest('form[data-confirm]');
    if (!form) return;
    const msg = form.getAttribute('data-confirm');
    if (msg && !window.confirm(msg)) {
        e.preventDefault();
    }
}

function handleAutoSubmitChange(e) {
    const target = e.target;
    if (!target) return;

    const autoSubmitSelect = target.closest('select[data-auto-submit]');
    if (autoSubmitSelect && autoSubmitSelect.form) {
        autoSubmitSelect.form.submit();
    }
}

function handleCommonUiClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    // パネルの表示切り替え (ID指定)
    const togglePanelBtn = target.closest('[data-toggle-panel]');
    if (togglePanelBtn) {
        const targetId = togglePanelBtn.getAttribute('data-toggle-panel');
        const panel = document.getElementById(targetId);
        if (panel) {
            panel.hidden = !panel.hidden;
            togglePanelBtn.setAttribute('aria-expanded', String(!panel.hidden));
        }
        return;
    }

    // 次の要素の表示切り替え
    const toggleNextBtn = target.closest('[data-toggle-next]');
    if (toggleNextBtn) {
        const next = toggleNextBtn.nextElementSibling;
        if (next) {
            next.hidden = !next.hidden;
        }
        return;
    }

    // 親要素の削除（ステータスメッセージの閉じるボタン等）
    const dismissBtn = target.closest('[data-dismiss-parent]');
    if (dismissBtn) {
        if (dismissBtn.parentElement) {
            dismissBtn.parentElement.remove();
        }
    }
}

export function initCommonUi() {
    // 確認ダイアログ付きフォーム送信
    document.addEventListener('submit', handleConfirmSubmit);

    // 選択変更時にフォームを自動送信
    document.addEventListener('change', handleAutoSubmitChange);

    document.addEventListener('click', handleCommonUiClick);
}
