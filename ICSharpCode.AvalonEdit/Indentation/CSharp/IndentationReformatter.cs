// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ICSharpCode.AvalonEdit.Indentation.CSharp
{
	sealed class IndentationSettings
	{
		public string IndentString = "\t";
		/// <summary>Leave empty lines empty.</summary>
		public bool LeaveEmptyLines = true;
	}
	
	sealed class IndentationReformatter
	{
		/// <summary>
		/// An indentation block. Tracks the state of the indentation.
		/// </summary>
		struct Block
		{
			/// <summary>
			/// The indentation outside of the block.
			/// </summary>
			public string OuterIndent;
			
			/// <summary>
			/// The indentation inside the block.
			/// </summary>
			public string InnerIndent;
			
			/// <summary>
			/// The last word that was seen inside this block.
			/// Because parenthesis open a sub-block and thus don't change their parent's LastWord,
			/// this property can be used to identify the type of block statement (if, while, switch)
			/// at the position of the '{'.
			/// </summary>
			public string LastWord;
			
			/// <summary>
			/// The type of bracket that opened this block (, [ or {
			/// </summary>
			public char Bracket;
			
			/// <summary>
			/// Gets whether there's currently a line continuation going on inside this block.
			/// </summary>
			public bool Continuation;
			
			/// <summary>
			/// Gets whether there's currently a 'one-line-block' going on. 'one-line-blocks' occur
			/// with if statements that don't use '{}'. They are not represented by a Block instance on
			/// the stack, but are instead handled similar to line continuations.
			/// This property is an integer because there might be multiple nested one-line-blocks.
			/// As soon as there is a finished statement, OneLineBlock is reset to 0.
			/// </summary>
			public int OneLineBlock;
			
			/// <summary>
			/// The previous value of one-line-block before it was reset.
			/// Used to restore the indentation of 'else' to the correct level.
			/// </summary>
			public int PreviousOneLineBlock;
			
			public void ResetOneLineBlock()
			{
				PreviousOneLineBlock = OneLineBlock;
				OneLineBlock = 0;
			}
			
			/// <summary>
			/// Gets the line number where this block started.
			/// </summary>
			public int StartLine;
			
			public void Indent(IndentationSettings set)
			{
				Indent(set.IndentString);
			}
			
			public void Indent(string indentationString)
			{
				OuterIndent = InnerIndent;
				InnerIndent += indentationString;
				Continuation = false;
				ResetOneLineBlock();
				LastWord = "";
			}
			
			public override string ToString()
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					"[Block StartLine={0}, LastWord='{1}', Continuation={2}, OneLineBlock={3}, PreviousOneLineBlock={4}]",
					this.StartLine, this.LastWord, this.Continuation, this.OneLineBlock, this.PreviousOneLineBlock);
			}
		}
		
		StringBuilder wordBuilder;
		Stack<Block> blocks; // blocks contains all blocks outside of the current
		Block block;  // block is the current block
		
		bool inString;
		bool inChar;
		bool verbatim;
		bool escape;
		
		bool lineComment;
		bool blockComment;
		
		char lastRealChar; // last non-comment char
		
		public void Reformat(IDocumentAccessor doc, IndentationSettings set)
		{
			Init();
			
			while (doc.MoveNext()) {
				Step(doc, set);
			}
		}
		
		public void Init()
		{
			wordBuilder = new StringBuilder();
			blocks = new Stack<Block>();
			block = new Block();
			block.InnerIndent = "";
			block.OuterIndent = "";
			block.Bracket = '{';
			block.Continuation = false;
			block.LastWord = "";
			block.OneLineBlock = 0;
			block.PreviousOneLineBlock = 0;
			block.StartLine = 0;
			
			inString = false;
			inChar   = false;
			verbatim = false;
			escape   = false;
			
			lineComment  = false;
			blockComment = false;
			
			lastRealChar = ' '; // last non-comment char
		}
		
		public void Step(IDocumentAccessor doc, IndentationSettings set)
		{
			string line = doc.Text;
			if (set.LeaveEmptyLines && line.Length == 0) return; // leave empty lines empty
			line = line.TrimStart();
			
			StringBuilder indent = new StringBuilder();
			if (line.Length == 0) {
				// Special treatment for empty lines:
				if (blockComment || (inString && verbatim))
					return;
				indent.Append(block.InnerIndent);
				indent.Append(Repeat(set.IndentString, block.OneLineBlock));
				if (block.Continuation)
					indent.Append(set.IndentString);
				if (doc.Text != indent.ToString())
					doc.Text = indent.ToString();
				return;
			}
			
			if (TrimEnd(doc))
				line = doc.Text.TrimStart();
			
			Block oldBlock = block;
			bool startInComment = blockComment;
			bool startInString = (inString && verbatim);
			
			#region Parse char by char
			lineComment = false;
			inChar = false;
			escape = false;
			if (!verbatim) inString = false;
			
			lastRealChar = '\n';
			
			char lastchar = ' ';
			char c = ' ';
			char nextchar = line[0];
			for (int i = 0; i < line.Length; i++) {
				if (lineComment) break; // cancel parsing current line
				
				lastchar = c;
				c = nextchar;
				if (i + 1 < line.Length)
					nextchar = line[i + 1];
				else
					nextchar = '\n';
				
				if (escape) {
					escape = false;
					continue;
				}
				
				#region Check for comment/string chars
				switch (c) {
					case '/':
						if (blockComment && lastchar == '*')
							blockComment = false;
						if (!inString && !inChar) {
							if (!blockComment && nextchar == '/')
								lineComment = true;
							if (!lineComment && nextchar == '*')
								blockComment = true;
						}
						break;
					case '#':
						if (!(inChar || blockComment || inString))
							lineComment = true;
						break;
					case '"':
						if (!(inChar || lineComment || blockComment)) {
							inString = !inString;
							if (!inString && verbatim) {
								if (nextchar == '"') {
									escape = true; // skip escaped quote
									inString = true;
								} else {
									verbatim = false;
								}
							} else if (inString && lastchar == '@') {
								verbatim = true;
							}
						}
						break;
					case '\'':
						if (!(inString || lineComment || blockComment)) {
							inChar = !inChar;
						}
						break;
					case '\\':
						if ((inString && !verbatim) || inChar)
							escape = true; // skip next character
						break;
				}
				#endregion
				
				if (lineComment || blockComment || inString || inChar) {
					if (wordBuilder.Length > 0)
						block.LastWord = wordBuilder.ToString();
					wordBuilder.Length = 0;
					continue;
				}
				
				if (!Char.IsWhiteSpace(c) && c != '[' && c != '/') {
					if (block.Bracket == '{')
						block.Continuation = true;
				}
				
				if (Char.IsLetterOrDigit(c)) {
					wordBuilder.Append(c);
				} else {
					if (wordBuilder.Length > 0)
						block.LastWord = wordBuilder.ToString();
					wordBuilder.Length = 0;
				}
				
				#region Push/Pop the blocks
				switch (c) {
					case '{':
						block.ResetOneLineBlock();
						blocks.Push(block);
						block.StartLine = doc.LineNumber;
						if (block.LastWord == "switch") {
							block.Indent(set.IndentString + set.IndentString);
							/* oldBlock refers to the previous line, not the previous block
							 * The block we want is not available anymore because it was never pushed.
							 * } else if (oldBlock.OneLineBlock) {
							// Inside a one-line-block is another statement
							// with a full block: indent the inner full block
							// by one additional level
							block.Indent(set, set.IndentString + set.IndentString);
							block.OuterIndent += set.IndentString;
							// Indent current line if it starts with the '{' character
							if (i == 0) {
								oldBlock.InnerIndent += set.IndentString;
							}*/
						} else {
							block.Indent(set);
						}
						block.Bracket = '{';
						break;
					case '}':
						while (block.Bracket != '{') {
							if (blocks.Count == 0) break;
							block = blocks.Pop();
						}
						if (blocks.Count == 0) break;
						block = blocks.Pop();
						block.Continuation = false;
						block.ResetOneLineBlock();
						break;
					case '(':
					case '[':
						blocks.Push(block);
						if (block.StartLine == doc.LineNumber)
							block.InnerIndent = block.OuterIndent;
						else
							block.StartLine = doc.LineNumber;
						block.Indent(Repeat(set.IndentString, oldBlock.OneLineBlock) +
						             (oldBlock.Continuation ? set.IndentString : "") +
						             (i == line.Length - 1 ? set.IndentString : new String(' ', i + 1)));
						block.Bracket = c;
						break;
					case ')':
						if (blocks.Count == 0) break;
						if (block.Bracket == '(') {
							block = blocks.Pop();
							if (IsSingleStatementKeyword(block.LastWord))
								block.Continuation = false;
						}
						break;
					case ']':
						if (blocks.Count == 0) break;
						if (block.Bracket == '[')
							block = blocks.Pop();
						break;
					case ';':
					case ',':
						block.Continuation = false;
						block.ResetOneLineBlock();
						break;
					case ':':
						if (block.LastWord == "case" 
						    || line.StartsWith("case ", StringComparison.Ordinal) 
						    || line.StartsWith(block.LastWord + ":", StringComparison.Ordinal)) 
						{
							block.Continuation = false;
							block.ResetOneLineBlock();
						}
						break;
				}
				
				if (!Char.IsWhiteSpace(c)) {
					// register this char as last char
					lastRealChar = c;
				}
				#endregion
			}
			#endregion
			
			if (wordBuilder.Length > 0)
				block.LastWord = wordBuilder.ToString();
			wordBuilder.Length = 0;
			
			if (startInString) return;
			if (startInComment && line[0] != '*') return;
			if (doc.Text.StartsWith("//\t", StringComparison.Ordinal) || doc.Text == "//")
				return;
			
			if (line[0] == '}') {
				indent.Append(oldBlock.OuterIndent);
				oldBlock.ResetOneLineBlock();
				oldBlock.Continuation = false;
			} else {
				indent.Append(oldBlock.InnerIndent);
			}
			
			if (indent.Length > 0 && oldBlock.Bracket == '(' && line[0] == ')') {
				indent.Remove(indent.Length - 1, 1);
			} else if (indent.Length > 0 && oldBlock.Bracket == '[' && line[0] == ']') {
				indent.Remove(indent.Length - 1, 1);
			}
			
			if (line[0] == ':') {
				oldBlock.Continuation = true;
			} else if (lastRealChar == ':' && indent.Length >= set.IndentString.Length) {
				if (block.LastWord == "case" || line.StartsWith("case ", StringComparison.Ordinal) || line.StartsWith(block.LastWord + ":", StringComparison.Ordinal))
					indent.Remove(indent.Length - set.IndentString.Length, set.IndentString.Length);
			} else if (lastRealChar == ')') {
				if (IsSingleStatementKeyword(block.LastWord)) {
					block.OneLineBlock++;
				}
			} else if (lastRealChar == 'e' && block.LastWord == "else") {
				block.OneLineBlock = Math.Max(1, block.PreviousOneLineBlock);
				block.Continuation = false;
				oldBlock.OneLineBlock = block.OneLineBlock - 1;
			}
			
			if (doc.IsReadOnly) {
				// We can't change the current line, but we should accept the existing
				// indentation if possible (=if the current statement is not a multiline
				// statement).
				if (!oldBlock.Continuation && oldBlock.OneLineBlock == 0 &&
				    oldBlock.StartLine == block.StartLine &&
				    block.StartLine < doc.LineNumber && lastRealChar != ':')
				{
					// use indent StringBuilder to get the indentation of the current line
					indent.Length = 0;
					line = doc.Text; // get untrimmed line
					for (int i = 0; i < line.Length; ++i) {
						if (!Char.IsWhiteSpace(line[i]))
							break;
						indent.Append(line[i]);
					}
					// /* */ multiline comments have an extra space - do not count it
					// for the block's indentation.
					if (startInComment && indent.Length > 0 && indent[indent.Length - 1] == ' ') {
						indent.Length -= 1;
					}
					block.InnerIndent = indent.ToString();
				}
				return;
			}
			
			if (line[0] != '{') {
				if (line[0] != ')' && oldBlock.Continuation && oldBlock.Bracket == '{')
					indent.Append(set.IndentString);
				indent.Append(Repeat(set.IndentString, oldBlock.OneLineBlock));
			}
			
			// this is only for blockcomment lines starting with *,
			// all others keep their old indentation
			if (startInComment)
				indent.Append(' ');
			
			if (indent.Length != (doc.Text.Length - line.Length) ||
			    !doc.Text.StartsWith(indent.ToString(), StringComparison.Ordinal) ||
			    Char.IsWhiteSpace(doc.Text[indent.Length]))
			{
				doc.Text = indent.ToString() + line;
			}
		}
		
		static string Repeat(string text, int count)
		{
			if (count == 0)
				return string.Empty;
			if (count == 1)
				return text;
			StringBuilder b = new StringBuilder(text.Length * count);
			for (int i = 0; i < count; i++)
				b.Append(text);
			return b.ToString();
		}
		
		static bool IsSingleStatementKeyword(string keyword)
		{
			switch (keyword) {
				case "if":
				case "for":
				case "while":
				case "do":
				case "foreach":
				case "using":
				case "lock":
					return true;
				default:
					return false;
			}
		}
		
		static bool TrimEnd(IDocumentAccessor doc)
		{
			string line = doc.Text;
			if (!Char.IsWhiteSpace(line[line.Length - 1])) return false;
			
			// one space after an empty comment is allowed
			if (line.EndsWith("// ", StringComparison.Ordinal) || line.EndsWith("* ", StringComparison.Ordinal))
				return false;
			
			doc.Text = line.TrimEnd();
			return true;
		}
	}
}
