// SNS Meguri 共通 JavaScript
(function () {
    'use strict';

    // リアクション送信 (Fetch API)
    async function toggleReaction(targetType, targetId, reactionType, btn) {
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
                const countSpan = btn.querySelector('.reaction-count');
                if (countSpan) {
                    countSpan.textContent = res.count;
                }
                btn.classList.toggle('reacted', Boolean(res.hasReacted));
            }
        } catch (err) {
            console.error('Reaction error:', err);
        }
    }

    // 画像の並び替え (上へ / 下へ)
    function moveImageOrder(btn, direction) {
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

    function removeImageOrderItem(btn) {
        const item = btn.closest('.image-order-item');
        if (item) {
            item.remove();
            updateImageOrderIndexes();
        }
    }

    function updateImageOrderIndexes() {
        document.querySelectorAll('#imageOrderContainer .image-order-item').forEach(function (item, index) {
            const numEl = item.querySelector('.order-number');
            if (numEl) numEl.textContent = String(index + 1);

            const inputEl = item.querySelector('.order-input');
            if (inputEl) inputEl.value = String(index);
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
        },

        removeTag: function (tagText) {
            this.selectedTags = this.selectedTags.filter(t => t !== tagText);
            this.render();
        },

        render: function () {
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
        },

        setupAutocomplete: function () {
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
    };

    // 添付画像管理＆プレビューコンポーネント (最大16枚)
    const ImageAttachmentManager = {
        selectedFiles: [],
        objectUrls: [],
        maxImages: 16,
        inputEl: null,
        previewSelector: '#imagePreviewContainer',
        countSelector: '#imageCountDisplay',

        createInstance: function (inputSelector, previewSelector, countSelector, maxImages) {
            const instance = Object.create(ImageAttachmentManager);
            instance.init(inputSelector, previewSelector, countSelector, maxImages);
            return instance;
        },

        init: function (inputSelector, previewSelector, countSelector, maxImages) {
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
        },

        getExistingActiveCount: function () {
            if (this.maxImages === 1) return 0;
            const totalExisting = document.querySelectorAll('input[name^="ExistingImages"][name$=".ImageId"]').length;
            const removedExisting = document.querySelectorAll('input[name^="ExistingImages"][name$=".Remove"]:checked').length;
            return Math.max(0, totalExisting - removedExisting);
        },

        alertMaxImages: function () {
            const fmt = (window.MeguriI18n && window.MeguriI18n.maxImagesAlertFormat) || '画像は最大{0}枚まで添付できます。';
            alert(fmt.replace('{0}', this.maxImages));
        },

        removeFile: function (index) {
            if (index < 0 || index >= this.selectedFiles.length) return;
            this.selectedFiles.splice(index, 1);
            this.syncInputFiles();
            this.render();
        },

        syncInputFiles: function () {
            if (!this.inputEl || typeof DataTransfer === 'undefined') return;
            const dt = new DataTransfer();
            this.selectedFiles.forEach(file => dt.items.add(file));
            this.inputEl.files = dt.files;
            if (this.selectedFiles.length === 0) {
                this.inputEl.value = '';
            }
        },

        clearObjectUrls: function () {
            if (this.objectUrls && this.objectUrls.length > 0) {
                this.objectUrls.forEach(url => URL.revokeObjectURL(url));
            }
            this.objectUrls = [];
        },

        render: function () {
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
    };

    // コメント返信機能
    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function replyToComment(commentId, commentNumber, userName) {
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

    function cancelReply() {
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

    // ページ内のコンポーネント初期化 (data-* 属性ベース)
    function initComponents() {
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

    // 共通イベントハンドラーの登録 (Event Delegation)
    function setupCommonEventHandlers() {
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
                toggleReaction(targetType, targetId, reactionType, reactionBtn);
                return;
            }

            // コメント返信ボタン
            const replyCommentBtn = target.closest('[data-reply-comment-id]');
            if (replyCommentBtn) {
                const commentId = replyCommentBtn.getAttribute('data-reply-comment-id');
                const commentNumber = replyCommentBtn.getAttribute('data-reply-comment-number');
                const userName = replyCommentBtn.getAttribute('data-reply-user');
                replyToComment(commentId, commentNumber, userName);
                return;
            }

            // コメント返信キャンセルボタン
            const cancelReplyBtn = target.closest('[data-cancel-reply]');
            if (cancelReplyBtn) {
                cancelReply();
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
                moveImageOrder(moveImageBtn, direction);
                return;
            }

            // メッセージ返信ボタン
            const replyMsgBtn = target.closest('[data-reply-id]');
            if (replyMsgBtn) {
                const parentInput = document.getElementById('parentMessageId');
                if (parentInput) {
                    parentInput.value = replyMsgBtn.dataset.replyId || '';
                }
                const subjectInput = document.getElementById('messageSubject');
                if (subjectInput) {
                    const s = replyMsgBtn.dataset.replySubject || '';
                    subjectInput.value = s.startsWith('Re: ') ? s : 'Re: ' + s;
                }
                const textInput = document.getElementById('messageText');
                if (textInput) {
                    textInput.focus();
                }
            }
        });
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

    document.addEventListener('DOMContentLoaded', function () {
        localizeTimes();
        initComponents();
        setupCommonEventHandlers();
    });
})();
