# Browser driver for Blazor EasyTests

The Blazor EasyTest adapter drives a real **Microsoft Edge** browser via Selenium and needs
`msedgedriver.exe` here, **matching your installed Edge version**. The binary (and the driver's own
`Driver_Notes/`) are git-ignored — only this README is tracked.

The test fixture points here via `webDriverPath` in `BlazorApplicationOptions`
(`XafEasyTestAI.E2E.Tests/Tests.cs`), resolved as `{TestBinDir}\..\..\..\..\tools\webdriver`.

## Download the matching driver (PowerShell)

```powershell
$ver = (Get-Item 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe').VersionInfo.ProductVersion
$dir = "$PSScriptRoot"   # this folder
Invoke-WebRequest "https://msedgedriver.microsoft.com/$ver/edgedriver_win64.zip" -OutFile "$env:TEMP\edgedriver.zip"
Expand-Archive "$env:TEMP\edgedriver.zip" -DestinationPath $dir -Force
& "$dir\msedgedriver.exe" --version   # should match your Edge version
```

When Edge auto-updates, the driver mismatches and Blazor tests fail with a version error — just re-run
the above. WinForms tests are unaffected.
