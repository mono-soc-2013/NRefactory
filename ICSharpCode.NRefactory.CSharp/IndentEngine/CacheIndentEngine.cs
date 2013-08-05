﻿//
// CacheIndentEngine.cs
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
	///     Represents a decorator of an IDocumentIndentEngine instance that provides
	///     logic for reseting and updating the engine on text changed events.
	/// </summary>
	/// <remarks>
	///     The decorator is based on periodical caching of the engine's state and
	///     delegating all logic behind indentation to the currently active engine.
	/// </remarks>
	public class CacheIndentEngine : IDisposable, IStateMachineIndentEngine
	{
		#region Properties

		/// <summary>
		///     Represents the cache interval in number of chars pushed to the engine.
		/// </summary>
		/// <remarks>
		///     When this many new chars are pushed to the engine, the currently active
		///     engine gets cloned and added to the end of <see cref="cachedEngines"/>.
		/// </remarks>
		readonly int cacheRate;

		/// <summary>
		///     Determines how much memory to reserve on initialization for the
		///     cached engines.
		/// </summary>
		const int cacheCapacity = 25;

		/// <summary>
		///     Currently active engine.
		/// </summary>
		/// <remarks>
		///     Should be equal to the last engine in <see cref="cachedEngines"/>.
		/// </remarks>
		IStateMachineIndentEngine currentEngine;

		/// <summary>
		///     List of cached engines sorted ascending by 
		///     <see cref="IDocumentIndentEngine.Offset"/>.
		/// </summary>
		IStateMachineIndentEngine[] cachedEngines;

		/// <summary>
		///     The number of engines that have been cached so far.
		/// </summary>
		/// <remarks>
		///     Should be equal to: currentEngine.Offset / CacheRate
		/// </remarks>
		int cachedEnginesCount;

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new CacheIndentEngine instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///     An instance of <see cref="IDocumentIndentEngine"/> to which the
		///     logic for indentation will be delegated.
		/// </param>
		/// <param name="cacheRate">
		///     The number of chars between caching.
		/// </param>
		public CacheIndentEngine(IStateMachineIndentEngine decoratedEngine, int cacheRate = 2000)
		{
			this.cachedEngines = new IStateMachineIndentEngine[cacheCapacity];

			this.cachedEngines[0] = decoratedEngine.Clone();
			this.currentEngine = this.cachedEngines[0];
			this.cacheRate = cacheRate;

			Document.TextChanged += textChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Handles the TextChanged event of <see cref="IDocumentIndentEngine.Document"/>.
		/// </summary>
		void textChanged(object sender, TextChangeEventArgs args)
		{
			Update(args.Offset + args.InsertionLength + args.RemovalLength);
		}

		/// <summary>
		///     Performs caching of the <see cref="CacheIndentEngine.currentEngine"/>.
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

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public IDocument Document
		{
			get { return currentEngine.Document; }
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

		/// <inheritdoc />
		/// <remarks>
		///     If the <paramref name="offset"/> is negative, the engine will
		///     update to: document.TextLength + (offset % document.TextLength+1)
		///     Otherwise it will update to: offset % document.TextLength+1
		/// </remarks>
		public void Update(int offset)
		{
			// map the given offset to the [0, document.TextLength] interval
			// using modulo arithmetics
			offset %= Document.TextLength + 1;
			if (offset < 0)
			{
				offset += Document.TextLength + 1;
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
			currentEngine.Update(offset);
		}

		#endregion

		#region IClonable

		/// <inheritdoc />
		public IStateMachineIndentEngine Clone()
		{
			return new CacheIndentEngine(currentEngine, cacheRate);
		}

		/// <inheritdoc />
		IDocumentIndentEngine IDocumentIndentEngine.Clone()
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
			Document.TextChanged -= textChanged;
		}

		#endregion

		#region IStateMachineIndentEngine

		public bool IsInsidePreprocessorDirective
		{
			get { return currentEngine.IsInsidePreprocessorDirective; }
		}

		public bool IsInsidePreprocessorComment
		{
			get { return currentEngine.IsInsidePreprocessorComment; }
		}

		public bool IsInsideStringLiteral
		{
			get { return currentEngine.IsInsideStringLiteral; }
		}

		public bool IsInsideVerbatimString
		{
			get { return currentEngine.IsInsideVerbatimString; }
		}

		public bool IsInsideCharacter
		{
			get { return currentEngine.IsInsideCharacter; }
		}

		public bool IsInsideString
		{
			get { return currentEngine.IsInsideString; }
		}

		public bool IsInsideLineComment
		{
			get { return currentEngine.IsInsideLineComment; }
		}

		public bool IsInsideMultiLineComment
		{
			get { return currentEngine.IsInsideMultiLineComment; }
		}

		public bool IsInsideDocLineComment
		{
			get { return currentEngine.IsInsideDocLineComment; }
		}

		public bool IsInsideComment
		{
			get { return currentEngine.IsInsideComment; }
		}

		public bool IsInsideOrdinaryComment
		{
			get { return currentEngine.IsInsideOrdinaryComment; }
		}

		public bool IsInsideOrdinaryCommentOrString
		{
			get { return currentEngine.IsInsideOrdinaryCommentOrString; }
		}

		public bool LineBeganInsideVerbatimString
		{
			get { return currentEngine.LineBeganInsideVerbatimString; }
		}

		public bool LineBeganInsideMultiLineComment
		{
			get { return currentEngine.LineBeganInsideMultiLineComment; }
		}

		#endregion
	}
}
