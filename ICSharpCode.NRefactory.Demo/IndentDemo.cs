//
// IndentDemo.cs
//
// Author:
//       Matej Miklečić <matej.miklecic@gmail.com>
//
// Copyright (c) 2013 Matej Miklečić (matej.miklecic@gmail.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ICSharpCode.NRefactory.Demo
{
	public partial class IndentDemo : UserControl, ITextSource
	{
		CSharpFormattingOptions policy;
		TextEditorOptions options;
		IndentEngine indentEngine;
		bool disableTextChangeEvents;

		public IndentDemo()
		{
			InitializeComponent();

			var document = new ReadOnlyDocument(this);
			policy = FormattingOptionsFactory.CreateMono();
			options = new TextEditorOptions();
			indentEngine = new IndentEngine(document, options, policy);

			indentEngine.OnThisLineIndentFinalized += (sender, args) =>
			{
				var e = (IndentEngine)sender;
				if (e.NeedsReindent)
				{
					reindent(e);
				}
			};
		}

		private void reindent(IndentEngine e)
		{
			var line = textBoxIndent.Lines[indentEngine.Location.Line - 1];

			disableTextChangeEvents = true;
			textBoxIndent.Text = string.Join(options.EolMarker, textBoxIndent.Lines.Take(indentEngine.Location.Line - 1)) +
				options.EolMarker + e.ThisLineIndent + line.TrimStart(' ', '\t') + options.EolMarker;
			disableTextChangeEvents = false;
		}

		private void textBoxIndent_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back || (Control.ModifierKeys & e.KeyCode) != 0)
			{
				e.SuppressKeyPress = true;
			}

			textBoxIndent.SelectionStart = textBoxIndent.Text.Length;
			textBoxIndent.ScrollToCaret();
		}

		private void textBoxIndent_TextChanged(object sender, EventArgs e)
		{
			if (disableTextChangeEvents)
			{
				return;
			}

			var ch = this.textBoxIndent.Text.LastOrDefault();
			indentEngine.Push(ch);

			if (indentEngine.NewLineChar == ch)
			{
				textBoxIndent.AppendText(indentEngine.ThisLineIndent);
			}

			refreshInformationLabels();
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			indentEngine.Reset();

			disableTextChangeEvents = true;
			textBoxIndent.Text = string.Empty;
			disableTextChangeEvents = false;

			refreshInformationLabels();
		}

		private void refreshInformationLabels()
		{
			lblThisLineIndent.Text = indentEngine.ThisLineIndent
				.Replace("\t", new string(' ', options.IndentSize))
				.Length.ToString();
			lblNextLineIndent.Text = indentEngine.NewLineIndent
				.Replace("\t", new string(' ', options.IndentSize))
				.Length.ToString();
			lblCurrentIndent.Text = indentEngine.CurrentIndent
				.Replace("\t", new string(' ', options.IndentSize))
				.Length.ToString();
			lblIsLineStart.Text = indentEngine.IsLineStart ? "Yes" : "No";
			lblNeedsReindent.Text = indentEngine.NeedsReindent ? "True" : "False";
			lblCurrentState.Text = indentEngine.CurrentState.GetType().Name;
			lblLineNo.Text = indentEngine.Location.ToString();
		}

		public ITextSourceVersion Version
		{
			get { throw new NotImplementedException(); }
		}

		public ITextSource CreateSnapshot()
		{
			return new StringTextSource(Text);
		}

		public ITextSource CreateSnapshot(int offset, int length)
		{
			return new StringTextSource(GetText(offset, length));
		}

		public System.IO.TextReader CreateReader()
		{
			return new StringReader(Text);
		}

		public System.IO.TextReader CreateReader(int offset, int length)
		{
			return new StringReader(GetText(offset, length));
		}

		public int TextLength
		{
			get { return Text.Length; }
		}

		public new string Text
		{
			get { return this.textBoxIndent.Text; }
		}

		public char GetCharAt(int offset)
		{
			return Text[offset];
		}

		public string GetText(int offset, int length)
		{
			return Text.Substring(offset, length);
		}

		public string GetText(ISegment segment)
		{
			return GetText(segment.Offset, segment.Length);
		}

		public void WriteTextTo(System.IO.TextWriter writer)
		{
			writer.Write(Text);
		}

		public void WriteTextTo(System.IO.TextWriter writer, int offset, int length)
		{
			writer.Write(GetText(offset, length));
		}

		public int IndexOf(char c, int startIndex, int count)
		{
			return Text.IndexOf(c, startIndex, count);
		}

		public int IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			return Text.IndexOfAny(anyOf, startIndex, count);
		}

		public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return Text.IndexOf(searchText, startIndex, count, comparisonType);
		}

		public int LastIndexOf(char c, int startIndex, int count)
		{
			return Text.LastIndexOf(c, startIndex, count);
		}

		public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
		{
			return Text.LastIndexOf(searchText, startIndex, count, comparisonType);
		}
	}
}
