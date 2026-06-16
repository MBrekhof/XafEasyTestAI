using System;
using System.IO;
using DevExpress.EasyTest.Framework;
using OpenQA.Selenium;          // ITakesScreenshot
using Xunit;

namespace XafEasyTestAI.Module.E2E.Tests
{
    // Manual-draft generator. NOT assertions — drives the live Blazor app and snaps screenshots.
    // Opt-in: run with  dotnet test --filter "kind=manual"  (excluded from normal/CI runs).
    public class ManualWalkthroughs : IDisposable
    {
        const string BlazorAppName = "XafEasyTestAIBlazor";
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
                    webDriverPath: string.Format(@"{0}\..\..\..\..\tools\webdriver", Environment.CurrentDirectory)));
            FixtureContext.RegisterDatabases(new DatabaseOptions(
                AppDBName, "XafEasyTestAIEasyTest",
                server: SqlServer, userID: SqlUser, password: SqlPassword));
        }

        public void Dispose() => FixtureContext.CloseRunningApplications();

        IApplicationContext StartSeededApp()
        {
            FixtureContext.DropDB(AppDBName);
            var app = FixtureContext.CreateApplicationContext(BlazorAppName);
            app.RunApplication();
            app.GetForm().FillForm(("User Name", "Admin"));
            app.GetAction("Log In").Execute();
            return app;
        }

        // Repo docs/manual dir, resolved from the test bin folder (same hop count as the app paths above).
        static string ManualDir =>
            Path.GetFullPath(string.Format(@"{0}\..\..\..\..\..\docs\manual", Environment.CurrentDirectory));

        // ponytail: the WinForms seam. Today: Selenium screenshot. Later: branch on host here.
        static Action<string> BlazorCapture(IApplicationContext app) => png =>
        {
            var driver = (IWebDriver)app.AsBlazor().WebDriver;
            ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(png);
        };

        [Trait("kind", "manual")]
        [Fact]
        public void CustomerManagement()
        {
            var app = StartSeededApp();
            using var doc = new ManualRecorder(ManualDir, "customer-management.md",
                "Managing Customers", DateTime.Now.ToString("yyyy-MM-dd"), BlazorCapture(app));

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
    }
}
