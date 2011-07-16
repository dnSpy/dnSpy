// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Text;

namespace ICSharpCode.NRefactory.VB.PrettyPrinter
{
	/// <summary>
	/// Base class of output formatters.
	/// </summary>
	public abstract class AbstractOutputFormatter : IOutputFormatter
	{
		StringBuilder text = new StringBuilder();
		
		int           indentationLevel = 0;
		bool          indent         = true;
		bool          doNewLine      = true;
		AbstractPrettyPrintOptions prettyPrintOptions;
		
		public bool IsInMemberBody { get; set; }
		
		public int IndentationLevel {
			get {
				return indentationLevel;
			}
			set {
				indentationLevel = value;
			}
		}
		
		public string Text {
			get {
				return text.ToString();
			}
		}
		
		public int TextLength {
			get {
				return text.Length;
			}
		}
		
		
		public bool DoIndent {
			get {
				return indent;
			}
			set {
				indent = value;
			}
		}
		
		public bool DoNewLine {
			get {
				return doNewLine;
			}
			set {
				doNewLine = value;
			}
		}
		
		protected AbstractOutputFormatter(AbstractPrettyPrintOptions prettyPrintOptions)
		{
			this.prettyPrintOptions = prettyPrintOptions;
		}
		
		internal bool isIndented = false;
		public void Indent()
		{
			if (DoIndent) {
				int indent = 0;
				while (indent < prettyPrintOptions.IndentSize * indentationLevel) {
					char ch = prettyPrintOptions.IndentationChar;
					if (ch == '\t' && indent + prettyPrintOptions.TabSize > prettyPrintOptions.IndentSize * indentationLevel) {
						ch = ' ';
					}
					text.Append(ch);
					if (ch == '\t') {
						indent += prettyPrintOptions.TabSize;
					} else {
						++indent;
					}
				}
				isIndented = true;
			}
		}
		
		public void Reset ()
		{
			text.Length = 0;
			isIndented = false;
		}
		
		public void Space()
		{
			text.Append(' ');
			isIndented = false;
		}
		
		internal int lastLineStart = 0;
		internal int lineBeforeLastStart = 0;
		
		public bool LastCharacterIsNewLine {
			get {
				return text.Length == lastLineStart;
			}
		}
		
		public bool LastCharacterIsWhiteSpace {
			get {
				return text.Length == 0 || char.IsWhiteSpace(text[text.Length - 1]);
			}
		}
		
		public virtual void NewLine()
		{
			if (DoNewLine) {
				if (!LastCharacterIsNewLine) {
					lineBeforeLastStart = lastLineStart;
				}
				text.AppendLine();
				lastLineStart = text.Length;
				isIndented = false;
			}
		}
		
		public virtual void EndFile()
		{
		}
		
		protected void WriteLineInPreviousLine(string txt, bool forceWriteInPreviousBlock)
		{
			WriteInPreviousLine(txt + Environment.NewLine, forceWriteInPreviousBlock);
		}
		protected void WriteLineInPreviousLine(string txt, bool forceWriteInPreviousBlock, bool indent)
		{
			WriteInPreviousLine(txt + Environment.NewLine, forceWriteInPreviousBlock, indent);
		}
		
		protected void WriteInPreviousLine(string txt, bool forceWriteInPreviousBlock)
		{
			WriteInPreviousLine(txt, forceWriteInPreviousBlock, true);
		}
		protected void WriteInPreviousLine(string txt, bool forceWriteInPreviousBlock, bool indent)
		{
			if (txt.Length == 0) return;
			
			bool lastCharacterWasNewLine = LastCharacterIsNewLine;
			if (lastCharacterWasNewLine) {
				if (forceWriteInPreviousBlock == false) {
					if (indent && txt != Environment.NewLine) Indent();
					text.Append(txt);
					lineBeforeLastStart = lastLineStart;
					lastLineStart = text.Length;
					return;
				}
				lastLineStart = lineBeforeLastStart;
			}
			string lastLine = text.ToString(lastLineStart, text.Length - lastLineStart);
			text.Remove(lastLineStart, text.Length - lastLineStart);
			if (indent && txt != Environment.NewLine) {
				if (forceWriteInPreviousBlock) ++indentationLevel;
				Indent();
				if (forceWriteInPreviousBlock) --indentationLevel;
			}
			text.Append(txt);
			lineBeforeLastStart = lastLineStart;
			lastLineStart = text.Length;
			text.Append(lastLine);
			if (lastCharacterWasNewLine) {
				lineBeforeLastStart = lastLineStart;
				lastLineStart = text.Length;
			}
			isIndented = false;
		}
		
		/// <summary>
		/// Prints a text that cannot be inserted before using WriteInPreviousLine
		/// into the current line
		/// </summary>
		protected void PrintSpecialText(string specialText)
		{
			lineBeforeLastStart = text.Length;
			text.Append(specialText);
			lastLineStart = text.Length;
			isIndented = false;
		}
		
		public void PrintTokenList(ArrayList tokenList)
		{
			foreach (int token in tokenList) {
				PrintToken(token);
				Space();
			}
		}
		
//		public abstract void PrintComment(Comment comment, bool forceWriteInPreviousBlock);
		
//		public virtual void PrintPreprocessingDirective(PreprocessingDirective directive, bool forceWriteInPreviousBlock)
//		{
//			if (!directive.Expression.IsNull) {
////				CSharpOutputVisitor visitor = new CSharpOutputVisitor();
////				directive.Expression.AcceptVisitor(visitor, null);
////				WriteLineInPreviousLine(directive.Cmd + " " + visitor.Text, forceWriteInPreviousBlock);
//			} else if (string.IsNullOrEmpty(directive.Arg))
//				WriteLineInPreviousLine(directive.Cmd, forceWriteInPreviousBlock);
//			else
//				WriteLineInPreviousLine(directive.Cmd + " " + directive.Arg, forceWriteInPreviousBlock);
//		}
		
		public void PrintBlankLine(bool forceWriteInPreviousBlock)
		{
			WriteInPreviousLine(Environment.NewLine, forceWriteInPreviousBlock);
		}
		
		public abstract void PrintToken(int token);
		
		public void PrintText(string text)
		{
			this.text.Append(text);
			isIndented = false;
		}
		
		public abstract void PrintIdentifier(string identifier);
	}
}
