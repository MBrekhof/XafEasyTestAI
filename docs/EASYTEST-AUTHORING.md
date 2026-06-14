# Authoring EasyTest Functional Tests (AI Playbook)

How to write EasyTest functional tests for this XAF solution, covering **both** the WinForms and
Blazor hosts from a single test body. EasyTest is especially valuable for WinForms (which has no
other practical automated-UI path), but the same tests drive Blazor too. Written so a future AI
session (or human) can generate correct tests from the **entities** and **ViewController logic** in
the codebase.

This is a working playbook: every gotcha below was hit and solved while building the reference
tests in `XafEasyTestAI.E2E.Tests/Tests.cs` (5 tests, 9 cases across both hosts). Start there for
live examples.

---

## 1. Why EasyTest

EasyTest drives the **real** application (launches the actual `.exe`, logs in, clicks real actions).
Its API is **semantic** — you address elements by their *caption / column name / nav path*, not by
pixel or automation-id. That is exactly why it pairs well with an AI author: the captions are
**derivable from the entity and controller code**, so tests can be generated grounded in source,
not guessed.

One test body runs both Win and Blazor (`[InlineData(WinAppName)]` / `[InlineData(BlazorAppName)]`).
Both hosts are targeted and green — **9 cases pass (5 Win, 4 Blazor)**. Only `OrderWithLines` is
Win-only so far, because nested-grid inline editing differs per platform (see §7.10); a Blazor
variant needs its own line-entry steps. Blazor requires a version-matched browser driver and uses
its own DB catalog (§2–§3).

---

## 2. One-time setup (already done in this repo)

These are wired up; listed so you can verify after a clean checkout or replicate elsewhere.

| Requirement | Where | Notes |
|---|---|---|
| `EasyTest` solution configuration | `Configurations` in `.Win` / `.Module` csproj | Three configs: `Debug;Release;EasyTest` |
| EasyTest remoting registration | `XafEasyTestAI.Win/Program.cs` | `EasyTestRemotingRegistration.Register()` under `#if EASYTEST` |
| Auto-create+seed DB under test | `XafEasyTestAI.Win/WinApplication.cs` | `DatabaseVersionMismatch` calls `e.Updater.Update()` under `#if EASYTEST` |
| Test DB connection | `XafEasyTestAI.Win/App.config` → `EasyTestConnectionString` | Used instead of `ConnectionString` when built `EASYTEST` |
| Seed data | `XafEasyTestAI.Module/DatabaseUpdate/Updater.cs` → `SeedSampleData()` | Idempotent; re-runs on every drop+recreate |
| Test fixture | `XafEasyTestAI.E2E.Tests/Tests.cs` | `EasyTestFixtureContext`, app + DB registration |
| **Blazor** auto-create+seed + conn switch | `Blazor.Server/BlazorApplication.cs`, `Startup.cs` | Same `#if EASYTEST` pattern as Win; reads `EasyTestConnectionString` from `appsettings.json` |
| **Blazor** EasyTest adapter | `Blazor.Server.csproj` | `DevExpress.ExpressApp.EasyTest.BlazorAdapter` — referenced **only in the `EasyTest` config** |
| **Blazor** browser driver | `XafEasyTestAI/tools/webdriver/msedgedriver.exe` | Version-matched to installed Edge; passed via `webDriverPath` in `BlazorApplicationOptions` |

**Database:** tests use a dedicated catalog **`XafEasyTestAIEasyTest`** on the same SQL Server
container the app uses (`xafeasy-sql`, `localhost,1433`, `sa` / `XafEasy!2026`). The fixture **drops
this catalog before every test**; the Win app recreates the schema and re-runs the seed `Updater` on
launch. So each test starts from a known, freshly-seeded state. It never touches the app's real
`XafEasyTestAI` catalog.

> If the container isn't running: `docker start xafeasy-sql` (or recreate per the project README).

---

## 3. How to run

