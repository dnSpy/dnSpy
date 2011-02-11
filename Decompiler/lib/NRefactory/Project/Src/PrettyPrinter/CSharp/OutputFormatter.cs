// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Collections;
using ICSharpCode.NRefactory.Parser.CSharp;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public sealed class CSharpOutputFormatter : AbstractOutputFormatter
	{
		PrettyPrintOptions prettyPrintOptions;
		
		bool          emitSemicolon  = true;
		
		public bool EmitSemicolon {
			get {
				return emitSemicolon;
			}
			set {
				emitSemicolon = value;
			}
		}
		
		public CSharpOutputFormatter(PrettyPrintOptions prettyPrintOptions) : base(prettyPrintOptions)
		{
			this.prettyPrintOptions = prettyPrintOptions;
		}
		
		public override void PrintToken(int token)
		{
			if (token == Tokens.Semicolon && !EmitSemicolon) {
				return;
			}
			PrintText(Tokens.GetTokenString(token));
		}
		
		Stack braceStack = new Stack();
		
		public void BeginBrace(BraceStyle style, bool indent)
		{
			switch (style) {
				case BraceStyle.EndOfLine:
					if (!LastCharacterIsWhiteSpace) {
						Space();
					}
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					if (indent)
						++IndentationLevel;
					break;
				case BraceStyle.EndOfLineWithoutSpace:
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					if (indent)
						++IndentationLevel;
					break;
				case BraceStyle.NextLine:
					NewLine();
					Indent();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					if (indent)
						++IndentationLevel;
					break;
				case BraceStyle.NextLineShifted:
					NewLine();
					if (indent)
						++IndentationLevel;
					Indent();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					break;
				case BraceStyle.NextLineShifted2:
					NewLine();
					if (indent)
						++IndentationLevel;
					Indent();
					PrintToken(Tokens.OpenCurlyBrace);
					NewLine();
					++IndentationLevel;
					break;
			}
			braceStack.Push(style);
		}
		
		public void EndBrace (bool indent)
		{
			EndBrace (indent, true);
		}
		
		public void EndBrace (bool indent, bool emitNewLine)
		{
			BraceStyle style = (BraceStyle)braceStack.Pop();
			switch (style) {
				case BraceStyle.EndOfLine:
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.NextLine:
					if (indent)
						--IndentationLevel;
					Indent();
					PrintToken(Tokens.CloseCurlyBrace);
					if (emitNewLine)
						NewLine();
					break;
				case BraceStyle.NextLineShifted:
					Indent();
					PrintToken(Tokens.CloseCurlyBrace);
					if (emitNewLine)
						NewLine();
					if (indent)
						--IndentationLevel;
					break;
				case BraceStyle.NextLineShifted2:
					if (indent)
						--IndentationLevel;
					Indent();
					PrintToken(Tokens.CloseCurlyBrace);
					if (emitNewLine)
						NewLine();
					--IndentationLevel;
					break;
			}
		}
		
		public override void PrintIdentifier(string identifier)
		{
			if (Keywords.GetToken(identifier) >= 0)
				PrintText("@");
			PrintText(identifier);
		}
		
		public override void PrintComment(Comment comment, bool forceWriteInPreviousBlock)
		{
			switch (comment.CommentType) {
				case CommentType.Block:
					bool wasIndented = isIndented;
					if (!wasIndented) {
						Indent ();
					}
					if (forceWriteInPreviousBlock) {
						WriteInPreviousLine("/*" + comment.CommentText + "*/", forceWriteInPreviousBlock);
					} else {
						PrintSpecialText("/*" + comment.CommentText + "*/");
					}
					if (wasIndented) {
						Indent ();
					}
					break;
				case CommentType.Documentation:
					WriteLineInPreviousLine("///" + comment.CommentText, forceWriteInPreviousBlock);
					break;
				default:
					// 3 because startposition is start of the text (after the tag)
					WriteLineInPreviousLine("//" + comment.CommentText, forceWriteInPreviousBlock, comment.StartPosition.Column != 3);
					break;
			}
		}
	}
}
