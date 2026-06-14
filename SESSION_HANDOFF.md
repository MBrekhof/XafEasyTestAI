# Session Handoff

_Last updated: 2026-06-14_

## Last session (2026-06-14) — docs + skill extraction

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
