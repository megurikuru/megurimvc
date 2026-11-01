/**
 * 画像の並び替え (上へ / 下へ)
 * 指定された画像アイテムをプレビューリスト内で「上」または「下」に移動させます。
 * 
 * @param {HTMLElement} btn - 並び替えボタン要素
 * @param {string} direction - 移動方向 ('up' または 'down')
 */
export function moveImageOrder(btn, direction) {
    const item = btn.closest('.image-order-item');
    if (!item || !item.parentElement) return;

    if (direction === 'up') {
        const prev = item.previousElementSibling;
        if (prev && prev.classList.contains('image-order-item')) {
            item.parentElement.insertBefore(item, prev);
        }
    } else if (direction === 'down') {
        const next = item.nextElementSibling;
        if (next && next.classList.contains('image-order-item')) {
            item.parentElement.insertBefore(next, item);
        }
    }
    updateImageOrderIndexes();
}

/**
 * 画像並び替えアイテムの削除
 * 指定された画像並び替えアイテムをDOMから削除し、インデックスを再採番します。
 * 
 * @param {HTMLElement} btn - 削除ボタン要素
 */
export function removeImageOrderItem(btn) {
    const item = btn.closest('.image-order-item');
    if (item) {
        item.remove();
        updateImageOrderIndexes();
    }
}

function updateImageItemOrder(item, index) {
    const numEl = item.querySelector('.order-number');
    if (numEl) numEl.textContent = String(index + 1);

    const inputEl = item.querySelector('.order-input');
    if (inputEl) inputEl.value = String(index);
}

/**
 * 画像の順序インデックスの更新
 * コンテナ内の画像アイテムの表示番号および隠しフィールドの値を順番に沿って再計算・更新します。
 */
export function updateImageOrderIndexes() {
    document.querySelectorAll('#imageOrderContainer .image-order-item').forEach(updateImageItemOrder);
}

function handleImageOrderClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const moveImageBtn = target.closest('[data-move-image]');
    if (moveImageBtn) {
        const direction = moveImageBtn.getAttribute('data-move-image');
        moveImageOrder(moveImageBtn, direction);
    }
}

export function initImageOrder() {
    document.addEventListener('click', handleImageOrderClick);
}
