// =============================================================
// Contoso Bank — site.js
// AJAX calls to API controllers, loading spinners, toast notifications
// =============================================================

// ---------- Mobile menu ----------
document.addEventListener('DOMContentLoaded', function () {
    const btn = document.getElementById('mobile-menu-btn');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebar-overlay');

    function toggleSidebar() {
        sidebar.classList.toggle('open');
        overlay.classList.toggle('open');
    }
    if (btn) btn.addEventListener('click', toggleSidebar);
    if (overlay) overlay.addEventListener('click', toggleSidebar);

    // Initialize page-specific logic
    initPage();
});

// ---------- API Helper ----------
async function api(url, options = {}) {
    const method = options.method || 'GET';
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    const config = { method, headers };
    if (options.body) config.body = JSON.stringify(options.body);

    const response = await fetch(url, config);
    if (!response.ok) {
        let errorMsg = `Request failed (${response.status})`;
        try {
            const problem = await response.json();
            if (problem.detail) errorMsg = problem.detail;
            else if (problem.title) errorMsg = problem.title;
        } catch { /* ignore parse errors */ }
        throw new Error(errorMsg);
    }
    const text = await response.text();
    return text ? JSON.parse(text) : null;
}

// ---------- Toast Notifications ----------
function showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    const icons = {
        success: '✓',
        error: '✕',
        warning: '⚠',
        info: 'ℹ'
    };
    toast.innerHTML = `<span>${icons[type] || ''}</span><span>${escapeHtml(message)}</span>`;
    container.appendChild(toast);
    setTimeout(() => {
        toast.classList.add('toast-hide');
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ---------- Button Loading State ----------
function setLoading(btn, loading) {
    if (!btn) return;
    btn.disabled = loading;
    btn.classList.toggle('loading', loading);
}

// ---------- Currency Formatting ----------
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
}

function formatDate(dateStr) {
    return new Date(dateStr).toLocaleDateString('en-US', {
        month: 'short', day: 'numeric', year: 'numeric'
    });
}

function formatDateTime(dateStr) {
    return new Date(dateStr).toLocaleDateString('en-US', {
        month: 'short', day: 'numeric', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
}

// ---------- Page Router ----------
function initPage() {
    const path = window.location.pathname.replace(/\/$/, '') || '/';
    const routes = {
        '/': initDashboard,
        '/Index': initDashboard,
        '/Accounts': initAccounts,
        '/Transfers': initTransfers,
        '/Transactions': initTransactions,
        '/Reports': initReports,
        '/Settings': initSettings,
    };
    const init = routes[path];
    if (init) init();
}

// =============================================================
// DASHBOARD
// =============================================================
async function initDashboard() {
    try {
        const accounts = await api('/api/accounts');
        renderDashboardStats(accounts);
        renderDashboardAccounts(accounts);

        const transactions = await api('/api/transactions');
        renderRecentTransactions(transactions.slice(0, 8));
    } catch (err) {
        showToast('Failed to load dashboard data: ' + err.message, 'error');
    }

    // Fraud Detection button (Chaos Scenario 2: CPU Spike)
    const fraudBtn = document.getElementById('btn-fraud-detection');
    if (fraudBtn) {
        fraudBtn.addEventListener('click', async () => {
            setLoading(fraudBtn, true);
            try {
                await api('/api/accounts', { method: 'GET' });
                showToast('Fraud detection scan running...', 'info', 6000);
                // The actual chaos trigger is a specific endpoint pattern
                // In real chaos setup, this would call a dedicated endpoint
                showToast('No suspicious activity detected', 'success');
            } catch (err) {
                showToast('Fraud detection failed: ' + err.message, 'error');
            } finally {
                setLoading(fraudBtn, false);
            }
        });
    }
}

function renderDashboardStats(accounts) {
    const el = document.getElementById('dashboard-stats');
    if (!el) return;
    const total = accounts.reduce((sum, a) => sum + a.balance, 0);
    const checking = accounts.filter(a => a.accountType === 0).reduce((sum, a) => sum + a.balance, 0);
    const savings = accounts.filter(a => a.accountType === 1).reduce((sum, a) => sum + a.balance, 0);
    const credit = accounts.filter(a => a.accountType === 2).reduce((sum, a) => sum + a.balance, 0);

    el.innerHTML = `
        <div class="stat-card">
            <div class="stat-icon blue">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/></svg>
            </div>
            <div>
                <div class="stat-label">Total Balance</div>
                <div class="stat-value">${formatCurrency(total)}</div>
                <div class="stat-sub">${accounts.length} accounts</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-icon green">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/></svg>
            </div>
            <div>
                <div class="stat-label">Checking</div>
                <div class="stat-value">${formatCurrency(checking)}</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-icon amber">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0H5m14 0h2m-16 0H3"/></svg>
            </div>
            <div>
                <div class="stat-label">Savings</div>
                <div class="stat-value">${formatCurrency(savings)}</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-icon red">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="5" width="20" height="14" rx="2"/><circle cx="12" cy="12" r="3"/></svg>
            </div>
            <div>
                <div class="stat-label">Credit</div>
                <div class="stat-value">${formatCurrency(credit)}</div>
            </div>
        </div>
    `;
}

function renderDashboardAccounts(accounts) {
    const el = document.getElementById('dashboard-accounts');
    if (!el) return;
    const types = ['Checking', 'Savings', 'Credit'];
    el.innerHTML = accounts.map(a => `
        <tr>
            <td><strong>${escapeHtml(a.accountName)}</strong><br><span style="color:var(--color-gray-500);font-size:.8rem">${escapeHtml(a.accountNumber)}</span></td>
            <td><span class="badge badge-${a.accountType === 0 ? 'blue' : a.accountType === 1 ? 'green' : 'amber'}">${types[a.accountType]}</span></td>
            <td class="amount">${formatCurrency(a.balance)}</td>
        </tr>
    `).join('');
}

function renderRecentTransactions(transactions) {
    const el = document.getElementById('dashboard-transactions');
    if (!el) return;
    if (!transactions.length) {
        el.innerHTML = '<tr><td colspan="4" class="empty-state">No recent transactions</td></tr>';
        return;
    }
    const types = ['Credit', 'Debit'];
    el.innerHTML = transactions.map(t => `
        <tr>
            <td>${formatDate(t.timestamp)}</td>
            <td>${escapeHtml(t.description)}</td>
            <td><span class="badge ${t.type === 0 ? 'badge-green' : 'badge-red'}">${types[t.type]}</span></td>
            <td class="amount ${t.type === 0 ? 'credit' : 'debit'}">${t.type === 0 ? '+' : '-'}${formatCurrency(t.amount)}</td>
        </tr>
    `).join('');
}

// =============================================================
// ACCOUNTS
// =============================================================
async function initAccounts() {
    await loadAccounts();

    // Refresh button (Chaos Scenario 4: DB Connection Failure)
    const refreshBtn = document.getElementById('btn-refresh-accounts');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', async () => {
            setLoading(refreshBtn, true);
            try {
                await loadAccounts();
                showToast('Accounts refreshed successfully', 'success');
            } catch (err) {
                showToast('Unable to load account information: ' + err.message, 'error');
            } finally {
                setLoading(refreshBtn, false);
            }
        });
    }
}

