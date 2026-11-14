class RecipientSelector {
    static selectedUsers = [];
    static lastSearchResults = [];
    static hasSearched = false;
    static searchTimeout = null;

    static isGroupMode() {
        const groupRadio = document.querySelector('input[name="IsGroup"][value="true"]');
        return Boolean(groupRadio && groupRadio.checked);
    }

    static getSearchInput() {
        return document.getElementById('recipientSearchInput');
    }

    static init() {
        const input = this.getSearchInput();
        if (!input) return;

        this.selectedUsers = [];
        this.lastSearchResults = [];
        this.hasSearched = false;

        if (input.dataset.initialUsers) {
            try {
                const parsed = JSON.parse(input.dataset.initialUsers);
                if (Array.isArray(parsed)) {
                    this.selectedUsers = parsed
                        .filter(u => u && u.id && u.userName)
                        .map(u => ({ id: String(u.id), userName: String(u.userName), avatarImageId: u.avatarImageId || null }));
                }
            } catch (err) {
                console.error('Failed to parse initial users:', err);
            }
        }

        if (!this.isGroupMode() && this.selectedUsers.length > 1) {
            this.selectedUsers = [this.selectedUsers[0]];
        }

        this.renderSelected();
        this.setupEvents(input);
    }

    static setupEvents(input) {
        input.addEventListener('input', () => {
            clearTimeout(this.searchTimeout);
            const query = input.value.trim();
            if (!query) {
                this.clearSearchResults();
                return;
            }
            this.searchTimeout = setTimeout(() => {
                this.performSearch(query);
            }, 250);
        });

        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                clearTimeout(this.searchTimeout);
                const query = input.value.trim();
                if (query) {
                    this.performSearch(query);
                } else {
                    this.clearSearchResults();
                }
            }
        });
    }

    static async performSearch(query) {
        const input = this.getSearchInput();
        const resultsContainer = document.getElementById('recipientSearchResults');
        if (!input || !resultsContainer) return;

        const trimmed = (query || '').trim();
        if (!trimmed) {
            this.clearSearchResults();
            return;
        }

        const searchUrl = input.dataset.searchUrl || '/Message/SearchUsers';
        try {
            const res = await fetch(`${searchUrl}?q=${encodeURIComponent(trimmed)}`, {
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin'
            });
            if (!res.ok) return;
            const data = await res.json();
            this.lastSearchResults = Array.isArray(data) ? data : [];
            this.hasSearched = true;
            this.renderSearchResults();
        } catch (err) {
            console.error('User search error:', err);
        }
    }

    static clearSearchResults() {
        const resultsContainer = document.getElementById('recipientSearchResults');
        this.lastSearchResults = [];
        this.hasSearched = false;
        if (resultsContainer) {
            resultsContainer.innerHTML = '';
            resultsContainer.hidden = true;
        }
    }

    static renderSearchResults() {
        const input = this.getSearchInput();
        const resultsContainer = document.getElementById('recipientSearchResults');
        if (!input || !resultsContainer) return;

        resultsContainer.innerHTML = '';

        if (!this.hasSearched) {
            resultsContainer.hidden = true;
            return;
        }

        const selectLabel = input.dataset.selectLabel || 'Select';
        const selectedLabel = input.dataset.selectedLabel || 'Selected';
        const noResultsLabel = input.dataset.noResultsLabel || 'No matching users found.';

        if (this.lastSearchResults.length === 0) {
            const emptyMsg = document.createElement('p');
            emptyMsg.className = 'recipient-empty-message';
            emptyMsg.textContent = noResultsLabel;
            resultsContainer.appendChild(emptyMsg);
            resultsContainer.hidden = false;
            return;
        }

        const selectedIds = new Set(this.selectedUsers.map(u => u.id));

        this.lastSearchResults.forEach(user => {
            if (!user || !user.id) return;
            const userId = String(user.id);
            const userName = String(user.userName || '');
            const avatarImageId = user.avatarImageId || null;
            const isAlreadySelected = selectedIds.has(userId);

            const row = document.createElement('div');
            row.className = 'recipient-user-item';

            const info = document.createElement('span');
            info.className = 'icon-wrap';
            const icon = document.createElement('img');
            if (avatarImageId) {
                icon.src = `/Image/File/${avatarImageId}`;
                icon.className = 'user-avatar-sm';
            } else {
                icon.src = '/icons/user.svg';
                icon.className = 'icon';
            }
            icon.alt = '';
            info.appendChild(icon);
            info.appendChild(document.createTextNode(userName));

            const actionBtn = document.createElement('button');
            actionBtn.type = 'button';
            actionBtn.className = 'btn-sm';
            if (isAlreadySelected) {
                actionBtn.disabled = true;
                actionBtn.textContent = selectedLabel;
            } else {
                actionBtn.textContent = selectLabel;
                actionBtn.dataset.selectRecipientId = userId;
                actionBtn.dataset.selectRecipientName = userName;
                if (avatarImageId) {
                    actionBtn.dataset.selectRecipientAvatar = String(avatarImageId);
                }
            }

            row.appendChild(info);
            row.appendChild(actionBtn);
            resultsContainer.appendChild(row);
        });

        resultsContainer.hidden = false;
    }

    static addUser(id, userName, avatarImageId) {
        if (!id || !userName) return;
        const userId = String(id);
        const name = String(userName);
        const avatarId = avatarImageId || null;

        if (this.selectedUsers.some(u => u.id === userId)) {
            return;
        }

        if (this.isGroupMode()) {
            this.selectedUsers.push({ id: userId, userName: name, avatarImageId: avatarId });
        } else {
            this.selectedUsers = [{ id: userId, userName: name, avatarImageId: avatarId }];
        }

        this.renderSelected();

        const input = this.getSearchInput();
        if (input) {
            input.value = '';
        }
        this.clearSearchResults();
    }

    static removeUser(id) {
        const userId = String(id);
        this.selectedUsers = this.selectedUsers.filter(u => u.id !== userId);
        this.renderSelected();
        if (this.hasSearched) {
            this.renderSearchResults();
        }
    }

    static enforceChatTypeLimit() {
        if (!this.isGroupMode() && this.selectedUsers.length > 1) {
            this.selectedUsers = [this.selectedUsers[0]];
            this.renderSelected();
            if (this.hasSearched) {
                this.renderSearchResults();
            }
        }
    }

    static renderSelected() {
        const input = this.getSearchInput();
        const selectedContainer = document.getElementById('selectedRecipientsContainer');
        const hiddenInputsContainer = document.getElementById('hiddenRecipientInputs');
        const countSpan = document.getElementById('selectedRecipientCount');
        if (!selectedContainer || !hiddenInputsContainer) return;

        selectedContainer.innerHTML = '';
        hiddenInputsContainer.innerHTML = '';

        if (countSpan) {
            countSpan.textContent = `(${this.selectedUsers.length})`;
        }

        const removeLabel = (input && input.dataset.removeLabel)
            || (window.MeguriI18n && window.MeguriI18n.removeLabel)
            || 'Remove';
        const noSelectedLabel = (input && input.dataset.noSelectedLabel) || '';

        if (this.selectedUsers.length === 0) {
            if (noSelectedLabel) {
                const emptyMsg = document.createElement('p');
                emptyMsg.className = 'recipient-empty-message';
                emptyMsg.textContent = noSelectedLabel;
                selectedContainer.appendChild(emptyMsg);
            }
            return;
        }

        this.selectedUsers.forEach(user => {
            const row = document.createElement('div');
            row.className = 'recipient-user-item selected-recipient-item';

            const info = document.createElement('span');
            info.className = 'icon-wrap';
            const icon = document.createElement('img');
            if (user.avatarImageId) {
                icon.src = `/Image/File/${user.avatarImageId}`;
                icon.className = 'user-avatar-sm';
            } else {
                icon.src = '/icons/user.svg';
                icon.className = 'icon';
            }
            icon.alt = '';
            info.appendChild(icon);
            info.appendChild(document.createTextNode(user.userName));

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'btn-sm';
            removeBtn.textContent = removeLabel;
            removeBtn.dataset.removeRecipientId = user.id;

            row.appendChild(info);
            row.appendChild(removeBtn);
            selectedContainer.appendChild(row);

            const hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = 'SelectedUserIds';
            hiddenInput.value = user.id;
            hiddenInputsContainer.appendChild(hiddenInput);
        });
    }
}

