(function () {
    const form = document.getElementById('connectionForm');
    const btnTest = document.getElementById('btnTestConnection');
    const testResult = document.getElementById('testResult');
    const authType = document.getElementById('authType');
    const usernameGroup = document.getElementById('usernameGroup');
    const passwordGroup = document.getElementById('passwordGroup');

    if (!form) return;

    const sqlAuthValue = '0'; // AuthenticationType.SqlServer
    const windowsAuthValue = '1'; // AuthenticationType.Windows

    function toggleAuthFields() {
        const isSqlAuth = authType?.value === sqlAuthValue;
        if (usernameGroup) usernameGroup.style.display = isSqlAuth ? '' : 'none';
        if (passwordGroup) passwordGroup.style.display = isSqlAuth ? '' : 'none';
    }

    authType?.addEventListener('change', toggleAuthFields);
    toggleAuthFields();

    btnTest?.addEventListener('click', async () => {
        const payload = {
            name: form.querySelector('[name="Name"]')?.value ?? '',
            server: form.querySelector('[name="Server"]')?.value ?? '',
            database: form.querySelector('[name="Database"]')?.value ?? '',
            authenticationType: Number(authType?.value ?? windowsAuthValue),
            username: form.querySelector('[name="Username"]')?.value ?? '',
            password: form.querySelector('[name="Password"]')?.value ?? ''
        };

        testResult.classList.remove('d-none', 'alert-success', 'alert-danger');
        testResult.classList.add('alert-info');
        testResult.textContent = 'Tekshirilmoqda...';

        try {
            const response = await fetch('/api/connections/test', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await response.json();
            testResult.classList.remove('alert-info');
            testResult.classList.add(result.success ? 'alert-success' : 'alert-danger');
            testResult.textContent = `${result.message} (${result.elapsedMs} ms)`;
        } catch (error) {
            testResult.classList.remove('alert-info');
            testResult.classList.add('alert-danger');
            testResult.textContent = error.message;
        }
    });
})();
