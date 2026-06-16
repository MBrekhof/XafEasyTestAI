# EasyTest-Driven Manual Generator Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Drive the live Blazor XAF app through its real user workflows and emit a captioned,
screenshot-bearing markdown draft of a user manual.

**Architecture:** A tiny `ManualRecorder` buffers markdown (`Note` = paragraph, `Step` = `###`
caption + screenshot) and, on dispose, writes one `.md` per scenario under `docs/manual/`.
Screenshot capture is an **injected `Action<string>` capture function** — a real one driving
`app.AsBlazor().WebDriver` for live runs, a fake one for unit tests. `ManualWalkthroughs` holds one
method per scenario, reusing the existing EasyTest fixture (`StartSeededApp`, DB-drop, Blazor
driver). The captions are the first-draft prose — no AI post-processing.

**Tech Stack:** .NET 8, xunit.v3, DevExpress EasyTest, Selenium (`ITakesScreenshot`), DevExpress XAF
25.2 Blazor host.

**Design doc:** `docs/plans/2026-06-16-easytest-manual-generator-design.md`

**Branch:** `CreatingManual`

---

## Conventions for this plan

- All paths are relative to repo root `C:\Projects\XafEasyTestAI`.
- The E2E project folder is `XafEasyTestAI/XafEasyTestAI.E2E.Tests/`.
- `ManualRecorder`'s markdown logic is **pure** (no app) → unit-testable under plain `dotnet test`.
  The walkthroughs are **integration** → verified by a real EasyTest run, not by red-green.
- Run prereqs for the integration parts (Tasks 4-5): SQL container up (`docker start xafeasy-sql`),
  Blazor host built `-c EasyTest`, version-matched `msedgedriver.exe` in `tools/webdriver/`. See
  `docs/EASYTEST-AUTHORING.md`.

---

### Task 1: `ManualRecorder` — markdown buffer (TDD, no app)

**Files:**
- Create: `XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualRecorder.cs`
- Test: `XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualRecorderTests.cs`

**Step 1: Write the failing test**

```csharp
using System.IO;
using Xunit;

namespace XafEasyTestAI.Module.E2E.Tests
{
    public class ManualRecorderTests
    {
        [Fact]
        public void Emits_banner_title_notes_and_captioned_screenshots()
        {
            var dir = Path.Combine(Path.GetTempPath(), "manualrec-" + Path.GetRandomFileName());
            var captured = new System.Collections.Generic.List<string>();

            using (var doc = new ManualRecorder(
                outDir: dir, fileName: "customer-management.md",
                title: "Managing Customers", generatedOn: "2026-06-16",
                capture: png => captured.Add(png)))   // fake capture: just record the path
            {
                doc.Note("Customers represent the companies you sell to.");
                doc.Step("Open the **Customers** list.", () => { });
                doc.Step("Click **New**.", () => { });
            }

            var md = File.ReadAllText(Path.Combine(dir, "customer-management.md"));
            Assert.Contains("# Managing Customers", md);
            Assert.Contains("_Draft", md);                       // banner present
            Assert.Contains("2026-06-16", md);
            Assert.Contains("Customers represent the companies", md);
            Assert.Contains("### Open the **Customers** list.", md);
            Assert.Contains("![](img/customer-management-01.png)", md);
            Assert.Contains("![](img/customer-management-02.png)", md);
            Assert.Equal(2, captured.Count);                     // one capture per Step, not per Note
            Assert.EndsWith("customer-management-02.png", captured[1]);
        }

        [Fact]
        public void Step_runs_its_action()
        {
            var dir = Path.Combine(Path.GetTempPath(), "manualrec-" + Path.GetRandomFileName());
            var ran = false;
            using (var doc = new ManualRecorder(dir, "x.md", "X", "2026-06-16", _ => { }))
                doc.Step("do it", () => ran = true);
            Assert.True(ran);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test XafEasyTestAI.slnx --filter "FullyQualifiedName~ManualRecorderTests"`
Expected: FAIL — `ManualRecorder` does not exist (compile error).

**Step 3: Write minimal implementation**

