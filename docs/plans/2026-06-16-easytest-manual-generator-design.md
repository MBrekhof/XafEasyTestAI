# EasyTest-Driven Manual Generator — Design

_Date: 2026-06-16 · Branch: `CreatingManual`_

## Idea

The EasyTests already encode real user workflows (navigate → New → fill fields → Save),
each with a plain-English comment describing user intent. Drive the **real Blazor app** through
those flows, capture a screenshot at each step, and emit a captioned markdown draft of a user
manual. The value of using EasyTest (vs. mining the source) is **real UI screenshots from a live
run**.

## Scope

- **Host:** Blazor only for the first cut (Selenium gives screenshots for free; cleaner UI, no OS
  chrome). WinForms is planned later via a single capture seam.
- **Scenarios (first cut):** the three green Blazor flows — Customer management, Mark-order-shipped,
  Project completion. `OrderWithLines` is Win-only today, so it's excluded until the WinForms seam
  lands.
- **Out of scope (add when needed):** AI prose post-processing, a capture abstraction/interface,
  WinForms capture.

## Approach (chosen: A — caption-driven recorder)

Captions written in the walkthrough **are** the first-draft prose. No separate AI pass — the run
produces a complete (if terse) draft that a human polishes.

### Components & layout

```
XafEasyTestAI.E2E.Tests/
  ManualRecorder.cs        # the helper (new)
  ManualWalkthroughs.cs    # one method per user scenario (new)
docs/manual/
  customer-management.md   # generated
  orders.md                # generated (mark-shipped)
  projects.md              # generated
  img/*.png                # generated screenshots
```

Both new files live in the existing `E2E.Tests` project so they reuse `EasyTestFixtureContext`,
`StartSeededApp`, the DB-drop fixture, and the Blazor driver wiring verbatim. No new project, no new
dependency.

### Recorder API

```csharp
// ponytail: deliberately tiny. One Step method = one captioned screenshot.
public sealed class ManualRecorder : IDisposable
{
    public ManualRecorder(IApplicationContext app, string title, string outFile);
    public void Step(string caption, Action action);   // run action, then snap + caption
    public void Note(string markdown);                  // prose-only, no screenshot
    public void Dispose();                              // flush markdown to outFile
}
```

A walkthrough reads like the existing test, but captions replace asserts:

```csharp
[Trait("kind", "manual")]                 // opt-in; excluded from normal test runs
[Fact]
public void CustomerManagement()
{
    var app = StartSeededApp(BlazorAppName);
    using var doc = new ManualRecorder(app, "Managing Customers", "customer-management.md");

    doc.Note("Customers represent the companies you sell to.");
    doc.Step("Open the **Customers** list from the navigation menu.",
        () => app.Navigate("Customer"));
    doc.Step("Click **New** to start a new customer.",
        () => app.GetAction("New").Execute());
    doc.Step("Fill in the company name and address, then click **Save**.",
        () => { app.GetForm().FillForm(("Name","Acme Corp"),("City","Berlin"),("Country","Germany"));
                app.GetAction("Save").Execute(); });
}
```

### Screenshot capture (the only host-specific spot)

```csharp
// ponytail: Blazor only for now. WinForms capture slots in right here later.
void Capture(string pngPath)
{
    var driver = app.AsBlazor().WebDriver;            // ITakesScreenshot
    ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(pngPath);
}
```

No capture interface yet (one impl). WinForms later → a two-line branch here, one-spot change. PNGs
land in `docs/manual/img/<scenario>-NN.png`, numbered by step order.

**Assumption to confirm at implementation:** `app.AsBlazor().WebDriver` returns a Selenium
`IWebDriver` implementing `ITakesScreenshot`. The TODO records this API as reachable
(screenshot-on-failure note) — confirm it compiles rather than re-deriving.

### Output format

```markdown
# Managing Customers
_Draft — generated from a live Blazor run on {date}. Review and edit._

Customers represent the companies you sell to.

### Open the **Customers** list from the navigation menu.
![](img/customer-management-01.png)

### Click **New** to start a new customer.
![](img/customer-management-02.png)
```

`Note` → plain paragraph. `Step` → `###` heading + image. The `_Draft_` banner is auto-prepended so
nobody mistakes it for finished docs. Date injected from the test at runtime (`DateTime`).

### Run & error handling

- **Run:** `dotnet test --filter "kind=manual"` (after building Blazor `-c EasyTest`; same prereqs as
  the existing EasyTests). Normal `dotnet test` excludes them via the trait — CI untouched.
- **No assertions:** if a UI step throws (control not found), the method fails loudly with the caption
  in the message — a useful signal that a flow drifted from the manual.
- **Idempotent output:** each run overwrites `docs/manual/*.md` + its `img/` PNGs. Generated files are
  committed (reviewable in PRs) but regenerated wholesale — no hand-merging.
- **Partial failure:** `Dispose` flushes completed steps, so a partial manual + the failing caption
  pinpoint the break.

### Verification

- Walkthroughs reuse flows already proven green, so capture is the only new risk.
- Verify by running `--filter "kind=manual"`, then **opening the generated PNGs and markdown** and
  confirming each screenshot matches its caption (webapp-testing rule — visual inspection, not just
  "it ran").

## Future (not now)

- WinForms capture via the seam → enables the Win-only `OrderWithLines` walkthrough.
- AI prose pass only if captions prove too terse.
- `docker compose` / driver auto-resolve already tracked in `TODO.md`.
