// SNS Meguri 共通 JavaScript (ES Modules / Class Syntax)

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
        this.selectedTags = this.selectedTags.filter(t => t !== tagText);
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
        this.selectedTags.forEach((tag, idx) => {
            const badge = document.createElement('span');
            badge.appendChild(document.createTextNode(`#${tag} `));

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.setAttribute('aria-label', removeLabel);
            removeBtn.textContent = '×';
            removeBtn.addEventListener('click', () => this.removeTag(tag));

            badge.appendChild(removeBtn);
            container.appendChild(badge);

            const hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = `Tags[${idx}]`;
            hiddenInput.value = tag;
            hiddenInputsContainer.appendChild(hiddenInput);
        });

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
        input.addEventListener('input', function () {
            clearTimeout(timeout);
            const query = this.value.trim();
            if (!query) {
                suggestions.innerHTML = '';
                suggestions.style.display = 'none';
                return;
            }
            timeout = setTimeout(async () => {
                try {
                    const res = await fetch('/Tag/Search?q=' + encodeURIComponent(query), {
                        headers: { 'Accept': 'application/json' },
                        credentials: 'same-origin'
                    });
                    if (!res.ok) return;
                    const data = await res.json();
                    suggestions.innerHTML = '';
                    if (data && data.length > 0) {
                        data.forEach(item => {
                            const opt = document.createElement('button');
                            opt.type = 'button';
                            opt.textContent = item.text;
                            opt.addEventListener('click', function () {
                                TagManager.addTag(item.text);
                            });
                            suggestions.appendChild(opt);
                        });
                        suggestions.style.display = '';
                    } else {
                        suggestions.style.display = 'none';
                    }
                } catch (err) {
                    console.error('Tag search error:', err);
                }
            }, 250);
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                TagManager.addTag(this.value);
            }
        });
    }
}

/**
 * 添付画像管理＆プレビューコンポーネント (最大16枚)
 */
export class ImageAttachmentManager {
    constructor() {
        this.selectedFiles = [];
        this.objectUrls = [];
        this.maxImages = 16;
        this.inputEl = null;
        this.previewSelector = '#imagePreviewContainer';
        this.countSelector = '#imageCountDisplay';
        this._changeHandler = null;
        this._existingChangeHandler = null;
    }

    /**
     * 画像添付マネージャーのインスタンスを作成して初期化します。
     * 
     * @param {string|HTMLInputElement} inputSelector - ファイル入力要素またはセレクター
     * @param {string} previewSelector - プレビュー表示コンテナのセレクター
     * @param {string} countSelector - 画像数表示要素のセレクター
     * @param {number} maxImages - 最大添付枚数
     * @returns {ImageAttachmentManager} 生成されたインスタンス
     */
    static createInstance(inputSelector, previewSelector, countSelector, maxImages) {
        const instance = new ImageAttachmentManager();
        instance.init(inputSelector, previewSelector, countSelector, maxImages);
        return instance;
    }

    /**
     * インスタンスの内部プロパティとイベントリスナーの初期化を行います。
     */
    init(inputSelector, previewSelector, countSelector, maxImages) {
        const input = typeof inputSelector === 'string'
            ? document.querySelector(inputSelector)
            : inputSelector;
        if (!input) return;

        this.inputEl = input;
        this.previewSelector = previewSelector || '#imagePreviewContainer';
        this.countSelector = countSelector !== undefined ? countSelector : '#imageCountDisplay';
        this.maxImages = maxImages || 16;
        this.selectedFiles = [];
        this.objectUrls = this.objectUrls || [];
        this.clearObjectUrls();

        const self = this;
        if (this._changeHandler) {
            this.inputEl.removeEventListener('change', this._changeHandler);
        }
        this._changeHandler = function () {
            const incoming = Array.from(self.inputEl.files || []);
            if (incoming.length === 0) {
                self.syncInputFiles();
                self.render();
                return;
            }

            if (self.maxImages === 1) {
                self.selectedFiles = [incoming[0]];
                self.syncInputFiles();
                self.render();
                return;
            }

            let exceeded = false;
            for (const file of incoming) {
                const isDuplicate = self.selectedFiles.some(f =>
                    f.name === file.name &&
                    f.size === file.size &&
                    f.lastModified === file.lastModified
                );
                if (isDuplicate) continue;

                if (self.getExistingActiveCount() + self.selectedFiles.length >= self.maxImages) {
                    exceeded = true;
                    break;
                }
                self.selectedFiles.push(file);
            }

            if (exceeded) {
                self.alertMaxImages();
            }

            self.syncInputFiles();
            self.render();
        };
        this.inputEl.addEventListener('change', this._changeHandler);

        if (this.maxImages > 1) {
            if (this._existingChangeHandler) {
                document.removeEventListener('change', this._existingChangeHandler);
            }
            this._existingChangeHandler = function (e) {
                const target = e.target;
                if (!target || !target.matches('input[name^="ExistingImages"][name$=".Remove"]')) return;

                if (!target.checked && self.getExistingActiveCount() + self.selectedFiles.length > self.maxImages) {
                    target.checked = true;
                    self.alertMaxImages();
                }
                self.render();
            };
            document.addEventListener('change', this._existingChangeHandler);
        }

        this.render();
    }

