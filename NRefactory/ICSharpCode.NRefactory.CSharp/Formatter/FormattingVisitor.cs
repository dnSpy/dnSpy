// 
// FormattingVisitor.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp
{
	[Obsolete("This class was replaced by CSharpFormatter.")]
	public class AstFormattingVisitor {}

	partial class FormattingVisitor  
	{
		readonly CSharpFormatter formatter;
		readonly FormattingChanges changes;
		readonly IDocument document;
		readonly CancellationToken token;

		Indent curIndent;

		public bool HadErrors {
			get;
			set;
		}
		

		CSharpFormattingOptions policy {
			get {
				return formatter.Policy;
			}
		}

		TextEditorOptions options {
			get {
				return formatter.TextEditorOptions;
			}
		}

		FormattingChanges.TextReplaceAction AddChange(int offset, int removedChars, string insertedText)
		{
			return changes.AddChange(offset, removedChars, insertedText);
		}

		public FormattingVisitor(CSharpFormatter formatter, IDocument document, FormattingChanges changes, CancellationToken token)
		{
			if (formatter == null)
				throw new ArgumentNullException("formatter");
			if (document == null)
				throw new ArgumentNullException("document");
			if (changes == null)
				throw new ArgumentNullException("changes");
		
			this.formatter = formatter;
			this.changes = changes;
			this.document = document;
			this.token = token;

			curIndent = new Indent(formatter.TextEditorOptions);
		}
		
		void VisitChildrenToFormat (AstNode parent, Action<AstNode> callback)
		{
			AstNode next;
			for (var child = parent.FirstChild; child != null; child = next) {
				token.ThrowIfCancellationRequested();
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.GetNextSibling(NoWhitespacePredicate);
				
				if (formatter.FormattingRegions.Count > 0) {
					if (formatter.FormattingRegions.Any(r => r.IsInside(child.StartLocation) || r.IsInside(child.EndLocation))) {
						callback(child);
					} else {
						var childRegion = child.Region;
						if (formatter.FormattingRegions.Any(r => childRegion.IsInside(r.Begin) || childRegion.IsInside(r.End)))
						    callback(child);
					}
					if (child.StartLocation > formatter.lastFormattingLocation)
						break;
				} else {
					callback(child);
				}
			}
		}
		
		protected override void VisitChildren (AstNode node)
		{
			VisitChildrenToFormat (node, n => n.AcceptVisitor (this));
		}

		#region NewLines

		void AdjustNewLineBlock(AstNode startNode, int targetMinimumNewLineCount)
		{
			var indentString = policy.EmptyLineFormatting == EmptyLineFormatting.Indent ? curIndent.IndentString : "";

			TextLocation newLineInsertPosition = startNode.EndLocation;
			var node = startNode.NextSibling;
			int currentNewLineCount = 0;
			// Check the existing newlines
			for (; currentNewLineCount < targetMinimumNewLineCount; node = node.NextSibling) {
				if (node is WhitespaceNode)
					continue;
				if (!(node is NewLineNode))
					break;
				newLineInsertPosition = node.EndLocation;
				currentNewLineCount++;
				if (policy.EmptyLineFormatting == EmptyLineFormatting.DoNotChange) {
					if (node.NextSibling == null)
						// end of file/block etc, nothing more to do but break before assigning null to node
						break;
					continue;
				}
				var isBlankLine = IsSpacing(document.GetLineByNumber(node.StartLocation.Line));
				if (!isBlankLine) {
					// remove EOL whitespace if appropriate
					if (policy.RemoveEndOfLineWhiteSpace) {
						var offset = document.GetOffset(node.StartLocation);
						var start = SearchWhitespaceStart(offset);
						if (start != offset)
							AddChange(start, offset - start, null);
					}
				} else {
					var actualIndent = GetIndentation(node.StartLocation.Line);
					if (actualIndent != indentString) {
						var start = document.GetOffset(new TextLocation(node.StartLocation.Line, 0));
						AddChange(start, actualIndent.Length, indentString);
					}
				}
				if (node.NextSibling == null)
					// end of file/block etc, nothing more to do but break before assigning null to node
					break;
			}
			if (currentNewLineCount < targetMinimumNewLineCount) {
				// We need to add more newlines
				var builder = new StringBuilder();
				for (; currentNewLineCount < targetMinimumNewLineCount; currentNewLineCount++) {
					if (currentNewLineCount > 0)
						// Don't indent the first line in the block since that is not an empty line.
						builder.Append(indentString);
					builder.Append(options.EolMarker);
				}
				var offset = document.GetOffset(newLineInsertPosition);
				if (offset >= 0)
					AddChange(offset, 0, builder.ToString());
			} else if (currentNewLineCount == targetMinimumNewLineCount && node is NewLineNode){
//				// Check to see if there are any newlines to remove
//				var endNode = node.GetNextSibling(n => !(n is NewLineNode || n is WhitespaceNode));
//				if (endNode != null) {
//					var startOffset = document.GetOffset(newLineInsertPosition);
//					var endOffset = document.GetOffset(new TextLocation(endNode.StartLocation.Line, 0));
//					EnsureText(startOffset, endOffset, null);
//				}
			}
		}

		public void EnsureMinimumNewLinesAfter(AstNode node, int blankLines)
		{
			if (node is PreProcessorDirective) {
				var directive = (PreProcessorDirective)node;
				if (directive.Type == PreProcessorDirectiveType.Pragma)
					return;
			}
			if (blankLines < 0)
				return;
			if (formatter.FormattingMode != FormattingMode.Intrusive)
				blankLines = Math.Min(1, blankLines);
			AdjustNewLineBlock(node, blankLines);
		}
		
		public void EnsureMinimumBlankLinesBefore(AstNode node, int blankLines)
		{
			if (formatter.FormattingMode != FormattingMode.Intrusive)
				return;
			var loc = node.StartLocation;
			int line = loc.Line;
			do {
				line--;
			} while (line > 0 && IsSpacing(document.GetLineByNumber(line)));
			if (line > 0 && !IsSpacing(document.GetLineByNumber(line)))
			    line++;

			if (loc.Line - line >= blankLines)
				return;

			var sb = new StringBuilder ();
			for (int i = 0; i < blankLines; i++)
				sb.Append(options.EolMarker);
			int end = document.GetOffset(loc.Line, 1);
			if (loc.Line == line) {
				AddChange(end, 0, sb.ToString());
				return;
			}
			if (line + 1 > document.LineCount)
				return;
			int start = document.GetOffset(line + 1, 1);
			if (end - start <= 0 && sb.Length == 0)
				return;
			AddChange(start, end - start, sb.ToString());
		}

		#endregion

		bool IsSimpleAccessor(Accessor accessor)
		{
			if (accessor.IsNull || accessor.Body.IsNull || accessor.Body.FirstChild == null) {
				return true;
			}
			var firstStatement = accessor.Body.Statements.FirstOrDefault();
			if (firstStatement == null)
				return true;

			if (!(firstStatement is ReturnStatement || firstStatement is ExpressionStatement|| firstStatement is EmptyStatement || firstStatement is ThrowStatement))
				return false;

			if (firstStatement.GetNextSibling(s => s.Role == BlockStatement.StatementRole) != null)
				return false;

			return !(accessor.Body.Statements.FirstOrDefault() is BlockStatement);
		}
		
		static bool IsSpacing(char ch)
		{
			return ch == ' ' || ch == '\t';
		}
		
		bool IsSpacing(ISegment segment)
		{
			int endOffset = segment.EndOffset;
			for (int i = segment.Offset; i < endOffset; i++) {
				if (!IsSpacing(document.GetCharAt(i))) {
					return false;
				}
			}
			return true;
		}
		
		int SearchLastNonWsChar(int startOffset, int endOffset)
		{
			startOffset = Math.Max(0, startOffset);
			endOffset = Math.Max(startOffset, endOffset);
			if (startOffset >= endOffset) {
				return startOffset;
			}
			int result = -1;
			bool inComment = false;
			
			for (int i = startOffset; i < endOffset && i < document.TextLength; i++) {
				char ch = document.GetCharAt(i);
				if (IsSpacing(ch)) {
					continue;
				}
				if (ch == '/' && i + 1 < document.TextLength && document.GetCharAt(i + 1) == '/') {
					return result;
				}
				if (ch == '/' && i + 1 < document.TextLength && document.GetCharAt(i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < document.TextLength && document.GetCharAt(i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment) {
					result = i;
				}
			}
			return result;
		}
		
		void ForceSpace(int startOffset, int endOffset, bool forceSpace)
		{
			int lastNonWs = SearchLastNonWsChar(startOffset, endOffset);
			if (lastNonWs < 0)
				return;

			var spaceCount = Math.Max(0, endOffset - lastNonWs - 1);
			if (forceSpace) {
				if (spaceCount != 1) {
					// Here we could technically remove spaceCount - 1 chars instead
					// and skip replacing that with new space, but we want to trigger the
					// overlap detection if this space is changed again for some reason
					AddChange(lastNonWs + 1, spaceCount, " ");
				}
			} else if (spaceCount > 0 && !forceSpace) {
				AddChange(lastNonWs + 1, spaceCount, "");
			}
		}
		
		void ForceSpacesAfter(AstNode n, bool forceSpaces)
		{
			if (n == null) {
				return;
			}
			TextLocation location = n.EndLocation;
			int offset = document.GetOffset(location);
			if (location.Column > document.GetLineByNumber(location.Line).Length) {
				return;
			}
			int i = offset;
			while (i < document.TextLength && IsSpacing (document.GetCharAt (i))) {
				i++;
			}
			ForceSpace(offset - 1, i, forceSpaces);
		}

		int ForceSpacesBefore(AstNode n, bool forceSpaces)
		{
			if (n == null || n.IsNull) {
				return 0;
			}
			TextLocation location = n.StartLocation;
			// respect manual line breaks.
			if (location.Column <= 1 || GetIndentation(location.Line).Length == location.Column - 1) {
				return 0;
			}
			
			int offset = document.GetOffset(location);
			int i = offset - 1;
			while (i >= 0 && IsSpacing (document.GetCharAt (i))) {
				i--;
			}
			ForceSpace(i, offset, forceSpaces);
			return i;
		}
		
		int ForceSpacesBeforeRemoveNewLines(AstNode n, bool forceSpace = true)
		{
			if (n == null || n.IsNull) {
				return 0;
			}
			int offset = document.GetOffset(n.StartLocation);
			int i = offset - 1;
			while (i >= 0) {
				char ch = document.GetCharAt(i);
				if (!IsSpacing(ch) && ch != '\r' && ch != '\n')
					break;
				i--;
			}
			var length = Math.Max(0, (offset - 1) - i);
			AddChange(i + 1, length, forceSpace ? " " : "");
			return i;
		}

		internal static bool NoWhitespacePredicate(AstNode arg)
		{
			return !(arg is NewLineNode || arg is WhitespaceNode);
		}

		static bool IsMember(AstNode nextSibling)
		{
			return nextSibling != null && nextSibling.NodeType == NodeType.Member;
		}

		static bool ShouldBreakLine(NewLinePlacement placement, CSharpTokenNode token)
		{
			if (placement == NewLinePlacement.NewLine)
				return true;
			if (placement == NewLinePlacement.SameLine)
				return false;
			if (token.IsNull)
				return false;
			var prevMeaningfulNode = token.GetPrevNode (n =>n.Role !=Roles.NewLine && n.Role != Roles.Whitespace && n.Role !=Roles.Comment);
			return prevMeaningfulNode.EndLocation.Line != token.StartLocation.Line;
		}

		void ForceSpaceBefore(AstNode node, bool forceSpace)
		{
			var offset = document.GetOffset(node.StartLocation);
			int end = offset;
			// ForceSpace inserts a space one char after start in the case of a missing space
			// Therefore, make sure that start < offset by starting at offset - 1
			int start = SearchWhitespaceStart(offset - 1);
			ForceSpace(start, end, forceSpace);
		}

		public void FixSemicolon(CSharpTokenNode semicolon)
		{
			if (semicolon.IsNull)
				return;
			int endOffset = document.GetOffset(semicolon.StartLocation);
			int offset = endOffset;
			while (offset - 1 > 0 && char.IsWhiteSpace (document.GetCharAt (offset - 1))) {
				offset--;
			}
			if (policy.SpaceBeforeSemicolon) {
				AddChange(offset, endOffset - offset, " ");
			} else {
				if (offset < endOffset)
					AddChange(offset, endOffset - offset, null);
			}
		}
		
		void PlaceOnNewLine(NewLinePlacement newLine, AstNode keywordNode)
		{
			if (keywordNode == null || keywordNode.StartLocation.IsEmpty)
				return;
			
			var prev = keywordNode.GetPrevNode (NoWhitespacePredicate);
			if (prev is Comment || prev is PreProcessorDirective)
				return;

			if (newLine == NewLinePlacement.DoNotCare)
				newLine = prev.EndLocation.Line == keywordNode.StartLocation.Line ? NewLinePlacement.SameLine : NewLinePlacement.NewLine;

			int offset = document.GetOffset(keywordNode.StartLocation);
			
			int whitespaceStart = SearchWhitespaceStart(offset);
			string indentString = newLine == NewLinePlacement.NewLine ? options.EolMarker + curIndent.IndentString : " ";
			AddChange(whitespaceStart, offset - whitespaceStart, indentString);
		}
		
		string nextStatementIndent;
		
		void FixStatementIndentation(TextLocation location)
		{
			if (location.Line < 1 || location.Column < 1) {
				Console.WriteLine("invalid location!");
				return;
			}
			int offset = document.GetOffset(location);
			if (offset <= 0) {
				Console.WriteLine("possible wrong offset");
				Console.WriteLine(Environment.StackTrace);
				return;
			}
			bool isEmpty = IsLineIsEmptyUpToEol(offset);
			int lineStart = SearchWhitespaceLineStart(offset);
			string indentString = nextStatementIndent ?? (isEmpty ? "" : options.EolMarker) + curIndent.IndentString;
			nextStatementIndent = null;
			EnsureText(lineStart, offset, indentString);
		}

		void FixIndentation (AstNode node)
		{
			FixIndentation(node.StartLocation, 0);
		}
		
		void FixIndentation(TextLocation location, int relOffset)
		{
			if (location.Line < 1 || location.Line > document.LineCount) {
				Console.WriteLine("Invalid location " + location);
				Console.WriteLine(Environment.StackTrace);
				return;
			}
			
			string lineIndent = GetIndentation(location.Line);
			string indentString = curIndent.IndentString;
			if (indentString != lineIndent && location.Column - 1 + relOffset == lineIndent.Length) {
				AddChange(document.GetOffset(location.Line, 1), lineIndent.Length, indentString);
			}
		}
		
		void FixIndentationForceNewLine(AstNode node)
		{
			var directive = node as PreProcessorDirective;
			if (node.GetPrevNode () is NewLineNode) {
				if (directive != null && !policy.IndentPreprocessorDirectives) {
					var startNode = node.GetPrevNode ();
					var startOffset = document.GetOffset(startNode.EndLocation);
					int endOffset = document.GetOffset(node.StartLocation);
					AddChange(startOffset, endOffset - startOffset, "");
					return;
				} else {
					FixIndentation(node);
				}
			} else {
				// if no new line preceeds an #endif directive it's excluded
				if (directive != null) {
					if (directive.Type == PreProcessorDirectiveType.Endif)
						return;
				}
				var startNode = node.GetPrevSibling(n => !(n is WhitespaceNode)) ?? node;
				var startOffset = document.GetOffset(startNode.EndLocation);
				int endOffset = document.GetOffset(node.StartLocation);
				if (startOffset >= endOffset)
					return;
				if (directive != null && !policy.IndentPreprocessorDirectives) {
					AddChange(startOffset, endOffset - startOffset, "");
					return;
				}

				AddChange(startOffset, endOffset - startOffset, curIndent.IndentString);
			}
		}

		string GetIndentation(int lineNumber)
		{
			var line = document.GetLineByNumber(lineNumber);
			var b = new StringBuilder ();
			int endOffset = line.EndOffset;
			for (int i = line.Offset; i < endOffset; i++) {
				char c = document.GetCharAt(i);
				if (!IsSpacing(c)) {
					break;
				}
				b.Append(c);
			}
			return b.ToString();
		}

		void EnsureText(int start, int end, string replacementText)
		{
			var length = end - start;
			if (length == 0 && string.IsNullOrEmpty(replacementText))
				return;
			if (replacementText == null || replacementText.Length != length) {
				AddChange(start, length, replacementText);
				return;
			}
			for (int i = 0; i < length; i++) {
				if (document.GetCharAt(start + i) != replacementText[i]) {
					AddChange(start, length, replacementText);
					break;
				}
			}
		}
		
		void FixOpenBrace(BraceStyle braceStyle, AstNode lbrace)
		{
			if (lbrace.IsNull)
				return;
			switch (braceStyle) {
				case BraceStyle.DoNotChange:
					return;

				case BraceStyle.BannerStyle:
				case BraceStyle.EndOfLine:
					var prev = lbrace.GetPrevNode (NoWhitespacePredicate);
					if (prev is PreProcessorDirective)
						return;
					int prevOffset = document.GetOffset(prev.EndLocation);

					if (prev is Comment || prev is PreProcessorDirective) {
						int next = document.GetOffset(lbrace.GetNextNode ().StartLocation);
						EnsureText(prevOffset, next, "");
						while (prev is Comment || prev is PreProcessorDirective)
							prev = prev.GetPrevNode();
						prevOffset = document.GetOffset(prev.EndLocation);
						AddChange(prevOffset, 0, " {");
					} else {
						int braceOffset2 = document.GetOffset(lbrace.StartLocation);
						EnsureText(prevOffset, braceOffset2, " ");
					}
					break;
				case BraceStyle.EndOfLineWithoutSpace:
					prev = lbrace.GetPrevNode (NoWhitespacePredicate);
					if (prev is PreProcessorDirective)
						return;
					prevOffset = document.GetOffset(prev.EndLocation);
					int braceOffset = document.GetOffset(lbrace.StartLocation);
					EnsureText(prevOffset, braceOffset, "");
					break;

				case BraceStyle.NextLine:
					prev = lbrace.GetPrevNode (NoWhitespacePredicate);
					if (prev is PreProcessorDirective)
						return;
					prevOffset = document.GetOffset(prev.EndLocation);
					braceOffset = document.GetOffset(lbrace.StartLocation);
					EnsureText(prevOffset, braceOffset, options.EolMarker + curIndent.IndentString);
					break;
				case BraceStyle.NextLineShifted:
					prev = lbrace.GetPrevNode (NoWhitespacePredicate);
					if (prev is PreProcessorDirective)
						return;
					prevOffset = document.GetOffset(prev.EndLocation);
					braceOffset = document.GetOffset(lbrace.StartLocation);
					curIndent.Push(IndentType.Block);
					EnsureText(prevOffset, braceOffset, options.EolMarker + curIndent.IndentString);
					curIndent.Pop();
					break;
				case BraceStyle.NextLineShifted2:
					prev = lbrace.GetPrevNode (NoWhitespacePredicate);
					if (prev is PreProcessorDirective)
						return;
					prevOffset = document.GetOffset(prev.EndLocation);
					braceOffset = document.GetOffset(lbrace.StartLocation);
					curIndent.Push(IndentType.Block);
					EnsureText(prevOffset, braceOffset, options.EolMarker + curIndent.IndentString);
					curIndent.Pop();
					break;
			}
		}

		void CorrectClosingBrace (AstNode rbrace)
		{
			if (rbrace.IsNull)
				return;
			int braceOffset = document.GetOffset(rbrace.StartLocation);
			var prevNode = rbrace.GetPrevNode();
			int prevNodeOffset = prevNode != null ? document.GetOffset(prevNode.EndLocation) : 0;
			if (prevNode is NewLineNode) {
				EnsureText(prevNodeOffset, braceOffset, curIndent.IndentString);
			} else {
				EnsureText(prevNodeOffset, braceOffset, options.EolMarker + curIndent.IndentString);
			}
		}

		void FixClosingBrace(BraceStyle braceStyle, AstNode rbrace)
		{
			if (rbrace.IsNull)
				return;
			switch (braceStyle) {
				case BraceStyle.DoNotChange:
					return;

				case BraceStyle.NextLineShifted:
				case BraceStyle.BannerStyle:
					curIndent.Push(IndentType.Block);
					CorrectClosingBrace (rbrace);
					curIndent.Pop ();
					break;
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.EndOfLine:
				case BraceStyle.NextLine:
					CorrectClosingBrace (rbrace);
					break;

				case BraceStyle.NextLineShifted2:
					curIndent.Push(IndentType.Block);
					CorrectClosingBrace (rbrace);
					curIndent.Pop ();
					break;
			}

		}

	}
}

