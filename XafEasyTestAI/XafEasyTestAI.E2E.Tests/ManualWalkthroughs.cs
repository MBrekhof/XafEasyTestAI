using System;
using System.Drawing.Imaging;
using System.IO;
using DevExpress.EasyTest.Framework;
using Xunit;

namespace XafEasyTestAI.Module.E2E.Tests
{
    // Manual-draft generator. NOT assertions — drives the live app and snaps screenshots.
    // Opt-in: run with  dotnet test --filter "kind=manual"  (excluded from normal/CI runs).
    public class ManualWalkthroughs : IDisposable
    {
        const string BlazorAppName = "XafEasyTestAIBlazor";
        const string WinAppName = "XafEasyTestAIWin";
        const string AppDBName = "XafEasyTestAI";
        const string SqlServer = "localhost,1433";
        const string SqlUser = "sa";
        const string SqlPassword = "XafEasy!2026";

        EasyTestFixtureContext FixtureContext { get; } = new EasyTestFixtureContext();

        public ManualWalkthroughs()
        {
            FixtureContext.RegisterApplications(
                new BlazorApplicationOptions(BlazorAppName,
                    string.Format(@"{0}\..\..\..\..\XafEasyTestAI.Blazor.Server", Environment.CurrentDirectory),
                    browser: "Chrome", // chromedriver.exe (matched to installed Chrome) lives in tools/webdriver
                    webDriverPath: string.Format(@"{0}\..\..\..\..\tools\webdriver", Environment.CurrentDirectory)),
                new WinApplicationOptions(WinAppName,
                    string.Format(@"{0}\..\..\..\..\XafEasyTestAI.Win\bin\EasyTest\net8.0-windows\XafEasyTestAI.Win.exe", Environment.CurrentDirectory)));
            FixtureContext.RegisterDatabases(new DatabaseOptions(
                AppDBName, "XafEasyTestAIEasyTest",
                server: SqlServer, userID: SqlUser, password: SqlPassword));
        }

        public void Dispose() => FixtureContext.CloseRunningApplications();

        IApplicationContext StartSeededApp(string appName)
        {
            FixtureContext.DropDB(AppDBName);
            var app = FixtureContext.CreateApplicationContext(appName);
            app.RunApplication();
            app.GetForm().FillForm(("User Name", "Admin"));
            app.GetAction("Log In").Execute();
            return app;
        }

        // Repo docs/manual dir, resolved from the test bin folder (same hop count as the app paths above).
        static string ManualDir =>
            Path.GetFullPath(string.Format(@"{0}\..\..\..\..\..\docs\manual", Environment.CurrentDirectory));

        // ponytail: EasyTest's GetScreen() is cross-platform (Blazor + WinForms), so one capture serves both.
        static Action<string> Capture(IApplicationContext app) => png =>
            app.GetScreen().GetScreenshot().Save(png, ImageFormat.Png);

        [Trait("kind", "manual")]
        [Fact]
        public void CustomerManagement()
        {
            var app = StartSeededApp(BlazorAppName);
            using var doc = new ManualRecorder(ManualDir, "customer-management.md",
                "Managing Customers", DateTime.Now.ToString("yyyy-MM-dd"), Capture(app));

            doc.Note("Customers are the companies you sell to. This section shows how to view and add them.");
            doc.Step("Open the **Customers** list from the navigation menu.",
                () => app.Navigate("Customer"));
            doc.Step("Click **New** to start a new customer.",
                () => app.GetAction("New").Execute());
            doc.Step("Enter the company **Name**, **City** and **Country**.",
                () => app.GetForm().FillForm(("Name", "Acme Corp"), ("City", "Berlin"), ("Country", "Germany")));
            doc.Step("Click **Save**. The new customer now appears in the list.",
                () => { app.GetAction("Save").Execute(); app.Navigate("Customer"); });
        }

        [Trait("kind", "manual")]
        [Fact]
        public void MarkingOrdersShipped()
        {
            var app = StartSeededApp(BlazorAppName);
            using var doc = new ManualRecorder(ManualDir, "orders.md",
                "Shipping Orders", DateTime.Now.ToString("yyyy-MM-dd"), Capture(app));

            doc.Note("When an order is ready to leave, mark it shipped from the Orders list.");
            doc.Step("Open the **Orders** list.", () => app.Navigate("Order"));
            doc.Step("Select the order you want to ship (here, **SO-1001**).",
                () => app.GetGrid().SelectRows("Order Number", "SO-1001"));
            doc.Step("Click **Mark Shipped**. The order's **Status** changes to *Shipped*.",
                () => app.GetAction("Mark Shipped").Execute());
        }

        [Trait("kind", "manual")]
        [Fact]
        public void CompletingProjects()
        {
            var app = StartSeededApp(BlazorAppName);
            using var doc = new ManualRecorder(ManualDir, "projects.md",
                "Completing Projects", DateTime.Now.ToString("yyyy-MM-dd"), Capture(app));

            doc.Note("A project can only be marked **Completed** once all of its tasks are done.");
            doc.Step("Open the **Projects** list.", () => app.Navigate("Project"));
            doc.Step("Open **Website Redesign**, which still has open tasks.",
                () => app.GetGrid().ProcessRow(("Name", "Website Redesign")));
            doc.Step("Set **Status** to *Completed* and click **Save** — the app blocks it and explains why.",
                () => { app.GetForm().FillForm(("Status", "Completed")); app.GetAction("Save").Execute(); });
        }

        // WinForms-only page: nested-grid in-place line entry isn't wired for Blazor yet (see Tests.cs OrderWithLines).
        [Trait("kind", "manual")]
        [Fact]
        public void OrderWithLines()
        {
            var app = StartSeededApp(WinAppName);
            using var doc = new ManualRecorder(ManualDir, "order-lines.md",
                "Creating an Order with Lines (Desktop)", DateTime.Now.ToString("yyyy-MM-dd"), Capture(app));

            doc.Note("This walkthrough uses the **Windows desktop** app. An order groups one or more line items.");
            doc.Step("Open the **Orders** list and click **New**.",
                () => { app.Navigate("Order"); app.GetAction("New").Execute(); });
            doc.Step("Enter the **Order Number** and pick the **Customer**.",
                () => app.GetForm().FillForm(("Order Number", "SO-9001"), ("Customer", "Contoso Ltd")));
            doc.Step("Click **Save** to create the order, then add a line in the **Order Lines** grid.",
                () =>
                {
                    app.GetAction("Save").Execute();
                    var lines = app.GetGrid("Order Lines");
                    lines.InlineNew();
                    lines.FillRow(("Product Name", "Test Widget"), ("Quantity", "3"), ("Unit Price", "10"));
                    lines.InlineUpdate();
                });
            doc.Step("Click **Save** again. The order and its line are stored together.",
                () => app.GetAction("Save").Execute());
        }
    }
}
