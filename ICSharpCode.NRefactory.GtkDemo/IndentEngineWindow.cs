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
using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using System.IO;

namespace ICSharpCode.NRefactory.GtkDemo
{
	public partial class IndentEngineWindow : Gtk.Window, ITextSource
	{
		CSharpFormattingOptions policy;
		TextEditorOptions options;
		IndentEngine indentEngine;
		bool disableTextChangeEvents;

		public IndentEngineWindow () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();

			var document = new ReadOnlyDocument(this);
			policy = FormattingOptionsFactory.CreateMono();
			options = new TextEditorOptions { EolMarker = "\n" };
			indentEngine = new IndentEngine(document, options, policy);

			indentEngine.OnThisLineIndentFinalized += (sender, args) =>
			{
				var e = (IndentEngine)sender;
				if (e.NeedsReindent)
				{
					reindent(e);
				}
			};

			textviewIndent.Buffer.Changed += (sender, args) => 
			{
				if (disableTextChangeEvents)
				{
					return;
				}

				var ch = textviewIndent.Buffer.Text.Last();
				indentEngine.Push(ch);

				if (indentEngine.NewLineChar == ch)
				{
					disableTextChangeEvents = true;

					textviewIndent.Buffer.Text += indentEngine.ThisLineIndent;
					foreach (var c in indentEngine.ThisLineIndent) 
					{
						indentEngine.Push(c);
					}

					disableTextChangeEvents = false;
				}

				refreshInformationLabels();
			};
		}

		private void reindent(IndentEngine e)
		{
			disableTextChangeEvents = true;

			var lines = textviewIndent.Buffer.Text.Split(new string[] { options.EolMarker }, StringSplitOptions.None);

			textviewIndent.Buffer.Text = string.Join (options.EolMarker, lines.Take (indentEngine.Location.Line - 1)) + 
				options.EolMarker + e.ThisLineIndent + 
				lines.Skip (indentEngine.Location.Line - 1).First().TrimStart (' ', '\t') + options.EolMarker;

			disableTextChangeEvents = false;
		}

		protected void OnBtnResetClicked (object sender, EventArgs e)
		{
			indentEngine.Reset();

			disableTextChangeEvents = true;
			textviewIndent.Buffer.Text = string.Empty;
			disableTextChangeEvents = false;

			refreshInformationLabels();
		}

		protected void OnTextviewIndentKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.BackSpace)
			{
				// prevent default
			}

			var iter = textviewIndent.Buffer.GetIterAtLineOffset(indentEngine.Location.Line - 1, (indentEngine.Location.Column - 1) / options.IndentSize);
			textviewIndent.ScrollToIter(iter, 0, false, 0, 0);
			textviewIndent.Buffer.PlaceCursor(iter);
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
			lblLocation.Text = indentEngine.Location.ToString();
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

		public string Text
		{
			get { return this.textviewIndent.Buffer.Text; }
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
