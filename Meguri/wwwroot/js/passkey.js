export default class PasskeyManager {
    constructor() {
        this.init();
    }

    /**
     * WebAuthn/Passkeyがサポートされているか確認する
     */
    static isSupported() {
        return window.PublicKeyCredential &&
            typeof PublicKeyCredential.parseCreationOptionsFromJSON === 'function' &&
            typeof PublicKeyCredential.parseRequestOptionsFromJSON === 'function';
    }

    /**
     * サーバーからオプションを取得する
     */
    async #fetchOptions(url, form) {
        const token = form.querySelector('input[name="__RequestVerificationToken"]');
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token ? token.value : '' },
            credentials: 'same-origin'
        });
        if (!response.ok) {
            throw new Error('options request failed');
        }
        return JSON.parse(await response.text());
    }

    /**
     * ボタンにパスキーのイベントリスナーをバインドする
     */
    #bind(button) {
        const form = button.closest('form');
        const mode = button.dataset.passkey;
        const credentialInput = form.querySelector('input[name="credentialJson"]');
        const message = form.querySelector('[data-passkey-message]');

        if (!PasskeyManager.isSupported()) {
            button.hidden = true;
            return;
        }

        button.addEventListener('click', async (e) => {
            e.preventDefault();
            button.disabled = true;
            if (message) { message.textContent = ''; }
            try {
                let credential;
                const options = await this.#fetchOptions(button.dataset.optionsUrl, form);
                if (mode === 'create') {
                    credential = await navigator.credentials.create({
                        publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(options)
                    });
                } else {
                    credential = await navigator.credentials.get({
                        publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(options)
                    });
                }
                credentialInput.value = JSON.stringify(credential);
                form.submit();
            } catch (err) {
                button.disabled = false;
                console.error('Passkey error:', err);
                if (message && err && err.name !== 'NotAllowedError') {
                    message.textContent = button.dataset.errorText || '';
                }
            }
        });
    }

    /**
     * 対象のボタンをすべて取得してバインドを実行する
     */
    init() {
        document.querySelectorAll('button[data-passkey]').forEach((button) => {
            this.#bind(button);
        });
    }
}

// モジュールとして読み込まれた際に自動で初期化を実行する場合
new PasskeyManager();