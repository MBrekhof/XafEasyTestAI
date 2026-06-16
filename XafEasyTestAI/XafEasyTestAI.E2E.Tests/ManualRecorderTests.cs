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
