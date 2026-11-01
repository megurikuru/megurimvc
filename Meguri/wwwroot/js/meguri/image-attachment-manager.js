/**
 * 添付画像管理＆プレビューコンポーネント (最大16枚)
 */
export class ImageAttachmentManager {
    static DEFAULT_MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

    constructor() {
        this.selectedFiles = [];
        this.objectUrls = [];
        this.maxImages = 16;
        this.maxFileSize = ImageAttachmentManager.DEFAULT_MAX_FILE_SIZE;
        this.inputEl = null;
        this.previewSelector = '#imagePreviewContainer';
        this.countSelector = '#imageCountDisplay';
        this._changeHandler = null;
        this._existingChangeHandler = null;
        this._submitHandler = null;
    }

    /**
     * 画像添付マネージャーのインスタンスを作成して初期化します。
     * 
     * @param {string|HTMLInputElement} inputSelector - ファイル入力要素またはセレクター
     * @param {string} previewSelector - プレビュー表示コンテナのセレクター
     * @param {string} countSelector - 画像数表示要素のセレクター
     * @param {number} maxImages - 最大添付枚数
     * @param {number} [maxFileSize] - 1ファイルあたりの最大バイト数
     * @returns {ImageAttachmentManager} 生成されたインスタンス
     */
    static createInstance(inputSelector, previewSelector, countSelector, maxImages, maxFileSize) {
        const instance = new ImageAttachmentManager();
        instance.init(inputSelector, previewSelector, countSelector, maxImages, maxFileSize);
        return instance;
    }

