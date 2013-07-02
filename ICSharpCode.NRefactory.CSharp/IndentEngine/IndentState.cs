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
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
    internal class IndentState
    {
        public IndentEngine Engine;
        public IndentState Parent;

        public StringBuilder WordBuf = new StringBuilder();
        public StringBuilder CurrentIndent = new StringBuilder();
        public Indent ThisLineindent;
        public Indent Indent;
        public Indent IndentDelta;

        protected IndentState(IndentEngine engine, IndentState parent = null)
        {
            Parent = parent;
            Engine = engine;

            InitializeState();
        }

        protected IndentState(IndentState prototype)
        {
            this.Engine = prototype.Engine;
            this.Parent = prototype.Parent.Clone();
            this.WordBuf = prototype.WordBuf;
            this.CurrentIndent = prototype.CurrentIndent;
            this.ThisLineindent = prototype.ThisLineindent;
            this.Indent = prototype.Indent;
            this.IndentDelta = prototype.IndentDelta;
        }

        public IndentState Clone()
        {
            return new IndentState(this);
        }

        public virtual void InitializeState()
        {
            this.Indent = new Indent(Engine.TextEditorOptions);
            this.IndentDelta = new Indent(Engine.TextEditorOptions);
            this.ThisLineindent = new Indent(Engine.TextEditorOptions);
        }

        public virtual void Push(char ch)
        { }
    }

    internal static class IndentStateFactory
    {
        public static IndentState Create(Type stateType, IndentEngine engine, IndentState parent = null)
        {
            IndentState state;

            if (typeof(IndentState).IsAssignableFrom(stateType))
            {
                state = (IndentState)Activator.CreateInstance(stateType, engine, parent);
            }
            else
            {
                state = new Null(engine, parent);
            }

            return state;
        }

        public static IndentState Create<T>(IndentEngine engine, IndentState parent = null)
        {
            return Create(typeof(T), engine, parent);
        }

        public static Func<IndentEngine, IndentState> Default = engine => Create<Empty>(engine);
    }

    internal class Null : IndentState
    {
        public Null(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }
    }

    internal class Empty : IndentState
    {
        public Empty(IndentEngine engine, IndentState parent = null)
            : base(engine, parent)
        { }
    }
}
