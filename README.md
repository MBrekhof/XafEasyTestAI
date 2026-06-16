# Making EasyTests the Easy Way

A proof-of-concept showing how an AI agent can author **DevExpress XAF EasyTest** functional tests —
the kind that drive the *real* application UI — grounded in the app's own entities and controller logic,
for both the **WinForms** and **Blazor** hosts.

> The thesis: EasyTest addresses UI elements *semantically* (by caption, column, nav path), and those
> names are **derivable from source**. So an AI can read your business objects and ViewControllers and
> write correct functional tests — without guessing selectors. WinForms in particular has no other
> practical automated-UI testing path, which is what this POC set out to prove.

📖 **The real deliverable is the playbook:** [`docs/EASYTEST-AUTHORING.md`](docs/EASYTEST-AUTHORING.md) —
how to derive a test from an entity or a controller, the EasyTest API cheat-sheet, and every gotcha hit
while building the suite. Read this document yourself, and make sure your AI agent sees/reads it too.

## What's in here

A standard **XAF 25.2 / .NET 8 / EF Core** scaffold (`XafEasyTestAI`) plus:

- **Sample domain** (`Module/BusinessObjects/`): `Customer`, `Contact`, `Order`, `OrderLine`,
  `Project`, `ProjectTask` — with an aggregated master-detail (Order→OrderLines), a computed `Total`,
  and a validation invariant (*a Project can't be Completed while it has open Tasks*).
- **A custom ViewController** (`MarkOrderShippedController`) — a "Mark Shipped" action with an
  enable-guard, to demonstrate controller-derived tests.
- **Idempotent seed data** (`DatabaseUpdate/Updater.cs`) so every test run starts from a known state.
- **9 green EasyTests** (`E2E.Tests/Tests.cs`) covering grid CRUD, master-detail editing, a custom
  action + guard, and both sides of a validation rule — running on **WinForms (5)** and **Blazor (4)**.

| Test | Platforms |
|---|---|
| `CustomerCrud` — seed asserts + create | Win + Blazor |
| `MarkOrderShipped` — ViewController action + guard | Win + Blazor |
| `ProjectCannotCompleteWithOpenTasks` — validation blocks Save | Win + Blazor |
| `ProjectCanCompleteWithoutOpenTasks` — validation allows Save | Win + Blazor |
| `OrderWithLines` — master-detail nested-grid editing | Win only* |

\* nested-grid in-place editing differs on Blazor; see the playbook.

## Bonus: a self-generating draft user manual

The same EasyTest machinery that *verifies* the app can also *document* it. `E2E.Tests/ManualWalkthroughs.cs`
drives the live app through real workflows and snaps a screenshot per step; the step captions are the
first-draft prose. Output lands under [`docs/manual/`](docs/manual/) (one `.md` per scenario + `img/`).
Screenshots come from EasyTest's cross-platform `GetScreen().GetScreenshot()`, so the **same walkthrough
code captures Blazor and WinForms** — the Win-only `OrderWithLines` page is generated from the desktop host.

```sh
dotnet test ... --filter "kind=manual"   # regenerate the drafts (both hosts built -c EasyTest)
```

These walkthroughs carry no asserts — they're opt-in via the `kind=manual` trait and excluded from normal/CI runs.

## Prerequisites

- .NET 8 SDK, Windows (WinForms host + EasyTest need an **interactive desktop session** — not headless).
- The **DevExpress NuGet feed** configured (packages are `DevExpress.* 25.2.*`; a valid license is required).
- **SQL Server** — the tests use a Docker container:
  ```sh
  docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=XafEasy!2026" -p 1433:1433 \
    --name xafeasy-sql -d mcr.microsoft.com/mssql/server:2022-latest
  ```
- **For Blazor tests:** Google Chrome + a matching `chromedriver.exe` in `XafEasyTestAI/tools/webdriver/`
  (not committed — download per [that folder's README](XafEasyTestAI/tools/webdriver/README.md)).

## Build & run

```sh
# App (Blazor UI on https://localhost:5001, logins Admin / User with blank passwords in non-RELEASE)
dotnet build XafEasyTestAI.slnx
dotnet run --project XafEasyTestAI/XafEasyTestAI.Blazor.Server

# EasyTests — build both hosts in the EasyTest config, then run
dotnet build XafEasyTestAI/XafEasyTestAI.Win/XafEasyTestAI.Win.csproj -c EasyTest
dotnet build XafEasyTestAI/XafEasyTestAI.Blazor.Server/XafEasyTestAI.Blazor.Server.csproj -c EasyTest
dotnet test  XafEasyTestAI/XafEasyTestAI.E2E.Tests/XafEasyTestAI.E2E.Tests.csproj

# Filter to one platform
dotnet test ... --filter "DisplayName~Win"      # WinForms only
dotnet test ... --filter "DisplayName~Blazor"   # Blazor only (needs Chrome + driver)
```

> The solution file (`XafEasyTestAI.slnx`) is at the repo root; the projects live under `XafEasyTestAI/`.

## Layout

```
XafEasyTestAI.slnx                      # solution (repo root)
XafEasyTestAI/
  XafEasyTestAI.Module/                 # entities, controller, seed, DbContext  (shared)
  XafEasyTestAI.Blazor.Server/          # Blazor Server host
  XafEasyTestAI.Win/                    # WinForms host
  XafEasyTestAI.E2E.Tests/              # EasyTest functional tests + manual generator (xUnit)
  tools/webdriver/                      # chromedriver.exe goes here (gitignored, see folder README)
docs/EASYTEST-AUTHORING.md              # the authoring playbook
docs/manual/                            # auto-generated draft user manual (see "Bonus" above)
```

## License

[MIT](LICENSE) © 2026 Martin Brekhof.

Built collaboratively with Claude Code as a demonstration of AI-assisted EasyTest authoring.
