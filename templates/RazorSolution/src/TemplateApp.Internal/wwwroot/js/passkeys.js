// Passkeys (WebAuthn) for the Account pages.
// Buttons: <button data-passkey="create|get" data-form="form-id" data-options-url="..." data-status="status-id">.
// The page handler at data-options-url returns WebAuthn options JSON (the API's state stays on the server);
// the credential goes back in the form's hidden CredentialJson field and the form is submitted normally.
(function () {
    'use strict';

    var supported = !!(window.PublicKeyCredential &&
        PublicKeyCredential.parseCreationOptionsFromJSON &&
        PublicKeyCredential.parseRequestOptionsFromJSON);

    function antiforgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    async function fetchOptions(url, body) {
        var response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiforgeryToken() },
            body: JSON.stringify(body || {})
        });
        if (!response.ok) {
            throw new Error('The server could not start the passkey request (' + response.status + ').');
        }
        return response.json();
    }

    document.querySelectorAll('[data-passkey]').forEach(function (button) {
        var status = document.getElementById(button.dataset.status);
        if (!supported) {
            button.disabled = true;
            if (status) { status.textContent = 'This browser does not support passkeys.'; }
            return;
        }

        button.addEventListener('click', async function () {
            var form = document.getElementById(button.dataset.form);
            button.disabled = true;
            if (status) { status.textContent = 'Follow the prompt from your browser or device…'; }
            try {
                var credential;
                if (button.dataset.passkey === 'create') {
                    var creation = await fetchOptions(button.dataset.optionsUrl);
                    credential = await navigator.credentials.create({ publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(creation) });
                } else {
                    var emailInput = button.dataset.emailFrom ? document.getElementById(button.dataset.emailFrom) : null;
                    var request = await fetchOptions(button.dataset.optionsUrl, { email: emailInput && emailInput.value ? emailInput.value : null });
                    credential = await navigator.credentials.get({ publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(request) });
                }
                form.querySelector('[name="CredentialJson"]').value = JSON.stringify(credential);
                form.submit();
            } catch (error) {
                if (status) {
                    status.textContent = error && error.name === 'NotAllowedError'
                        ? 'The passkey request was cancelled or timed out.'
                        : (error && error.message) || 'The passkey request failed.';
                }
                button.disabled = false;
            }
        });
    });
})();
