//
// IndentEngine.cs
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
using ICSharpCode.NRefactory.Editor;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
    public class IndentEngine
    {
        internal readonly IDocument Document;
        internal readonly CSharpFormattingOptions Options;
        internal readonly TextEditorOptions TextEditorOptions;
        internal IndentState CurrentState;

        internal int offset;
        internal int line;
        internal int column;
        internal bool isLineStart = true;

        public TextLocation Location
        {
            get
            {
                return new TextLocation(line, column);
            }
        }

        public int Offset
        {
            get
            {
                return offset;
            }
        }

        public string ThisLineIndent
        {
            get
            {
                return CurrentState.ThisLineindent.IndentString;
            }
        }

        public string NewLineIndent
        {
            get
            {
                return CurrentState.Indent.IndentString + CurrentState.IndentDelta.IndentString;
            }
        }

        public bool NeedsReindent
        {
            get
            {
                return ThisLineIndent != CurrentState.CurrentIndent.ToString();
            }
        }

        public IndentEngine(IDocument document, TextEditorOptions textEditorOptions, CSharpFormattingOptions formattingOptions)
        {
            this.Document = document;
            this.Options = formattingOptions;
            this.TextEditorOptions = textEditorOptions;
            this.CurrentState = IndentStateFactory.Default(this);
        }

        IndentEngine(IndentEngine prototype)
        {
            this.Document = prototype.Document;
            this.Options = prototype.Options;
            this.TextEditorOptions = prototype.TextEditorOptions;
            this.CurrentState = CurrentState.Clone();

            this.offset = prototype.offset;
            this.line = prototype.line;
            this.column = prototype.column;
            this.isLineStart = prototype.isLineStart;
        }

        public IndentEngine Clone()
        {
            return new IndentEngine(this);
        }

        public void Push(char ch)
        {
            CurrentState.Push(ch);
        }
    }
}