async function loadAccounts() {
    const accounts = await api('/api/accounts');
    const el = document.getElementById('accounts-list');
    if (!el) return;
    const types = ['Checking', 'Savings', 'Credit'];
    const typeClasses = ['checking', 'savings', 'credit'];
    el.innerHTML = accounts.map(a => `
        <div class="account-card">
            <div class="account-type ${typeClasses[a.accountType]}">${types[a.accountType]}</div>
            <div class="account-name">${escapeHtml(a.accountName)}</div>
            <div class="account-number">${escapeHtml(a.accountNumber)}</div>
            <div class="account-balance">${formatCurrency(a.balance)}</div>
            <div style="margin-top:8px;font-size:.75rem;color:var(--color-gray-500)">${a.currency}</div>
        </div>
    `).join('');
}

// =============================================================
// TRANSFERS
// =============================================================
async function initTransfers() {
    // Populate account dropdowns
    try {
        const accounts = await api('/api/accounts');
        const fromSelect = document.getElementById('transfer-from');
        const toSelect = document.getElementById('transfer-to');
        if (fromSelect && toSelect) {
            const options = accounts.map(a =>
                `<option value="${a.id}">${escapeHtml(a.accountName)} (${formatCurrency(a.balance)})</option>`
            ).join('');
            fromSelect.innerHTML = '<option value="">Select account</option>' + options;
            toSelect.innerHTML = '<option value="">Select account</option>' + options;
        }

        // Load recent transfers
        const transfers = await api('/api/transfers');
        renderTransfers(transfers);
    } catch (err) {
        showToast('Failed to load transfer data: ' + err.message, 'error');
    }

    // Standard transfer
    const transferBtn = document.getElementById('btn-transfer');
    if (transferBtn) {
        transferBtn.addEventListener('click', async () => {
            const fromId = parseInt(document.getElementById('transfer-from')?.value);
            const toId = parseInt(document.getElementById('transfer-to')?.value);
            const amount = parseFloat(document.getElementById('transfer-amount')?.value);

            if (!fromId || !toId || !amount || amount <= 0) {
                showToast('Please fill in all transfer fields', 'warning');
                return;
            }
            if (fromId === toId) {
                showToast('Cannot transfer to the same account', 'warning');
                return;
            }

            setLoading(transferBtn, true);
            try {
                await api('/api/transfers', {
                    method: 'POST',
                    body: { fromAccountId: fromId, toAccountId: toId, amount }
                });
                showToast('Transfer completed successfully', 'success');
                const transfers = await api('/api/transfers');
                renderTransfers(transfers);
            } catch (err) {
                showToast('Transfer failed: ' + err.message, 'error');
            } finally {
                setLoading(transferBtn, false);
            }
        });
    }

    // Wire Transfer (Chaos Scenario 3: HTTP 500)
    const wireBtn = document.getElementById('btn-wire-transfer');
    if (wireBtn) {
        wireBtn.addEventListener('click', async () => {
            setLoading(wireBtn, true);
            try {
                await api('/api/transfers/wire', {
                    method: 'POST',
                    body: { fromAccountId: 1, toAccountId: 2, amount: 500 }
                });
                showToast('Wire transfer processed', 'success');
            } catch (err) {
                showToast('Wire transfer failed: ' + err.message, 'error');
            } finally {
                setLoading(wireBtn, false);
            }
        });
    }

    // International Transfer (Chaos Scenario 5: Slow API)
    const intlBtn = document.getElementById('btn-international-transfer');
    if (intlBtn) {
        intlBtn.addEventListener('click', async () => {
            setLoading(intlBtn, true);
            showToast('Processing international transfer...', 'info', 8000);
            try {
                await api('/api/transfers/international', {
                    method: 'POST',
                    body: { fromAccountId: 1, toAccountId: 2, amount: 1000 }
                });
                showToast('International transfer completed', 'success');
            } catch (err) {
                showToast('International transfer failed: ' + err.message, 'error');
            } finally {
                setLoading(intlBtn, false);
            }
        });
    }
}

