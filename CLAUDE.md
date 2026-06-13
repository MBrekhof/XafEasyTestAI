# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

DevExpress XAF 25.2 application (`XafEasyTestAI`) on .NET 8 with EF Core. Standard XAF scaffold:

- `XafEasyTestAI.Module` — shared business logic, EF Core entities, XAF model (`Model.DesignedDiffs.xafml`), DB seeding (`DatabaseUpdate/Updater.cs`).
- `XafEasyTestAI.Blazor.Server` — Blazor Server UI host (`Model.xafml`).
- `XafEasyTestAI.Win` — WinForms host.
- `XafEasyTestAI.E2E.Tests` — DevExpress EasyTest + Selenium + xunit.v3 functional tests.

## Layout gotcha

The `.slnx` solution file (`XafEasyTestAI.slnx`) is at the **repo root**, but the projects live in the `XafEasyTestAI\` subfolder. Build/test against the root solution; reference projects by their subfolder paths.

## Build & run

```sh
dotnet build XafEasyTestAI.slnx
dotnet run --project XafEasyTestAI/XafEasyTestAI.Blazor.Server   # https://localhost:5001
```

- Requires the DevExpress NuGet feed configured (packages are `DevExpress.*` `25.2.*`).
- Database is SQL Server **LocalDB** (`(localdb)\mssqllocaldb`, catalog `XafEasyTestAI`); schema is created/updated automatically on first run.
- Default logins (seeded only in **non-RELEASE** builds): `Admin` and `User`, both with **blank passwords**.

## E2E tests (EasyTest)

Not a plain `dotnet test` run — they launch the real apps via Selenium and drop/recreate the `XafEasyTestAIEasyTest` database:

1. Build the Win host in the **EasyTest** configuration (the test expects `XafEasyTestAI.Win\bin\EasyTest\net8.0-windows\XafEasyTestAI.Win.exe`). The solution defines three configs: `Debug`, `Release`, `EasyTest`.
2. Install the matching Selenium browser driver (`chromedriver.exe` / `msedgedriver.exe`) and put it on `PATH`.
3. Tests run non-parallel (`DisableTestParallelization`).

## XAF conventions

- Edit the data model through the XAF Model Editor (`.xafml` files), not by hand.
- EF Core business objects have XAF-specific requirements (virtual nav properties, no `OwnsOne`, `BaseObject` pattern) — see the `xaf-efcore-entities` skill before adding/changing entities.
- Verify DevExpress behavior against the dxdocs MCP rather than assuming.