```csharp
using System;
using System.IO;
using System.Text;

namespace XafEasyTestAI.Module.E2E.Tests
{
    // ponytail: a markdown buffer with one host-bound seam (the injected capture func).
    // No app dependency here on purpose — keeps the markdown logic unit-testable and makes
    // WinForms a one-lambda swap later.
    public sealed class ManualRecorder : IDisposable
    {
        readonly string _outDir;
        readonly string _fileName;
        readonly Action<string> _capture;   // given an absolute png path, save a screenshot
        readonly StringBuilder _md = new();
        readonly string _slug;
        int _step;

        public ManualRecorder(string outDir, string fileName, string title,
                              string generatedOn, Action<string> capture)
        {
            _outDir = outDir;
            _fileName = fileName;
            _capture = capture;
            _slug = Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(Path.Combine(_outDir, "img"));

            _md.AppendLine($"# {title}");
            _md.AppendLine($"_Draft — generated from a live Blazor run on {generatedOn}. Review and edit._");
            _md.AppendLine();
        }

        public void Note(string markdown)
        {
            _md.AppendLine(markdown);
            _md.AppendLine();
        }

        public void Step(string caption, Action action)
        {
            action();
            _step++;
            var name = $"{_slug}-{_step:00}.png";
            _capture(Path.Combine(_outDir, "img", name));
            _md.AppendLine($"### {caption}");
            _md.AppendLine($"![](img/{name})");
            _md.AppendLine();
        }

        public void Dispose() =>
            File.WriteAllText(Path.Combine(_outDir, _fileName), _md.ToString());
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test XafEasyTestAI.slnx --filter "FullyQualifiedName~ManualRecorderTests"`
Expected: PASS (2 tests).

**Step 5: Commit**

```bash
git add XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualRecorder.cs \
        XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualRecorderTests.cs
git commit -m "feat: ManualRecorder markdown buffer with injected capture seam"
```

---

### Task 2: Blazor capture function + shared walkthrough fixture

This is host-bound (drives Selenium) — verified by compile now and a real run in Task 4. No unit
test (no app available under plain `dotnet test`).

**Files:**
- Create: `XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualWalkthroughs.cs`

**Step 1: Write the fixture + capture helper (no scenarios yet)**

Mirror the setup in `Tests.cs:28-53` so the walkthroughs reuse the exact same fixture wiring.

```csharp
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
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build XafEasyTestAI.slnx`
Expected: `0 Error(s)`.

> If `app.AsBlazor().WebDriver` or `SaveAsFile` doesn't resolve, this is the one design assumption to
> reconcile: inspect the EasyTest `BlazorAdapter` surface (the TODO's "screenshot-on-failure" note
> says the Selenium driver is reachable) and adjust the two lines in `BlazorCapture`. Do not proceed
> until it compiles. Check `OpenQA.Selenium` is referenced (it comes transitively via the Blazor
> EasyTest adapter; add the package only if missing).

**Step 3: Commit**

```bash
git add XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualWalkthroughs.cs
git commit -m "feat: Blazor walkthrough fixture + Selenium capture seam"
```

---

### Task 3: First scenario — Customer management

**Files:**
- Modify: `XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualWalkthroughs.cs`

**Step 1: Add the scenario method** (inside the class, after `BlazorCapture`)

```csharp
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
```

**Step 2: Build the Blazor host in EasyTest config**

Run: `dotnet build XafEasyTestAI/XafEasyTestAI.Blazor.Server/XafEasyTestAI.Blazor.Server.csproj -c EasyTest`
Expected: `0 Error(s)`. (Also confirm `docker start xafeasy-sql` and the Edge driver are in place.)

**Step 3: Run the walkthrough**

Run: `dotnet test XafEasyTestAI.slnx --filter "FullyQualifiedName~CustomerManagement"`
Expected: PASS, and `docs/manual/customer-management.md` + `docs/manual/img/customer-management-0{1..4}.png` exist.

**Step 4: Verify visually (REQUIRED — webapp-testing rule)**

Open the 4 PNGs and the markdown. Confirm: shot 01 = Customers list, 02 = blank new detail view,
03 = filled form, 04 = list containing "Acme Corp". Each caption matches its image. If a shot is
blank/wrong, the capture timing needs a settle (add a short wait inside the `Step` action before the
snap) — diagnose with systematic-debugging, don't guess.

