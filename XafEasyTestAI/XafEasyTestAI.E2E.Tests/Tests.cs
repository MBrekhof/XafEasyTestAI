using DevExpress.EasyTest.Framework;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// POC: AI-authored EasyTest functional tests for the WinForms XAF host.
// Tests run the REAL XafEasyTestAI.Win.exe (built in the EasyTest configuration) and drive its UI.
// The target DB is a dedicated catalog (XafEasyTestAIEasyTest) on the same Docker SQL Server the app uses.
// Each test drops that catalog; the Win app recreates the schema and re-runs the seed Updater on launch.
//
// Requires: chromedriver/msedgedriver on PATH only if you also run the Blazor variant. Win tests need none,
// but DO need an interactive Windows desktop session (EasyTest drives the real WinForms UI, not headless).
namespace XafEasyTestAI.Module.E2E.Tests
{
    public class XafEasyTestAITests : IDisposable
    {
        const string BlazorAppName = "XafEasyTestAIBlazor";
        const string WinAppName = "XafEasyTestAIWin";
        const string AppDBName = "XafEasyTestAI";

        // Same Docker SQL Server container the running app uses; tests get their own throwaway catalog.
        const string SqlServer = "localhost,1433";
        const string SqlUser = "sa";
        const string SqlPassword = "XafEasy!2026";

        EasyTestFixtureContext FixtureContext { get; } = new EasyTestFixtureContext();

        public XafEasyTestAITests()
        {
            FixtureContext.RegisterApplications(
                new BlazorApplicationOptions(BlazorAppName,
                    string.Format(@"{0}\..\..\..\..\XafEasyTestAI.Blazor.Server", Environment.CurrentDirectory),
                    browser: "Chrome", // chromedriver.exe (matched to installed Chrome) lives here; see docs/EASYTEST-AUTHORING.md.
                    webDriverPath: string.Format(@"{0}\..\..\..\..\tools\webdriver", Environment.CurrentDirectory)),
                new WinApplicationOptions(WinAppName, string.Format(@"{0}\..\..\..\..\XafEasyTestAI.Win\bin\EasyTest\net8.0-windows\XafEasyTestAI.Win.exe", Environment.CurrentDirectory))
            );
            FixtureContext.RegisterDatabases(new DatabaseOptions(
                AppDBName, "XafEasyTestAIEasyTest",
                server: SqlServer, userID: SqlUser, password: SqlPassword));
        }

        public void Dispose() => FixtureContext.CloseRunningApplications();

        // Logs in as Admin (blank password) on a freshly seeded DB and returns the ready app context.
        IApplicationContext StartSeededApp(string applicationName)
        {
            FixtureContext.DropDB(AppDBName);
            var appContext = FixtureContext.CreateApplicationContext(applicationName);
            appContext.RunApplication();
            appContext.GetForm().FillForm(("User Name", "Admin"));
            appContext.GetAction("Log In").Execute();
            return appContext;
        }

        // CASE 1 - Customer CRUD + grid asserts. Verifies seed data, then creates a Customer via the
        // New -> DetailView -> Save flow and confirms it lands in the list.
        [Theory]
        [InlineData(WinAppName)]
        [InlineData(BlazorAppName)]
        public void CustomerCrud(string applicationName)
        {
            var app = StartSeededApp(applicationName);
            app.Navigate("Customer");

            // Seed data is present.
            Assert.True(app.GetGrid().RowExists(("Name", "Contoso Ltd")));
            Assert.True(app.GetGrid().RowExists(("Name", "Fabrikam Inc")));
            Assert.Equal(2, app.GetGrid().GetRowCount());

            // Create a new Customer through the detail view.
            app.GetAction("New").Execute();
            app.GetForm().FillForm(("Name", "Acme Corp"), ("City", "Berlin"), ("Country", "Germany"));
            app.GetAction("Save").Execute();

            // No "Close" — it doesn't exist on Blazor post-save. Navigating away is clean on both
            // platforms once the object is saved (no unsaved changes to prompt about).
            app.Navigate("Customer");
            Assert.True(app.GetGrid().RowExists(("Name", "Acme Corp")));
            Assert.Equal(3, app.GetGrid().GetRowCount());
        }