    /**
     * インスタンスの内部プロパティとイベントリスナーの初期化を行います。
     */
    init(inputSelector, previewSelector, countSelector, maxImages, maxFileSize) {
        const input = typeof inputSelector === 'string'
            ? document.querySelector(inputSelector)
            : inputSelector;
        if (!input) return;

        this.inputEl = input;
        this.previewSelector = previewSelector || '#imagePreviewContainer';
        this.countSelector = countSelector !== undefined ? countSelector : '#imageCountDisplay';
        this.maxImages = maxImages || 16;
        this.maxFileSize = maxFileSize || Number(input.dataset.maxFileSize) || ImageAttachmentManager.DEFAULT_MAX_FILE_SIZE;
        this.selectedFiles = [];
        this.objectUrls = this.objectUrls || [];
        this.clearObjectUrls();

        const self = this;
        if (this._changeHandler) {
            this.inputEl.removeEventListener('change', this._changeHandler);
        }

        function isSameFile(existingFile, incomingFile) {
            return existingFile.name === incomingFile.name &&
                existingFile.size === incomingFile.size &&
                existingFile.lastModified === incomingFile.lastModified;
        }

        function handleInputChange() {
            const incoming = Array.from(self.inputEl.files || []);
            if (incoming.length === 0) {
                self.syncInputFiles();
                self.validateFiles();
                self.render();
                return;
            }

            if (self.maxImages === 1) {
                self.selectedFiles = [incoming[0]];
                self.syncInputFiles();
                self.validateFiles();
                self.render();
                return;
            }

            let exceeded = false;
            for (const file of incoming) {
                function checkDuplicate(existingFile) {
                    return isSameFile(existingFile, file);
                }
                const isDuplicate = self.selectedFiles.some(checkDuplicate);
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
            self.validateFiles();
            self.render();
        }

        this._changeHandler = handleInputChange;
        this.inputEl.addEventListener('change', this._changeHandler);

        const form = this.inputEl.form || this.inputEl.closest('form');
        if (form) {
            if (this._submitHandler) {
                form.removeEventListener('submit', this._submitHandler, true);
            }

            function handleFormSubmit(e) {
                if (!self.validateFiles()) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    const details = self.inputEl.closest('details');
                    if (details) {
                        details.open = true;
                    }
                    const valMsgEl = self.getValidationMessageElement();
                    if (valMsgEl && typeof valMsgEl.scrollIntoView === 'function') {
                        valMsgEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
            }

            this._submitHandler = handleFormSubmit;
            form.addEventListener('submit', this._submitHandler, true);
        }

        if (this.maxImages > 1) {
            if (this._existingChangeHandler) {
                document.removeEventListener('change', this._existingChangeHandler);
            }

            function handleExistingImagesChange(e) {
                const target = e.target;
                if (!target || !target.matches('input[name^="ExistingImages"][name$=".Remove"]')) return;

                if (!target.checked && self.getExistingActiveCount() + self.selectedFiles.length > self.maxImages) {
                    target.checked = true;
                    self.alertMaxImages();
                }
                self.render();
            }

            this._existingChangeHandler = handleExistingImagesChange;
            document.addEventListener('change', this._existingChangeHandler);
        }

        this.render();
    }

    /**
     * バリデーションエラーメッセージを表示する要素を取得（存在しない場合は生成）します。
     * 
     * @returns {HTMLElement|null} バリデーションメッセージ表示要素
     */
    getValidationMessageElement() {
        if (!this.inputEl) return null;
        const form = this.inputEl.form || this.inputEl.closest('form') || document;
        const inputName = this.inputEl.name;
        if (inputName) {
            const candidates = form.querySelectorAll('[data-valmsg-for]');
            for (const el of candidates) {
                if (el.getAttribute('data-valmsg-for') === inputName ||
                    el.getAttribute('data-valmsg-for')?.toLowerCase() === inputName.toLowerCase()) {
                    return el;
                }
            }
        }

        let fallback = this.inputEl.parentElement?.querySelector('.image-size-validation-message');
        if (!fallback && this.inputEl.parentElement) {
            fallback = document.createElement('span');
            fallback.className = 'field-validation-valid image-size-validation-message';
            if (inputName) {
                fallback.setAttribute('data-valmsg-for', inputName);
            }
            fallback.setAttribute('data-valmsg-replace', 'true');
            this.inputEl.insertAdjacentElement('afterend', fallback);
        }
        return fallback || null;
    }

    /**
     * サイズ超過ファイルに対するローカライズ済みエラーメッセージを生成します。
     * 
     * @param {File} file - 対象ファイル
     * @returns {string} エラーメッセージ
     */
    formatMaxSizeError(file) {
        const fmt = (window.MeguriI18n && window.MeguriI18n.maxImageSizeErrorFormat)
            || '画像サイズが上限の5MBを超えています (現在: {0:F2}MB)。';
        const sizeMb = (file.size / (1024 * 1024)).toFixed(2);
        const detail = fmt.replace(/\{0(?::[^}]*)?\}/g, sizeMb);
        return `${file.name}: ${detail}`;
    }

    /**
     * 選択されている画像ファイルのサイズを検証し、画面のエラー表示を更新します。
     * 
     * @returns {boolean} すべてのファイルが制限内の場合は true、超過ファイルがある場合は false
     */
    validateFiles() {
        const filesToCheck = this.selectedFiles.length > 0
            ? this.selectedFiles
            : Array.from(this.inputEl?.files || []);

        const errors = [];
        for (const file of filesToCheck) {
            if (file && file.size > this.maxFileSize) {
                errors.push(this.formatMaxSizeError(file));
            }
        }

        const valMsgEl = this.getValidationMessageElement();
        if (valMsgEl) {
            if (errors.length > 0) {
                valMsgEl.textContent = errors.join(' ');
                valMsgEl.classList.remove('field-validation-valid');
                valMsgEl.classList.add('field-validation-error');
                valMsgEl.setAttribute('role', 'alert');
                this.inputEl?.classList.add('input-validation-error');
            } else {
                valMsgEl.textContent = '';
                valMsgEl.classList.remove('field-validation-error');
                valMsgEl.classList.add('field-validation-valid');
                valMsgEl.removeAttribute('role');
                this.inputEl?.classList.remove('input-validation-error');
            }
        }

        return errors.length === 0;
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
        this.validateFiles();
        this.render();
    }

    /**
     * 選択されたファイル一覧をファイル入力要素（input[type="file"]）の files プロパティに同期します。
     */
    syncInputFiles() {
        if (!this.inputEl || typeof DataTransfer === 'undefined') return;
        const dt = new DataTransfer();
        function addFileToDataTransfer(file) {
            dt.items.add(file);
        }
        this.selectedFiles.forEach(addFileToDataTransfer);
        this.inputEl.files = dt.files;
        if (this.selectedFiles.length === 0) {
            this.inputEl.value = '';
        }
    }

    /**
     * 生成したオブジェクトURLを解放し、メモリリークを防ぎます。
     */
    clearObjectUrls() {
        function revokeUrl(url) {
            URL.revokeObjectURL(url);
        }
        if (this.objectUrls && this.objectUrls.length > 0) {
            this.objectUrls.forEach(revokeUrl);
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

        const self = this;
        const existingActive = this.getExistingActiveCount();
        const totalCount = existingActive + this.selectedFiles.length;
        if (this.countSelector) {
            const countEl = document.querySelector(this.countSelector);
            if (countEl) {
                countEl.textContent = `${totalCount}/${this.maxImages}`;
            }
        }

        const removeLabel = (window.MeguriI18n && window.MeguriI18n.removeLabel) || '削除';

        function renderPreviewItem(file, idx) {
            const url = URL.createObjectURL(file);
            self.objectUrls.push(url);

            const item = document.createElement('div');
            item.className = 'image-preview-item';

            if (self.maxImages > 1) {
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

            function handleRemoveImageClick() {
                self.removeFile(idx);
            }

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'image-preview-remove';
            removeBtn.textContent = '削除';
            removeBtn.setAttribute('aria-label', removeLabel);
            removeBtn.title = removeLabel;
            removeBtn.addEventListener('click', handleRemoveImageClick);

            item.append(img, nameSpan, removeBtn);
            container.appendChild(item);
        }

        this.selectedFiles.forEach(renderPreviewItem);
    }
}

function initImageAttachmentInput(input) {
    const previewSelector = input.dataset.imagePreview;
    const countSelector = input.dataset.imageCount || null;
    const maxImages = Number(input.dataset.maxImages) || 16;
    ImageAttachmentManager.createInstance(input, previewSelector, countSelector, maxImages);
}

export function initImageAttachments() {
    document.querySelectorAll('input[type="file"][data-image-preview]').forEach(initImageAttachmentInput);
}