    /**
     * 既存の画像のうち、削除されていない有効な枚数を取得します。
     * 
     * @returns {number} 有効な既存画像の数
     */
    getExistingActiveCount() {
        if (this.maxImages === 1) return 0;
        const totalExisting = document.querySelectorAll('input[name^="ExistingImages"][name$=".ImageId"]').length;
        const removedExisting = document.querySelectorAll('input[name^="ExistingImages"][name$=".Remove"]:checked').length;
        return Math.max(0, totalExisting - removedExisting);
    }

    /**
     * 最大枚数制限を超過した際の警告アラートを表示します。
     */
    alertMaxImages() {
        const fmt = (window.MeguriI18n && window.MeguriI18n.maxImagesAlertFormat) || '画像は最大{0}枚まで添付できます。';
        alert(fmt.replace('{0}', this.maxImages));
    }

    /**
     * 選択されたファイル配列から指定インデックスのファイルを削除します。
     * 
     * @param {number} index - 削除するファイルのインデックス
     */
    removeFile(index) {
        if (index < 0 || index >= this.selectedFiles.length) return;
        this.selectedFiles.splice(index, 1);
        this.syncInputFiles();
        this.render();
    }

    /**
     * 選択されたファイル一覧をファイル入力要素（input[type="file"]）の files プロパティに同期します。
     */
    syncInputFiles() {
        if (!this.inputEl || typeof DataTransfer === 'undefined') return;
        const dt = new DataTransfer();
        this.selectedFiles.forEach(file => dt.items.add(file));
        this.inputEl.files = dt.files;
        if (this.selectedFiles.length === 0) {
            this.inputEl.value = '';
        }
    }

    /**
     * 生成したオブジェクトURLを解放し、メモリリークを防ぎます。
     */
    clearObjectUrls() {
        if (this.objectUrls && this.objectUrls.length > 0) {
            this.objectUrls.forEach(url => URL.revokeObjectURL(url));
        }
        this.objectUrls = [];
    }

    /**
     * 添付画像のプレビューとカウント表示を再描画します。
     */
    render() {
        this.clearObjectUrls();
        const container = document.querySelector(this.previewSelector);
        if (!container) return;
        container.innerHTML = '';

        const existingActive = this.getExistingActiveCount();
        const totalCount = existingActive + this.selectedFiles.length;
        if (this.countSelector) {
            const countEl = document.querySelector(this.countSelector);
            if (countEl) {
                countEl.textContent = `${totalCount}/${this.maxImages}`;
            }
        }

        const removeLabel = (window.MeguriI18n && window.MeguriI18n.removeLabel) || '削除';

        this.selectedFiles.forEach((file, idx) => {
            const url = URL.createObjectURL(file);
            this.objectUrls.push(url);

            const item = document.createElement('div');
            item.className = 'image-preview-item';

            if (this.maxImages > 1) {
                const header = document.createElement('div');
                header.className = 'image-preview-header';

                const orderSpan = document.createElement('span');
                orderSpan.className = 'image-preview-order';
                orderSpan.textContent = `#${existingActive + idx + 1}`;

                header.appendChild(orderSpan);
                item.appendChild(header);
            }

            const img = document.createElement('img');
            img.src = url;
            img.alt = file.name;

            const nameSpan = document.createElement('span');
            nameSpan.className = 'image-preview-name';
            nameSpan.textContent = file.name;
            nameSpan.title = file.name;

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'image-preview-remove';
            removeBtn.textContent = '削除';
            removeBtn.setAttribute('aria-label', removeLabel);
            removeBtn.title = removeLabel;
            removeBtn.addEventListener('click', () => this.removeFile(idx));

            item.append(img, nameSpan, removeBtn);
            container.appendChild(item);
        });
    }
}

/**
 * SNS Meguri アプリケーション管理クラス
 */
