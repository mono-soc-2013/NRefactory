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
using System.Linq;
using System.Windows.Forms;

namespace ICSharpCode.NRefactory.Demo
{
	public partial class IndentDemo : UserControl
	{
		IDocument document;
		CSharpFormattingOptions policy;
		TextEditorOptions options;
		IDocumentIndentEngine indentEngine;

		public IndentDemo()
		{
			InitializeComponent();

			document = new StringBuilderDocument();
			policy = FormattingOptionsFactory.CreateMono();
			options = new TextEditorOptions();
			indentEngine = new CacheIndentEngine(new CSharpIndentEngine(document, options, policy));
		}

		private void reindent()
		{
			var line = document.GetLineByNumber(indentEngine.Location.Line);
			var length = document.GetText(line)
			                     .TakeWhile(c => char.IsWhiteSpace(c))
			                     .Count();

			document.Replace(line.Offset, length, indentEngine.ThisLineIndent);
			refreshTextBox(indentEngine.ThisLineIndent.Length - length);
		}

		private void textBoxIndent_KeyDown(object sender, KeyEventArgs e)
		{
			int carretOffset = 0;

			if (e.KeyCode == Keys.Delete && textBoxIndent.SelectionStart < document.TextLength)
			{
				if (textBoxIndent.SelectionLength <= 0)
				{
					document.Remove(textBoxIndent.SelectionStart, 1);
				}
				else
				{
					document.Remove(textBoxIndent.SelectionStart, textBoxIndent.SelectionLength);
				}
			}
			else if (e.KeyCode == Keys.Back && textBoxIndent.SelectionStart > 0)
			{
				if (textBoxIndent.SelectionLength <= 0)
				{
					document.Remove(textBoxIndent.SelectionStart - 1, 1);
					carretOffset = -1;
				}
				else
				{
					document.Remove(textBoxIndent.SelectionStart, textBoxIndent.SelectionLength);
				}
			}
			else
			{
				return;
			}

			refreshTextBox(carretOffset);
			e.SuppressKeyPress = true;
		}

		private void textBoxIndent_KeyPress(object sender, KeyPressEventArgs e)
		{
			var ch = e.KeyChar.ToString();
			
			if (options.EolMarker.Contains(ch))
			{
				document.Replace(textBoxIndent.SelectionStart, textBoxIndent.SelectionLength, options.EolMarker);
				refreshTextBox(options.EolMarker.Length);
			}
			else
			{
				document.Replace(textBoxIndent.SelectionStart, textBoxIndent.SelectionLength, ch);
				refreshTextBox(1);
			}

			if (indentEngine.NeedsReindent)
			{
				reindent();
			}

			e.Handled = true;
		}

		private void refreshTextBox(int carretOffset = 0)
		{
			var carret = textBoxIndent.SelectionStart + carretOffset;
			carret = Math.Max(0, Math.Min(document.TextLength, carret));

			textBoxIndent.Text = document.Text;

			textBoxIndent.SelectionStart = carret;
		}

		private void textBoxIndent_TextChanged(object sender, EventArgs e)
		{
			refreshInformationLabels();
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			indentEngine.Reset();
			document.Remove(0, document.TextLength);

			textBoxIndent.Text = document.Text;
			refreshInformationLabels();
		}

		private void refreshInformationLabels()
		{
			lblThisLineIndent.Text = indentEngine.ThisLineIndent
				.Replace("\t", new string(' ', options.IndentSize))
				.Length.ToString();
			lblNextLineIndent.Text = indentEngine.NextLineIndent
				.Replace("\t", new string(' ', options.IndentSize))
				.Length.ToString();
			lblCurrentIndent.Text = indentEngine.CurrentIndent
				.Replace("\t", new string(' ', options.IndentSize))
				.Length.ToString();
			lblNeedsReindent.Text = indentEngine.NeedsReindent ? "True" : "False";
			lblLineNo.Text = indentEngine.Location.ToString();
		}
	}
}