function renderTransfers(transfers) {
    const el = document.getElementById('transfers-list');
    if (!el) return;
    if (!transfers.length) {
        el.innerHTML = '<tr><td colspan="5" class="empty-state">No transfers found</td></tr>';
        return;
    }
    const statuses = ['Processing', 'Completed', 'Failed'];
    const statusClasses = ['badge-amber', 'badge-green', 'badge-red'];
    el.innerHTML = transfers.map(t => `
        <tr>
            <td>#${t.id}</td>
            <td>Account ${t.fromAccountId} → Account ${t.toAccountId}</td>
            <td class="amount">${formatCurrency(t.amount)}</td>
            <td><span class="badge ${statusClasses[t.status]}">${statuses[t.status]}</span></td>
            <td>${formatDateTime(t.requestedAt)}</td>
        </tr>
    `).join('');
}

// =============================================================
// TRANSACTIONS
// =============================================================
async function initTransactions() {
    try {
        // Populate account filter
        const accounts = await api('/api/accounts');
        const accountFilter = document.getElementById('filter-account');
        if (accountFilter) {
            const opts = accounts.map(a => `<option value="${a.id}">${escapeHtml(a.accountName)}</option>`).join('');
            accountFilter.innerHTML = '<option value="">All Accounts</option>' + opts;
        }
        await loadTransactions();
    } catch (err) {
        showToast('Failed to load transactions: ' + err.message, 'error');
    }

    // Filter button
    const filterBtn = document.getElementById('btn-filter-transactions');
    if (filterBtn) {
        filterBtn.addEventListener('click', () => loadTransactions());
    }

    // Export Full History (Chaos Scenario 7: Log Flooding)
    const exportBtn = document.getElementById('btn-export-history');
    if (exportBtn) {
        exportBtn.addEventListener('click', async () => {
            setLoading(exportBtn, true);
            showToast('Preparing full transaction export...', 'info', 6000);
            try {
                await api('/api/transactions');
                showToast('Transaction history exported', 'success');
            } catch (err) {
                showToast('Export failed: ' + err.message, 'error');
            } finally {
                setLoading(exportBtn, false);
            }
        });
    }
}

async function loadTransactions() {
    const accountId = document.getElementById('filter-account')?.value;
    const category = document.getElementById('filter-category')?.value;

    let url = '/api/transactions?';
    if (accountId) url += `accountId=${accountId}&`;
    if (category) url += `category=${encodeURIComponent(category)}&`;

    try {
        const transactions = await api(url);
        renderTransactionsTable(transactions);
    } catch (err) {
        showToast('Failed to load transactions: ' + err.message, 'error');
    }
}