```sh
# 1. Build both hosts in the EasyTest configuration (the fixture launches these)
dotnet build XafEasyTestAI/XafEasyTestAI.Win/XafEasyTestAI.Win.csproj -c EasyTest
dotnet build XafEasyTestAI/XafEasyTestAI.Blazor.Server/XafEasyTestAI.Blazor.Server.csproj -c EasyTest

# 2. Build the test project (default Debug config is fine)
dotnet build XafEasyTestAI/XafEasyTestAI.E2E.Tests/XafEasyTestAI.E2E.Tests.csproj

# 3. Run all, one test, or only one platform
dotnet test XafEasyTestAI/XafEasyTestAI.E2E.Tests/XafEasyTestAI.E2E.Tests.csproj --no-build
dotnet test ... --no-build --filter "FullyQualifiedName~MarkOrderShipped"   # one test, both platforms
dotnet test ... --no-build --filter "DisplayName~Blazor"                     # only Blazor variants
dotnet test ... --no-build --filter "DisplayName~Win"                        # only Win variants
```

**Win:** not headless — opens the real app window; needs an **interactive Windows desktop session**.
~13s/test.

**Blazor:** drives a real **Edge** browser via Selenium (`DevExpress.ExpressApp.EasyTest.BlazorAdapter`).
~30s/test. The adapter does **not** auto-download the driver — `msedgedriver.exe` must match the installed
Edge and be on PATH or pointed to by `webDriverPath`. Refresh it when Edge updates:
```powershell
# get Edge version, then download the matching driver into tools/webdriver
(Get-Item 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe').VersionInfo.ProductVersion
# Invoke-WebRequest "https://msedgedriver.microsoft.com/<version>/edgedriver_win64.zip" -OutFile edgedriver.zip
```
A `BlazorApplicationOptions` can also set `runHeadless: true` for CI (a driver is still required).

---

## 4. The authoring method

The whole point: **derive the test from the code, don't guess.** Two sources.

> **Verify DevExpress assumptions through the dxdocs MCP server** (`devexpress_docs_search` /
> `devexpress_docs_get_content`) before relying on any EasyTest API, attribute, or XAF behavior.
> Every API and gotcha in this playbook was confirmed against dxdocs for 25.2 — do the same for
> anything new (method signatures, validation/edit-mode behavior, platform differences). If dxdocs
> has no answer, say so and label it an unverified assumption rather than guessing.

### 4a. From entities → captions, columns, nav items

For each business object you want to test, read its class in `XafEasyTestAI.Module/BusinessObjects/`:

| You need… | Read from the entity | Becomes in the test |
|---|---|---|
| Navigation item name | `[DefaultClassOptions]` present → object is in the nav | `app.Navigate("Customer")` |
| List grid column captions | property names (XAF humanizes `OrderNumber` → "Order Number") | `GetGrid().RowExists(("Order Number","SO-1001"))` |
| Detail form field captions | property names; `[DisplayName("…")]` overrides | `FillForm(("Order Number","SO-9001"))` |
| Lookup value to type | the related object's `[DefaultProperty]` | `FillForm(("Customer","Contoso Ltd"))` |
| What rows already exist | `Updater.SeedSampleData()` | assert against known seed |
| Inline-editable nested grid | `[DefaultListViewOptions(true, …)]` on the child | `GetGrid("Order Lines").InlineNew()` |

Humanization rule: PascalCase → space-separated Title Case. `UnitPrice` → "Unit Price". A `[DisplayName]`
or `[DefaultProperty]` attribute wins over the humanized name — check for those first.

### 4b. From ViewControllers → actions and guards to assert

Read controllers in `XafEasyTestAI.Module/Controllers/` (and platform-specific ones in the hosts).
A controller's **actions** and **enable/visibility guards** are the highest-value things to test —
they're custom logic, not framework behavior.

Worked example — `MarkOrderShippedController.cs`:

```csharp
markShipped = new SimpleAction(this, "MarkShipped", PredefinedCategory.RecordEdit) {
    Caption = "Mark Shipped",                                 // ← address by this caption
    SelectionDependencyType = SelectionDependencyType.RequireSingleObject
};
markShipped.Enabled["ConfirmedOnly"] =                        // ← the guard to assert
    View.CurrentObject is Order o && o.Status == OrderStatus.Confirmed;
markShipped.Execute += ...;                                   // ← Status: Confirmed → Shipped
```

Reading that controller tells you the full test without running anything:
- Action caption = **"Mark Shipped"** → `app.GetAction("Mark Shipped").Execute()`
- Guard: only a single **Confirmed** order → select the seeded Confirmed order `SO-1001`
- Effect: status flips to **Shipped** → assert the grid's `Status` cell == `"Shipped"`

