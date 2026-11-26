/**
 * GitHub互換 Markdown エディター（書式ツールバー・サーバープレビュー・自分の画像挿入ピッカー）
 */

function getAntiForgeryToken(rootEl) {
    const form = rootEl.closest('form');
    const tokenInput = form?.querySelector('input[name="__RequestVerificationToken"]')
        || document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function escapeMarkdownAlt(text) {
    return (text || 'image')
        .replace(/[\r\n]+/g, ' ')
        .replace(/([\[\]\\])/g, '\\$1')
        .trim() || 'image';
}

function replaceSelection(textarea, replacement, selectStartOffset, selectEndOffset) {
    const start = textarea.selectionStart ?? textarea.value.length;
    const end = textarea.selectionEnd ?? textarea.value.length;
    const before = textarea.value.slice(0, start);
    const after = textarea.value.slice(end);

    textarea.value = before + replacement + after;
    const newStart = start + (selectStartOffset ?? replacement.length);
    const newEnd = start + (selectEndOffset ?? replacement.length);
    textarea.focus();
    textarea.setSelectionRange(newStart, newEnd);
    textarea.dispatchEvent(new Event('input', { bubbles: true }));
}

function wrapSelection(textarea, prefix, suffix, placeholder) {
    const start = textarea.selectionStart ?? 0;
    const end = textarea.selectionEnd ?? 0;
    const selected = textarea.value.slice(start, end);
    const content = selected || placeholder;
    const replacement = `${prefix}${content}${suffix}`;
    replaceSelection(textarea, replacement, prefix.length, prefix.length + content.length);
}

function prefixLines(textarea, prefixFactory, placeholder) {
    const start = textarea.selectionStart ?? 0;
    const end = textarea.selectionEnd ?? 0;
    const value = textarea.value;

    const lineStart = value.lastIndexOf('\n', Math.max(0, start - 1)) + 1;
    const nextNewline = value.indexOf('\n', end);
    const lineEnd = nextNewline === -1 ? value.length : nextNewline;

    const block = value.slice(lineStart, lineEnd);
    const lines = block.length > 0 ? block.split('\n') : [placeholder];
    const transformed = lines.map((line, idx) => `${prefixFactory(idx)}${line}`).join('\n');

    textarea.setSelectionRange(lineStart, lineEnd);
    replaceSelection(textarea, transformed, 0, transformed.length);
}

function insertBlock(textarea, blockText, cursorOffset) {
    const start = textarea.selectionStart ?? textarea.value.length;
    const before = textarea.value.slice(0, start);
    const needsLeadingNewline = before.length > 0 && !before.endsWith('\n');
    const prefix = needsLeadingNewline ? '\n\n' : '';
    const text = `${prefix}${blockText}`;
    const offset = cursorOffset != null ? prefix.length + cursorOffset : text.length;
    replaceSelection(textarea, text, offset, offset);
}

function applyToolbarAction(textarea, action) {
    switch (action) {
        case 'heading':
            prefixLines(textarea, () => '## ', '見出し');
            break;
        case 'bold':
            wrapSelection(textarea, '**', '**', '太字');
            break;
        case 'italic':
            wrapSelection(textarea, '*', '*', '斜体');
            break;
        case 'strikethrough':
            wrapSelection(textarea, '~~', '~~', '取り消し線');
            break;
        case 'quote':
            prefixLines(textarea, () => '> ', '引用テキスト');
            break;
        case 'code':
            wrapSelection(textarea, '`', '`', 'code');
            break;
        case 'codeblock': {
            const start = textarea.selectionStart ?? 0;
            const end = textarea.selectionEnd ?? 0;
            const selected = textarea.value.slice(start, end);
            if (selected) {
                const replacement = `\`\`\`\n${selected}\n\`\`\``;
                replaceSelection(textarea, replacement, 4, 4 + selected.length);
            } else {
                insertBlock(textarea, '```\n\n```', 4);
            }
            break;
        }
        case 'link': {
            const start = textarea.selectionStart ?? 0;
            const end = textarea.selectionEnd ?? 0;
            const selected = textarea.value.slice(start, end) || 'リンクテキスト';
            const url = 'https://';
            const replacement = `[${selected}](${url})`;
            const urlStart = selected.length + 3;
            replaceSelection(textarea, replacement, urlStart, urlStart + url.length);
            break;
        }
        case 'ul':
            prefixLines(textarea, () => '- ', '項目');
            break;
        case 'ol':
            prefixLines(textarea, (idx) => `${idx + 1}. `, '項目');
            break;
        case 'task':
            prefixLines(textarea, () => '- [ ] ', 'タスク');
            break;
        case 'table':
            insertBlock(
                textarea,
                '| 列1 | 列2 |\n| --- | --- |\n| 内容1 | 内容2 |',
                2
            );
            break;
        default:
            break;
    }
}

function setupEditor(rootEl) {
    const wrapperEl = rootEl.closest('[data-markdown-editor-root]');
    const toggleCheckbox = wrapperEl?.querySelector('[data-md-toggle]') || null;
    const headerEl = rootEl.querySelector('[data-md-header]');
    const hintEl = rootEl.querySelector('[data-md-hint]');
    const textarea = rootEl.querySelector('[data-md-textarea]');
    const writePanel = rootEl.querySelector('[data-md-panel="write"]');
    const previewPanel = rootEl.querySelector('[data-md-panel="preview"]');
    const previewContainer = rootEl.querySelector('[data-md-preview]');
    const toolbar = rootEl.querySelector('[data-md-toolbar]');
    const tabButtons = rootEl.querySelectorAll('[data-md-tab]');

    if (!textarea || !writePanel || !previewPanel || !previewContainer) {
        return;
    }

    function isMarkdownEnabled() {
        return !toggleCheckbox || toggleCheckbox.checked;
    }

    const previewUrl = rootEl.dataset.previewUrl || '/Post/PreviewMarkdown';
    const myImagesUrl = rootEl.dataset.myImagesUrl || '/Image/MyImages';
    const uploadImageUrl = rootEl.dataset.uploadImageUrl || '/Image/Upload';
    const msgPreviewEmpty = rootEl.dataset.msgPreviewEmpty || 'Nothing to preview.';
    const msgPreviewLoading = rootEl.dataset.msgPreviewLoading || 'Loading preview...';
    const msgPreviewError = rootEl.dataset.msgPreviewError || 'Failed to load preview.';
    const msgImagesLoading = rootEl.dataset.msgImagesLoading || 'Loading images...';
    const msgImagesEmpty = rootEl.dataset.msgImagesEmpty || 'No uploaded images.';
    const msgImagesError = rootEl.dataset.msgImagesError || 'Failed to load images.';
    const msgUploadLoading = rootEl.dataset.msgUploadLoading || 'Uploading...';
    const msgUploadError = rootEl.dataset.msgUploadError || 'Failed to upload image.';

    let previewSeq = 0;

    async function renderPreview() {
        const text = textarea.value;
        if (!text || !text.trim()) {
            previewContainer.innerHTML = '';
            const emptyP = document.createElement('p');
            emptyP.className = 'markdown-preview-message';
            emptyP.textContent = msgPreviewEmpty;
            previewContainer.appendChild(emptyP);
            return;
        }

        const currentSeq = ++previewSeq;
        previewContainer.innerHTML = '';
        const loadingP = document.createElement('p');
        loadingP.className = 'markdown-preview-message';
        loadingP.textContent = msgPreviewLoading;
        previewContainer.appendChild(loadingP);

        try {
            const response = await fetch(previewUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken(rootEl)
                },
                body: JSON.stringify({ markdown: text })
            });

            if (currentSeq !== previewSeq) return;
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();
            if (currentSeq !== previewSeq) return;

            if (data && typeof data.html === 'string' && data.html.trim().length > 0) {
                previewContainer.innerHTML = data.html;
            } else {
                previewContainer.innerHTML = '';
                const emptyP = document.createElement('p');
                emptyP.className = 'markdown-preview-message';
                emptyP.textContent = msgPreviewEmpty;
                previewContainer.appendChild(emptyP);
            }
        } catch {
            if (currentSeq !== previewSeq) return;
            previewContainer.innerHTML = '';
            const errP = document.createElement('p');
            errP.className = 'markdown-preview-message is-error';
            errP.textContent = msgPreviewError;
            previewContainer.appendChild(errP);
        }
    }

    function switchTab(targetTab) {
        const isPreview = targetTab === 'preview';
        tabButtons.forEach((btn) => {
            const active = btn.dataset.mdTab === targetTab;
            btn.classList.toggle('is-active', active);
            btn.setAttribute('aria-selected', active ? 'true' : 'false');
        });
        writePanel.hidden = isPreview;
        previewPanel.hidden = !isPreview;
        if (toolbar) {
            toolbar.hidden = isPreview;
        }
        if (isPreview) {
            renderPreview();
        } else {
            textarea.focus();
        }
    }

    tabButtons.forEach((btn) => {
        btn.addEventListener('click', () => {
            switchTab(btn.dataset.mdTab);
        });
    });

    // 画像ピッカーモーダル
    const pickerOverlay = rootEl.querySelector('[data-md-image-picker]');
    const pickerCloseBtn = rootEl.querySelector('[data-md-image-picker-close]');
    const pickerSearchInput = rootEl.querySelector('[data-md-image-search]');
    const pickerUploadInput = rootEl.querySelector('[data-md-image-upload-input]');
    const pickerUploadBtn = rootEl.querySelector('[data-md-image-upload-btn]');
    const pickerUploadStatus = rootEl.querySelector('[data-md-image-upload-status]');
    const pickerStatus = rootEl.querySelector('[data-md-image-status]');
    const pickerGrid = rootEl.querySelector('[data-md-image-grid]');
    const pickerMoreWrap = rootEl.querySelector('[data-md-image-more-wrap]');
    const pickerLoadMoreBtn = rootEl.querySelector('[data-md-image-load-more]');

    let savedSelection = { start: 0, end: 0 };
    let pickerSkip = 0;
    const pickerTake = 24;
    let pickerLoading = false;
    let pickerUploading = false;
    let pickerSearchTimer = null;
    let pickerRequestSeq = 0;

    function setUploadStatus(message, isError = false) {
        if (!pickerUploadStatus) return;
        if (!message) {
            pickerUploadStatus.hidden = true;
            pickerUploadStatus.textContent = '';
            pickerUploadStatus.classList.remove('is-error');
            return;
        }
        pickerUploadStatus.hidden = false;
        pickerUploadStatus.textContent = message;
        pickerUploadStatus.classList.toggle('is-error', isError);
    }

    async function uploadAndInsertImage(file) {
        if (!file || pickerUploading) return;
        pickerUploading = true;
        if (pickerUploadBtn) pickerUploadBtn.disabled = true;
        setUploadStatus(msgUploadLoading, false);

        try {
            const formData = new FormData();
            formData.append('file', file);

            const response = await fetch(uploadImageUrl, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken(rootEl),
                    'Accept': 'application/json'
                },
                body: formData
            });

            const data = await response.json().catch(() => null);
            if (!response.ok || !data || !data.url) {
                const errMsg = data?.error || msgUploadError;
                setUploadStatus(errMsg, true);
                return;
            }

            setUploadStatus('', false);
            insertPickedImage(data);
        } catch {
            setUploadStatus(msgUploadError, true);
        } finally {
            pickerUploading = false;
            if (pickerUploadBtn) pickerUploadBtn.disabled = false;
            if (pickerUploadInput) pickerUploadInput.value = '';
        }
    }

    function setPickerStatus(message, isError = false) {
        if (!pickerStatus) return;
        if (!message) {
            pickerStatus.hidden = true;
            pickerStatus.textContent = '';
            pickerStatus.classList.remove('is-error');
            return;
        }
        pickerStatus.hidden = false;
        pickerStatus.textContent = message;
        pickerStatus.classList.toggle('is-error', isError);
    }

    function closeImagePicker() {
        if (!pickerOverlay || pickerOverlay.hidden) return;
        pickerOverlay.hidden = true;
        textarea.focus();
    }

    function insertPickedImage(item) {
        if (!item || !item.url) return;
        const alt = escapeMarkdownAlt(item.alt || item.name);
        const markdown = `![${alt}](${item.url})`;
        closeImagePicker();
        textarea.setSelectionRange(savedSelection.start, savedSelection.end);
        replaceSelection(textarea, markdown, markdown.length, markdown.length);
    }

    async function loadMyImages(reset) {
        if (!pickerGrid) return;
        if (pickerLoading && !reset) return;

        if (reset) {
            pickerSkip = 0;
            pickerGrid.innerHTML = '';
            if (pickerMoreWrap) pickerMoreWrap.hidden = true;
        }

        const currentSeq = ++pickerRequestSeq;
        pickerLoading = true;
        if (reset) {
            setPickerStatus(msgImagesLoading, false);
        }
        if (pickerLoadMoreBtn) {
            pickerLoadMoreBtn.disabled = true;
        }

        try {
            const params = new URLSearchParams({
                skip: String(pickerSkip),
                take: String(pickerTake)
            });
            const searchTerm = pickerSearchInput?.value?.trim();
            if (searchTerm) {
                params.set('search', searchTerm);
            }

            const response = await fetch(`${myImagesUrl}?${params.toString()}`, {
                headers: { 'Accept': 'application/json' }
            });

            if (currentSeq !== pickerRequestSeq) return;
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();
            if (currentSeq !== pickerRequestSeq) return;

            const items = Array.isArray(data.items) ? data.items : [];
            if (reset && items.length === 0) {
                setPickerStatus(msgImagesEmpty, false);
                if (pickerMoreWrap) pickerMoreWrap.hidden = true;
                return;
            }

            setPickerStatus('', false);

            items.forEach((item) => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'md-image-picker-item';
                btn.title = item.name || item.alt || '';

                const thumbWrap = document.createElement('span');
                thumbWrap.className = 'md-image-picker-thumb';

                const img = document.createElement('img');
                img.src = item.url;
                img.alt = item.alt || item.name || '';
                img.loading = 'lazy';
                thumbWrap.appendChild(img);

                const caption = document.createElement('span');
                caption.className = 'md-image-picker-name';
                caption.textContent = item.name || item.alt || `#${item.id}`;

                btn.appendChild(thumbWrap);
                btn.appendChild(caption);
                btn.addEventListener('click', () => insertPickedImage(item));
                pickerGrid.appendChild(btn);
            });

            pickerSkip += items.length;
            if (pickerMoreWrap) {
                pickerMoreWrap.hidden = !data.hasMore;
            }
        } catch {
            if (currentSeq !== pickerRequestSeq) return;
            setPickerStatus(msgImagesError, true);
        } finally {
            if (currentSeq === pickerRequestSeq) {
                pickerLoading = false;
                if (pickerLoadMoreBtn) {
                    pickerLoadMoreBtn.disabled = false;
                }
            }
        }
    }

    function openImagePicker() {
        if (!pickerOverlay) return;
        savedSelection = {
            start: textarea.selectionStart ?? textarea.value.length,
            end: textarea.selectionEnd ?? textarea.value.length
        };
        setUploadStatus('', false);
        pickerOverlay.hidden = false;
        loadMyImages(true);
        if (pickerSearchInput) {
            pickerSearchInput.focus();
        }
    }

    if (pickerUploadBtn && pickerUploadInput) {
        pickerUploadBtn.addEventListener('click', () => {
            if (pickerUploading) return;
            pickerUploadInput.click();
        });
        pickerUploadInput.addEventListener('change', () => {
            const file = pickerUploadInput.files?.[0];
            if (file) {
                uploadAndInsertImage(file);
            }
        });
    }

    if (pickerCloseBtn) {
        pickerCloseBtn.addEventListener('click', closeImagePicker);
    }

    if (pickerOverlay) {
        pickerOverlay.addEventListener('click', (e) => {
            if (e.target === pickerOverlay) {
                closeImagePicker();
            }
        });
    }

    if (pickerLoadMoreBtn) {
        pickerLoadMoreBtn.addEventListener('click', () => loadMyImages(false));
    }

    if (pickerSearchInput) {
        pickerSearchInput.addEventListener('input', () => {
            clearTimeout(pickerSearchTimer);
            pickerSearchTimer = setTimeout(() => loadMyImages(true), 250);
        });
    }

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && pickerOverlay && !pickerOverlay.hidden) {
            e.preventDefault();
            closeImagePicker();
        }
    });

    // ツールバーボタン
    if (toolbar) {
        toolbar.addEventListener('click', (e) => {
            if (!isMarkdownEnabled()) return;
            const btn = e.target.closest('[data-md-action]');
            if (!btn) return;
            const action = btn.dataset.mdAction;
            if (action === 'insert-my-image') {
                openImagePicker();
                return;
            }
            applyToolbarAction(textarea, action);
        });
    }

    // キーボードショートカット (Ctrl+B, Ctrl+I, Ctrl+K)
    textarea.addEventListener('keydown', (e) => {
        if (!isMarkdownEnabled()) return;
        if (!(e.ctrlKey || e.metaKey) || e.altKey) return;
        const key = e.key.toLowerCase();
        if (key === 'b') {
            e.preventDefault();
            applyToolbarAction(textarea, 'bold');
        } else if (key === 'i') {
            e.preventDefault();
            applyToolbarAction(textarea, 'italic');
        } else if (key === 'k') {
            e.preventDefault();
            applyToolbarAction(textarea, 'link');
        }
    });

    // 「マークダウンを使用する」チェックボックス連動
    function applyMarkdownToggleState() {
        const enabled = isMarkdownEnabled();
        rootEl.classList.toggle('is-plain', !enabled);
        if (headerEl) {
            headerEl.hidden = !enabled;
        }
        if (hintEl) {
            hintEl.hidden = !enabled;
        }
        if (!enabled) {
            closeImagePicker();
            switchTab('write');
        }
    }

    if (toggleCheckbox) {
        toggleCheckbox.addEventListener('change', applyMarkdownToggleState);
        applyMarkdownToggleState();
    }
}

export function initMarkdownEditor() {
    const editors = document.querySelectorAll('[data-markdown-editor]');
    editors.forEach(setupEditor);
}
