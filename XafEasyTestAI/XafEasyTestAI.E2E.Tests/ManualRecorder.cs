using System;
using System.IO;
using System.Text;

namespace XafEasyTestAI.Module.E2E.Tests
{
    // ponytail: a markdown buffer with one host-bound seam (the injected capture func).
    // No app dependency here on purpose — keeps the markdown logic unit-testable and makes
    // WinForms a one-lambda swap later.
    public sealed class ManualRecorder : IDisposable
    {
        readonly string _outDir;
        readonly string _fileName;
        readonly Action<string> _capture;   // given an absolute png path, save a screenshot
        readonly StringBuilder _md = new();
        readonly string _slug;
        int _step;

        public ManualRecorder(string outDir, string fileName, string title,
                              string generatedOn, Action<string> capture)
        {
            _outDir = outDir;
            _fileName = fileName;
            _capture = capture;
            _slug = Path.GetFileNameWithoutExtension(fileName);
            Directory.CreateDirectory(Path.Combine(_outDir, "img"));

            _md.AppendLine($"# {title}");
            _md.AppendLine($"_Draft — generated from a live Blazor run on {generatedOn}. Review and edit._");
            _md.AppendLine();
        }

        public void Note(string markdown)
        {
            _md.AppendLine(markdown);
            _md.AppendLine();
        }

        public void Step(string caption, Action action)
        {
            action();
            _step++;
            var name = $"{_slug}-{_step:00}.png";
            _capture(Path.Combine(_outDir, "img", name));
            _md.AppendLine($"### {caption}");
            _md.AppendLine($"![](img/{name})");
            _md.AppendLine();
        }

        public void Dispose() =>
            File.WriteAllText(Path.Combine(_outDir, _fileName), _md.ToString());
    }
}