That is the `MarkOrderShipped` test verbatim.

---

## 5. Test skeleton

Copy this shape. The `StartSeededApp` helper (in `Tests.cs`) does drop-DB → launch → login.

```csharp
[Theory]
[InlineData(WinAppName)]
// [InlineData(BlazorAppName)]   // add when the flow is cross-platform (see §7.10)
public void MyFlow(string applicationName)
{
    var app = StartSeededApp(applicationName);   // fresh seeded DB + logged in as Admin
    app.Navigate("Customer");                    // nav item caption (entity with [DefaultClassOptions])

    // ... drive + assert ...
}
```

---

## 6. EasyTest API cheat-sheet

All verified against this app. `app` is an `IApplicationContext`.

**Forms & actions**
```csharp
app.GetForm().FillForm(("User Name","Admin"), ("City","Berlin"));  // fields by caption
app.GetAction("Log In").Execute();                                  // toolbar/action by caption
app.GetAction("Save").Execute();   app.GetAction("New").Execute();  app.GetAction("Close").Execute();
app.Navigate("Order");                                              // nav; dotted path e.g. "Reports.Reports"
```

**Grid — read**
```csharp
app.GetGrid().GetRowCount();
app.GetGrid().RowExists(("Name","Contoso Ltd"));
int? i = app.GetGrid().GetRowIndex(("Order Number","SO-1001"));     // nullable!
app.GetGrid().GetRow(i.Value, "Order Number","Status");            // string[] of those columns
app.GetGrid().GetRows("Name","City");                              // all rows
```

**Grid — select / inline edit**
```csharp
app.GetGrid().SelectRows("Order Number","SO-1001");                // then run a ListView action
app.GetGrid().ProcessRow(("Order Number","SO-1001"));             // open the row's DetailView
var lines = app.GetGrid("Order Lines");                           // nested grid by caption
lines.InlineNew(); lines.FillRow(("Product Name","X"),("Quantity","3")); lines.InlineUpdate();
```

**Validation** (after a Save that breaks a rule)
```csharp
app.GetAction("Save").Execute();                                  // triggers Save-context rules
var msgs = app.GetValidation().GetValidationMessages();           // string[] of displayed messages
Assert.Contains(msgs, m => m.Contains("open"));
// app.GetValidation().GetValidationHeader();                     // Win-only popup header
```
Test validation by reading the displayed messages — don't assume the Save threw. Prefer `Contains`
over exact equality (messages can be prefixed/formatted by platform).

---

## 7. Gotchas (all hit while building the reference tests)

1. **Dates are locale-bound.** A date editor expects the **host's short-date culture** (here `nl-NL`
   = `dd-MM-yyyy`). `"6/13/2026"` throws `"…doesn't correspond to the 'd' target input format"`.
   Prefer omitting non-required dates, or format to the host culture. → see `OrderWithLines`.

2. **Nested grids are read-only by default.** `InlineNew()` fails with
   `Cannot get the ActiveEditor … (-1, …)` unless the child entity has
   `[DefaultListViewOptions(true, NewItemRowPosition.Top)]` (or the list view's `AllowEdit` is set in
   the model). → fixed on `OrderLine`.

3. **Assert nested-grid rows *before* `Save`.** After a Save the `"Order Lines"` locator re-resolves
   to a toolbar button (`IGridBase … not supported … BarButtonItemLink`). Do `RowExists` while the
   grid is the active control, then Save. → see `OrderWithLines`.

4. **Nested in-line edits persist only on the *root* Save.** `InlineUpdate()` commits the row into the
   parent's in-memory collection; the actual DB write happens when you Save the **Order** detail.

5. **`GetRowIndex` returns `int?`** — null when the row is absent. Null-check before `.Value`.

6. **Non-headless.** See §3. No interactive desktop → nothing runs.

7. **Build config matters.** The fixture launches the **EasyTest**-config builds of both hosts. If you
   forget `-c EasyTest`, you'll run a stale/Debug build (wrong DB connection, no remoting/adapter) and
   tests hang or hit the wrong database.

8. **No `Close` action on Blazor.** After a Save, Win has a `Close` action; Blazor doesn't, so
   `GetAction("Close")` returns null → NRE. Don't `Close` to leave a saved detail — just `Navigate`
   away (clean on both once there are no unsaved changes). → see `CustomerCrud`.

