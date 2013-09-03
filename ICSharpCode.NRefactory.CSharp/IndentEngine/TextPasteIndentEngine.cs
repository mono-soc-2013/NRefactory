﻿//
// TextPasteIndentEngine.cs
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
using System.Reflection;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	///     Represents a decorator of an IStateMachineIndentEngine instance
	///     that provides logic for text paste events.
	/// </summary>
	public class TextPasteIndentEngine : IDocumentIndentEngine, ITextPasteHandler
	{
		#region Properties

		/// <summary>
		///     An instance of IStateMachineIndentEngine which handles
		///     the indentation logic.
		/// </summary>
		IStateMachineIndentEngine engine;

		/// <summary>
		///     Text editor options.
		/// </summary>
		internal readonly TextEditorOptions textEditorOptions;

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new TextPasteIndentEngine instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///     An instance of <see cref="IStateMachineIndentEngine"/> to which the
		///     logic for indentation will be delegated.
		/// </param>
		/// <param name="textEditorOptions">
		///    Text editor options for indentation.
		/// </param>
		public TextPasteIndentEngine(IStateMachineIndentEngine decoratedEngine, TextEditorOptions textEditorOptions)
		{
			this.engine = decoratedEngine;
			this.textEditorOptions = textEditorOptions;
		}

		#endregion

		#region ITextPasteHandler

		/// <inheritdoc />
		string ITextPasteHandler.FormatPlainText(int offset, string text, byte[] copyData)
		{
			if (copyData != null && copyData.Length == 1)
			{
				var strategy = TextPasteUtils.Strategies[(PasteStrategy)copyData[0]];
				text = strategy.Decode(text);
			}

			engine.Update(offset);
			if (engine.IsInsideStringLiteral)
			{
				return TextPasteUtils.StringLiteralStrategy.Encode(text);
			}
			else if (engine.IsInsideVerbatimString)
			{
				return TextPasteUtils.VerbatimStringStrategy.Encode(text);
			}

			StringBuilder indentedText = new StringBuilder();
			foreach (var line in text.Split(text.Contains('\r') ? '\r' : '\n'))
			{
				foreach (var ch in line)
				{
					engine.Push(ch);
				}

				indentedText.Append(engine.ThisLineIndent +
									line.TrimStart('\r', '\n', ' ', '\t') +
				                    textEditorOptions.EolMarker);
			}

			return indentedText.ToString().Trim('\r', '\n', ' ', '\t');
		}

		/// <inheritdoc />
		byte[] ITextPasteHandler.GetCopyData(ISegment segment)
		{
			engine.Update(segment.Offset);

			if (engine.IsInsideStringLiteral)
			{
				return new[] { (byte)PasteStrategy.StringLiteral };
			}
			else if (engine.IsInsideVerbatimString)
			{
				return new[] { (byte)PasteStrategy.VerbatimString };
			}

			return null;
		}

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public IDocument Document
		{
			get { return engine.Document; }
		}

		/// <inheritdoc />
		public string ThisLineIndent
		{
			get { return engine.ThisLineIndent; }
		}

		/// <inheritdoc />
		public string NextLineIndent
		{
			get { return engine.NextLineIndent; }
		}

		/// <inheritdoc />
		public string CurrentIndent
		{
			get { return engine.CurrentIndent; }
		}

		/// <inheritdoc />
		public bool NeedsReindent
		{
			get { return engine.NeedsReindent; }
		}

		/// <inheritdoc />
		public int Offset
		{
			get { return engine.Offset; }
		}

		/// <inheritdoc />
		public TextLocation Location
		{
			get { return engine.Location; }
		}

		/// <inheritdoc />
		public void Push(char ch)
		{
			engine.Push(ch);
		}

		/// <inheritdoc />
		public void Reset()
		{
			engine.Reset();
		}

		/// <inheritdoc />
		public void Update(int offset)
		{
			engine.Update(offset);
		}

		#endregion

		#region IClonable

		public IDocumentIndentEngine Clone()
		{
			return new TextPasteIndentEngine(engine, textEditorOptions);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}

	/// <summary>
	///     Types of text-paste strategies.
	/// </summary>
	public enum PasteStrategy : byte
	{
		PlainText = 0,
		StringLiteral = 1,
		VerbatimString = 2
	}

	/// <summary>
	///     Defines some helper methods for dealing with text-paste events.
	/// </summary>
	public static class TextPasteUtils
	{
		/// <summary>
		///     Collection of text-paste strategies.
		/// </summary>
		public static TextPasteStrategies Strategies = new TextPasteStrategies();

		/// <summary>
		///     The interface for a text-paste strategy.
		/// </summary>
		public interface IPasteStrategy
		{
			/// <summary>
			///     Formats the given text according with this strategy rules.
			/// </summary>
			/// <param name="text">
			///    The text to format.
			/// </param>
			/// <returns>
			///     Formatted text.
			/// </returns>
			string Encode(string text);

			/// <summary>
			///     Converts text formatted according with this strategy rules
			///     to its original form.
			/// </summary>
			/// <param name="text">
			///     Formatted text to convert.
			/// </param>
			/// <returns>
			///     Original form of the given formatted text.
			/// </returns>
			string Decode(string text);

			/// <summary>
			///     Type of this strategy.
			/// </summary>
			PasteStrategy Type { get; }
		}

		/// <summary>
		///     Wrapper that discovers all defined text-paste strategies and defines a way
		///     to easily access them through their <see cref="PasteStrategy"/> type.
		/// </summary>
		public sealed class TextPasteStrategies
		{
			/// <summary>
			///     Collection of discovered text-paste strategies.
			/// </summary>
			IDictionary<PasteStrategy, IPasteStrategy> strategies;

			/// <summary>
			///     Uses reflection to find all types derived from <see cref="IPasteStrategy"/>
			///     and adds an instance of each strategy to <see cref="strategies"/>.
			/// </summary>
			public TextPasteStrategies()
			{
				strategies = Assembly.GetExecutingAssembly()
					.GetTypes()
					.Where(t => typeof(IPasteStrategy).IsAssignableFrom(t) && t.IsClass)
					.Select(t => (IPasteStrategy)t.GetProperty("Instance").GetValue(null, null))
					.ToDictionary(s => s.Type);
			}

			/// <summary>
			///     Checks if there is a strategy of the given type and returns it.
			/// </summary>
			/// <param name="strategy">
			///     Type of the strategy instance.
			/// </param>
			/// <returns>
			///     A strategy instance of the requested type,
			///     or <see cref="DefaultStrategy"/> if it wasn't found.
			/// </returns>
			public IPasteStrategy this[PasteStrategy strategy]
			{
				get
				{
					if (strategies.ContainsKey(strategy))
					{
						return strategies[strategy];
					}

					return DefaultStrategy;
				}
			}
		}

		/// <summary>
		///     Doesn't do any formatting. Serves as the default strategy.
		/// </summary>
		public class PlainTextPasteStrategy : IPasteStrategy
		{
			#region Singleton

			public static IPasteStrategy Instance
			{
				get
				{
					return instance ?? (instance = new PlainTextPasteStrategy());
				}
			}
			static PlainTextPasteStrategy instance;

			protected PlainTextPasteStrategy() { }

			#endregion

			/// <inheritdoc />
			public string Encode(string text)
			{
				return text;
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				return text;
			}

			/// <inheritdoc />
			public PasteStrategy Type
			{
				get { return PasteStrategy.PlainText; }
			}
		}

		/// <summary>
		///     Escapes chars in the given text so that they don't
		///     break a valid string literal.
		/// </summary>
		public class StringLiteralPasteStrategy : IPasteStrategy
		{
			#region Singleton

			public static IPasteStrategy Instance
			{
				get
				{
					return instance ?? (instance = new StringLiteralPasteStrategy());
				}
			}
			static StringLiteralPasteStrategy instance;

			protected StringLiteralPasteStrategy() { }

			#endregion

			Dictionary<char, IEnumerable<char>> encodeReplace = new Dictionary<char, IEnumerable<char>>
				{
					{'\"', "\\\""},
					{'\\', "\\\\"},
					{'\n', "\\n"},
					{'\r', "\\r"},
					{'\t', "\\t"},
				};

			Dictionary<char, char> decodeReplace = new Dictionary<char, char>
				{
					{'"', '"'},
					{'\\', '\\'},
					{'n', '\n'},
					{'r', '\r'},
					{'t', '\t'},
				};

			/// <inheritdoc />
			public string Encode(string text)
			{
				return string.Concat(text.SelectMany(c => encodeReplace.ContainsKey(c) ? encodeReplace[c] : new[] { c }));
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				var result = new StringBuilder();
				bool isEscaped = false;

				foreach (var ch in text)
				{
					if (isEscaped)
					{
						if (decodeReplace.ContainsKey(ch))
						{
							result.Append(decodeReplace[ch]);
						}
						else
						{
							result.Append('\\', ch);
						}
					}
					else if (ch != '\\')
					{
						result.Append(ch);
					}

					isEscaped = !isEscaped && ch == '\\';
				}

				return result.ToString();
			}

			/// <inheritdoc />
			public PasteStrategy Type
			{
				get { return PasteStrategy.StringLiteral; }
			}
		}

		/// <summary>
		///     Escapes chars in the given text so that they don't
		///     break a valid verbatim string.
		/// </summary>
		public class VerbatimStringPasteStrategy : IPasteStrategy
		{
			#region Singleton

			public static IPasteStrategy Instance
			{
				get
				{
					return instance ?? (instance = new VerbatimStringPasteStrategy());
				}
			}
			static VerbatimStringPasteStrategy instance;

			protected VerbatimStringPasteStrategy() { }

			#endregion

			Dictionary<char, IEnumerable<char>> encodeReplace = new Dictionary<char, IEnumerable<char>>
				{
					{'\"', "\"\""},
				};

			/// <inheritdoc />
			public string Encode(string text)
			{
				return string.Concat(text.SelectMany(c => encodeReplace.ContainsKey(c) ? encodeReplace[c] : new[] { c }));
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				bool isEscaped = false;
				return string.Concat(text.Where(c => !(isEscaped = !isEscaped && c == '"')));
			}

			/// <inheritdoc />
			public PasteStrategy Type
			{
				get { return PasteStrategy.VerbatimString; }
			}
		}

		/// <summary>
		///     The default text-paste strategy.
		/// </summary>
		public static IPasteStrategy DefaultStrategy = PlainTextPasteStrategy.Instance;

		/// <summary>
		///     String literal text-paste strategy.
		/// </summary>
		public static IPasteStrategy StringLiteralStrategy = StringLiteralPasteStrategy.Instance;

		/// <summary>
		///     Verbatim string text-paste strategy.
		/// </summary>
		public static IPasteStrategy VerbatimStringStrategy = VerbatimStringPasteStrategy.Instance;
	}
}
