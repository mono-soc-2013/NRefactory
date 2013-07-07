using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace ICSharpCode.NRefactory.IndentationTests
{
    internal static class Helper
    {
        public static IndentEngine CreateEngine(string text)
        {
            var policy = FormattingOptionsFactory.CreateMono();

            var sb = new StringBuilder();
            int offset = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '$')
                {
                    offset = i;
                    continue;
                }
                sb.Append(ch);
            }

            var document = new ReadOnlyDocument(sb.ToString());
            var options = new TextEditorOptions();

            var result = new IndentEngine(document, options, policy);
            result.UpdateToOffset(offset);
            return result;
        }

        public static void ReadAndTest(string filePath)
        {
            if (File.Exists(filePath))
            {
                var code = File.ReadAllText(filePath);
                var policy = FormattingOptionsFactory.CreateMono();
                var document = new ReadOnlyDocument(code);
                var options = new TextEditorOptions { TabsToSpaces = true };
                var engine = new IndentEngine(document, options, policy);

                engine.OnThisLineIndentChanged += (sender, args) =>
                {
                    var e = (IndentEngine)sender;
                    Assert.IsFalse(e.NeedsReindent,
                            string.Format("Line: {0}, Indent: {1}, Current indent: {2}",
                            e.Location.Line.ToString(), engine.ThisLineIndent.Length, engine.CurrentIndent.Length));
                };

                engine.UpdateToOffset(code.Length);
            }
            else
            {
                Assert.Fail("File " + filePath + " doesn't exist.");
            }
        }
    }
}
