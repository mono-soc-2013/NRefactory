﻿//
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
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
    /// <summary>
    ///     The indentation engine.
    /// </summary>
    /// <remarks>
    ///     Represents the context for transitions between <see cref="IndentState"/>.
    ///     Delegates the responsibility for pushing a new char to the current 
    ///     state and changes between states depending on the pushed chars.
    /// </remarks>
    public class IndentEngine
    {
        #region Properties

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
        ///     Represents the new line character.
        /// </summary>
        public char NewLineChar
        {
            get
            {
                return TextEditorOptions.EolMarker[0];
            }
        }

        /// <summary>
        ///     The current indentation state.
        /// </summary>
        internal IndentState CurrentState;

        /// <summary>
        ///     The current location of the engine in <see cref="Document"/>.
        /// </summary>
        public TextLocation Location
        {
            get
            {
                return new TextLocation(line, column);
            }
        }

        /// <summary>
        ///     The current offset of the engine in <see cref="Document"/>.
        /// </summary>
        public int Offset
        {
            get
            {
                return offset;
            }
        }

        /// <summary>
        ///     The indentation string of the current line.
        /// </summary>
        public string ThisLineIndent
        {
            get
            {
                return CurrentState.ThisLineIndent.IndentString;
            }
        }

        /// <summary>
        ///     The indentation string of the next line.
        /// </summary>
        public string NewLineIndent
        {
            get
            {
                return CurrentState.NextLineIndent.IndentString;
            }
        }

        /// <summary>
        ///     True if the current line needs to be reindented.
        /// </summary>
        public bool NeedsReindent
        {
            get
            {
                return !IsLineStart && (ThisLineIndent != CurrentIndent.ToString());
            }
        }

        /// <summary>
        ///     Raised when <see cref="ThisLineIndent"/> has changed.
        /// </summary>
        public event EventHandler OnThisLineIndentChanged;

        /// <summary>
        ///     Stores the current indent on the beginning of the current line.
        /// </summary>
        public StringBuilder CurrentIndent = new StringBuilder();

        /// <summary>
        ///     Stores conditional symbols of the #define directives.
        /// </summary>
        internal HashSet<string> ConditionalSymbols = new HashSet<string>();

        /// <summary>
        ///     True if any of the preprocessor if/elif directives in the current
        ///     block (between #if and #endif) were evaluated to true.
        /// </summary>
        internal bool IfDirectiveEvalResult;

        #endregion

        #region Fields

        internal int offset = 0;
        internal int line = 1;
        internal int column = 1;

        public bool IsLineStart = true;
        public char CurrentChar = '\0';
        public char PreviousChar = '\0';

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
            this.CurrentState = prototype.CurrentState.Clone();
            this.ConditionalSymbols = new HashSet<string>(prototype.ConditionalSymbols);
            this.IfDirectiveEvalResult = prototype.IfDirectiveEvalResult;
            this.CurrentIndent = new StringBuilder(prototype.CurrentIndent.ToString());

            this.offset = prototype.offset;
            this.line = prototype.line;
            this.column = prototype.column;
            this.IsLineStart = prototype.IsLineStart;
            this.CurrentChar = prototype.CurrentChar;
            this.PreviousChar = prototype.PreviousChar;
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
        ///     Resets the engine.
        /// </summary>
        public void Reset()
        {
            CurrentState = IndentStateFactory.Default(this);
            ConditionalSymbols.Clear();
            IfDirectiveEvalResult = false;
            CurrentIndent.Clear();

            offset = 0;
            line = 1;
            column = 1;
            IsLineStart = true;
            PreviousChar = '\0';
        }

        /// <summary>
        ///     Calls the <see cref="OnThisLineIndentChanged"/> event.
        /// </summary>
        internal void ThisLineIndentChanged()
        {
            if (OnThisLineIndentChanged != null)
            {
                OnThisLineIndentChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     Pushes a new char into the current state which calculates the new
        ///     indentation level.
        /// </summary>
        /// <param name="ch">
        ///     A new character.
        /// </param>
        public void Push(char ch)
        {
            CurrentChar = ch;

            CurrentState.Push(ch);

            if (!TextEditorOptions.EolMarker.Contains(ch))
            {
                IsLineStart &= char.IsWhiteSpace(ch);

                if (IsLineStart)
                {
                    CurrentIndent.Append(ch);
                }

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
                if (ch != NewLineChar)
                {
                    return;
                }

                CurrentIndent.Clear();
                IsLineStart = true;
                column = 1;
                line++;
            }

            offset++;
            PreviousChar = ch;
        }

        public void UpdateToOffset(int toOffset)
        {
            if (toOffset < offset)
            {
                Reset();
            }

            for (int i = offset; i < toOffset; i++)
            {
                Push(Document.GetCharAt(i));
            }
        }

        #endregion
    }
}
