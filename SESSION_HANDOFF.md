# Session Handoff

_Last updated: 2026-06-16_

## Last session (2026-06-16) — EasyTest-driven manual generator (branch `CreatingManual`)

**Idea:** use the EasyTests to generate a *draft user manual* — drive the live app through its real
workflows and capture a screenshot per step. The walkthrough captions ARE the first-draft prose (no
AI post-processing). Blazor first; WinForms later via a single capture seam.

**Branch:** `CreatingManual` (NOT merged to main). Brainstormed → designed → planned → executing via
subagent-driven-development.

- **Design:** `docs/plans/2026-06-16-easytest-manual-generator-design.md`
- **Plan:** `docs/plans/2026-06-16-easytest-manual-generator.md` (5 tasks).

**Progress (all committed on `CreatingManual`):**
- ✅ **Task 1** — `ManualRecorder` (E2E.Tests): pure markdown buffer, screenshot capture is an
  injected `Action<string>` (unit-testable, and the WinForms seam). 2 unit tests green. (`87d1c24`)
- ✅ **Task 2** — `ManualWalkthroughs.cs`: Blazor-only EasyTest fixture (mirrors `Tests.cs`) +
  `BlazorCapture` helper. Compiles in Debug. **Closed the one design risk:** `app.AsBlazor().WebDriver`
  → Selenium `IWebDriver`/`ITakesScreenshot` resolves (Selenium 4.4.0 + BlazorAdapter 25.2.5 are
  referenced unconditionally; `SaveAsFile(string)` works). (`2def2d2`)
- ⏸️ **Task 3–4** — the 3 walkthroughs (CustomerManagement, MarkingOrdersShipped, CompletingProjects),
  each `[Trait("kind","manual")]` so `dotnet test --filter "kind=manual"` runs them and normal/CI runs
  skip them. **Not started — they need the live run.**
- ⏸️ **Task 5** — `docs/manual/README.md` index + TODO/handoff updates.

**To resume (the run is blocked on environment):**
1. `docker start xafeasy-sql` (container exists but was Exited).
2. **Edge driver missing:** `tools/webdriver/` doesn't exist. Drop a `msedgedriver.exe` matching the
   installed Edge **149.0.4022.62** there (git-ignored).
3. Build Blazor `-c EasyTest`, then implement Task 3 (add `CustomerManagement`, run
   `dotnet test --filter "FullyQualifiedName~CustomerManagement"`), **open the 4 PNGs +
   `docs/manual/customer-management.md` and eyeball them** before committing.
4. Task 4 adds the other two; Task 5 writes the index + updates docs.

**Verification note for next session:** these walkthroughs have no asserts — a green run only proves
the flow didn't throw. The real check is opening the generated screenshots and confirming each matches
its caption (webapp-testing rule).

---

## Earlier session (2026-06-14) — docs + skill extraction

- **Playbook (`docs/EASYTEST-AUTHORING.md`) de-staled:** dropped "WinForms-only / POC targets Win
  only" framing — both hosts are targeted and green (9 cases, 5 Win + 4 Blazor). Fixed "three
  reference tests" → 5 tests / 9 cases. Added a `[InlineData(BlazorAppName)]` cross-platform hint to
  the §5 skeleton.
- **README:** the playbook pointer now tells readers to share it with their AI agent.
- Both committed + pushed to `main` (`af9eaeb`).
- **TODO:** added the rationale for extracting a global `xaf-easytest-authoring` skill (generic
  method + API + gotchas belong XAF-wide, not in this repo).
- **Started that skill in the `xafskills` repo** (separate repo, `github.com/MBrekhof/xafskills`):
  wrote `skills/xaf-easytest-authoring/SKILL.md` + bumped the README index to 12 skills.
  **Uncommitted — to be reviewed/pushed in a dedicated `xafskills` session.** It won't trigger until
  pushed (the active copy is the plugin cache, which auto-updates from the pushed commit).



## What this project is

A POC proving an AI agent can author **DevExpress XAF EasyTest** functional tests (driving the real
WinForms + Blazor UI) from the app's own entities and ViewController logic. The headline deliverable is
the playbook at **`docs/EASYTEST-AUTHORING.md`**.

## Current state — all green

- **Build:** Debug solution clean (`0 warnings, 0 errors`). Win & Blazor build in the `EasyTest` config.
- **Tests:** **9 EasyTests pass** (5 WinForms + 4 Blazor), ~2m20s total.
  - `CustomerCrud` (Win+Blazor), `MarkOrderShipped` (Win+Blazor),
    `ProjectCannotCompleteWithOpenTasks` (Win+Blazor), `ProjectCanCompleteWithoutOpenTasks` (Win+Blazor),
    `OrderWithLines` (Win only).

## How it's wired (the non-obvious bits)

- **DB:** dedicated catalog `XafEasyTestAIEasyTest` on a Docker SQL container `xafeasy-sql`
  (`localhost,1433`, `sa` / `XafEasy!2026`). The fixture **drops it before each test**; the host
  recreates schema + re-runs `Updater.SeedSampleData()` on launch. Never touches the real
  `XafEasyTestAI` catalog (which the running app uses).
- **EasyTest config:** both hosts switch to `EasyTestConnectionString` and auto-update the DB only
  under `#if EASYTEST` — so they **must** be built `-c EasyTest`. The fixture launches those builds.
- **Blazor:** needs `DevExpress.ExpressApp.EasyTest.BlazorAdapter` (EasyTest config only) + a
  version-matched `msedgedriver.exe` in `tools/webdriver/` (git-ignored), passed via `webDriverPath`.

## Domain added this session

`Customer, Contact, Order, OrderLine, Project, ProjectTask` (Module/BusinessObjects/), the
`MarkOrderShippedController`, the `Project.CanBeCompleted` validation rule, and seed data.

## Gotchas learned (full list in the playbook §7)

- Dates are host-locale bound; nested grids are read-only until `[DefaultListViewOptions(true,…)]`;
  assert nested-grid rows *before* Save; `GetRowIndex` is `int?`; **Blazor has no `Close` action** post-save
  (just `Navigate`); the Blazor driver is **not** auto-downloaded.

## To resume

1. Start the SQL container if down: `docker start xafeasy-sql`.
2. If Edge updated, refresh the driver (see `tools/webdriver/README.md`) or Blazor tests fail on version.
3. Build `-c EasyTest` (both hosts) → `dotnet test … E2E.Tests`.
4. Next work: see `TODO.md` (Blazor `OrderWithLines`, externalize creds, CI).