9. **Blazor driver isn't auto-managed.** The adapter throws `Browser driver is not found` unless
   `msedgedriver.exe` (matching Edge) is on PATH or `webDriverPath`. Raw Selenium would auto-download;
   the DevExpress adapter does not.

10. **Some flows are platform-specific.** Nested-grid in-place editing (`InlineNew`/`FillRow`) is the
    main one — Win uses a New Item Row; Blazor uses a different `InlineEditMode`/edit-form. `OrderWithLines`
    is Win-only for that reason; a Blazor variant needs its own line-entry steps. When a test is
    cross-platform, make it a `[Theory]` with both `[InlineData]`s; when not, keep it single-platform and
    say why in a comment.

---

## 8. Reference tests (in `Tests.cs`)

| Test | Platforms | Covers | Key calls |
|---|---|---|---|
| `CustomerCrud` | Win + Blazor | seed asserts + create via New→DetailView→Save | `RowExists`, `New`, `FillForm`, `Save` |
| `OrderWithLines` | Win only | master-detail; add a line in the nested grid | `GetGrid("Order Lines")`, `InlineNew/FillRow/InlineUpdate` |
| `MarkOrderShipped` | Win + Blazor | custom ViewController action + guard | `SelectRows`, `GetAction("Mark Shipped")`, `GetRow` |
| `ProjectCannotCompleteWithOpenTasks` | Win + Blazor | validation rule blocks an invalid Save | `ProcessRow`, `FillForm`, `GetValidation().GetValidationMessages()` |
| `ProjectCanCompleteWithoutOpenTasks` | Win + Blazor | positive: rule allows a valid Save | `New`, `FillForm`, `Save`, `RowExists` |

The fixture registers the Blazor app with `webDriverPath` pointing at `tools/webdriver`; cross-platform
tests carry both `[InlineData(WinAppName)]` and `[InlineData(BlazorAppName)]`. 9 cases total (5 Win, 4 Blazor).

---

## 9. Adding a new test (checklist for the AI author)

1. **Identify the flow** — from the user's request or a controller's logic.
2. **Read the entity/entities** involved → collect exact nav name, field captions, column names,
   lookup display values (§4a). Don't guess captions; humanize or read `[DisplayName]`/`[DefaultProperty]`.
3. **Read any ViewController** in play → action captions + enable/visibility guards to assert (§4b).
4. **Check the seed** (`Updater.SeedSampleData`) for rows you can rely on, or create what you need.
5. **Verify any DevExpress assumption via the dxdocs MCP** (`devexpress_docs_search` /
   `devexpress_docs_get_content`) — EasyTest method signatures, validation/edit-mode behavior, attribute
   constructors, platform differences. Don't rely on memory for DX APIs.
6. **Write the test** from the skeleton (§5) using the API cheat-sheet (§6); apply the gotchas (§7).
7. **Build `-c EasyTest`, run with a `--filter`**, iterate. On failure, read the EasyTest exception —
   it names the control/field/format precisely (that's how every gotcha above was found).
8. If a flow needs UI config that doesn't exist yet (e.g. an editable grid), prefer a **code attribute**
   (`[DefaultListViewOptions]`) over hand-editing `.xafml`.

---

## 10. User-specific cases

> _Add the specific flows you want covered here; the AI will turn each into a test using §9._

- **A Project can't be Completed while it has open Tasks.** ✅ Implemented.
  - Rule: `Project.CanBeCompleted` (`[RuleFromBoolProperty]`, `DefaultContexts.Save`) in
    `Module/BusinessObjects/Project.cs`.
  - Tests (both sides of the rule):
    - `ProjectCannotCompleteWithOpenTasks` (negative) — opens the seeded "Website Redesign" project
      (Active, with incomplete tasks), sets Status = Completed, Saves, asserts the validation message.
    - `ProjectCanCompleteWithoutOpenTasks` (positive) — creates a new project (no tasks, so no *open*
      tasks), sets Status = Completed, Saves, and confirms it lands in the list.
  - Pattern for the next one: a business invariant → a `RuleFromBoolProperty` (true = valid) → two
    EasyTests — one drives the object invalid and reads `GetValidationMessages()`, one drives it valid
    and confirms the Save persisted (e.g. `RowExists`). Test both sides.
