//
// IDocumentIndentEngine.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	///     Interface for indentation engines based on 
	///     <see cref="ICSharpCode.NRefactory.Editor.IDocument"/>.
	/// </summary>
	public interface IDocumentIndentEngine : IIndentEngine
	{
		/// <summary>
		///		A reference to the document that's parsed by the engine.
		/// </summary>
		IDocument Document { get; }

		/// <summary>
		///		Clones the engine and preserves the current state.
		/// </summary>
		/// <returns>
		///		An indentical clone which can operate without interference
		///		with this engine.
		/// </returns>
		new IDocumentIndentEngine Clone();
	}
}
