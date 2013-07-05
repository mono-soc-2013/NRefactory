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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
    /// <summary>
    ///     The indentation engine.
    /// </summary>
    /// <remarks>
    ///     Represents the context for transitions between <see cref="IndentState"/>.
    ///     Delegates the responsibility for pushing a new char to the
    ///     current state and changes between states depending on the pushed char.
    ///     chars.
    /// </remarks>
    public class IndentEngine
    {
        #region Properties

        /// <summary>
        ///     Defined conditional symbols for preprocessor directives.
        /// </summary>
        public IList<string> ConditionalSymbols = new List<string>();

        /// <summary>
        ///     Document that's parsed by the engine.
        /// </summary>
        internal readonly IDocument Document;

        /// <summary>
        ///     Formatting options.
        /// </summary>
        internal readonly CSharpFormattingOptions Options;

        /// <summary>
        ///     Text editor options.
        /// </summary>
        internal readonly TextEditorOptions TextEditorOptions;

        /// <summary>
        ///     The current indentation state.
        /// </summary>
        internal IndentState CurrentState;

        /// <summary>
        ///     
        /// </summary>
        public TextLocation Location
        {
            get
            {
                return new TextLocation(line, column);
            }
        }

        /// <summary>
        ///     
        /// </summary>
        public int Offset
        {
            get
            {
                return offset;
            }
        }

        /// <summary>
        ///     
        /// </summary>
        public string ThisLineIndent
        {
            get
            {
                return CurrentState.ThisLineIndent.IndentString;
            }
        }

        /// <summary>
        ///     
        /// </summary>
        public string NewLineIndent
        {
            get
            {
                return CurrentState.NextLineIndent.IndentString;
            }
        }

        /// <summary>
        ///     
        /// </summary>
        public bool NeedsReindent
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Fields

        internal int offset = 0;
        internal int line = 1;
        internal int column = 1;
        internal bool isLineStart = true;
        internal char previousChar = '\0';
        internal char lastSignificantChar = '\0';

        #endregion

        #region Constructors

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
            this.previousChar = prototype.previousChar;
        }

        #endregion

        #region IClonable

        public IndentEngine Clone()
        {
            return new IndentEngine(this);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Pushes a new char into the current state and calculates the new
        ///     indentation level.
        /// </summary>
        /// <param name="ch">
        ///     A new character.
        /// </param>
        public void Push(char ch)
        {
            offset++;
            if (!Environment.NewLine.Contains(ch))
            {
                isLineStart &= char.IsWhiteSpace(ch);

                if (ch == '\t')
                {
                    var nextTabStop = (column - 1 + TextEditorOptions.IndentSize) / TextEditorOptions.IndentSize;
                    column = 1 + nextTabStop * TextEditorOptions.IndentSize;
                    offset++;
                }
                else
                {
                    column++;
                }
            }
            else
            {
                if (ch == '\n' && previousChar == '\r')
                {
                    return;
                }

                isLineStart = true;
                column = 1;
                line++;
            }

            CurrentState.Push(ch);
            previousChar = ch;
            if (!char.IsWhiteSpace(ch))
            {
                lastSignificantChar = ch;
            }
        }

        public void UpdateToOffset(int toOffset)
        {
            for (int i = offset; i < toOffset; i++)
            {
                Push(Document.GetCharAt(i));
            }
        }

        #endregion
    }
}
