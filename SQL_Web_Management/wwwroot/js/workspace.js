(function () {
    const workspace = document.getElementById('workspace');
    if (!workspace) return;

    const connectionId = Number(workspace.dataset.connectionId);
    const state = {
        tabs: [],
        activeTabId: null,
        lastResult: null,
        currentDatabase: workspace.dataset.database || 'master',
        databases: [],
        theme: localStorage.getItem('sqlws-theme') || 'dark',
        activeTreeRow: null
    };

    const els = {
        tree: document.getElementById('objectTree'),
        tabs: document.getElementById('queryTabs'),
        editor: document.getElementById('editorContainer'),
        resultsGrid: document.getElementById('resultsGrid'),
        columnsGrid: document.getElementById('columnsGrid'),
        messages: document.getElementById('messagesPanel'),
        historyList: document.getElementById('historyList'),
        resultsInfo: document.getElementById('resultsInfo'),
        statusDatabase: document.getElementById('statusDatabase'),
        statusElapsed: document.getElementById('statusElapsed'),
        statusRows: document.getElementById('statusRows'),
        databaseContext: document.getElementById('databaseContext'),
        btnExportCsv: document.getElementById('btnExportCsv')
    };

    applyTheme(state.theme);
    initPanelResize();
    initMonaco().then(async () => {
        createTab('Query 1', `-- ${state.currentDatabase}\nSELECT TOP 100 *\nFROM `);
        await loadExplorer();
        loadHistory();
    });

    document.getElementById('btnNewTab')?.addEventListener('click', () => {
        createTab(`Query ${state.tabs.length + 1}`, `-- ${state.currentDatabase}\n`);
    });

    document.getElementById('btnExecute')?.addEventListener('click', executeQuery);
    document.getElementById('btnThemeToggle')?.addEventListener('click', toggleTheme);
    document.getElementById('btnExportCsv')?.addEventListener('click', exportCsv);
    document.getElementById('btnClearMessages')?.addEventListener('click', () => {
        els.messages.textContent = 'Tayyor.';
    });

    els.databaseContext?.addEventListener('change', () => {
        setCurrentDatabase(els.databaseContext.value);
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'F5' || (e.ctrlKey && e.key === 'Enter')) {
            e.preventDefault();
            executeQuery();
        }
    });

    function initPanelResize() {
        const handle = document.getElementById('panelResizeHandle');
        const bottom = document.getElementById('workspaceBottom');
        const center = document.querySelector('.workspace-center');
        if (!handle || !bottom || !center) return;

        const savedHeight = Number(localStorage.getItem('sqlws-bottom-height'));
        if (savedHeight >= 120) {
            applyBottomHeight(bottom, center, handle, savedHeight);
        }

        handle.addEventListener('mousedown', (e) => {
            e.preventDefault();
            const startY = e.clientY;
            const startHeight = bottom.offsetHeight;

            document.body.classList.add('workspace-resizing');

            const onMove = (ev) => {
                const delta = startY - ev.clientY;
                const maxHeight = center.clientHeight - handle.offsetHeight - 120;
                const newHeight = Math.min(Math.max(startHeight + delta, 120), maxHeight);
                applyBottomHeight(bottom, center, handle, newHeight);
                layoutEditors();
            };

            const onUp = () => {
                document.body.classList.remove('workspace-resizing');
                localStorage.setItem('sqlws-bottom-height', String(bottom.offsetHeight));
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup', onUp);
                layoutEditors();
            };

            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        });
    }

    function applyBottomHeight(bottom, center, handle, height) {
        const maxHeight = center.clientHeight - handle.offsetHeight - 120;
        const clamped = Math.min(Math.max(height, 120), Math.max(maxHeight, 120));
        bottom.style.height = `${clamped}px`;
        bottom.style.flexBasis = `${clamped}px`;
    }

    function layoutEditors() {
        state.tabs.forEach(tab => tab.editor?.layout());
    }

    async function initMonaco() {
        return new Promise((resolve) => {
            require.config({
                paths: { vs: 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs' }
            });
            require(['vs/editor/editor.main'], () => resolve());
        });
    }

    function createTab(title, sql) {
        const id = `tab-${Date.now()}-${Math.random().toString(16).slice(2)}`;
        const tab = { id, title, sql, editor: null };
        state.tabs.push(tab);
        renderTabs();
        activateTab(id, sql);
    }

    function closeTab(tabId) {
        if (state.tabs.length <= 1) return;
        const index = state.tabs.findIndex(t => t.id === tabId);
        if (index < 0) return;

        const tab = state.tabs[index];
        tab.editor?.dispose();
        state.tabs.splice(index, 1);

        if (state.activeTabId === tabId) {
            const next = state.tabs[Math.max(0, index - 1)];
            activateTab(next.id);
        } else {
            renderTabs();
        }
    }

    function renderTabs() {
        els.tabs.innerHTML = '';
        state.tabs.forEach(tab => {
            const wrapper = document.createElement('div');
            wrapper.className = `query-tab-wrap ${tab.id === state.activeTabId ? 'active' : ''}`;

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'query-tab';
            btn.textContent = tab.title;
            btn.addEventListener('click', () => {
                saveActiveEditor();
                activateTab(tab.id);
            });

            const closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'query-tab-close';
            closeBtn.textContent = '×';
            closeBtn.title = 'Yopish';
            closeBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                closeTab(tab.id);
            });

            wrapper.appendChild(btn);
            wrapper.appendChild(closeBtn);
            els.tabs.appendChild(wrapper);
        });
    }

    function activateTab(tabId, initialSql) {
        const tab = state.tabs.find(t => t.id === tabId);
        if (!tab) return;

        saveActiveEditor();
        state.activeTabId = tabId;
        renderTabs();

        if (tab.editor) {
            tab.editor.setValue(initialSql ?? tab.sql);
            tab.editor.focus();
            return;
        }

        els.editor.innerHTML = '';
        tab.editor = monaco.editor.create(els.editor, {
            value: initialSql ?? tab.sql,
            language: 'sql',
            theme: state.theme === 'dark' ? 'vs-dark' : 'vs',
            automaticLayout: true,
            minimap: { enabled: false },
            fontSize: 14,
            lineNumbers: 'on',
            scrollBeyondLastLine: false,
            wordWrap: 'on'
        });
        tab.editor.focus();
    }

    function saveActiveEditor() {
        const tab = state.tabs.find(t => t.id === state.activeTabId);
        if (tab?.editor) {
            tab.sql = tab.editor.getValue();
        }
    }

    function getActiveSql() {
        saveActiveEditor();
        const tab = state.tabs.find(t => t.id === state.activeTabId);
        return tab?.sql?.trim() ?? '';
    }

    function setActiveSql(sql) {
        const tab = state.tabs.find(t => t.id === state.activeTabId);
        if (tab?.editor) {
            tab.editor.setValue(sql);
            tab.sql = sql;
            tab.editor.focus();
        }
    }

    function setCurrentDatabase(database) {
        if (!database) return;
        state.currentDatabase = database;
        if (els.statusDatabase) els.statusDatabase.textContent = database;
        if (els.databaseContext && els.databaseContext.value !== database) {
            els.databaseContext.value = database;
        }
    }

    function populateDatabaseSelect(databases) {
        state.databases = databases;
        if (!els.databaseContext) return;

        els.databaseContext.innerHTML = '';
        databases.forEach(db => {
            const option = document.createElement('option');
            option.value = db;
            option.textContent = db;
            els.databaseContext.appendChild(option);
        });

        els.databaseContext.value = state.databases.includes(state.currentDatabase)
            ? state.currentDatabase
            : state.databases[0] ?? state.currentDatabase;
        setCurrentDatabase(els.databaseContext.value);
    }

    function setActiveTreeRow(row) {
        if (state.activeTreeRow) {
            state.activeTreeRow.classList.remove('active');
        }
        state.activeTreeRow = row;
        row?.classList.add('active');
    }

    async function executeQuery() {
        const sql = getActiveSql();
        if (!sql) {
            appendMessage('SQL bo\'sh.', true);
            return;
        }

        appendMessage(`-- Database: ${state.currentDatabase}\nExecuting...\n${sql}\n`);
        els.btnExportCsv.disabled = true;

        try {
            const result = await fetchJson('/api/query/execute', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    connectionId,
                    sql,
                    database: state.currentDatabase
                })
            });
            state.lastResult = result.success ? result : null;

            els.statusElapsed.textContent = formatElapsed(result.elapsedMs);
            els.statusRows.textContent = result.rows?.length
                ? `${result.rows.length} rows`
                : `${result.rowsAffected ?? 0} affected`;

            if (result.success) {
                appendMessage(`✓ ${result.message}\nVaqt: ${result.elapsedMs} ms\n`);
                renderResults(result.columns ?? [], result.rows ?? []);
                els.resultsInfo.textContent = result.message || 'Buyruq bajarildi';
                els.btnExportCsv.disabled = !(result.rows?.length > 0);

                if ((result.rows?.length ?? 0) > 0) {
                    showResultsTab();
                } else {
                    showMessagesTab();
                }

                if (/CREATE\s+DATABASE/i.test(sql)) {
                    await loadExplorer();
                }
            } else {
                appendMessage(`✗ XATO:\n${result.errorMessage}\nVaqt: ${result.elapsedMs} ms\n`);
                renderResults([], []);
                els.resultsInfo.textContent = result.errorMessage || 'Xato yuz berdi';
                showMessagesTab();
            }

            loadHistory();
        } catch (error) {
            appendMessage(`✗ ${error.message}`);
        }
    }

    function showResultsTab() {
        document.querySelector('[data-bs-target="#resultsPane"]')?.click();
    }

    function showMessagesTab() {
        document.querySelector('[data-bs-target="#messagesPane"]')?.click();
    }

    function appendMessage(text, replace = false) {
        if (replace) {
            els.messages.textContent = text;
        } else {
            els.messages.textContent += `\n${text}`;
        }
        els.messages.scrollTop = els.messages.scrollHeight;
    }

    function renderResults(columns, rows) {
        if (!columns.length) {
            els.resultsGrid.innerHTML = '<div class="p-3 text-secondary small">Natija yo\'q</div>';
            return;
        }

        const table = document.createElement('table');
        const thead = document.createElement('thead');
        const headRow = document.createElement('tr');
        columns.forEach(col => {
            const th = document.createElement('th');
            th.textContent = col;
            headRow.appendChild(th);
        });
        thead.appendChild(headRow);
        table.appendChild(thead);

        const tbody = document.createElement('tbody');
        rows.forEach(row => {
            const tr = document.createElement('tr');
            columns.forEach(col => {
                const td = document.createElement('td');
                const value = row[col];
                const isNull = value === null || value === undefined;
                td.textContent = isNull ? 'NULL' : String(value);
                if (isNull) td.classList.add('cell-null');
                td.title = td.textContent;
                tr.appendChild(td);
            });
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);
        els.resultsGrid.innerHTML = '';
        els.resultsGrid.appendChild(table);
    }

    function renderColumns(columns) {
        if (!columns.length) {
            els.columnsGrid.innerHTML = '<div class="p-3 text-secondary small">Jadval tanlanmagan</div>';
            return;
        }

        const table = document.createElement('table');
        table.innerHTML = `
            <thead>
                <tr>
                    <th>Column</th>
                    <th>Type</th>
                    <th>Length</th>
                    <th>Nullable</th>
                    <th>PK</th>
                </tr>
            </thead>`;
        const tbody = document.createElement('tbody');
        columns.forEach(col => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${escapeHtml(col.columnName)}</td>
                <td>${escapeHtml(col.dataType)}</td>
                <td>${col.maxLength ?? ''}</td>
                <td>${col.isNullable ? 'YES' : 'NO'}</td>
                <td>${col.isPrimaryKey ? 'YES' : ''}</td>`;
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);
        els.columnsGrid.innerHTML = '';
        els.columnsGrid.appendChild(table);
    }

    async function fetchJson(url, options) {
        const response = await fetch(url, options);
        if (!response.ok) {
            let message = `So'rov muvaffaqiyatsiz (${response.status})`;
            const contentType = response.headers.get('content-type') ?? '';
            if (contentType.includes('application/json')) {
                const body = await response.json();
                message = body.message ?? body.title ?? message;
            } else if (response.status === 401) {
                message = 'Sessiya tugagan. Qayta login qiling.';
            }
            throw new Error(message);
        }

        const contentType = response.headers.get('content-type') ?? '';
        if (!contentType.includes('application/json')) {
            throw new Error('Server JSON emas javob qaytardi. Qayta login qiling.');
        }

        return response.json();
    }

    function parseSchemaObject(node) {
        const schema = node.schema || (node.name.includes('.') ? node.name.split('.')[0] : 'dbo');
        const objectName = node.schema
            ? node.name.slice(node.schema.length + 1)
            : (node.name.includes('.') ? node.name.split('.').slice(1).join('.') : node.name);
        return { schema, objectName };
    }

    async function loadExplorer() {
        try {
            const root = await fetchJson(`/api/explorer/${connectionId}/root`);
            els.tree.innerHTML = '';
            renderTreeNode(root, els.tree, 0);

            const dbNames = (root.children ?? []).map(c => c.name).filter(Boolean);
            if (dbNames.length) {
                populateDatabaseSelect(dbNames);
            }
        } catch (error) {
            els.tree.innerHTML = `<div class="text-danger p-3 small">${escapeHtml(error.message)}</div>`;
        }
    }

    function getNodeIcon(type) {
        switch (type) {
            case 'Server': return '🖥️';
            case 'Database': return '🗄️';
            case 'TablesFolder': return '📁';
            case 'ViewsFolder': return '👁️';
            case 'ProceduresFolder': return '⚙️';
            case 'Table': return '📋';
            case 'View': return '👁️';
            case 'StoredProcedure': return '⚙️';
            default: return '•';
        }
    }

    function renderTreeNode(node, container, depth) {
        const item = document.createElement('div');
        item.className = 'tree-node';
        item.style.setProperty('--tree-depth', depth);

        const row = document.createElement('div');
        row.className = 'tree-item';

        const toggle = document.createElement('span');
        toggle.className = 'tree-toggle';
        toggle.textContent = node.hasChildren ? '▸' : '';

        const icon = document.createElement('span');
        icon.className = 'tree-icon';
        icon.textContent = getNodeIcon(node.type);

        const label = document.createElement('span');
        label.className = 'tree-label';
        label.textContent = node.name;
        label.title = node.name;

        row.appendChild(toggle);
        row.appendChild(icon);
        row.appendChild(label);
        item.appendChild(row);

        const childContainer = document.createElement('div');
        childContainer.className = 'tree-children';
        childContainer.style.display = 'none';
        item.appendChild(childContainer);

        let loaded = false;
        let expanded = false;

        row.addEventListener('click', async (e) => {
            e.stopPropagation();
            setActiveTreeRow(row);

            if (node.type === 'Database' && node.name) {
                setCurrentDatabase(node.name);
            }

            if (node.hasChildren) {
                expanded = !expanded;
                toggle.textContent = expanded ? '▾' : '▸';
                childContainer.style.display = expanded ? 'block' : 'none';

                if (expanded && !loaded) {
                    if (!node.children?.length) {
                        await loadChildren(node, childContainer, depth + 1);
                    }
                    loaded = true;
                }
            }

            if (node.type === 'Table') {
                if (node.database) setCurrentDatabase(node.database);
                try {
                    await handleTableClick(node);
                } catch (error) {
                    appendMessage(`✗ ${error.message}`);
                }
            } else if (node.type === 'View') {
                if (node.database) setCurrentDatabase(node.database);
                const { schema, objectName } = parseSchemaObject(node);
                setActiveSql(`SELECT TOP 100 *\nFROM [${node.database}].[${schema}].[${objectName}];`);
            } else if (node.type === 'StoredProcedure') {
                if (node.database) setCurrentDatabase(node.database);
                const { schema, objectName } = parseSchemaObject(node);
                setActiveSql(`EXEC [${node.database}].[${schema}].[${objectName}];`);
            }
        });

        container.appendChild(item);

        if (node.children?.length) {
            node.children.forEach(child => renderTreeNode(child, childContainer, depth + 1));
        }
    }

    async function loadChildren(node, container, depth) {
        container.innerHTML = '<div class="small text-secondary px-3">Yuklanmoqda...</div>';

        try {
            if (node.type === 'TablesFolder') {
                await loadObjectType(container, node.database, 'tables', depth);
                return;
            }

            if (node.type === 'ViewsFolder') {
                await loadObjectType(container, node.database, 'views', depth);
                return;
            }

            if (node.type === 'ProceduresFolder') {
                await loadObjectType(container, node.database, 'procedures', depth);
            }
        } catch (error) {
            container.innerHTML = `<div class="text-danger small px-3">${escapeHtml(error.message)}</div>`;
        }
    }

    async function loadObjectType(container, database, type, depth) {
        const items = await fetchJson(`/api/explorer/${connectionId}/database/${encodeURIComponent(database)}/objects?type=${type}`);
        container.innerHTML = '';
        items.forEach(item => renderTreeNode(item, container, depth));
    }

    async function handleTableClick(node) {
        const { schema, objectName: table } = parseSchemaObject(node);

        const [scriptData, columns] = await Promise.all([
            fetchJson(`/api/explorer/${connectionId}/database/${encodeURIComponent(node.database)}/table/${encodeURIComponent(schema)}/${encodeURIComponent(table)}/select-top`),
            fetchJson(`/api/explorer/${connectionId}/database/${encodeURIComponent(node.database)}/table/${encodeURIComponent(schema)}/${encodeURIComponent(table)}/columns`)
        ]);

        setActiveSql(scriptData.script ?? scriptData);
        renderColumns(Array.isArray(columns) ? columns : []);
    }

    async function loadHistory() {
        try {
            const history = await fetchJson(`/api/query/history?connectionId=${connectionId}&take=50`);
            els.historyList.innerHTML = '';

            if (!history.length) {
                els.historyList.innerHTML = '<div class="p-3 text-secondary small">History bo\'sh</div>';
                return;
            }

            history.forEach(item => {
                const div = document.createElement('div');
                div.className = `history-item ${item.success ? '' : 'history-error'}`;
                div.innerHTML = `
                    <div class="meta">${item.success ? '✓' : '✗'} ${new Date(item.executedAt).toLocaleString()} — ${item.elapsedMs} ms</div>
                    <div class="sql">${escapeHtml(item.sql)}</div>`;
                div.addEventListener('click', () => setActiveSql(item.sql));
                els.historyList.appendChild(div);
            });
        } catch {
            // ignore
        }
    }

    async function exportCsv() {
        if (!state.lastResult?.rows?.length) return;

        try {
            const response = await fetch('/api/query/export-csv', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    columns: state.lastResult.columns,
                    rows: state.lastResult.rows,
                    fileName: `${state.currentDatabase}-query`
                })
            });

            if (!response.ok) {
                throw new Error(`CSV eksport muvaffaqiyatsiz (${response.status})`);
            }

            const blob = await response.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${state.currentDatabase}-query-${Date.now()}.csv`;
            a.click();
            URL.revokeObjectURL(url);
        } catch (error) {
            appendMessage(`✗ ${error.message}`);
        }
    }

    function toggleTheme() {
        state.theme = state.theme === 'dark' ? 'light' : 'dark';
        localStorage.setItem('sqlws-theme', state.theme);
        applyTheme(state.theme);

        const tab = state.tabs.find(t => t.id === state.activeTabId);
        if (tab?.editor) {
            monaco.editor.setTheme(state.theme === 'dark' ? 'vs-dark' : 'vs');
        }
    }

    function applyTheme(theme) {
        workspace.classList.toggle('light-theme', theme === 'light');
    }

    function formatElapsed(ms) {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        const millis = ms % 1000;
        const pad = (n, l = 2) => String(n).padStart(l, '0');
        return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}.${pad(millis, 3)}`;
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;');
    }
})();
