//
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
		internal readonly CSharpFormattingOptions formattingOptions;
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
		/// <param name="formattingOptions">
		///     C# formatting options.
		/// </param>
		public TextPasteIndentEngine(IStateMachineIndentEngine decoratedEngine, TextEditorOptions textEditorOptions, CSharpFormattingOptions formattingOptions)
		{
			this.engine = decoratedEngine;
			this.textEditorOptions = textEditorOptions;
			this.formattingOptions = formattingOptions;

			this.engine.EnableCustomIndentLevels = false;
		}

		#endregion

		#region ITextPasteHandler

		/// <inheritdoc />
		string ITextPasteHandler.FormatPlainText(int offset, string text, byte[] copyData)
		{
			if (copyData != null && copyData.Length == 1) {
				var strategy = TextPasteUtils.Strategies [(PasteStrategy)copyData [0]];
				text = strategy.Decode(text);
			}
			engine.Update(offset);
			if (engine.IsInsideStringLiteral) {
				int idx = text.IndexOf('"');
				if (idx > 0) {
					var o = offset;
					while (o < engine.Document.TextLength) {
						char ch = engine.Document.GetCharAt(o);
						engine.Push(ch); 
						if (NewLine.IsNewLine(ch))
							break;
						o++;
						if (!engine.IsInsideStringLiteral)
							return TextPasteUtils.StringLiteralStrategy.Encode(text);
					}
					return TextPasteUtils.StringLiteralStrategy.Encode(text.Substring(0, idx)) + text.Substring(idx);
				}
				return TextPasteUtils.StringLiteralStrategy.Encode(text);

			} else if (engine.IsInsideVerbatimString) {

				int idx = text.IndexOf('"');
				if (idx > 0) {
					var o = offset;
					while (o < engine.Document.TextLength) {
						char ch = engine.Document.GetCharAt(o);
						engine.Push(ch); 
						o++;
						if (!engine.IsInsideVerbatimString)
							return TextPasteUtils.VerbatimStringStrategy.Encode(text);
					}
					return TextPasteUtils.VerbatimStringStrategy.Encode(text.Substring(0, idx)) + text.Substring(idx);
				}

				return TextPasteUtils.VerbatimStringStrategy.Encode(text);
			}
			var line = engine.Document.GetLineByOffset(offset);
			var pasteAtLineStart = line.Offset == offset;
			var indentedText = new StringBuilder();
			var curLine = new StringBuilder();
			var clonedEngine = engine.Clone();
			bool isNewLine = false, gotNewLine = false;
			for (int i = 0; i < text.Length; i++) {
				var ch = text [i];
				if (clonedEngine.IsInsideVerbatimString || clonedEngine.IsInsideMultiLineComment) {
					clonedEngine.Push(ch);
					curLine.Append(ch);
					continue;
				}

				var delimiterLength = NewLine.GetDelimiterLength(ch, i + 1 < text.Length ? text[i + 1] : ' ');
				if (delimiterLength > 0) {
					isNewLine = true;
					if (gotNewLine || pasteAtLineStart) {
						if (curLine.Length > 0 || formattingOptions.EmptyLineFormatting == EmptyLineFormatting.Indent)
							indentedText.Append(clonedEngine.ThisLineIndent);
					}
					indentedText.Append(curLine);
					indentedText.Append(textEditorOptions.EolMarker);
					curLine.Length = 0;
					gotNewLine = true;
					i += delimiterLength - 1;
					// textEditorOptions.EolMarker[0] is the newLineChar used by the indentation engine.
					clonedEngine.Push(textEditorOptions.EolMarker[0]);
				} else {
					if (isNewLine) {
						if (ch == '\t' || ch == ' ') {
							clonedEngine.Push(ch);
							continue;
						}
						isNewLine = false;
					}
					curLine.Append(ch);
					clonedEngine.Push(ch);
				}
				if (clonedEngine.IsInsideVerbatimString || clonedEngine.IsInsideMultiLineComment && 
				    !(clonedEngine.LineBeganInsideVerbatimString || clonedEngine.LineBeganInsideMultiLineComment)) {
					if (gotNewLine) {
						if (curLine.Length > 0 || formattingOptions.EmptyLineFormatting == EmptyLineFormatting.Indent)
							indentedText.Append(clonedEngine.ThisLineIndent);
					}
					pasteAtLineStart = false;
					indentedText.Append(curLine);
					curLine.Length = 0;
					gotNewLine = false;
					continue;
				}
			}
			if (gotNewLine && (!pasteAtLineStart || curLine.Length > 0)) {
				indentedText.Append(clonedEngine.ThisLineIndent);
			}
			if (curLine.Length > 0) {
				indentedText.Append(curLine);
			}
			return indentedText.ToString();
		}

		/// <inheritdoc />
		byte[] ITextPasteHandler.GetCopyData(ISegment segment)
		{
			engine.Update(segment.Offset);
			
			if (engine.IsInsideStringLiteral) {
				return new[] { (byte)PasteStrategy.StringLiteral };
			} else if (engine.IsInsideVerbatimString) {
				return new[] { (byte)PasteStrategy.VerbatimString };
			}
			
			return null;
		}

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public IDocument Document {
			get { return engine.Document; }
		}

		/// <inheritdoc />
		public string ThisLineIndent {
			get { return engine.ThisLineIndent; }
		}

		/// <inheritdoc />
		public string NextLineIndent {
			get { return engine.NextLineIndent; }
		}

		/// <inheritdoc />
		public string CurrentIndent {
			get { return engine.CurrentIndent; }
		}

		/// <inheritdoc />
		public bool NeedsReindent {
			get { return engine.NeedsReindent; }
		}

		/// <inheritdoc />
		public int Offset {
			get { return engine.Offset; }
		}

		/// <inheritdoc />
		public TextLocation Location {
			get { return engine.Location; }
		}

		/// <inheritdoc />
		public bool EnableCustomIndentLevels {
			get { return engine.EnableCustomIndentLevels; }
			set { engine.EnableCustomIndentLevels = value; }
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
			return new TextPasteIndentEngine(engine, textEditorOptions, formattingOptions);
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
			public IPasteStrategy this [PasteStrategy strategy] {
				get {
					if (strategies.ContainsKey(strategy)) {
						return strategies [strategy];
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

			public static IPasteStrategy Instance {
				get {
					return instance ?? (instance = new PlainTextPasteStrategy());
				}
			}

			static PlainTextPasteStrategy instance;

			protected PlainTextPasteStrategy()
			{
			}

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
			public PasteStrategy Type {
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

			public static IPasteStrategy Instance {
				get {
					return instance ?? (instance = new StringLiteralPasteStrategy());
				}
			}

			static StringLiteralPasteStrategy instance;

			protected StringLiteralPasteStrategy()
			{
			}

			#endregion

			/// <inheritdoc />
			public string Encode(string text)
			{
				return CSharpOutputVisitor.ConvertString(text);
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				var result = new StringBuilder();
				bool isEscaped = false;

				for (int i = 0; i < text.Length; i++) {
					var ch = text[i];
					if (isEscaped) {
						switch (ch) {
							case 'a':
								result.Append('\a');
								break;
							case 'b':
								result.Append('\b');
								break;
							case 'n':
								result.Append('\n');
								break;
							case 't':
								result.Append('\t');
								break;
							case 'v':
								result.Append('\v');
								break;
							case 'r':
								result.Append('\r');
								break;
							case '\\':
								result.Append('\\');
								break;
							case 'f':
								result.Append('\f');
								break;
							case '0':
								result.Append(0);
								break;
							case '"':
								result.Append('"');
								break;
							case '\'':
								result.Append('\'');
								break;
							case 'x':
								char r;
								if (TryGetHex(text, -1, ref i, out r)) {
									result.Append(r);
									break;
								}
								goto default;
							case 'u':
								if (TryGetHex(text, 4, ref i, out r)) {
									result.Append(r);
									break;
								}
								goto default;
							case 'U':
								if (TryGetHex(text, 8, ref i, out r)) {
									result.Append(r);
									break;
								}
								goto default;
							default:
								result.Append('\\');
								result.Append(ch);
								break;
						}
						isEscaped = false;
						continue;
					}
					if (ch != '\\') {
						result.Append(ch);
					}
					else {
						isEscaped = true;
					}
				}

				return result.ToString();
			}

			static bool TryGetHex(string text, int count, ref int idx, out char r)
			{
				int i;
				int total = 0;
				int top = count != -1 ? count : 4;

				for (i = 0; i < top; i++) {
					int c = text[idx + 1 + i];

					if (c >= '0' && c <= '9')
						c = (int) c - (int) '0';
					else if (c >= 'A' && c <= 'F')
						c = (int) c - (int) 'A' + 10;
					else if (c >= 'a' && c <= 'f')
						c = (int) c - (int) 'a' + 10;
					else {
						r = '\0';
						return false;
					}
					total = (total * 16) + c;
				}

				if (top == 8) {
					if (total > 0x0010FFFF) {
						r = '\0';
						return false;
					}

					if (total >= 0x00010000)
						total = ((total - 0x00010000) / 0x0400 + 0xD800);
				}
				r = (char)total;
				idx += top;
				return true;
			}

			/// <inheritdoc />
			public PasteStrategy Type {
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

			public static IPasteStrategy Instance {
				get {
					return instance ?? (instance = new VerbatimStringPasteStrategy());
				}
			}

			static VerbatimStringPasteStrategy instance;

			protected VerbatimStringPasteStrategy()
			{
			}

			#endregion

			static readonly Dictionary<char, IEnumerable<char>> encodeReplace = new Dictionary<char, IEnumerable<char>> {
				{ '\"', "\"\"" },
			};

			/// <inheritdoc />
			public string Encode(string text)
			{
				return string.Concat(text.SelectMany(c => encodeReplace.ContainsKey(c) ? encodeReplace [c] : new[] { c }));
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				bool isEscaped = false;
				return string.Concat(text.Where(c => !(isEscaped = !isEscaped && c == '"')));
			}

			/// <inheritdoc />
			public PasteStrategy Type {
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
