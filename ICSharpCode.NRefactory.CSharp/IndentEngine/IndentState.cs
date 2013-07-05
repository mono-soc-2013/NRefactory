//
// IndentState.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
    #region IndentState

    /// <summary>
    ///     The base class for indentation states. 
    ///     Each state defines the logic for indentation based on chars that
    ///     are pushed to it.
    /// </summary>
    /// <remarks>
    ///     Should be abstract, but because of IClonable it's implemented as a 
    ///     normal class with a protected constructor.
    /// </remarks>
    internal class IndentState
    {
        #region Properties

        /// <summary>
        ///     The indentation engine using this state.
        /// </summary>
        public IndentEngine Engine;

        /// <summary>
        ///     The parent state. 
        ///     This state can use the indentation level of its parent.
        /// </summary>
        public IndentState Parent;

        /// <summary>
        ///     Type of the current block body.
        /// </summary>
        public Body CurrentBody;

        /// <summary>
        ///     Type of the next block body.
        ///     Same as <paramref name="CurrentBody"/> if none of the
        ///     <see cref="Body"/> keywords have been read.
        /// </summary>
        public Body NextBody;

        /// <summary>
        ///     Stores the last sequence of characters that can form a
        ///     valid keyword or variable.
        /// </summary>
        public StringBuilder WordBuf = new StringBuilder();

        /// <summary>
        ///     The indentation of this line.
        ///     This is set when the state is created and can change when the
        ///     newline char is pushed.
        /// </summary>
        public Indent ThisLineIndent;

        /// <summary>
        ///     The indentation of the next line.
        ///     This is set when the state is created and can change depending
        ///     on the pushed chars.
        /// </summary>
        public Indent NextLineIndent;

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a new indentation state.
        /// </summary>
        /// <param name="engine">
        ///     The indentation engine that uses this state.
        /// </param>
        /// <param name="parent">
        ///     The parent state, or null if this state doesn't have one ->
        ///         e.g. the state represents the global space.
        /// </param>
        protected IndentState(IndentEngine engine, IndentState parent = null)
        {
            Parent = parent;
            Engine = engine;

            CurrentBody = NextBody = Parent != null ? Parent.NextBody : Body.None;

            InitializeState();
        }

        /// <summary>
        ///     Creates a new indentation state that is a copy of the given
        ///     prototype.
        /// </summary>
        /// <param name="prototype">
        ///     The prototype state.
        /// </param>
        protected IndentState(IndentState prototype)
        {
            this.Engine = prototype.Engine;
            this.Parent = prototype.Parent.Clone();

            this.CurrentBody = prototype.CurrentBody;
            this.NextBody = prototype.NextBody;
            this.ThisLineIndent = prototype.ThisLineIndent;
            this.NextLineIndent = prototype.NextLineIndent;
            // this.WordBuf = prototype.WordBuf; // ??
        }

        #endregion

        #region IClonable

        public IndentState Clone()
        {
            return new IndentState(this);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Initializes the state:
        ///       - sets the indentation levels.
        /// </summary>
        /// <remarks>
        ///     Each state can override this method if it needs a different
        ///     logic for setting up the indentations.
        /// </remarks>
        public virtual void InitializeState()
        {
            ThisLineIndent = new Indent(Engine.TextEditorOptions);
            NextLineIndent = ThisLineIndent.Clone();
        }

        /// <summary>
        ///     Changes the current <see cref="IndentationEngine"/> state using the current
        ///     state as the parent for the new one.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the new state. Must be assignable from <see cref="IndentState"/>.
        /// </typeparam>
        public virtual void ChangeState<T>()
            where T : IndentState
        {
            Engine.CurrentState = IndentStateFactory.Create<T>(Engine.CurrentState);
        }

        /// <summary>
        ///     Exits this state by setting the current <see cref="IndentEngine"/>
        ///     state to the its parent.
        /// </summary>
        public virtual void ExitState()
        {
            Engine.CurrentState = Engine.CurrentState.Parent;
        }

        /// <summary>
        ///     Common logic behind the push method.
        ///     Each state derives this method and implements its own logic.
        ///     The state can also call the base push, but it's not necessary.
        /// </summary>
        /// <param name="ch">
        ///     The current character that's pushed.
        /// </param>
        public virtual void Push(char ch)
        {
            if ((WordBuf.Length == 0 ? char.IsLetter(ch) : char.IsLetterOrDigit(ch)) || ch == '_')
            {
                WordBuf.Append(ch);
            }
            else
            {
                CheckKeyword(WordBuf.ToString());
				WordBuf.Length = 0;
            }

            if (Environment.NewLine.Contains(ch))
            {
                if (Engine.lastSignificantChar == ';')
                {
                    while (NextLineIndent.Count > 0 && NextLineIndent.Peek() == IndentType.Continuation)
                    {
                        NextLineIndent.Pop();
                    }
                } 
                
                ThisLineIndent = NextLineIndent.Clone();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Types of block bodies.
        /// </summary>
        internal enum Body
		{
			None,
			Namespace,
			Class,
			Struct,
			Interface,
			Enum,
			Switch
		}

        /// <summary>
        ///     Checks if the given string is a keyword and sets the
        ///     <see cref="NextBody"/> and the indentation level appropriately.
        /// </summary>
        /// <param name="keyword">
        ///     A possible keyword.
        /// </param>
        internal virtual void CheckKeyword(string keyword)
        {
            var blocks = new Dictionary<string, Body>
            {
                { "namespace", Body.Namespace },
                { "class", Body.Class },
                { "struct", Body.Struct },
                { "interface", Body.Interface },
                { "enum", Body.Enum },
                { "switch", Body.Switch },
            };
            var specials = new HashSet<string>
            {
                "do", "if", "else", "for", "foreach", "while"
            };

            if (blocks.ContainsKey(keyword))
            {
                NextBody = blocks[keyword];
            }
            else if (specials.Contains(keyword))
            {
                NextLineIndent.Push(IndentType.Continuation);
            }
        }

        /// <summary>
        ///     Pushes a new level of indentation depending on the given
        ///     <paramref name="braceStyle"/>.
        /// </summary>
        void AddIndentation(BraceStyle braceStyle)
        {
            switch (braceStyle)
            {
                case BraceStyle.DoNotChange:
                case BraceStyle.EndOfLine:
                case BraceStyle.EndOfLineWithoutSpace:
                case BraceStyle.NextLine:
                case BraceStyle.NextLineShifted:
                case BraceStyle.BannerStyle:
                    NextLineIndent.Push(IndentType.Block);
                    break;
                case BraceStyle.NextLineShifted2:
                    NextLineIndent.Push(IndentType.DoubleBlock);
                    break;
            }
        }

        /// <summary>
        ///     Pushes a new level of indentation depending on the given
        ///     <paramref name="body"/>.
        /// </summary>
        internal void AddIndentation(Body body)
        {
            switch (body)
            {
                case Body.None:
                    NextLineIndent.Push(IndentType.Block);
                    break;
                case Body.Namespace:
                    AddIndentation(Engine.Options.NamespaceBraceStyle);
                    break;
                case Body.Class:
                    AddIndentation(Engine.Options.ClassBraceStyle);
                    break;
                case Body.Struct:
                    AddIndentation(Engine.Options.StructBraceStyle);
                    break;
                case Body.Interface:
                    AddIndentation(Engine.Options.InterfaceBraceStyle);
                    break;
                case Body.Enum:
                    AddIndentation(Engine.Options.EnumBraceStyle);
                    break;
                case Body.Switch:
                    if (Engine.Options.IndentSwitchBody)
                        NextLineIndent.Push(IndentType.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Pre processor evaluation (from cs-tokenizer.cs)

        static bool is_identifier_start_character(int c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || Char.IsLetter((char)c);
        }

        static bool is_identifier_part_character(char c)
        {
            if (c >= 'a' && c <= 'z')
                return true;

            if (c >= 'A' && c <= 'Z')
                return true;

            if (c == '_' || (c >= '0' && c <= '9'))
                return true;

            if (c < 0x80)
                return false;

            return Char.IsLetter(c) || Char.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation;
        }

        bool eval_val(string s)
        {
            if (s == "true")
                return true;
            if (s == "false")
                return false;

            return Engine.ConditionalSymbols != null && Engine.ConditionalSymbols.Contains(s);
        }

        bool pp_primary(ref string s)
        {
            s = s.Trim();
            int len = s.Length;

            if (len > 0)
            {
                char c = s[0];

                if (c == '(')
                {
                    s = s.Substring(1);
                    bool val = pp_expr(ref s, false);
                    if (s.Length > 0 && s[0] == ')')
                    {
                        s = s.Substring(1);
                        return val;
                    }
                    return false;
                }

                if (is_identifier_start_character(c))
                {
                    int j = 1;

                    while (j < len)
                    {
                        c = s[j];

                        if (is_identifier_part_character(c))
                        {
                            j++;
                            continue;
                        }
                        bool v = eval_val(s.Substring(0, j));
                        s = s.Substring(j);
                        return v;
                    }
                    bool vv = eval_val(s);
                    s = "";
                    return vv;
                }
            }
            return false;
        }

        bool pp_unary(ref string s)
        {
            s = s.Trim();
            int len = s.Length;

            if (len > 0)
            {
                if (s[0] == '!')
                {
                    if (len > 1 && s[1] == '=')
                    {
                        return false;
                    }
                    s = s.Substring(1);
                    return !pp_primary(ref s);
                }
                else
                    return pp_primary(ref s);
            }
            else
            {
                return false;
            }
        }

        bool pp_eq(ref string s)
        {
            bool va = pp_unary(ref s);

            s = s.Trim();
            int len = s.Length;
            if (len > 0)
            {
                if (s[0] == '=')
                {
                    if (len > 2 && s[1] == '=')
                    {
                        s = s.Substring(2);
                        return va == pp_unary(ref s);
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (s[0] == '!' && len > 1 && s[1] == '=')
                {
                    s = s.Substring(2);

                    return va != pp_unary(ref s);

                }
            }

            return va;

        }

        bool pp_and(ref string s)
        {
            bool va = pp_eq(ref s);

            s = s.Trim();
            int len = s.Length;
            if (len > 0)
            {
                if (s[0] == '&')
                {
                    if (len > 2 && s[1] == '&')
                    {
                        s = s.Substring(2);
                        return (va & pp_and(ref s));
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return va;
        }

        //
        // Evaluates an expression for `#if' or `#elif'
        //
        bool pp_expr(ref string s, bool isTerm)
        {
            bool va = pp_and(ref s);
            s = s.Trim();
            int len = s.Length;
            if (len > 0)
            {
                char c = s[0];

                if (c == '|')
                {
                    if (len > 2 && s[1] == '|')
                    {
                        s = s.Substring(2);
                        return va | pp_expr(ref s, isTerm);
                    }
                    else
                    {

                        return false;
                    }
                }
                if (isTerm)
                {
                    return false;
                }
            }

            return va;
        }

        internal bool eval(string s)
        {
            bool v = pp_expr(ref s, true);
            s = s.Trim();
            if (s.Length != 0)
            {
                return false;
            }

            return v;
        }

        #endregion

        #endregion
    }

    #endregion

    #region IndentStateFactory

    /// <summary>
    ///     Indentation state factory.
    /// </summary>
    internal static class IndentStateFactory
    {
        /// <summary>
        ///     Creates a new state.
        /// </summary>
        /// <param name="stateType">
        ///     Type of the state. Must be assignable from <see cref="IndentState"/>.
        /// </param>
        /// <param name="engine">
        ///     Indentation engine for the state.
        /// </param>
        /// <param name="parent">
        ///     Parent state.
        /// </param>
        /// <returns>
        ///     A new state of type <paramref name="stateType"/>.
        /// </returns>
        static IndentState Create(Type stateType, IndentEngine engine, IndentState parent = null)
        {
            return (IndentState)Activator.CreateInstance(stateType, engine, parent);
        }

        /// <summary>
        ///     Creates a new state.
        /// </summary>
        /// <typeparam name="T">
        ///     Type of the state. Must be assignable from <see cref="IndentState"/>.
        /// </typeparam>
        /// <param name="engine">
        ///     Indentation engine for the state.
        /// </param>
        /// <param name="parent">
        ///     Parent state.
        /// </param>
        /// <returns>
        ///     A new state of type <typeparamref name="T"/>.
        /// </returns>
        public static IndentState Create<T>(IndentEngine engine, IndentState parent = null)
            where T : IndentState
        {
            return Create(typeof(T), engine, parent);
        }

        /// <summary>
        ///     Creates a new state.
        /// </summary>
        /// <typeparam name="T">
        ///     Type of the state. Must be assignable from <see cref="IndentState"/>.
        /// </typeparam>
        /// <param name="prototype">
        ///     Parent state. Also, the indentation engine of the prototype is
        ///     used as the engine for the new state.
        /// </param>
        /// <returns>
        ///     A new state of type <typeparamref name="T"/>.
        /// </returns>
        public static IndentState Create<T>(IndentState prototype)
            where T : IndentState
        {
            return Create(typeof(T), prototype.Engine, prototype);
        }

        /// <summary>
        ///     The default state, used for the global space.
        /// </summary>
        public static Func<IndentEngine, IndentState> Default = engine => Create<GlobalBody>(engine);
    }

    #endregion

    #region Null state

    /// <summary>
    ///     Null state.
    /// </summary>
    /// <remarks>
    ///     Doesn't define any transitions to new states.
    /// </remarks>
    internal class Null : IndentState
    {
        public Null(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }
    }

    #endregion

    #region BracketsBody state

    /// <summary>
    ///     Brackets body state.
    /// </summary>
    /// <remarks>
    ///     Represents a block of code between a pair of brackets.
    /// </remarks>
    internal class BracketsBody : IndentState
    {
        /// <summary>
        ///     Defines transitions for all types of open brackets.
        /// </summary>
        internal static Dictionary<char, Action<IndentState>> OpenBrackets = 
            new Dictionary<char, Action<IndentState>>
        { 
            { '{', state => state.ChangeState<BracesBody>() }, 
            { '(', state => state.ChangeState<ParenthesesBody>() }, 
            { '[', state => state.ChangeState<SquareBracketsBody>() }, 
            { '<', state => state.ChangeState<AngleBracketsBody>() }
        };

        /// <summary>
        ///     When derived in a concrete bracket body state, represents
        ///     the closed bracket character pair.
        /// </summary>
        internal virtual char ClosedBracket 
        { 
            get { throw new NotImplementedException(); }
        }

        protected BracketsBody(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            base.Push(ch);

            if (ch == '#')
            {
                ChangeState<PreProcessor>();
            }
            else if (ch == '/' && Engine.previousChar == '/')
            {
                ChangeState<LineComment>();
            }
            else if (ch == '*' && Engine.previousChar == '/')
            {
                ChangeState<MultiLineComment>();
            }
            else if (ch == '"')
            {
                if (Engine.previousChar == '@')
                {
                    ChangeState<VerbatimString>();
                }
                else
                {
                    ChangeState<StringLiteral>();
                }
            }
            else if (ch == '\'')
            {
                ChangeState<Character>();
            }
            else if (OpenBrackets.ContainsKey(ch))
            {
                OpenBrackets[ch](this);
            }
            else if (ch == ClosedBracket)
            {
                ExitState();
            }
        }
    }

    #region Global body state

    /// <summary>
    ///     Global body state.
    /// </summary>
    /// <remarks>
    ///     Represents the global space of the program.
    /// </remarks>
    internal class GlobalBody : BracketsBody
    {
        internal override char ClosedBracket
        {
            get { return '\0'; }
        }

        public GlobalBody(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }
    }

    #endregion

    #region Braces body state

    /// <summary>
    ///     Braces body state.
    /// </summary>
    /// <remarks>
    ///     Represents a block of code between { and }.
    /// </remarks>
    internal class BracesBody : BracketsBody
    {
        internal override char ClosedBracket
        {
            get { return '}'; }
        }

        public BracesBody(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
            AddIndentation(CurrentBody);
        }
    }

    #endregion

    #region Parentheses body state

    /// <summary>
    ///     Parentheses body state.
    /// </summary>
    /// <remarks>
    ///     Represents a block of code between ( and ).
    /// </remarks>
    internal class ParenthesesBody : BracketsBody
    {
        internal override char ClosedBracket
        {
	        get { return ')'; }
        }

        public ParenthesesBody(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
            NextLineIndent.ExtraSpaces = Engine.column - NextLineIndent.CurIndent - 1;
        }
    }

    #endregion

    #region Square brackets body state

    /// <summary>
    ///     Square brackets body state.
    /// </summary>
    /// <remarks>
    ///     Represents a block of code between [ and ].
    /// </remarks>
    internal class SquareBracketsBody : BracketsBody
    {
        internal override char ClosedBracket
        {
            get { return ']'; }
        }

        public SquareBracketsBody(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
            NextLineIndent.ExtraSpaces = Engine.column - NextLineIndent.CurIndent - 1;
        }
    }

    #endregion

    #region Angle brackets body state

    /// <summary>
    ///     Angle brackets body state.
    /// </summary>
    /// <remarks>
    ///     Represents a block of code between < and >.
    /// </remarks>
    internal class AngleBracketsBody : BracketsBody
    {
        internal override char ClosedBracket
        {
            get { return '>'; }
        }

        public AngleBracketsBody(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
            NextLineIndent.ExtraSpaces = Engine.column - NextLineIndent.CurIndent - 1;
        }
    }

    #endregion

    #endregion

    #region PreProcessor state

    /// <summary>
    ///     PreProcessor directive state.
    /// </summary>
    /// <remarks>
    ///     Activated when the '#' char is pushed.
    /// </remarks>
    internal class PreProcessor : IndentState
    {
        /// <summary>
        ///     True if this preprocessor directive is #if or #elif.
        /// </summary>
        internal bool IsPreProcessorIfStatement;

        /// <summary>
        ///     If <see cref="IsPreProcessorIfStatement"/> is true, this
        ///     stores the expression of the directive.
        /// </summary>
        internal StringBuilder IfStatement = new StringBuilder();

        public PreProcessor(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            base.Push(ch);

            if (IsPreProcessorIfStatement)
            {
                IfStatement.Append(ch);
            }

            if (Environment.NewLine.Contains(ch))
            {
                if (eval(IfStatement.ToString()))
                {
                    // the if directive is true -> continue with the previous state
                    ExitState();
                }
                else
                {
                    // the if directive is false -> change to a state that will 
                    // ignore any chars until #endif or #elif
                    ExitState();
                    ChangeState<PreProcessorComment>();
                }
            }
            else if (new string[] { "if", "elif" }.Contains(WordBuf.ToString()))
            {
                IsPreProcessorIfStatement = true;
            }
            else if (WordBuf.ToString() == "else")
            {
                // TODO: was the last #if or #elif true?
                ExitState();
            }
            else if (new string[] { "region", "endregion" }.Contains(WordBuf.ToString()))
            {
                ExitState();
                ChangeState<Region>();
            }
            else if (new string[] { "pragma", "warning", "error", "line", "define", "undef" }.Contains(WordBuf.ToString()))
            {
                // TODO: Make a seperate state for define and undef so that the
                //       #if directive can correctly determine if it's true
                ExitState();
                ChangeState<PreProcessorStatement>();
            }
            else if (WordBuf.ToString() == "endif")
            {
                ExitState();
            }
        }

        public override void InitializeState()
        {
            ThisLineIndent = new Indent(Engine.TextEditorOptions);
            NextLineIndent = Parent.ThisLineIndent.Clone();
        }

        internal override void CheckKeyword(string keyword)
        {
            return;
        }
    }

    #endregion

    #region PreProcessorComment state

    /// <summary>
    ///     PreProcessor comment directive state.
    /// </summary>
    /// <remarks>
    ///     Activates when the #if or #elif directive is false and ignores
    ///     all pushed chars until the next '#'.
    /// </remarks>
    internal class PreProcessorComment : IndentState
    {
        public PreProcessorComment(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            base.Push(ch);

            if (ch == '#')
            {
                ExitState();
                ChangeState<PreProcessor>();
            }
        }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.NextLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
        }
    }

    #endregion

    #region PreProcessorStatement state

    /// <summary>
    ///     PreProcessor single-line directive state.
    /// </summary>
    /// <remarks>
    ///     e.g. pragma, warning, region, define...
    /// </remarks>
    internal class PreProcessorStatement : IndentState
    {
        public PreProcessorStatement(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            base.Push(ch);

            if (Environment.NewLine.Contains(ch))
            {
                ExitState();
            }
        }

        public override void InitializeState()
        {
            ThisLineIndent = new Indent(Engine.TextEditorOptions);
            NextLineIndent = Parent.NextLineIndent.Clone();
        }
    }

    #endregion

    #region Region state

    /// <summary>
    ///     Region directive state.
    /// </summary>
    /// <remarks>
    ///     Activates on #region or #endregion
    /// </remarks>
    internal class Region : IndentState
    {
        public Region(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            base.Push(ch);

            if (Environment.NewLine.Contains(ch))
            {
                ExitState();
            }
        }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.NextLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
        }
    }

    #endregion

    #region Comment state

    /// <summary>
    ///     Base for all comment states.
    /// </summary>
    internal class CommentBase : IndentState
    {
        protected CommentBase(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = Parent.NextLineIndent.Clone();
        }
    }

    #endregion

    #region LineComment state

    /// <summary>
    ///     Single-line comment state.
    /// </summary>
    internal class LineComment : CommentBase
    {
        /// <summary>
        ///     It's possible that this should be the DocComment state ->
        ///     Check if the first next pushed char is equal to '/'.
        /// </summary>
        internal bool CheckForDocComment = true;

        public LineComment(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            if (Environment.NewLine.Contains(ch))
            {
                ExitState();
            }
            else if (ch == '/' && CheckForDocComment)
            {
                // wrong state, should be DocComment.
                ExitState();
                ChangeState<DocComment>();
            }

            CheckForDocComment = false;
        }

        internal override void CheckKeyword(string keyword)
        {
            return;
        }
    }

    #endregion

    #region DocComment state

    /// <summary>
    ///     DocComment state.
    /// </summary>
    internal class DocComment : CommentBase
    {
        public DocComment(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            if (Environment.NewLine.Contains(ch))
            {
                ExitState();
            }
        }

        internal override void CheckKeyword(string keyword)
        {
            return;
        }
    }

    #endregion

    #region MultiLineComment state

    /// <summary>
    ///     Multi-line comment state.
    /// </summary>
    internal class MultiLineComment : CommentBase
    {
        public MultiLineComment(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            base.Push(ch);

            if (ch == '/' && Engine.previousChar == '*')
            {
                ExitState();
            }
        }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = Parent.NextLineIndent.Clone();
            // add extra spaces so that the next line of the comment is align
            // to the first line
            NextLineIndent.ExtraSpaces = Engine.column - NextLineIndent.CurIndent;
        }

        internal override void CheckKeyword(string keyword)
        {
            return;
        }
    }

    #endregion

    #region StringLiteral state

    /// <summary>
    ///     StringLiteral state.
    /// </summary>
    internal class StringLiteral : IndentState
    {
        /// <summary>
        ///     True if the next char is escaped with '\'.
        /// </summary>
        internal bool IsEscaped;

        public StringLiteral(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            if (Environment.NewLine.Contains(ch))
            {
                ExitState();
            }
            else if (!IsEscaped && ch == '"')
            {
                ExitState();
            }
            
            IsEscaped = ch == '\\' && !IsEscaped;
        }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
        }

        internal override void CheckKeyword(string keyword)
        {
            return;
        }
    }

    #endregion

    #region Verbatim string state

    /// <summary>
    ///     Verbatim string state.
    /// </summary>
    internal class VerbatimString : IndentState
    {
        /// <summary>
        ///     True if there are odd number of '"' in a row.
        /// </summary>
        internal bool IsEscaped;

        public VerbatimString(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }
        
        public override void Push(char ch)
        {
            base.Push(ch);

            if (IsEscaped && ch != '"')
            {
                ExitState();
                // the char has been pushed to the wrong state
                Engine.CurrentState.Push(ch);
            }

            IsEscaped = ch == '"' && !IsEscaped;
        }
        
        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = new Indent(Engine.TextEditorOptions);
        }

        internal override void CheckKeyword(string keyword)
        {
            return;
        }
    }

    #endregion

    #region Character state

    /// <summary>
    ///     Character state.
    /// </summary>
    internal class Character : IndentState
    {
        /// <summary>
        ///     True if the next char is escaped with '\'.
        /// </summary>
        internal bool IsEscaped;

        public Character(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }

        public override void Push(char ch)
        {
            if (Environment.NewLine.Contains(ch))
            {
                ExitState();
            }
            else if (!IsEscaped && ch == '\'')
            {
                ExitState();
            }

            IsEscaped = ch == '\\' && !IsEscaped;
        }

        public override void InitializeState()
        {
            ThisLineIndent = Parent.ThisLineIndent.Clone();
            NextLineIndent = ThisLineIndent.Clone();
        }
    }

    #endregion
}