        // CASE 2 - Master-detail: create an Order with OrderLines and confirm the line landed.
        // Win-only: nested-grid in-place editing (InlineNew/FillRow) differs on Blazor (different
        // InlineEditMode model + edit-form behavior). A Blazor variant needs its own line-entry flow.
        [Theory]
        [InlineData(WinAppName)]
        public void OrderWithLines(string applicationName)
        {
            var app = StartSeededApp(applicationName);
            app.Navigate("Order");

            app.GetAction("New").Execute();
            // NOTE: date editors expect the HOST's short-date culture (e.g. nl-NL = dd-MM-yyyy), which makes
            // literal dates locale-fragile. OrderDate isn't required, so we omit it here. See the authoring doc.
            app.GetForm().FillForm(
                ("Order Number", "SO-9001"),
                ("Customer", "Contoso Ltd"));
            app.GetAction("Save").Execute();

            // Add a line in the nested OrderLines grid on the Order detail view.
            var lines = app.GetGrid("Order Lines");
            lines.InlineNew();
            lines.FillRow(("Product Name", "Test Widget"), ("Quantity", "3"), ("Unit Price", "10"));
            lines.InlineUpdate();

            // Assert while the nested grid is still the active control. After a Save the "Order Lines"
            // locator re-resolves to a toolbar button, so verify the row here, then persist.
            Assert.True(lines.RowExists(("Product Name", "Test Widget")));

            app.GetAction("Save").Execute();
        }

        // CASE 3 - ViewController action + guard: "Mark Shipped" flips a Confirmed order to Shipped.
        [Theory]
        [InlineData(WinAppName)]
        [InlineData(BlazorAppName)]
        public void MarkOrderShipped(string applicationName)
        {
            var app = StartSeededApp(applicationName);
            app.Navigate("Order");

            // Seed: SO-1001 is Confirmed, SO-1002 is already Shipped.
            app.GetGrid().SelectRows("Order Number", "SO-1001");
            app.GetAction("Mark Shipped").Execute();

            int? idx = app.GetGrid().GetRowIndex(("Order Number", "SO-1001"));
            Assert.NotNull(idx);
            var row = app.GetGrid().GetRow(idx.Value, "Order Number", "Status");
            Assert.Equal(new[] { "SO-1001", "Shipped" }, row);
        }

        // CASE 4 - Validation rule: a Project can't be marked Completed while it has open Tasks.
        // Seed: "Website Redesign" is Active with two incomplete tasks, so Save must be blocked.
        [Theory]
        [InlineData(WinAppName)]
        [InlineData(BlazorAppName)]
        public void ProjectCannotCompleteWithOpenTasks(string applicationName)
        {
            var app = StartSeededApp(applicationName);
            app.Navigate("Project");

            app.GetGrid().ProcessRow(("Name", "Website Redesign"));   // open its DetailView
            app.GetForm().FillForm(("Status", "Completed"));
            app.GetAction("Save").Execute();                         // triggers Save-context validation

            var messages = app.GetValidation().GetValidationMessages();
            Assert.Contains(messages, m => m.Contains("open"));      // the rule's message
        }

        // CASE 5 - Positive: a Project with no open tasks CAN be completed (the rule allows the Save).
        // A brand-new project has no tasks, so Tasks.All(IsCompleted) is trivially true => valid.
        [Theory]
        [InlineData(WinAppName)]
        [InlineData(BlazorAppName)]
        public void ProjectCanCompleteWithoutOpenTasks(string applicationName)
        {
            var app = StartSeededApp(applicationName);
            app.Navigate("Project");

            app.GetAction("New").Execute();
            app.GetForm().FillForm(("Name", "Done Project"), ("Status", "Completed"));
            app.GetAction("Save").Execute();                         // no open tasks => save succeeds

            // Proof it persisted: it shows up in the Project list. (No "Close" — absent on Blazor.)
            app.Navigate("Project");
            Assert.True(app.GetGrid().RowExists(("Name", "Done Project")));
        }
    }
}