**Step 5: Commit**

```bash
git add XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualWalkthroughs.cs docs/manual/
git commit -m "feat: customer-management manual walkthrough + generated draft"
```

---

### Task 4: Remaining scenarios — orders + projects

**Files:**
- Modify: `XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualWalkthroughs.cs`

**Step 1: Add two more scenario methods** (mirror the green Blazor tests `Tests.cs:117` and `:137/:155`)

```csharp
[Trait("kind", "manual")]
[Fact]
public void MarkingOrdersShipped()
{
    var app = StartSeededApp();
    using var doc = new ManualRecorder(ManualDir, "orders.md",
        "Shipping Orders", DateTime.Now.ToString("yyyy-MM-dd"), BlazorCapture(app));

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
    var app = StartSeededApp();
    using var doc = new ManualRecorder(ManualDir, "projects.md",
        "Completing Projects", DateTime.Now.ToString("yyyy-MM-dd"), BlazorCapture(app));

    doc.Note("A project can only be marked **Completed** once all of its tasks are done.");
    doc.Step("Open the **Projects** list.", () => app.Navigate("Project"));
    doc.Step("Open **Website Redesign**, which still has open tasks.",
        () => app.GetGrid().ProcessRow(("Name", "Website Redesign")));
    doc.Step("Set **Status** to *Completed* and click **Save** — the app blocks it and explains why.",
        () => { app.GetForm().FillForm(("Status", "Completed")); app.GetAction("Save").Execute(); });
}
```

**Step 2: Run both**

Run: `dotnet test XafEasyTestAI.slnx --filter "kind=manual"`
Expected: 3 PASS (Customer + the two new). `docs/manual/orders.md`, `projects.md` and their `img/`
shots exist.

**Step 3: Verify visually (REQUIRED)**

Open the new PNGs + markdown. `orders` shot 03 shows SO-1001 as *Shipped*; `projects` last shot
shows the validation message. Captions match images.

**Step 4: Commit**

```bash
git add XafEasyTestAI/XafEasyTestAI.E2E.Tests/ManualWalkthroughs.cs docs/manual/
git commit -m "feat: orders + projects manual walkthroughs + generated drafts"
```

---

### Task 5: Index page + docs/handoff update

**Files:**
- Create: `docs/manual/README.md`
- Modify: `TODO.md`, `SESSION_HANDOFF.md`

**Step 1: Write the manual index** (`docs/manual/README.md`)

```markdown
# XafEasyTestAI — User Manual (draft)

Auto-generated from live Blazor EasyTest walkthroughs (`ManualWalkthroughs.cs`).
Regenerate with: `dotnet test --filter "kind=manual"` (Blazor host built `-c EasyTest`).
These are **drafts** — review and edit the prose; screenshots refresh on each run.

- [Managing Customers](customer-management.md)
- [Shipping Orders](orders.md)
- [Completing Projects](projects.md)
```

**Step 2: Update `TODO.md`** — under "Tooling / reuse" add:

```markdown
- [ ] **WinForms manual variant.** Swap `BlazorCapture` for a WinForms screen-capture lambda
      (GDI/PrintWindow) at the seam in `ManualWalkthroughs.cs`; enables a Win `OrderWithLines` page.
```

**Step 3: Update `SESSION_HANDOFF.md`** — add a short "manual generator" bullet to the latest
session notes (what it is, how to run it, that drafts live in `docs/manual/`).

**Step 4: Commit**

```bash
git add docs/manual/README.md TODO.md SESSION_HANDOFF.md
git commit -m "docs: manual index + TODO/handoff for the manual generator"
```

---

## Done when

- `dotnet test --filter "FullyQualifiedName~ManualRecorderTests"` → green (unit).
- `dotnet test --filter "kind=manual"` → 3 green, and `docs/manual/` holds 3 `.md` + a README + the
  PNGs under `img/`.
- Screenshots visually match their captions (inspected, not assumed).
- Normal `dotnet test` (no filter) is unchanged — the `kind=manual` trait keeps walkthroughs opt-in.