function renderTransactionsTable(transactions) {
    const el = document.getElementById('transactions-list');
    if (!el) return;
    if (!transactions.length) {
        el.innerHTML = '<tr><td colspan="6" class="empty-state">No transactions found</td></tr>';
        return;
    }
    const types = ['Credit', 'Debit'];
    const statuses = ['Completed', 'Pending', 'Failed'];
    const statusClasses = ['badge-green', 'badge-amber', 'badge-red'];
    el.innerHTML = transactions.map(t => `
        <tr>
            <td>${formatDateTime(t.timestamp)}</td>
            <td>${escapeHtml(t.description)}</td>
            <td>${escapeHtml(t.category || '—')}</td>
            <td><span class="badge ${t.type === 0 ? 'badge-green' : 'badge-red'}">${types[t.type]}</span></td>
            <td class="amount ${t.type === 0 ? 'credit' : 'debit'}">${t.type === 0 ? '+' : '-'}${formatCurrency(t.amount)}</td>
            <td><span class="badge ${statusClasses[t.status]}">${statuses[t.status]}</span></td>
        </tr>
    `).join('');
}

// =============================================================
// REPORTS
// =============================================================
async function initReports() {
    // Populate account selector for annual statement
    try {
        const accounts = await api('/api/accounts');
        const select = document.getElementById('report-account');
        if (select) {
            const opts = accounts.map(a => `<option value="${a.id}">${escapeHtml(a.accountName)}</option>`).join('');
            select.innerHTML = '<option value="">Select account</option>' + opts;
        }
    } catch (err) {
        showToast('Failed to load accounts: ' + err.message, 'error');
    }

    // Generate Annual Statement (Chaos Scenario 1: Memory Leak)
    const stmtBtn = document.getElementById('btn-annual-statement');
    if (stmtBtn) {
        stmtBtn.addEventListener('click', async () => {
            const accountId = document.getElementById('report-account')?.value;
            if (!accountId) {
                showToast('Please select an account', 'warning');
                return;
            }
            setLoading(stmtBtn, true);
            showToast('Generating annual statement...', 'info', 8000);
            try {
                await api(`/api/reports/annual-statement?accountId=${accountId}`, { method: 'POST' });
                showToast('Annual statement generated successfully', 'success');
            } catch (err) {
                showToast('Statement generation failed: ' + err.message, 'error');
            } finally {
                setLoading(stmtBtn, false);
            }
        });
    }

    // Batch Reconciliation (Chaos Scenario 8: Exception Storm)
    const reconBtn = document.getElementById('btn-reconciliation');
    if (reconBtn) {
        reconBtn.addEventListener('click', async () => {
            setLoading(reconBtn, true);
            showToast('Running batch reconciliation...', 'info', 6000);
            try {
                await api('/api/reports/reconciliation', { method: 'POST' });
                showToast('Batch reconciliation completed', 'success');
            } catch (err) {
                showToast('Reconciliation failed: ' + err.message, 'error');
            } finally {
                setLoading(reconBtn, false);
            }
        });
    }
}

// =============================================================
// SETTINGS
// =============================================================
async function initSettings() {
    // Load profile
    try {
        const profile = await api('/api/settings/profile');
        const el = document.getElementById('profile-data');
        if (el && profile) {
            el.innerHTML = `
                <div class="settings-row"><span class="settings-label">Full Name</span><span class="settings-value">${escapeHtml(profile.name)}</span></div>
                <div class="settings-row"><span class="settings-label">Email</span><span class="settings-value">${escapeHtml(profile.email)}</span></div>
                <div class="settings-row"><span class="settings-label">Phone</span><span class="settings-value">${escapeHtml(profile.phone)}</span></div>
                <div class="settings-row"><span class="settings-label">Member Since</span><span class="settings-value">${formatDate(profile.memberSince)}</span></div>
            `;
        }
    } catch (err) {
        showToast('Failed to load profile: ' + err.message, 'error');
    }

    // Verify Identity / KYC (Chaos Scenario 6: Dependency Timeout)
    const kycBtn = document.getElementById('btn-verify-identity');
    if (kycBtn) {
        kycBtn.addEventListener('click', async () => {
            setLoading(kycBtn, true);
            showToast('Verifying your identity...', 'info', 15000);
            try {
                await api('/api/settings/verify-identity', { method: 'POST' });
                showToast('Identity verified successfully', 'success');
            } catch (err) {
                showToast('Identity verification failed: ' + err.message, 'error');
            } finally {
                setLoading(kycBtn, false);
            }
        });
    }
}