function handleChatTypeChange(e) {
    const target = e.target;
    if (!target) return;

    if (target.matches('input[name="IsGroup"]')) {
        const container = document.getElementById('groupTitleContainer');
        if (container) {
            container.hidden = target.value !== 'true';
        }
        RecipientSelector.enforceChatTypeLimit();
    }
}

function handleMessageUiClick(e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;

    const searchRecipientBtn = target.closest('[data-search-recipient]');
    if (searchRecipientBtn) {
        const input = RecipientSelector.getSearchInput();
        if (input) {
            clearTimeout(RecipientSelector.searchTimeout);
            RecipientSelector.performSearch(input.value);
        }
        return;
    }

    const selectRecipientBtn = target.closest('[data-select-recipient-id]');
    if (selectRecipientBtn) {
        RecipientSelector.addUser(
            selectRecipientBtn.dataset.selectRecipientId,
            selectRecipientBtn.dataset.selectRecipientName,
            selectRecipientBtn.dataset.selectRecipientAvatar
        );
        return;
    }

    const removeRecipientBtn = target.closest('[data-remove-recipient-id]');
    if (removeRecipientBtn) {
        RecipientSelector.removeUser(removeRecipientBtn.dataset.removeRecipientId);
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
}

export function initMessageUi() {
    RecipientSelector.init();

    // メッセージ新規作成のチャット種別切り替え（ダイレクト / グループ）
    document.addEventListener('change', handleChatTypeChange);

    document.addEventListener('click', handleMessageUiClick);
}