export class MeguriApp {
    /**
     * リアクション送信処理 (Fetch API)
     * 指定されたターゲットに対してリアクションの追加・削除（トグル）を非同期で行います。
     * 
     * @param {string} targetType - ターゲットの種類
     * @param {number|string} targetId - ターゲットのID
     * @param {number|string} reactionType - リアクションの種類
     * @param {HTMLElement} btn - 操作されたボタン要素
     */
    static async toggleReaction(targetType, targetId, reactionType, btn) {
        const matchingButtons = Array.from(document.querySelectorAll('[data-reaction-target-type]')).filter(function (button) {
            return button.dataset.reactionTargetType === targetType
                && button.dataset.reactionTargetId === String(targetId)
                && button.dataset.reactionType === String(reactionType);
        });
        if (matchingButtons.some(function (button) { return button.disabled; })) return;
        matchingButtons.forEach(function (button) { button.disabled = true; });
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
                matchingButtons.forEach(function (button) {
                    const countSpan = button.querySelector('.reaction-count');
                    if (countSpan) countSpan.textContent = res.count;
                    button.classList.toggle('reacted', Boolean(res.hasReacted));
                    button.setAttribute('aria-pressed', String(Boolean(res.hasReacted)));
                    if (button.closest('.message-reaction-summary')) button.hidden = Number(res.count) === 0;
                });
            }
        } catch (err) {
            console.error('Reaction error:', err);
        } finally {
            matchingButtons.forEach(function (button) { button.disabled = false; });
        }
    }

    /**
     * 画像の並び替え (上へ / 下へ)
     * 指定された画像アイテムをプレビューリスト内で「上」または「下」に移動させます。
     * 
     * @param {HTMLElement} btn - 並び替えボタン要素
     * @param {string} direction - 移動方向 ('up' または 'down')
     */
    static moveImageOrder(btn, direction) {
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
        this.updateImageOrderIndexes();
    }

    /**
     * 画像並び替えアイテムの削除
     * 指定された画像並び替えアイテムをDOMから削除し、インデックスを再採番します。
     * 
     * @param {HTMLElement} btn - 削除ボタン要素
     */
    static removeImageOrderItem(btn) {
        const item = btn.closest('.image-order-item');
        if (item) {
            item.remove();
            this.updateImageOrderIndexes();
        }
    }

    /**
     * 画像の順序インデックスの更新
     * コンテナ内の画像アイテムの表示番号および隠しフィールドの値を順番に沿って再計算・更新します。
     */
    static updateImageOrderIndexes() {
        document.querySelectorAll('#imageOrderContainer .image-order-item').forEach(function (item, index) {
            const numEl = item.querySelector('.order-number');
            if (numEl) numEl.textContent = String(index + 1);

            const inputEl = item.querySelector('.order-input');
            if (inputEl) inputEl.value = String(index);
        });
    }

    /**
     * HTML特殊文字のエスケープ処理
     * 文字列内のHTML特殊文字を安全な実体参照に変換します。
     * 
     * @param {string} str - エスケープ対象の文字列
     * @returns {string} エスケープ済みの文字列
     */
    static escapeHtml(str) {
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
    static replyToComment(commentId, commentNumber, userName) {
        const parentInput = document.getElementById('replyParentId');
        if (parentInput) {
            parentInput.value = commentId;
        }

        const fmt = (window.MeguriI18n && window.MeguriI18n.replyNoticeFormat) || '<strong>>>{0} ({1})</strong>';
        const cancelLabel = (window.MeguriI18n && window.MeguriI18n.cancelLabel) || 'Cancel';
        const safeUser = this.escapeHtml(userName || '');
        const noticeText = fmt.replace('{0}', commentNumber).replace('{1}', safeUser);

        const replyNotice = document.getElementById('replyNotice');
        if (replyNotice) {
            replyNotice.innerHTML = `${noticeText} <button type="button" class="btn-sm" aria-label="${this.escapeHtml(cancelLabel)}" data-cancel-reply>×</button>`;
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
    static cancelReply() {
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

    /**
     * ページ内のコンポーネント初期化処理
     * data-* 属性などを基に、タグや画像プレビューなどのコンポーネントを初期化します。
     */
    static initComponents() {
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

        document.querySelectorAll('input[type="file"][data-image-preview]').forEach(function (input) {
            const previewSelector = input.dataset.imagePreview;
            const countSelector = input.dataset.imageCount || null;
            const maxImages = Number(input.dataset.maxImages) || 16;
            ImageAttachmentManager.createInstance(input, previewSelector, countSelector, maxImages);
        });
    }

    /**
     * 共通イベントハンドラーの登録 (イベントデリゲーション)
     * フォーム送信時の確認ダイアログや、各種ボタン・要素のクリック/変更イベントを一括管理します。
     */
    static setupCommonEventHandlers() {
        const self = this;

        // 確認ダイアログ付きフォーム送信
        document.addEventListener('submit', function (e) {
            const form = e.target.closest('form[data-confirm]');
            if (!form) return;
            const msg = form.getAttribute('data-confirm');
            if (msg && !window.confirm(msg)) {
                e.preventDefault();
            }
        });

        // change イベント委譲
        document.addEventListener('change', function (e) {
            const target = e.target;
            if (!target) return;

            // 選択変更時にフォームを自動送信
            const autoSubmitSelect = target.closest('select[data-auto-submit]');
            if (autoSubmitSelect && autoSubmitSelect.form) {
                autoSubmitSelect.form.submit();
                return;
            }

            // メッセージ新規作成のチャット種別切り替え（ダイレクト / グループ）
            if (target.matches('input[name="IsGroup"]')) {
                const container = document.getElementById('groupTitleContainer');
                if (container) {
                    container.hidden = target.value !== 'true';
                }
            }
        });

        // click イベント委譲
        document.addEventListener('click', function (e) {
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
                return;
            }

            // リアクションボタン
            const reactionBtn = target.closest('[data-reaction-target-type]');
            if (reactionBtn) {
                const targetType = reactionBtn.getAttribute('data-reaction-target-type');
                const targetId = Number(reactionBtn.getAttribute('data-reaction-target-id'));
                const reactionType = Number(reactionBtn.getAttribute('data-reaction-type'));
                self.toggleReaction(targetType, targetId, reactionType, reactionBtn);
                return;
            }

            // コメント返信ボタン
            const replyCommentBtn = target.closest('[data-reply-comment-id]');
            if (replyCommentBtn) {
                const commentId = replyCommentBtn.getAttribute('data-reply-comment-id');
                const commentNumber = replyCommentBtn.getAttribute('data-reply-comment-number');
                const userName = replyCommentBtn.getAttribute('data-reply-user');
                self.replyToComment(commentId, commentNumber, userName);
                return;
            }

            // コメント返信キャンセルボタン
            const cancelReplyBtn = target.closest('[data-cancel-reply]');
            if (cancelReplyBtn) {
                self.cancelReply();
                return;
            }

            // タグ追加ボタン
            const addTagBtn = target.closest('[data-add-tag]');
            if (addTagBtn) {
                const tagInput = document.getElementById('tagInput');
                TagManager.addTag(tagInput ? tagInput.value : '');
                return;
            }

            // 画像順序の並び替えボタン
            const moveImageBtn = target.closest('[data-move-image]');
            if (moveImageBtn) {
                const direction = moveImageBtn.getAttribute('data-move-image');
                self.moveImageOrder(moveImageBtn, direction);
                return;
            }

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
        });
    }

    /**
     * 日時要素（<time>）のローカライズ処理
     * UTCで記述された日時を、ブラウザの言語設定やタイムゾーンに合わせてフォーマット・表示を変換します。
     */
    static localizeTimes() {
        let previousChatDate = null;
        document.querySelectorAll('time.local-time').forEach(function (el) {
            const d = new Date(el.getAttribute('datetime'));
            if (isNaN(d)) return;
            if (el.dataset.format === 'chat') {
                const item = el.closest('.message-item');
                if (item && previousChatDate !== d.toDateString()) {
                    const divider = document.createElement('div');
                    divider.className = 'message-date-divider';
                    divider.textContent = d.toLocaleDateString(navigator.language, { year: 'numeric', month: 'long', day: 'numeric' });
                    item.before(divider);
                    previousChatDate = d.toDateString();
                }
                el.textContent = d.toLocaleTimeString(navigator.language, { hour: '2-digit', minute: '2-digit' });
                el.title = d.toLocaleString(navigator.language);
                el.setAttribute('aria-label', el.title);
                return;
            }
            const opts = el.dataset.format === 'date'
                ? { year: 'numeric', month: '2-digit', day: '2-digit' }
                : { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' };
            el.textContent = d.toLocaleString(navigator.language, opts);
            el.title = d.toISOString();
        });
    }

    /**
     * アプリケーション全体の初期化を実行します。
     */
    static init() {
        this.localizeTimes();
        this.initComponents();
        this.setupCommonEventHandlers();
    }
}

// DOM読み込み完了時に初期化処理を実行
document.addEventListener('DOMContentLoaded', () => {
    MeguriApp.init();
});