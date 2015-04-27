//
// CSharpIndentEngine.cs
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
	///     Indentation engine based on a state machine.
	///     Supports only pushing new chars to the end.
	/// </summary>
	/// <remarks>
	///     Represents the context for transitions between <see cref="IndentState"/>.
	///     Delegates the responsibility for pushing a new char to the current 
	///     state and changes between states depending on the pushed chars.
	/// </remarks>
	public class CSharpIndentEngine : IStateMachineIndentEngine
	{
		#region Properties

		/// <summary>
		///     Formatting options.
		/// </summary>
		internal readonly CSharpFormattingOptions formattingOptions;

		/// <summary>
		///     Text editor options.
		/// </summary>
		internal readonly TextEditorOptions textEditorOptions;

		/// <summary>
		///     A readonly reference to the document that's parsed
		///     by the engine.
		/// </summary>
		internal readonly IDocument document;

		/// <summary>
		///     Represents the new line character.
		/// </summary>
		internal readonly char newLineChar;

		/// <summary>
		///     The current indentation state.
		/// </summary>
		internal IndentState currentState;

		/// <summary>
		///     Stores conditional symbols of #define directives.
		/// </summary>
		internal HashSet<string> conditionalSymbols;

		/// <summary>
		///     Stores custom conditional symbols.
		/// </summary>
		internal HashSet<string> customConditionalSymbols;

		/// <summary>
		///     Stores the results of evaluations of the preprocessor if/elif directives 
		///     in the current block (between #if and #endif).
		/// </summary>
		internal CloneableStack<bool> ifDirectiveEvalResults = new CloneableStack<bool> ();

		/// <summary>
		///     Stores the indentation levels of the if directives in the current block.
		/// </summary>
		internal CloneableStack<Indent> ifDirectiveIndents = new CloneableStack<Indent>();

		/// <summary>
		///     Stores the last sequence of characters that can form a
		///     valid keyword or variable name.
		/// </summary>
		internal StringBuilder wordToken;

		/// <summary>
		///     Stores the previous sequence of chars that formed a
		///     valid keyword or variable name.
		/// </summary>
		internal string previousKeyword;

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
			get
			{
				// OPTION: IndentBlankLines
				// remove the indentation of this line if isLineStart is true
//				if (!textEditorOptions.IndentBlankLines && isLineStart)
//				{
//					return string.Empty;
//				}

				return currentState.ThisLineIndent.IndentString;
			}
		}

		/// <inheritdoc />
		public string NextLineIndent
		{
			get
			{
				return currentState.NextLineIndent.IndentString;
			}
		}

		/// <inheritdoc />
		public string CurrentIndent
		{
			get
			{
				return currentIndent.ToString();
			}
		}

		/// <inheritdoc />
		/// <remarks>
		///     This is set depending on the current <see cref="Location"/> and
		///     can change its value until the <see cref="newLineChar"/> char is
		///     pushed. If this is true, that doesn't necessarily mean that the
		///     current line has an incorrect indent (this can be determined
		///     only at the end of the current line).
		/// </remarks>
		public bool NeedsReindent
		{
			get
			{
				// return true if it's the first column of the line and it has an indent
				if (Location.Column == 1)
				{
					return ThisLineIndent.Length > 0;
				}

				// ignore incorrect indentations when there's only ws on this line
				if (isLineStart)
				{
					return false;
				}

				return ThisLineIndent != CurrentIndent.ToString();
			}
		}

		/// <inheritdoc />
		public int Offset
		{
			get
			{
				return offset;
			}
		}

		/// <inheritdoc />
		public TextLocation Location
		{
			get
			{
				return new TextLocation(line, column);
			}
		}

		/// <inheritdoc />
		public bool EnableCustomIndentLevels
		{
			get;
			set;
		}

		#endregion

		#region Fields

		/// <summary>
		///    Represents the number of pushed chars.
		/// </summary>
		internal int offset = 0;

		/// <summary>
		///    The current line number.
		/// </summary>
		internal int line = 1;

		/// <summary>
		///    The current column number.
		/// </summary>
		/// <remarks>
		///    One char can take up multiple columns (e.g. \t).
		/// </remarks>
		internal int column = 1;

		/// <summary>
		///    True if <see cref="char.IsWhiteSpace(char)"/> is true for all
		///    chars at the current line.
		/// </summary>
		internal bool isLineStart = true;

		/// <summary>
		///    True if <see cref="isLineStart"/> was true before the current
		///    <see cref="wordToken"/>.
		/// </summary>
		internal bool isLineStartBeforeWordToken = true;

		/// <summary>
		///    Current char that's being pushed.
		/// </summary>
		internal char currentChar = '\0';

		/// <summary>
		///    Last non-whitespace char that has been pushed.
		/// </summary>
		internal char previousChar = '\0';

		/// <summary>
		///    Previous new line char
		/// </summary>
		internal char previousNewline = '\0';

		/// <summary>
		///    Current indent level on this line.
		/// </summary>
		internal StringBuilder currentIndent = new StringBuilder();

		/// <summary>
		///     True if this line began in <see cref="VerbatimStringState"/>.
		/// </summary>
		internal bool lineBeganInsideVerbatimString = false;

		/// <summary>
		///     True if this line began in <see cref="MultiLineCommentState"/>.
		/// </summary>
		internal bool lineBeganInsideMultiLineComment = false;

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new CSharpIndentEngine instance.
		/// </summary>
		/// <param name="document">
		///     An instance of <see cref="IDocument"/> which is being parsed.
		/// </param>
		/// <param name="formattingOptions">
		///     C# formatting options.
		/// </param>
		/// <param name="textEditorOptions">
		///    Text editor options for indentation.
		/// </param>
		public CSharpIndentEngine(IDocument document, TextEditorOptions textEditorOptions, CSharpFormattingOptions formattingOptions)
		{
			this.formattingOptions = formattingOptions;
			this.textEditorOptions = textEditorOptions;
			this.document = document;

			this.currentState = new GlobalBodyState(this);

			this.conditionalSymbols = new HashSet<string>();
			this.customConditionalSymbols = new HashSet<string>();
			this.wordToken = new StringBuilder();
			this.previousKeyword = string.Empty;
			this.newLineChar = textEditorOptions.EolMarker[0];
		}

		/// <summary>
		///     Creates a new CSharpIndentEngine instance from the given prototype.
		/// </summary>
		/// <param name="prototype">
		///     An CSharpIndentEngine instance.
		/// </param>
		public CSharpIndentEngine(CSharpIndentEngine prototype)
		{
			this.formattingOptions = prototype.formattingOptions;
			this.textEditorOptions = prototype.textEditorOptions;
			this.document = prototype.document;

			this.newLineChar = prototype.newLineChar;
			this.currentState = prototype.currentState.Clone(this);
			this.conditionalSymbols = new HashSet<string>(prototype.conditionalSymbols);
			this.customConditionalSymbols = new HashSet<string>(prototype.customConditionalSymbols);

			this.wordToken = new StringBuilder(prototype.wordToken.ToString());
			this.previousKeyword = string.Copy(prototype.previousKeyword);

			this.offset = prototype.offset;
			this.line = prototype.line;
			this.column = prototype.column;
			this.isLineStart = prototype.isLineStart;
			this.isLineStartBeforeWordToken = prototype.isLineStartBeforeWordToken;
			this.currentChar = prototype.currentChar;
			this.previousChar = prototype.previousChar;
			this.previousNewline = prototype.previousNewline;
			this.currentIndent = new StringBuilder(prototype.CurrentIndent.ToString());
			this.lineBeganInsideMultiLineComment = prototype.lineBeganInsideMultiLineComment;
			this.lineBeganInsideVerbatimString = prototype.lineBeganInsideVerbatimString;
			this.ifDirectiveEvalResults = prototype.ifDirectiveEvalResults.Clone();
			this.ifDirectiveIndents = prototype.ifDirectiveIndents.Clone();

			this.EnableCustomIndentLevels = prototype.EnableCustomIndentLevels;
		}

		#endregion

		#region IClonable

		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <inheritdoc />
		IDocumentIndentEngine IDocumentIndentEngine.Clone()
		{
			return Clone();
		}

		public IStateMachineIndentEngine Clone()
		{
			return new CSharpIndentEngine(this);
		}

		#endregion

		#region Methods

		/// <inheritdoc />
		public void Push(char ch)
		{
			// append this char to the wordbuf if it can form a valid keyword, otherwise check
			// if the last sequence of chars form a valid keyword and reset the wordbuf.
			if ((wordToken.Length == 0 ? char.IsLetter(ch) : char.IsLetterOrDigit(ch)) || ch == '_')
			{
				wordToken.Append(ch);
			}
			else if (wordToken.Length > 0)
			{
				currentState.CheckKeyword(wordToken.ToString());
				previousKeyword = wordToken.ToString();
				wordToken.Length = 0;
				isLineStartBeforeWordToken = false;
			}

			var isNewLine = NewLine.IsNewLine(ch);
			if (!isNewLine) {
				currentState.Push(currentChar = ch);
				offset++;
				previousNewline = '\0';
				// ignore whitespace and newline chars
				var isWhitespace = currentChar == ' ' || currentChar == '\t';
				if (!isWhitespace)
				{
					previousChar = currentChar;
					isLineStart = false;
				}

				if (isLineStart)
				{
					currentIndent.Append(ch);
				}

				if (ch == '\t')
				{
					var nextTabStop = (column - 1 + textEditorOptions.IndentSize) / textEditorOptions.IndentSize;
					column = 1 + nextTabStop * textEditorOptions.IndentSize;
				}
				else
				{
					column++;
				}
			} else {
				if (ch == NewLine.LF && previousNewline == NewLine.CR) {
					offset++;
					return;
				}
				currentState.Push(currentChar = newLineChar);
				offset++;

				previousNewline = ch;
				// there can be more than one chars that determine the EOL,
				// the engine uses only one of them defined with newLineChar
				if (currentChar != newLineChar)
				{
					return;
				}
				currentIndent.Length = 0;
				isLineStart = true;
				isLineStartBeforeWordToken = true;
				column = 1;
				line++;

				lineBeganInsideMultiLineComment = IsInsideMultiLineComment;
				lineBeganInsideVerbatimString = IsInsideVerbatimString;
			}
		}

		/// <inheritdoc />
		public void Reset()
		{
			currentState = new GlobalBodyState(this);
			conditionalSymbols.Clear();
			ifDirectiveEvalResults.Clear();
			ifDirectiveIndents.Clear();

			offset = 0;
			line = 1;
			column = 1;
			isLineStart = true;
			currentChar = '\0';
			previousChar = '\0';
			currentIndent.Length = 0;
			lineBeganInsideMultiLineComment = false;
			lineBeganInsideVerbatimString = false;
		}

		/// <inheritdoc />
		public void Update(int offset)
		{
			if (Offset > offset)
			{
				Reset();
			}

			while (Offset < offset)
			{
				Push(Document.GetCharAt(Offset));
			}
		}

		/// <summary>
		/// Defines the conditional symbol.
		/// </summary>
		/// <param name="defineSymbol">The symbol to define.</param>
		public void DefineSymbol(string defineSymbol)
		{
			if (!customConditionalSymbols.Contains(defineSymbol))
				customConditionalSymbols.Add(defineSymbol);
		}

		/// <summary>
		/// Removes the symbol.
		/// </summary>
		/// <param name="undefineSymbol">The symbol to undefine.</param>
		public void RemoveSymbol(string undefineSymbol)
		{
			if (customConditionalSymbols.Contains(undefineSymbol))
				customConditionalSymbols.Remove(undefineSymbol);
		}
		#endregion

		#region IStateMachineIndentEngine

		public bool IsInsidePreprocessorDirective
		{
			get { return currentState is PreProcessorState; }
		}

		public bool IsInsidePreprocessorComment
		{
			get { return currentState is PreProcessorCommentState; }
		}

		public bool IsInsideStringLiteral
		{
			get { return currentState is StringLiteralState; }
		}

		public bool IsInsideVerbatimString
		{
			get { return currentState is VerbatimStringState; }
		}

		public bool IsInsideCharacter
		{
			get { return currentState is CharacterState; }
		}

		public bool IsInsideString
		{
			get { return IsInsideStringLiteral || IsInsideVerbatimString || IsInsideCharacter; }
		}

		public bool IsInsideLineComment
		{
			get { return currentState is LineCommentState; }
		}

		public bool IsInsideMultiLineComment
		{
			get { return currentState is MultiLineCommentState; }
		}

		public bool IsInsideDocLineComment
		{
			get { return currentState is DocCommentState; }
		}

		public bool IsInsideComment
		{
			get { return IsInsideLineComment || IsInsideMultiLineComment || IsInsideDocLineComment; }
		}

		public bool IsInsideOrdinaryComment
		{
			get { return IsInsideLineComment || IsInsideMultiLineComment; }
		}

		public bool IsInsideOrdinaryCommentOrString
		{
			get { return IsInsideOrdinaryComment || IsInsideString; }
		}

		public bool LineBeganInsideVerbatimString
		{
			get { return lineBeganInsideVerbatimString; }
		}

		public bool LineBeganInsideMultiLineComment
		{
			get { return lineBeganInsideMultiLineComment; }
		}

		#endregion
	}
}
