/**
 * タグ管理コンポーネント (最大10個)
 */
export class TagManager {
    static selectedTags = [];
    static maxTags = 10;

    /**
     * タグ管理コンポーネントの初期化
     * 
     * @param {Array<string>} initialTags - 初期タグの配列
     */
    static init(initialTags) {
        if (initialTags && Array.isArray(initialTags)) {
            this.selectedTags = [...initialTags];
        }
        this.render();
        this.setupAutocomplete();
    }

    /**
     * タグの追加
     * 指定されたテキストのタグを追加します（最大数チェック、重複チェックあり）。
     * 
     * @param {string} tagText - 追加するタグのテキスト
     */
    static addTag(tagText) {
        tagText = (tagText || '').trim();
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
        const tagInput = document.getElementById('tagInput');
        if (tagInput) tagInput.value = '';

        const suggestions = document.getElementById('tagSuggestions');
        if (suggestions) {
            suggestions.innerHTML = '';
            suggestions.style.display = 'none';
        }
    }

    /**
     * タグの削除
     * 選択されたタグの中から指定されたタグを除外します。
     * 
     * @param {string} tagText - 削除するタグのテキスト
     */
    static removeTag(tagText) {
        function isNotMatchingTag(t) {
            return t !== tagText;
        }

        this.selectedTags = this.selectedTags.filter(isNotMatchingTag);
        this.render();
    }

    /**
     * タグの画面描画
     * 選択されたタグのバッジ、非同期送信用の隠しフィールド、件数表示を更新します。
     */
    static render() {
        const container = document.getElementById('selectedTagsContainer');
        const hiddenInputsContainer = document.getElementById('hiddenTagsInputs');
        if (!container || !hiddenInputsContainer) return;

        container.innerHTML = '';
        hiddenInputsContainer.innerHTML = '';

        const removeLabel = (window.MeguriI18n && window.MeguriI18n.removeLabel) || 'Remove';

        function renderTagBadge(tag, idx) {
            const badge = document.createElement('span');
            badge.appendChild(document.createTextNode(`#${tag} `));

            function handleRemoveTagClick() {
                TagManager.removeTag(tag);
            }

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.setAttribute('aria-label', removeLabel);
            removeBtn.textContent = '×';
            removeBtn.addEventListener('click', handleRemoveTagClick);

            badge.appendChild(removeBtn);
            container.appendChild(badge);

            const hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = `Tags[${idx}]`;
            hiddenInput.value = tag;
            hiddenInputsContainer.appendChild(hiddenInput);
        }

        this.selectedTags.forEach(renderTagBadge);

        const countDisplay = document.getElementById('tagCountDisplay');
        if (countDisplay) {
            countDisplay.textContent = `${this.selectedTags.length}/${this.maxTags}`;
        }
    }

    /**
     * タグ入力時のオートコンプリート機能のセットアップ
     * 入力文字に応じて候補を非同期で取得し、サジェスト表示を行います。
     */
    static setupAutocomplete() {
        const input = document.getElementById('tagInput');
        const suggestions = document.getElementById('tagSuggestions');
        if (!input || !suggestions) return;

        let timeout = null;

        function renderSuggestionItem(item) {
            function handleSuggestionClick() {
                TagManager.addTag(item.text);
            }

            const opt = document.createElement('button');
            opt.type = 'button';
            opt.textContent = item.text;
            opt.addEventListener('click', handleSuggestionClick);
            suggestions.appendChild(opt);
        }

        async function fetchTagSuggestions(query) {
            try {
                const res = await fetch('/Tag/Search?q=' + encodeURIComponent(query), {
                    headers: { 'Accept': 'application/json' },
                    credentials: 'same-origin'
                });
                if (!res.ok) return;
                const data = await res.json();
                suggestions.innerHTML = '';
                if (data && data.length > 0) {
                    data.forEach(renderSuggestionItem);
                    suggestions.style.display = '';
                } else {
                    suggestions.style.display = 'none';
                }
            } catch (err) {
                console.error('Tag search error:', err);
            }
        }

        function handleTagInput() {
            clearTimeout(timeout);
            const query = input.value.trim();
            if (!query) {
                suggestions.innerHTML = '';
                suggestions.style.display = 'none';
                return;
            }
            function triggerFetch() {
                fetchTagSuggestions(query);
            }
            timeout = setTimeout(triggerFetch, 250);
        }

        function handleTagKeydown(e) {
            if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                TagManager.addTag(input.value);
            }
        }

        input.addEventListener('input', handleTagInput);
        input.addEventListener('keydown', handleTagKeydown);
    }
}

function handleTagManagerClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const addTagBtn = target.closest('[data-add-tag]');
    if (addTagBtn) {
        const input = document.getElementById('tagInput');
        TagManager.addTag(input ? input.value : '');
    }
}

export function initTagManager() {
    const tagInput = document.getElementById('tagInput');
    if (tagInput) {
        let initialTags = [];
        if (tagInput.dataset.initialTags) {
            try {
                initialTags = JSON.parse(tagInput.dataset.initialTags);
            } catch (e) {
                console.error('Failed to parse initial tags:', e);
            }
        }
        TagManager.init(initialTags);
    }

    document.addEventListener('click', handleTagManagerClick);
}
