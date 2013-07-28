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
using ICSharpCode.NRefactory.Editor;
using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	///		Represents a decorator of an IIndentEngine instance that provides
	///		logic for reseting and updating the engine on text changed events.
	/// </summary>
	/// <remarks>
	///		The decorator is based on periodical caching of the engine's state and
	///		delegating all logic behind indentation to the currently active engine.
	/// </remarks>
	public class CacheIndentEngine : IDisposable, IDocumentIndentEngine
	{
		#region Properties

		/// <summary>
		///		Represents the cache interval in number of chars pushed to the engine.
		/// </summary>
		/// <remarks>
		///		When this many new chars are pushed to the engine, the currently active
		///		engine gets cloned and added to the end of <see cref="cachedEngines"/>.
		/// </remarks>
		readonly int cacheRate;

		/// <summary>
		///		Determines how much memory to reserve on initialization for the
		///		cached engines.
		/// </summary>
		const int cacheCapacity = 25;

		/// <summary>
		///		Currently active engine.
		/// </summary>
		/// <remarks>
		///		Should be equal to the last engine in <see cref="cachedEngines"/>.
		/// </remarks>
		IIndentEngine currentEngine;

		/// <summary>
		///		List of cached engines sorted ascending by 
		///		<see cref="IIndentEngine.Offset"/>.
		/// </summary>
		IIndentEngine[] cachedEngines;

		/// <summary>
		///		The number of engines that have been cached so far.
		/// </summary>
		/// <remarks>
		///		Should be equal to: currentEngine.Offset / CacheRate
		/// </remarks>
		int cachedEnginesCount;

		/// <summary>
		///		A readonly reference to the document that's parsed
		///		by the <see cref="currentEngine"/>.
		/// </summary>
		readonly IDocument document;

		#endregion

		#region Constructors

		/// <summary>
		///		Creates a new DocumentStateTracker instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///		An instance of <see cref="IIndentEngine"/> to which the
		///		logic for indentation will be delegated.
		/// </param>
		/// <param name="document">
		///		An instance of <see cref="IDocument"/> which is being parsed.
		/// </param>
		/// <param name="cacheRate">
		///		The number of chars between caching.
		/// </param>
		public CacheIndentEngine(IIndentEngine decoratedEngine, IDocument document, int cacheRate = 2000)
		{
			this.cachedEngines = new IIndentEngine[cacheCapacity];

			this.cachedEngines[0] = decoratedEngine.Clone();
			this.currentEngine = decoratedEngine;
			this.document = document;
			this.cacheRate = cacheRate;

			this.document.TextChanged += textChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		///		Handles the TextChanged event of <see cref="CacheIndentEngine.document"/>.
		/// </summary>
		void textChanged(object sender, TextChangeEventArgs args)
		{
			UpdateEngine();
		}

		/// <summary>
		///		Performs caching of the <see cref="CacheIndentEngine.currentEngine"/>.
		/// </summary>
		void cache()
		{
			if (currentEngine.Offset % cacheRate != 0)
			{
				throw new Exception("The current engine's offset is not divisable with the cacheRate.");
			}

			// determine the correct index of the current engine in cachedEngines
			var engineIndex = currentEngine.Offset / cacheRate;

			cachedEnginesCount = Math.Max(cachedEnginesCount, engineIndex);
			if (cachedEngines.Length < cachedEnginesCount)
			{
				Array.Resize(ref cachedEngines, cachedEnginesCount * 2);
			}

			cachedEngines[engineIndex] = currentEngine.Clone();
		}

		/// <summary>
		///		Updates the engine to the end of <see cref="CacheIndentEngine.document"/>.
		/// </summary>
		public void UpdateEngine()
		{
			UpdateEngine(document.TextLength);
		}

		/// <summary>
		///		Updates the engine to the given <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset">
		///		The offset to which the engine should be updated.
		/// </param>
		/// <remarks>
		///		If the <paramref name="offset"/> is negative, the engine will
		///		update to: document.TextLength + (offset % document.TextLength+1)
		///		Otherwise it will update to: offset % document.TextLength+1
		/// </remarks>
		public void UpdateEngine(int offset)
		{
			// map the given offset to the [0, document.TextLength] interval
			// using modulo arithmetics
			offset %= document.TextLength + 1;
			if (offset < 0)
			{
				offset += document.TextLength + 1;
			}

			// check if the engine has to be updated to some previous offset
			if (currentEngine.Offset > offset)
			{
				// replace the currentEngine with the first one whose offset
				// is less then the given <paramref name="offset"/>
				cachedEnginesCount = offset / cacheRate;
				currentEngine = cachedEngines[cachedEnginesCount].Clone();
			}
			
			// update the engine to the given offset
			while (currentEngine.Offset < offset)
			{
				Push(document.GetCharAt(currentEngine.Offset));
			}
		}

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public IDocument Document
		{
			get { return document; }
		}

		/// <inheritdoc />
		public string ThisLineIndent
		{
			get { return currentEngine.ThisLineIndent; }
		}

		/// <inheritdoc />
		public string NextLineIndent
		{
			get { return currentEngine.NextLineIndent; }
		}

		/// <inheritdoc />
		public string CurrentIndent
		{
			get { return currentEngine.CurrentIndent; }
		}

		/// <inheritdoc />
		public bool NeedsReindent
		{
			get { return currentEngine.NeedsReindent; }
		}

		/// <inheritdoc />
		public int Offset
		{
			get { return currentEngine.Offset; }
		}

		/// <inheritdoc />
		public TextLocation Location
		{
			get { return currentEngine.Location; }
		}

		/// <inheritdoc />
		public void Push(char ch)
		{
			currentEngine.Push(ch);

			if (currentEngine.Offset % cacheRate == 0)
			{
				cache();
			}
		}

		/// <inheritdoc />
		public void Reset()
		{
			currentEngine = cachedEngines[cachedEnginesCount = 0];
		}

		#endregion

		#region IClonable

		/// <inheritdoc />
		public IDocumentIndentEngine Clone()
		{
			return new CacheIndentEngine(currentEngine, document, cacheRate);
		}

		IIndentEngine IIndentEngine.Clone()
		{
			return Clone();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			document.TextChanged -= textChanged;
		}

		#endregion
	}
}
