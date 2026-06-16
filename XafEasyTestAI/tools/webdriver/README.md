# Browser driver for Blazor EasyTests

The Blazor EasyTest adapter drives a real **Google Chrome** browser via Selenium and needs
`chromedriver.exe` here, **matching your installed Chrome version**. The binary (and the driver's own
`Driver_Notes/`) are git-ignored — only this README is tracked.

The test fixture points here via `webDriverPath` in `BlazorApplicationOptions`
(`XafEasyTestAI.E2E.Tests/Tests.cs` and `ManualWalkthroughs.cs`, with `browser: "Chrome"`), resolved as
`{TestBinDir}\..\..\..\..\tools\webdriver`.

## Download the matching driver (PowerShell)

```powershell
$build = (Get-Item 'C:\Program Files\Google\Chrome\Application\chrome.exe').VersionInfo.ProductVersion -replace '\.\d+$',''
$json  = Invoke-RestMethod 'https://googlechromelabs.github.io/chrome-for-testing/latest-patch-versions-per-build-with-downloads.json'
$url   = ($json.builds.$build.downloads.chromedriver | Where-Object platform -eq 'win64').url
Invoke-WebRequest $url -OutFile "$env:TEMP\chromedriver.zip"
Expand-Archive "$env:TEMP\chromedriver.zip" -DestinationPath "$env:TEMP\cd" -Force
Copy-Item "$env:TEMP\cd\chromedriver-win64\chromedriver.exe" $PSScriptRoot -Force
& "$PSScriptRoot\chromedriver.exe" --version   # should match your Chrome build
```

Selenium keys on the major/build, so a patch-level gap is fine. When Chrome auto-updates to a new
build the driver mismatches and Blazor tests fail with a version error — just re-run the above.
WinForms tests are unaffected (they drive the native UI, no browser driver).
