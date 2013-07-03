using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
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
    }
}
