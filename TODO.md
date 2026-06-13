# TODO

## Test coverage
- [ ] **Blazor variant of `OrderWithLines`** (master-detail line entry). Win uses the New Item Row;
      Blazor needs its own flow (different `InlineEditMode` / edit-form). Currently Win-only.
- [ ] More invariants → tests (e.g. Order can't ship with zero lines; Contact email uniqueness).
- [ ] A few negative/permission cases (role-based visibility of the "Mark Shipped" action).

## Hardening
- [ ] **Externalize the dev DB credentials.** `XafEasy!2026` is hardcoded in `appsettings.json`,
      `Win/App.config`, and `E2E.Tests/Tests.cs`. It only unlocks the localhost Docker container, but
      should move to an env var / user-secrets before this is anything more than a POC.
- [ ] **CI story.** WinForms EasyTest needs an interactive desktop session (not a headless agent);
      Blazor can run `runHeadless: true` but still needs a version-matched `msedgedriver`. Document or
      script a self-hosted Windows runner approach.
- [ ] **Pin/auto-resolve the Edge driver.** Today it's manually downloaded to `tools/webdriver/` and
      breaks on Edge updates. Consider a build step that fetches the matching driver.

## Nice to have
- [ ] Wire the Win `EasyTestConnectionString` and Blazor `EasyTestConnectionString` to a single source.
- [ ] Add a `docker compose` for the SQL container so setup is one command.
- [ ] Screenshot-on-failure for Blazor (Selenium API is reachable via `appContext.AsBlazor().WebDriver`).
