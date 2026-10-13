(function () {
    'use strict';

    function supported() {
        return window.PublicKeyCredential &&
            typeof PublicKeyCredential.parseCreationOptionsFromJSON === 'function' &&
            typeof PublicKeyCredential.parseRequestOptionsFromJSON === 'function';
    }

    async function fetchOptions(url, form) {
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

    function bind(button) {
        const form = button.closest('form');
        const mode = button.dataset.passkey;
        const credentialInput = form.querySelector('input[name="credentialJson"]');
        const message = form.querySelector('[data-passkey-message]');

        if (!supported()) {
            button.hidden = true;
            return;
        }

        button.addEventListener('click', async function (e) {
            e.preventDefault();
            button.disabled = true;
            if (message) { message.textContent = ''; }
            try {
                let credential;
                const options = await fetchOptions(button.dataset.optionsUrl, form);
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

    document.querySelectorAll('button[data-passkey]').forEach(bind);
})();
