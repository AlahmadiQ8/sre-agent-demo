using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ContosoBank.Tests.Integration;

[Collection("Integration")]
public class RazorPageTests
{
    private readonly HttpClient _client;

    public RazorPageTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // --- All 6 pages load without errors ---

    [Theory]
    [InlineData("/", "Dashboard")]
    [InlineData("/Index", "Dashboard")]
    [InlineData("/Accounts", "Accounts")]
    [InlineData("/Transfers", "Transfers")]
    [InlineData("/Transactions", "Transactions")]
    [InlineData("/Reports", "Reports")]
    [InlineData("/Settings", "Settings")]
    public async Task Page_Returns200_AndContainsTitle(string url, string expectedTitle)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedTitle, content);
    }

    // --- Sidebar navigation present on all pages ---

    [Theory]
    [InlineData("/")]
    [InlineData("/Accounts")]
    [InlineData("/Transfers")]
    [InlineData("/Transactions")]
    [InlineData("/Reports")]
    [InlineData("/Settings")]
    public async Task Page_ContainsSidebarNavigation(string url)
    {
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("sidebar", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Dashboard", content);
        Assert.Contains("Accounts", content);
        Assert.Contains("Transfers", content);
        Assert.Contains("Transactions", content);
        Assert.Contains("Reports", content);
        Assert.Contains("Settings", content);
    }

    // --- Active page highlighting ---

    [Theory]
    [InlineData("/")]
    [InlineData("/Accounts")]
    [InlineData("/Transfers")]
    [InlineData("/Transactions")]
    [InlineData("/Reports")]
    [InlineData("/Settings")]
    public async Task Page_HighlightsActiveNavLink(string url)
    {
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        // The active nav link should have the "active" CSS class
        Assert.Contains("nav-link active", content);
    }

    // --- Dashboard page content ---

    [Fact]
    public async Task Dashboard_ContainsExpectedSections()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("dashboard-stats", content);
        Assert.Contains("dashboard-accounts", content);
        Assert.Contains("dashboard-transactions", content);
        Assert.Contains("Quick Actions", content);
        Assert.Contains("btn-fraud-detection", content);
    }

    // --- Accounts page content ---

    [Fact]
    public async Task Accounts_ContainsRefreshButton()
    {
        var response = await _client.GetAsync("/Accounts");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Your Accounts", content);
        Assert.Contains("btn-refresh-accounts", content);
        Assert.Contains("Refresh", content);
    }

    // --- Transfers page content ---

    [Fact]
    public async Task Transfers_ContainsFormAndChaosButtons()
    {
        var response = await _client.GetAsync("/Transfers");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("New Transfer", content);
        Assert.Contains("transfer-from", content);
        Assert.Contains("transfer-to", content);
        Assert.Contains("transfer-amount", content);
        Assert.Contains("btn-transfer", content);
        Assert.Contains("btn-wire-transfer", content);
        Assert.Contains("btn-international-transfer", content);
        Assert.Contains("Process Wire Transfer", content);
        Assert.Contains("International Transfer", content);
    }

    // --- Transactions page content ---

    [Fact]
    public async Task Transactions_ContainsFilterAndExportButton()
    {
        var response = await _client.GetAsync("/Transactions");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Transaction History", content);
        Assert.Contains("filter-account", content);
        Assert.Contains("filter-category", content);
        Assert.Contains("btn-export-history", content);
        Assert.Contains("Export Full History", content);
    }

    // --- Reports page content ---

    [Fact]
    public async Task Reports_ContainsChaosButtons()
    {
        var response = await _client.GetAsync("/Reports");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Reports & Statements", content);
        Assert.Contains("Annual Statement", content);
        Assert.Contains("Batch Reconciliation", content);
        Assert.Contains("btn-annual-statement", content);
        Assert.Contains("btn-reconciliation", content);
        Assert.Contains("Generate Annual Statement", content);
        Assert.Contains("Run Batch Reconciliation", content);
    }

    // --- Settings page content ---

    [Fact]
    public async Task Settings_ContainsProfileAndKycButton()
    {
        var response = await _client.GetAsync("/Settings");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Account Settings", content);
        Assert.Contains("Profile Information", content);
        Assert.Contains("Security & Verification", content);
        Assert.Contains("btn-verify-identity", content);
        Assert.Contains("Verify Identity (KYC)", content);
        Assert.Contains("Notification Preferences", content);
    }

    // --- Mobile-friendly layout elements ---

    [Fact]
    public async Task Layout_ContainsMobileElements()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("mobile-header", content);
        Assert.Contains("mobile-menu-btn", content);
        Assert.Contains("sidebar-overlay", content);
        Assert.Contains("viewport", content);
    }

    // --- Layout contains toast container for notifications ---

    [Fact]
    public async Task Layout_ContainsToastContainer()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("toast-container", content);
    }

    // --- CSS and JS assets are referenced ---

    [Fact]
    public async Task Layout_ReferencesStaticAssets()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // .NET 10 MapStaticAssets fingerprints file names (e.g., site.abc123.css)
        Assert.Matches(@"/css/site\.\w+\.css", content);
        Assert.Matches(@"/js/site\.\w+\.js", content);
    }

    // --- Static assets are served ---

    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/js/site.js")]
    public async Task StaticAsset_Returns200(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- CSS contains banking theme styles ---

    [Fact]
    public async Task SiteCss_ContainsBankingTheme()
    {
        var response = await _client.GetAsync("/css/site.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Contains("--color-navy-900", css);
        Assert.Contains(".sidebar", css);
        Assert.Contains(".nav-link", css);
        Assert.Contains(".card", css);
        Assert.Contains(".toast", css);
        Assert.Contains(".btn-primary", css);
        Assert.Contains(".data-table", css);
    }

    // --- JS contains AJAX and toast functions ---

    [Fact]
    public async Task SiteJs_ContainsRequiredFunctions()
    {
        var response = await _client.GetAsync("/js/site.js");
        var js = await response.Content.ReadAsStringAsync();

        Assert.Contains("async function api(", js);
        Assert.Contains("function showToast(", js);
        Assert.Contains("function setLoading(", js);
        Assert.Contains("function initDashboard(", js);
        Assert.Contains("function initAccounts(", js);
        Assert.Contains("function initTransfers(", js);
        Assert.Contains("function initTransactions(", js);
        Assert.Contains("function initReports(", js);
        Assert.Contains("function initSettings(", js);
    }
}
