// 
// AstFormattingVisitor.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp
{
	public enum FormattingMode {
		OnTheFly,
		Intrusive
	}

	public class AstFormattingVisitor : DepthFirstAstVisitor
	{
		sealed class TextReplaceAction
		{
			internal readonly int Offset;
			internal readonly int RemovalLength;
			internal readonly string NewText;
			internal TextReplaceAction DependsOn;

#if DEBUG
			internal readonly string StackTrace;
#endif

			public TextReplaceAction (int offset, int removalLength, string newText)
			{
				this.Offset = offset;
				this.RemovalLength = removalLength;
				this.NewText = newText ?? string.Empty;
				#if DEBUG
				this.StackTrace = Environment.StackTrace;
				#endif
			}
			
			public override bool Equals(object obj)
			{
				TextReplaceAction other = obj as TextReplaceAction;
				if (other == null) {
					return false;
				}
				return this.Offset == other.Offset && this.RemovalLength == other.RemovalLength && this.NewText == other.NewText;
			}
			
			public override int GetHashCode()
			{
				return 0;
			}

			public override string ToString()
			{
				return string.Format("[TextReplaceAction: Offset={0}, RemovalLength={1}, NewText={2}]", Offset, RemovalLength, NewText);
			}
		}
		
		CSharpFormattingOptions policy;
		IDocument document;
		List<TextReplaceAction> changes = new List<TextReplaceAction> ();
		Indent curIndent;
		readonly TextEditorOptions options;
		
		public FormattingMode FormattingMode {
			get;
			set;
		}

		public bool HadErrors {
			get;
			set;
		}

		public DomRegion FormattingRegion {
			get;
			set;
		}
		
		public AstFormattingVisitor(CSharpFormattingOptions policy, IDocument document, TextEditorOptions options = null)
		{
			if (policy == null) {
				throw new ArgumentNullException("policy");
			}
			if (document == null) {
				throw new ArgumentNullException("document");
			}
			this.policy = policy;
			this.document = document;
			this.options = options ?? TextEditorOptions.Default;
			curIndent = new Indent(this.options);
		}

		protected override void VisitChildren (AstNode node)
		{
			if (!FormattingRegion.IsEmpty) {
				if (node.EndLocation < FormattingRegion.Begin || node.StartLocation > FormattingRegion.End)
					return;
			}

			AstNode next;
			for (var child = node.FirstChild; child != null; child = next) {
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.NextSibling;
				child.AcceptVisitor (this);
			}
		}
		
		/// <summary>
		/// Applies the changes to the input document.
		/// </summary>
		public void ApplyChanges()
		{
			ApplyChanges(0, document.TextLength, document.Replace, (o, l, v) => document.GetText(o, l) == v);
		}
		
		public void ApplyChanges(int startOffset, int length)
		{
			ApplyChanges(startOffset, length, document.Replace, (o, l, v) => document.GetText(o, l) == v);
		}
		
		/// <summary>
		/// Applies the changes to the given Script instance.
		/// </summary>
		public void ApplyChanges(Script script)
		{
			ApplyChanges(0, document.TextLength, script.Replace);
		}
		
		public void ApplyChanges(int startOffset, int length, Script script)
		{
			ApplyChanges(startOffset, length, script.Replace);
		}
		
		public void ApplyChanges(int startOffset, int length, Action<int, int, string> documentReplace, Func<int, int, string, bool> filter = null)
		{
			int endOffset = startOffset + length;
			TextReplaceAction previousChange = null;
			int delta = 0;
			var depChanges = new List<TextReplaceAction> ();
			foreach (var change in changes.OrderBy(c => c.Offset)) {
				if (previousChange != null) {
					if (change.Equals(previousChange)) {
						// ignore duplicate changes
						continue;
					}
					if (change.Offset < previousChange.Offset + previousChange.RemovalLength) {
						#if DEBUG
						Console.WriteLine ("change 1:" + change + " at " + document.GetLocation (change.Offset));
						Console.WriteLine (change.StackTrace);

						Console.WriteLine ("change 2:" + previousChange + " at " + document.GetLocation (previousChange.Offset));
						Console.WriteLine (previousChange.StackTrace);
						#endif
						throw new InvalidOperationException ("Detected overlapping changes " + change + "/" + previousChange);
					}
				}
				previousChange = change;
				
				bool skipChange = change.Offset < startOffset || change.Offset > endOffset;
				skipChange |= filter != null && filter(change.Offset + delta, change.RemovalLength, change.NewText);
				skipChange &= !depChanges.Contains(change);

				if (!skipChange) {
					documentReplace(change.Offset + delta, change.RemovalLength, change.NewText);
					delta += change.NewText.Length - change.RemovalLength;
					if (change.DependsOn != null) {
						depChanges.Add(change.DependsOn);
					}
				}
			}
			changes.Clear();
		}

		public override void VisitSyntaxTree(SyntaxTree unit)
		{
			base.VisitSyntaxTree(unit);
		}

		public void EnsureBlankLinesAfter(AstNode node, int blankLines)
		{
			if (FormattingMode != FormattingMode.Intrusive)
				return;
			var loc = node.EndLocation;
			int line = loc.Line;
			do {
				line++;
			} while (line < document.LineCount && IsSpacing(document.GetLineByNumber(line)));
			var start = document.GetOffset(node.EndLocation);
			
			int foundBlankLines = line - loc.Line - 1;
			
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < blankLines - foundBlankLines; i++) {
				sb.Append(this.options.EolMarker);
			}
			
			int ws = start;
			while (ws < document.TextLength && IsSpacing (document.GetCharAt (ws))) {
				ws++;
			}
			int removedChars = ws - start;
			if (foundBlankLines > blankLines) {
				removedChars += document.GetLineByNumber(loc.Line + foundBlankLines - blankLines).EndOffset
					- document.GetLineByNumber(loc.Line).EndOffset;
			}
			AddChange(start, removedChars, sb.ToString());
		}

		public void EnsureBlankLinesBefore(AstNode node, int blankLines)
		{
			if (FormattingMode != FormattingMode.Intrusive)
				return;
			var loc = node.StartLocation;
			int line = loc.Line;
			do {
				line--;
			} while (line > 0 && IsSpacing(document.GetLineByNumber(line)));
			int end = document.GetOffset(loc.Line, 1);
			int start = document.GetOffset(line + 1, 1);
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < blankLines; i++) {
				sb.Append(this.options.EolMarker);
			}
			if (end - start == 0 && sb.Length == 0)
				return;
			AddChange(start, end - start, sb.ToString());
		}

		public override void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			if (usingDeclaration.PrevSibling != null && !(usingDeclaration.PrevSibling is UsingDeclaration || usingDeclaration.PrevSibling is UsingAliasDeclaration)) {
				EnsureBlankLinesBefore(usingDeclaration, policy.BlankLinesBeforeUsings);
			} else if (!(usingDeclaration.NextSibling is UsingDeclaration || usingDeclaration.NextSibling  is UsingAliasDeclaration)) {
				FixIndentationForceNewLine(usingDeclaration.StartLocation);
				EnsureBlankLinesAfter(usingDeclaration, policy.BlankLinesAfterUsings);
			} else {
				FixIndentationForceNewLine(usingDeclaration.StartLocation);
			}
		}

		public override void VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration)
		{
			if (usingDeclaration.PrevSibling != null && !(usingDeclaration.PrevSibling is UsingDeclaration || usingDeclaration.PrevSibling  is UsingAliasDeclaration)) {
				EnsureBlankLinesBefore(usingDeclaration, policy.BlankLinesBeforeUsings);
			} else if (!(usingDeclaration.NextSibling is UsingDeclaration || usingDeclaration.NextSibling  is UsingAliasDeclaration)) {
				FixIndentationForceNewLine(usingDeclaration.StartLocation);
				EnsureBlankLinesAfter(usingDeclaration, policy.BlankLinesAfterUsings);
			} else {
				FixIndentationForceNewLine(usingDeclaration.StartLocation);
			}
		}

		public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			var firstNsMember = namespaceDeclaration.Members.FirstOrDefault();
			if (firstNsMember != null) {
				EnsureBlankLinesBefore(firstNsMember, policy.BlankLinesBeforeFirstDeclaration);
			}
			FixIndentationForceNewLine(namespaceDeclaration.StartLocation);
			EnforceBraceStyle(policy.NamespaceBraceStyle, namespaceDeclaration.LBraceToken, namespaceDeclaration.RBraceToken);
			if (policy.IndentNamespaceBody) {
				curIndent.Push(IndentType.Block);
			}
			base.VisitNamespaceDeclaration(namespaceDeclaration);
			if (policy.IndentNamespaceBody) {
				curIndent.Pop ();
			}
			FixIndentation(namespaceDeclaration.RBraceToken.StartLocation);
		}

		public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			FormatAttributedNode(typeDeclaration);
			BraceStyle braceStyle;
			bool indentBody = false;
			switch (typeDeclaration.ClassType) {
				case ClassType.Class:
					braceStyle = policy.ClassBraceStyle;
					indentBody = policy.IndentClassBody;
					break;
				case ClassType.Struct:
					braceStyle = policy.StructBraceStyle;
					indentBody = policy.IndentStructBody;
					break;
				case ClassType.Interface:
					braceStyle = policy.InterfaceBraceStyle;
					indentBody = policy.IndentInterfaceBody;
					break;
				case ClassType.Enum:
					braceStyle = policy.EnumBraceStyle;
					indentBody = policy.IndentEnumBody;
					break;
				default:
					throw new InvalidOperationException("unsupported class type : " + typeDeclaration.ClassType);
			}

			EnforceBraceStyle(braceStyle, typeDeclaration.LBraceToken, typeDeclaration.RBraceToken);
			
			if (indentBody) {
				curIndent.Push(IndentType.Block);
			}
			base.VisitTypeDeclaration(typeDeclaration);
			if (indentBody) {
				curIndent.Pop ();
			}
			
			if (typeDeclaration.NextSibling is TypeDeclaration || typeDeclaration.NextSibling is DelegateDeclaration) {
				EnsureBlankLinesAfter(typeDeclaration, policy.BlankLinesBetweenTypes);
			}

		}

		bool IsSimpleAccessor(Accessor accessor)
		{
			if (accessor.IsNull || accessor.Body.IsNull || accessor.Body.FirstChild == null) {
				return true;
			}
			if (accessor.Body.Statements.Count() != 1) {
				return false;
			}
			return !(accessor.Body.Statements.FirstOrDefault() is BlockStatement);
			
		}

		bool IsSpacing(char ch)
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
			startOffset = System.Math.Max(0, startOffset);
			endOffset = System.Math.Max(startOffset, endOffset);
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
			AddChange(lastNonWs + 1, System.Math.Max(0, endOffset - lastNonWs - 1), forceSpace ? " " : "");
		}
//		void ForceSpacesAfter (AstNode n, bool forceSpaces)
//		{
//			if (n == null)
//				return;
//			AstLocation location = n.EndLocation;
//			int offset = data.LocationToOffset (location.Line, location.Column);
//			int i = offset;
//			while (i < data.Length && IsSpacing (data.GetCharAt (i))) {
//				i++;
//			}
//			ForceSpace (offset - 1, i, forceSpaces);
//		}
		
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
		
//		int ForceSpacesBefore (AstNode n, bool forceSpaces)
//		{
//			if (n == null || n.IsNull)
//				return 0;
//			AstLocation location = n.StartLocation;
//
//			int offset = data.LocationToOffset (location.Line, location.Column);
//			int i = offset - 1;
//
//			while (i >= 0 && IsSpacing (data.GetCharAt (i))) {
//				i--;
//			}
//			ForceSpace (i, offset, forceSpaces);
//			return i;
//		}
		
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
			var length = System.Math.Max(0, (offset - 1) - i);
			AddChange(i + 1, length, forceSpace ? " " : "");
			return i;
		}

		public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			FormatAttributedNode(propertyDeclaration);
			bool oneLine = false;
			switch (policy.PropertyFormatting) {
				case PropertyFormatting.AllowOneLine:
					bool isSimple = IsSimpleAccessor(propertyDeclaration.Getter) && IsSimpleAccessor(propertyDeclaration.Setter);
					int accessorLine = propertyDeclaration.RBraceToken.StartLocation.Line;
					if (!propertyDeclaration.Getter.IsNull && propertyDeclaration.Setter.IsNull) {
						accessorLine = propertyDeclaration.Getter.StartLocation.Line;
					} else if (propertyDeclaration.Getter.IsNull && !propertyDeclaration.Setter.IsNull) {
						accessorLine = propertyDeclaration.Setter.StartLocation.Line;
					} else {
						var acc = propertyDeclaration.Getter.StartLocation < propertyDeclaration.Setter.StartLocation ?
							propertyDeclaration.Getter : propertyDeclaration.Setter;
						accessorLine = acc.StartLocation.Line;
					}
					if (!isSimple || propertyDeclaration.LBraceToken.StartLocation.Line != accessorLine) {
						EnforceBraceStyle(policy.PropertyBraceStyle, propertyDeclaration.LBraceToken, propertyDeclaration.RBraceToken);
					} else {
						ForceSpacesBefore(propertyDeclaration.Getter, true);
						ForceSpacesBefore(propertyDeclaration.Setter, true);
						ForceSpacesBeforeRemoveNewLines(propertyDeclaration.RBraceToken, true);
						oneLine = true;
					}
					break;
				case PropertyFormatting.ForceNewLine:
					EnforceBraceStyle(policy.PropertyBraceStyle, propertyDeclaration.LBraceToken, propertyDeclaration.RBraceToken);
					break;
				case PropertyFormatting.ForceOneLine:
					isSimple = IsSimpleAccessor(propertyDeclaration.Getter) && IsSimpleAccessor(propertyDeclaration.Setter);
					if (isSimple) {
						int offset = this.document.GetOffset(propertyDeclaration.LBraceToken.StartLocation);
					
						int start = SearchWhitespaceStart(offset);
						int end = SearchWhitespaceEnd(offset);
						AddChange(start, offset - start, " ");
						AddChange(offset + 1, end - offset - 2, " ");
					
						offset = this.document.GetOffset(propertyDeclaration.RBraceToken.StartLocation);
						start = SearchWhitespaceStart(offset);
						AddChange(start, offset - start, " ");
						oneLine = true;
				
					} else {
						EnforceBraceStyle(policy.PropertyBraceStyle, propertyDeclaration.LBraceToken, propertyDeclaration.RBraceToken);
					}
					break;
			}
			if (policy.IndentPropertyBody) {
				curIndent.Push(IndentType.Block);
			}
			///System.Console.WriteLine ("one line: " + oneLine);
			if (!propertyDeclaration.Getter.IsNull) {
				if (!oneLine) {
					if (!IsLineIsEmptyUpToEol(propertyDeclaration.Getter.StartLocation)) {
						int offset = this.document.GetOffset(propertyDeclaration.Getter.StartLocation);
						int start = SearchWhitespaceStart(offset);
						string indentString = this.curIndent.IndentString;
						AddChange(start, offset - start, this.options.EolMarker + indentString);
					} else {
						FixIndentation(propertyDeclaration.Getter.StartLocation);
					}
				} else {
					int offset = this.document.GetOffset(propertyDeclaration.Getter.StartLocation);
					int start = SearchWhitespaceStart(offset);
					AddChange(start, offset - start, " ");
					
					ForceSpacesBefore(propertyDeclaration.Getter.Body.LBraceToken, true);
					ForceSpacesBefore(propertyDeclaration.Getter.Body.RBraceToken, true);
				}
				if (!propertyDeclaration.Getter.Body.IsNull) {
					if (!policy.AllowPropertyGetBlockInline || propertyDeclaration.Getter.Body.LBraceToken.StartLocation.Line != propertyDeclaration.Getter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle(policy.PropertyGetBraceStyle, propertyDeclaration.Getter.Body.LBraceToken, propertyDeclaration.Getter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixingBraces(propertyDeclaration.Getter.Body, policy.IndentBlocks);
				}
			}
			
			if (!propertyDeclaration.Setter.IsNull) {
				if (!oneLine) {
					if (!IsLineIsEmptyUpToEol(propertyDeclaration.Setter.StartLocation)) {
						int offset = this.document.GetOffset(propertyDeclaration.Setter.StartLocation);
						int start = SearchWhitespaceStart(offset);
						string indentString = this.curIndent.IndentString;
						AddChange(start, offset - start, this.options.EolMarker + indentString);
					} else {
						FixIndentation(propertyDeclaration.Setter.StartLocation);
					}
				} else {
					int offset = this.document.GetOffset(propertyDeclaration.Setter.StartLocation);
					int start = SearchWhitespaceStart(offset);
					AddChange(start, offset - start, " ");
					
					ForceSpacesBefore(propertyDeclaration.Setter.Body.LBraceToken, true);
					ForceSpacesBefore(propertyDeclaration.Setter.Body.RBraceToken, true);
				}
				if (!propertyDeclaration.Setter.Body.IsNull) {
					if (!policy.AllowPropertySetBlockInline || propertyDeclaration.Setter.Body.LBraceToken.StartLocation.Line != propertyDeclaration.Setter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle(policy.PropertySetBraceStyle, propertyDeclaration.Setter.Body.LBraceToken, propertyDeclaration.Setter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixingBraces(propertyDeclaration.Setter.Body, policy.IndentBlocks);
				}
			}
			
			if (policy.IndentPropertyBody) {
				curIndent.Pop ();
			}
			if (IsMember(propertyDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(propertyDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			ForceSpacesBefore(indexerDeclaration.LBracketToken, policy.SpaceBeforeIndexerDeclarationBracket);
			ForceSpacesAfter(indexerDeclaration.LBracketToken, policy.SpaceWithinIndexerDeclarationBracket);

			FormatParameters(indexerDeclaration);

			FormatAttributedNode(indexerDeclaration);
			EnforceBraceStyle(policy.PropertyBraceStyle, indexerDeclaration.LBraceToken, indexerDeclaration.RBraceToken);
			if (policy.IndentPropertyBody) {
				curIndent.Push(IndentType.Block);
			}
			
			if (!indexerDeclaration.Getter.IsNull) {
				FixIndentation(indexerDeclaration.Getter.StartLocation);
				if (!indexerDeclaration.Getter.Body.IsNull) {
					if (!policy.AllowPropertyGetBlockInline || indexerDeclaration.Getter.Body.LBraceToken.StartLocation.Line != indexerDeclaration.Getter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle(policy.PropertyGetBraceStyle, indexerDeclaration.Getter.Body.LBraceToken, indexerDeclaration.Getter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixingBraces(indexerDeclaration.Getter.Body, policy.IndentBlocks);
				}
			}
			
			if (!indexerDeclaration.Setter.IsNull) {
				FixIndentation(indexerDeclaration.Setter.StartLocation);
				if (!indexerDeclaration.Setter.Body.IsNull) {
					if (!policy.AllowPropertySetBlockInline || indexerDeclaration.Setter.Body.LBraceToken.StartLocation.Line != indexerDeclaration.Setter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle(policy.PropertySetBraceStyle, indexerDeclaration.Setter.Body.LBraceToken, indexerDeclaration.Setter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixingBraces(indexerDeclaration.Setter.Body, policy.IndentBlocks);
				}
			}
			if (policy.IndentPropertyBody) {
				curIndent.Pop ();
			}
			if (IsMember(indexerDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(indexerDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		static bool IsSimpleEvent(AstNode node)
		{
			return node is EventDeclaration;
		}

		public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			FormatAttributedNode(eventDeclaration);
			EnforceBraceStyle(policy.EventBraceStyle, eventDeclaration.LBraceToken, eventDeclaration.RBraceToken);
			if (policy.IndentEventBody) {
				curIndent.Push(IndentType.Block);
			}
			
			if (!eventDeclaration.AddAccessor.IsNull) {
				FixIndentation(eventDeclaration.AddAccessor.StartLocation);
				if (!eventDeclaration.AddAccessor.Body.IsNull) {
					if (!policy.AllowEventAddBlockInline || eventDeclaration.AddAccessor.Body.LBraceToken.StartLocation.Line != eventDeclaration.AddAccessor.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle(policy.EventAddBraceStyle, eventDeclaration.AddAccessor.Body.LBraceToken, eventDeclaration.AddAccessor.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					
					VisitBlockWithoutFixingBraces(eventDeclaration.AddAccessor.Body, policy.IndentBlocks);
				}
			}
			
			if (!eventDeclaration.RemoveAccessor.IsNull) {
				FixIndentation(eventDeclaration.RemoveAccessor.StartLocation);
				if (!eventDeclaration.RemoveAccessor.Body.IsNull) {
					if (!policy.AllowEventRemoveBlockInline || eventDeclaration.RemoveAccessor.Body.LBraceToken.StartLocation.Line != eventDeclaration.RemoveAccessor.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle(policy.EventRemoveBraceStyle, eventDeclaration.RemoveAccessor.Body.LBraceToken, eventDeclaration.RemoveAccessor.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixingBraces(eventDeclaration.RemoveAccessor.Body, policy.IndentBlocks);
				}
			}
			
			if (policy.IndentEventBody) {
				curIndent.Pop ();
			}
			
			if (eventDeclaration.NextSibling is EventDeclaration && IsSimpleEvent(eventDeclaration) && IsSimpleEvent(eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(eventDeclaration, policy.BlankLinesBetweenEventFields);
			} else if (IsMember(eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(eventDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			FormatAttributedNode(eventDeclaration);
			if (eventDeclaration.NextSibling is EventDeclaration && IsSimpleEvent(eventDeclaration) && IsSimpleEvent(eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(eventDeclaration, policy.BlankLinesBetweenEventFields);
			} else if (IsMember(eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(eventDeclaration, policy.BlankLinesBetweenMembers);
			}
			
			var lastLoc = eventDeclaration.StartLocation;
			curIndent.Push(IndentType.Block);
			foreach (var initializer in eventDeclaration.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					FixStatementIndentation(initializer.StartLocation);
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}
			curIndent.Pop ();
		}

		public override void VisitAccessor(Accessor accessor)
		{
			FixIndentationForceNewLine(accessor.StartLocation);
			base.VisitAccessor(accessor);
		}

		public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			FormatAttributedNode(fieldDeclaration);
			fieldDeclaration.ReturnType.AcceptVisitor(this);
			FormatCommas(fieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);
			if (fieldDeclaration.NextSibling is FieldDeclaration || fieldDeclaration.NextSibling is FixedFieldDeclaration) {
				EnsureBlankLinesAfter(fieldDeclaration, policy.BlankLinesBetweenFields);
			} else if (IsMember(fieldDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(fieldDeclaration, policy.BlankLinesBetweenMembers);
			}
			
			var lastLoc = fieldDeclaration.StartLocation;
			curIndent.Push(IndentType.Block);
			foreach (var initializer in fieldDeclaration.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					FixStatementIndentation(initializer.StartLocation);
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}
			curIndent.Pop ();
		}

		public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			FormatAttributedNode(fixedFieldDeclaration);
			FormatCommas(fixedFieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);
			if (fixedFieldDeclaration.NextSibling is FieldDeclaration || fixedFieldDeclaration.NextSibling is FixedFieldDeclaration) {
				EnsureBlankLinesAfter(fixedFieldDeclaration, policy.BlankLinesBetweenFields);
			} else if (IsMember(fixedFieldDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(fixedFieldDeclaration, policy.BlankLinesBetweenMembers);
			}
			
			var lastLoc = fixedFieldDeclaration.StartLocation;
			curIndent.Push(IndentType.Block);
			foreach (var initializer in fixedFieldDeclaration.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					FixStatementIndentation(initializer.StartLocation);
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}
			curIndent.Pop ();
		}

		public override void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			FormatAttributedNode(enumMemberDeclaration);
			base.VisitEnumMemberDeclaration(enumMemberDeclaration);
		}

		public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			FormatAttributedNode(delegateDeclaration);
			
			ForceSpacesBefore(delegateDeclaration.LParToken, policy.SpaceBeforeDelegateDeclarationParentheses);
			if (delegateDeclaration.Parameters.Any()) {
				ForceSpacesAfter(delegateDeclaration.LParToken, policy.SpaceWithinDelegateDeclarationParentheses);
				ForceSpacesBefore(delegateDeclaration.RParToken, policy.SpaceWithinDelegateDeclarationParentheses);
			} else {
				ForceSpacesAfter(delegateDeclaration.LParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
				ForceSpacesBefore(delegateDeclaration.RParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
			}
			FormatCommas(delegateDeclaration, policy.SpaceBeforeDelegateDeclarationParameterComma, policy.SpaceAfterDelegateDeclarationParameterComma);

			if (delegateDeclaration.NextSibling is TypeDeclaration || delegateDeclaration.NextSibling is DelegateDeclaration) {
				EnsureBlankLinesAfter(delegateDeclaration, policy.BlankLinesBetweenTypes);
			} else if (IsMember(delegateDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(delegateDeclaration, policy.BlankLinesBetweenMembers);
			}

			base.VisitDelegateDeclaration(delegateDeclaration);
		}

		static bool IsMember(AstNode nextSibling)
		{
			return nextSibling != null && nextSibling.NodeType == NodeType.Member;
		}
		
		void FormatAttributedNode(AstNode node)
		{
			if (node == null) {
				return;
			}
			AstNode child = node.FirstChild;
			while (child != null && child is AttributeSection) {
				FixIndentationForceNewLine(child.StartLocation);
				child = child.NextSibling;
			}
			if (child != null) {
				FixIndentationForceNewLine(child.StartLocation);
			}
		}


		void FormatParameters(AstNode node)
		{
			Wrapping methodCallArgumentWrapping;
			bool newLineAferMethodCallOpenParentheses;
			bool methodClosingParenthesesOnNewLine;
			bool spaceWithinMethodCallParentheses;
			bool spaceAfterMethodCallParameterComma;
			bool spaceBeforeMethodCallParameterComma;
				
			CSharpTokenNode rParToken;
			AstNodeCollection<ParameterDeclaration> parameters;

			var constructorDeclaration = node as ConstructorDeclaration;
			if (constructorDeclaration != null) {
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;

				spaceWithinMethodCallParentheses = policy.SpaceWithinConstructorDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterConstructorDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeConstructorDeclarationParameterComma;
				rParToken = constructorDeclaration.RParToken;
				parameters = constructorDeclaration.Parameters;
			} else if (node is IndexerDeclaration) {
				var indexer = (IndexerDeclaration)node;
				methodCallArgumentWrapping = policy.IndexerDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferIndexerDeclarationOpenBracket;
				methodClosingParenthesesOnNewLine = policy.IndexerDeclarationClosingBracketOnNewLine;

				spaceWithinMethodCallParentheses = policy.SpaceWithinIndexerDeclarationBracket;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterIndexerDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeIndexerDeclarationParameterComma;
				rParToken = indexer.RBracketToken;
				parameters = indexer.Parameters;
			} else if (node is OperatorDeclaration) {
				var op = (OperatorDeclaration)node;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				rParToken = op.RParToken;
				parameters = op.Parameters;
			} else {
				var methodDeclaration = node as MethodDeclaration;
				methodCallArgumentWrapping = policy.MethodDeclarationParameterWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodDeclarationOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodDeclarationClosingParenthesesOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodDeclarationParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodDeclarationParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodDeclarationParameterComma;
				rParToken = methodDeclaration.RParToken;
				parameters = methodDeclaration.Parameters;
			}
			if (FormattingMode == ICSharpCode.NRefactory.CSharp.FormattingMode.OnTheFly)
				methodCallArgumentWrapping = Wrapping.DoNotChange;

			bool wrapMethodCall = DoWrap(methodCallArgumentWrapping, rParToken, parameters.Count);
			if (wrapMethodCall && parameters.Any()) {
				if (newLineAferMethodCallOpenParentheses) {
					curIndent.Push(IndentType.Continuation);
					foreach (var arg in parameters) {
						FixStatementIndentation(arg.StartLocation);
					}
					curIndent.Pop();
				} else {
					int extraSpaces = parameters.First().StartLocation.Column - 1 - curIndent.IndentString.Length;
					curIndent.ExtraSpaces += extraSpaces;
					foreach (var arg in parameters.Skip(1)) {
						FixStatementIndentation(arg.StartLocation);
					}
					curIndent.ExtraSpaces -= extraSpaces;
				}
				if (!rParToken.IsNull) {
					if (methodClosingParenthesesOnNewLine) {
						FixStatementIndentation(rParToken.StartLocation);
					} else {
						ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
					}
				}
			} else {
				foreach (var arg in parameters) {
					if (arg.PrevSibling != null) {
						if (methodCallArgumentWrapping == Wrapping.DoNotWrap) {
							ForceSpacesBeforeRemoveNewLines(arg, spaceAfterMethodCallParameterComma && arg.PrevSibling.Role == Roles.Comma);
						} else {
							ForceSpacesBefore(arg, spaceAfterMethodCallParameterComma && arg.PrevSibling.Role == Roles.Comma);
						}
					}
					arg.AcceptVisitor(this);
				}
				if (!rParToken.IsNull) {
					if (methodCallArgumentWrapping == Wrapping.DoNotWrap) {
						ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
					} else {
						bool sameLine = rParToken.GetPrevNode().StartLocation.Line == rParToken.StartLocation.Line;
						if (sameLine) {
							ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
						} else {
							FixStatementIndentation(rParToken.StartLocation);
						}
					}
				}
			}
			if (!rParToken.IsNull) {
				foreach (CSharpTokenNode comma in rParToken.Parent.Children.Where(n => n.Role == Roles.Comma)) {
					ForceSpacesBefore(comma, spaceBeforeMethodCallParameterComma);
				}
			}
		}

		public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			FormatAttributedNode(methodDeclaration);
			
			ForceSpacesBefore(methodDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (methodDeclaration.Parameters.Any()) {
				ForceSpacesAfter(methodDeclaration.LParToken, policy.SpaceWithinMethodDeclarationParentheses);
				FormatParameters(methodDeclaration);
			} else {
				ForceSpacesAfter(methodDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore(methodDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}

			if (!methodDeclaration.Body.IsNull) {
				EnforceBraceStyle(policy.MethodBraceStyle, methodDeclaration.Body.LBraceToken, methodDeclaration.Body.RBraceToken);
				VisitBlockWithoutFixingBraces(methodDeclaration.Body, policy.IndentMethodBody);
			}
			if (IsMember(methodDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(methodDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			FormatAttributedNode(operatorDeclaration);
			
			ForceSpacesBefore(operatorDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (operatorDeclaration.Parameters.Any()) {
				ForceSpacesAfter(operatorDeclaration.LParToken, policy.SpaceWithinMethodDeclarationParentheses);
				FormatParameters(operatorDeclaration);
			} else {
				ForceSpacesAfter(operatorDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore(operatorDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}

			if (!operatorDeclaration.Body.IsNull) {
				EnforceBraceStyle(policy.MethodBraceStyle, operatorDeclaration.Body.LBraceToken, operatorDeclaration.Body.RBraceToken);
				VisitBlockWithoutFixingBraces(operatorDeclaration.Body, policy.IndentMethodBody);
			}
			if (IsMember(operatorDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(operatorDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			FormatAttributedNode(constructorDeclaration);
			
			ForceSpacesBefore(constructorDeclaration.LParToken, policy.SpaceBeforeConstructorDeclarationParentheses);
			if (constructorDeclaration.Parameters.Any()) {
				ForceSpacesAfter(constructorDeclaration.LParToken, policy.SpaceWithinConstructorDeclarationParentheses);
				FormatParameters(constructorDeclaration);
			} else {
				ForceSpacesAfter(constructorDeclaration.LParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
				ForceSpacesBefore(constructorDeclaration.RParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
			}

			if (!constructorDeclaration.Body.IsNull) {
				EnforceBraceStyle(policy.ConstructorBraceStyle, constructorDeclaration.Body.LBraceToken, constructorDeclaration.Body.RBraceToken);
				VisitBlockWithoutFixingBraces(constructorDeclaration.Body, policy.IndentMethodBody);
			}
			if (IsMember(constructorDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(constructorDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			FormatAttributedNode(destructorDeclaration);
			
			CSharpTokenNode lParen = destructorDeclaration.LParToken;
			int offset = this.document.GetOffset(lParen.StartLocation);
			ForceSpaceBefore(offset, policy.SpaceBeforeConstructorDeclarationParentheses);
			
			if (!destructorDeclaration.Body.IsNull) {
				EnforceBraceStyle(policy.DestructorBraceStyle, destructorDeclaration.Body.LBraceToken, destructorDeclaration.Body.RBraceToken);
				VisitBlockWithoutFixingBraces(destructorDeclaration.Body, policy.IndentMethodBody);
			}
			if (IsMember(destructorDeclaration.NextSibling)) {
				EnsureBlankLinesAfter(destructorDeclaration, policy.BlankLinesBetweenMembers);
			}
		}

		#region Statements
		public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			base.VisitExpressionStatement(expressionStatement);
			FixSemicolon(expressionStatement.SemicolonToken);
		}

		void VisitBlockWithoutFixingBraces(BlockStatement blockStatement, bool indent)
		{
			if (indent) {
				curIndent.Push(IndentType.Block);
			}
			foreach (var child in blockStatement.Children) {
				if (child.Role == Roles.LBrace || child.Role == Roles.RBrace) {
					continue;
				}
				if (child is Statement) {
					FixStatementIndentation(child.StartLocation);
					child.AcceptVisitor(this);
				} else if (child is Comment) {
					child.AcceptVisitor(this);
				} else {
					// pre processor directives at line start, if they are there.
					if (child.StartLocation.Column > 1)
						FixStatementIndentation(child.StartLocation);
				}
			}
			if (indent) {
				curIndent.Pop ();
			}
		}

		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			FixIndentation(blockStatement.StartLocation);
			VisitBlockWithoutFixingBraces(blockStatement, policy.IndentBlocks);
			FixIndentation(blockStatement.EndLocation, -1);
		}

		public override void VisitComment(Comment comment)
		{
			if (comment.StartsLine && !HadErrors && (!policy.KeepCommentsAtFirstColumn || comment.StartLocation.Column > 1)) {
				FixIndentation(comment.StartLocation);
			}
		}

		public override void VisitBreakStatement(BreakStatement breakStatement)
		{
			FixSemicolon(breakStatement.SemicolonToken);
		}

		public override void VisitCheckedStatement(CheckedStatement checkedStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, policy.FixedBraceForcement, checkedStatement.Body);
		}

		public override void VisitContinueStatement(ContinueStatement continueStatement)
		{
			FixSemicolon(continueStatement.SemicolonToken);
		}

		public override void VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			// Empty
		}

		public override void VisitFixedStatement(FixedStatement fixedStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, policy.FixedBraceForcement, fixedStatement.EmbeddedStatement);
		}

		public override void VisitForeachStatement(ForeachStatement foreachStatement)
		{
			ForceSpacesBefore(foreachStatement.LParToken, policy.SpaceBeforeForeachParentheses);

			ForceSpacesAfter(foreachStatement.LParToken, policy.SpacesWithinForeachParentheses);
			ForceSpacesBefore(foreachStatement.RParToken, policy.SpacesWithinForeachParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, policy.ForEachBraceForcement, foreachStatement.EmbeddedStatement);
		}

		void FixEmbeddedStatment(BraceStyle braceStyle, BraceForcement braceForcement, AstNode node)
		{
			FixEmbeddedStatment(braceStyle, braceForcement, null, false, node);
		}

		void FixEmbeddedStatment(BraceStyle braceStyle, BraceForcement braceForcement, CSharpTokenNode token, bool allowInLine, AstNode node, bool statementAlreadyIndented = false)
		{
			if (node == null) {
				return;
			}
			bool isBlock = node is BlockStatement;
			TextReplaceAction beginBraceAction = null;
			TextReplaceAction endBraceAction = null;

			switch (braceForcement) {
				case BraceForcement.DoNotChange:
					//nothing
					break;
				case BraceForcement.AddBraces:
					if (!isBlock) {
						AstNode n = node.Parent.GetCSharpNodeBefore(node);
						int start = document.GetOffset(n.EndLocation);
						string startBrace = "";
						switch (braceStyle) {
							case BraceStyle.EndOfLineWithoutSpace:
								startBrace = "{";
								break;
							case BraceStyle.BannerStyle:
							case BraceStyle.EndOfLine:
								startBrace = " {";
								break;
							case BraceStyle.NextLine:
								startBrace = this.options.EolMarker + curIndent.IndentString + "{";
								break;
							case BraceStyle.NextLineShifted2:
							case BraceStyle.NextLineShifted:
								curIndent.Push(IndentType.Block);
								startBrace = this.options.EolMarker + curIndent.IndentString + "{";
								curIndent.Pop();
								break;
						}
						beginBraceAction = AddChange(start, 0, startBrace);
					}
					break;
				case BraceForcement.RemoveBraces:
					if (isBlock) {
						BlockStatement block = node as BlockStatement;
						if (block.Statements.Count() == 1) {
							int offset1 = document.GetOffset(node.StartLocation);
							int start = SearchWhitespaceStart(offset1);
							
							int offset2 = document.GetOffset(node.EndLocation);
							int end = SearchWhitespaceStart(offset2 - 1);
							
							beginBraceAction = AddChange(start, offset1 - start + 1, null);
							endBraceAction = AddChange(end + 1, offset2 - end, null);
							node = block.FirstChild;
							isBlock = false;
						}
					}
					break;
			}
			if (isBlock) {
				BlockStatement block = node as BlockStatement;
				if (allowInLine && block.StartLocation.Line == block.EndLocation.Line && block.Statements.Count() <= 1) {
					if (block.Statements.Count() == 1) {
						nextStatementIndent = " ";
					}
				} else {
					if (!statementAlreadyIndented) {
						EnforceBraceStyle(braceStyle, block.LBraceToken, block.RBraceToken);
					}
				}
				if (braceStyle == BraceStyle.NextLineShifted2) {
					curIndent.Push(IndentType.Block);
				}
			} else {
				if (allowInLine && token.StartLocation.Line == node.EndLocation.Line) {
					nextStatementIndent = " ";
				}
			}
			if (policy.IndentBlocks && !(policy.AlignEmbeddedIfStatements && node is IfElseStatement && node.Parent is IfElseStatement || policy.AlignEmbeddedUsingStatements && node is UsingStatement && node.Parent is UsingStatement)) { 
				curIndent.Push(IndentType.Block);
			}
			if (isBlock) {
				VisitBlockWithoutFixingBraces((BlockStatement)node, false);
			} else {
				if (!statementAlreadyIndented) {
					FixStatementIndentation(node.StartLocation);
				}
				node.AcceptVisitor(this);
			}
			if (policy.IndentBlocks && !(policy.AlignEmbeddedIfStatements && node is IfElseStatement && node.Parent is IfElseStatement || policy.AlignEmbeddedUsingStatements && node is UsingStatement && node.Parent is UsingStatement)) { 
				curIndent.Pop();
			}
			switch (braceForcement) {
				case BraceForcement.DoNotChange:
					break;
				case BraceForcement.AddBraces:
					if (!isBlock) {
						int offset = document.GetOffset(node.EndLocation);
						if (!char.IsWhiteSpace(document.GetCharAt(offset))) {
							offset++;
						}
						string startBrace = "";
						switch (braceStyle) {
							case BraceStyle.DoNotChange:
								startBrace = null;
								break;
							case BraceStyle.EndOfLineWithoutSpace:
								startBrace = this.options.EolMarker + curIndent.IndentString + "}";
								break;
							case BraceStyle.EndOfLine:
								startBrace = this.options.EolMarker + curIndent.IndentString + "}";
								break;
							case BraceStyle.NextLine:
								startBrace = this.options.EolMarker + curIndent.IndentString + "}";
								break;
							case BraceStyle.BannerStyle:
							case BraceStyle.NextLineShifted2:
							case BraceStyle.NextLineShifted:
								curIndent.Push(IndentType.Block);
								startBrace = this.options.EolMarker + curIndent.IndentString + "}";
								curIndent.Pop ();
								break;

						}
						if (startBrace != null) {
							endBraceAction = AddChange(offset, 0, startBrace);
						}
					}
					break;
			}
			if (beginBraceAction != null && endBraceAction != null) {
				beginBraceAction.DependsOn = endBraceAction;
				endBraceAction.DependsOn = beginBraceAction;
			}
		}

		void EnforceBraceStyle(BraceStyle braceStyle, AstNode lbrace, AstNode rbrace)
		{
			if (lbrace.IsNull || rbrace.IsNull) {
				return;
			}
			
			//			LineSegment lbraceLineSegment = data.Document.GetLine (lbrace.StartLocation.Line);
			int lbraceOffset = document.GetOffset(lbrace.StartLocation);
			
			//			LineSegment rbraceLineSegment = data.Document.GetLine (rbrace.StartLocation.Line);
			int rbraceOffset = document.GetOffset(rbrace.StartLocation);
			int whitespaceStart = SearchWhitespaceStart(lbraceOffset);
			int whitespaceEnd = SearchWhitespaceLineStart(rbraceOffset);
			string startIndent = "";
			string endIndent = "";
			switch (braceStyle) {
				case BraceStyle.DoNotChange:
					startIndent = endIndent = null;
					break;
				case BraceStyle.EndOfLineWithoutSpace:
					startIndent = "";
					endIndent = IsLineIsEmptyUpToEol(rbraceOffset) ? curIndent.IndentString : this.options.EolMarker + curIndent.IndentString;
					break;
				case BraceStyle.BannerStyle:
					var prevNode = lbrace.GetPrevNode();
					if (prevNode is Comment) {
						// delete old bracket
						AddChange(whitespaceStart, lbraceOffset - whitespaceStart + 1, "");
					
						while (prevNode is Comment) {
							prevNode = prevNode.GetPrevNode();
						}
						whitespaceStart = document.GetOffset(prevNode.EndLocation);
						lbraceOffset = whitespaceStart;
						startIndent = " {";
					} else {
						startIndent = " ";
					}
					curIndent.Push(IndentType.Block);
					endIndent = IsLineIsEmptyUpToEol(rbraceOffset) ? curIndent.IndentString : this.options.EolMarker + curIndent.IndentString;
					curIndent.Pop();
					break;
				case BraceStyle.EndOfLine:
					prevNode = lbrace.GetPrevNode();
					if (prevNode is Comment) {
						// delete old bracket
						AddChange(whitespaceStart, lbraceOffset - whitespaceStart + 1, "");
					
						while (prevNode is Comment) {
							prevNode = prevNode.GetPrevNode();
						}
						whitespaceStart = document.GetOffset(prevNode.EndLocation);
						lbraceOffset = whitespaceStart;
						startIndent = " {";
					} else {
						startIndent = " ";
					}
					endIndent = IsLineIsEmptyUpToEol(rbraceOffset) ? curIndent.IndentString : this.options.EolMarker + curIndent.IndentString;
					break;
				case BraceStyle.NextLine:
					startIndent = this.options.EolMarker + curIndent.IndentString;
					endIndent = IsLineIsEmptyUpToEol(rbraceOffset) ? curIndent.IndentString : this.options.EolMarker + curIndent.IndentString;
					break;
				case BraceStyle.NextLineShifted2:
				case BraceStyle.NextLineShifted:
					curIndent.Push(IndentType.Block);
					startIndent = this.options.EolMarker + curIndent.IndentString;
					endIndent = IsLineIsEmptyUpToEol(rbraceOffset) ? curIndent.IndentString : this.options.EolMarker + curIndent.IndentString;
					curIndent.Pop ();
					break;
			}
			
			if (lbraceOffset > 0 && startIndent != null) {
				AddChange(whitespaceStart, lbraceOffset - whitespaceStart, startIndent);
			}
			if (rbraceOffset > 0 && endIndent != null) {
				AddChange(whitespaceEnd, rbraceOffset - whitespaceEnd, endIndent);
			}
		}

		TextReplaceAction AddChange(int offset, int removedChars, string insertedText)
		{
			if (removedChars == 0 && string.IsNullOrEmpty (insertedText))
				return null;
			var action = new TextReplaceAction (offset, removedChars, insertedText);
			changes.Add(action);
			return action;
		}

		public bool IsLineIsEmptyUpToEol(TextLocation startLocation)
		{
			return IsLineIsEmptyUpToEol(document.GetOffset(startLocation) - 1);
		}

		bool IsLineIsEmptyUpToEol(int startOffset)
		{
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = document.GetCharAt(offset);
				if (ch != ' ' && ch != '\t') {
					return ch == '\n' || ch == '\r';
				}
			}
			return true;
		}

		int SearchWhitespaceStart(int startOffset)
		{
			if (startOffset < 0) {
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			}
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = document.GetCharAt(offset);
				if (!Char.IsWhiteSpace(ch)) {
					return offset + 1;
				}
			}
			return 0;
		}

		int SearchWhitespaceEnd(int startOffset)
		{
			if (startOffset > document.TextLength) {
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			}
			for (int offset = startOffset + 1; offset < document.TextLength; offset++) {
				char ch = document.GetCharAt(offset);
				if (!Char.IsWhiteSpace(ch)) {
					return offset + 1;
				}
			}
			return document.TextLength - 1;
		}

		int SearchWhitespaceLineStart(int startOffset)
		{
			if (startOffset < 0) {
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			}
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = document.GetCharAt(offset);
				if (ch != ' ' && ch != '\t') {
					return offset + 1;
				}
			}
			return 0;
		}

		public override void VisitForStatement(ForStatement forStatement)
		{
			foreach (AstNode node in forStatement.Children) {
				if (node.Role == Roles.Semicolon) {
					if (node.NextSibling is CSharpTokenNode || node.NextSibling is EmptyStatement) {
						continue;
					}
					ForceSpacesBefore(node, policy.SpaceBeforeForSemicolon);
					ForceSpacesAfter(node, policy.SpaceAfterForSemicolon);
				}
			}

			ForceSpacesBefore(forStatement.LParToken, policy.SpaceBeforeForParentheses);

			ForceSpacesAfter(forStatement.LParToken, policy.SpacesWithinForParentheses);
			ForceSpacesBefore(forStatement.RParToken, policy.SpacesWithinForParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, policy.ForBraceForcement, forStatement.EmbeddedStatement);
		}

		public override void VisitGotoStatement(GotoStatement gotoStatement)
		{
			VisitChildren(gotoStatement);
			FixSemicolon(gotoStatement.SemicolonToken);
		}

		public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			ForceSpacesBefore(ifElseStatement.LParToken, policy.SpaceBeforeIfParentheses);

			ForceSpacesAfter(ifElseStatement.LParToken, policy.SpacesWithinIfParentheses);
			ForceSpacesBefore(ifElseStatement.RParToken, policy.SpacesWithinIfParentheses);

			if (!(ifElseStatement.Parent is IfElseStatement && ((IfElseStatement)ifElseStatement.Parent).FalseStatement == ifElseStatement)) {
				FixStatementIndentation(ifElseStatement.StartLocation);
			}
			
			if (!ifElseStatement.Condition.IsNull) {
				ifElseStatement.Condition.AcceptVisitor(this);
			}
			
			if (!ifElseStatement.TrueStatement.IsNull) {
				FixEmbeddedStatment(policy.StatementBraceStyle, policy.IfElseBraceForcement, ifElseStatement.IfToken, policy.AllowIfBlockInline, ifElseStatement.TrueStatement);
			}
			
			if (!ifElseStatement.FalseStatement.IsNull) {
				var placeElseOnNewLine = policy.ElseNewLinePlacement;
				if (!(ifElseStatement.TrueStatement is BlockStatement) && policy.IfElseBraceForcement != BraceForcement.AddBraces)
					placeElseOnNewLine = NewLinePlacement.NewLine;
				PlaceOnNewLine(placeElseOnNewLine, ifElseStatement.ElseToken);
				var forcement = policy.IfElseBraceForcement;
				if (ifElseStatement.FalseStatement is IfElseStatement) {
					forcement = BraceForcement.DoNotChange;
					PlaceOnNewLine(policy.ElseIfNewLinePlacement, ((IfElseStatement)ifElseStatement.FalseStatement).IfToken);
				}
				FixEmbeddedStatment(policy.StatementBraceStyle, forcement, ifElseStatement.ElseToken, policy.AllowIfBlockInline, ifElseStatement.FalseStatement, ifElseStatement.FalseStatement is IfElseStatement);
			}
		}

		public override void VisitLabelStatement(LabelStatement labelStatement)
		{
			// TODO
			VisitChildren(labelStatement);
		}

		public override void VisitLockStatement(LockStatement lockStatement)
		{
			ForceSpacesBefore(lockStatement.LParToken, policy.SpaceBeforeLockParentheses);

			ForceSpacesAfter(lockStatement.LParToken, policy.SpacesWithinLockParentheses);
			ForceSpacesBefore(lockStatement.RParToken, policy.SpacesWithinLockParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, policy.FixedBraceForcement, lockStatement.EmbeddedStatement);
		}

		public override void VisitReturnStatement(ReturnStatement returnStatement)
		{
			VisitChildren(returnStatement);
			FixSemicolon(returnStatement.SemicolonToken);
		}

		public override void VisitSwitchStatement(SwitchStatement switchStatement)
		{
			ForceSpacesBefore(switchStatement.LParToken, policy.SpaceBeforeSwitchParentheses);

			ForceSpacesAfter(switchStatement.LParToken, policy.SpacesWithinSwitchParentheses);
			ForceSpacesBefore(switchStatement.RParToken, policy.SpacesWithinSwitchParentheses);

			EnforceBraceStyle(policy.StatementBraceStyle, switchStatement.LBraceToken, switchStatement.RBraceToken);
			VisitChildren(switchStatement);
		}

		public override void VisitSwitchSection(SwitchSection switchSection)
		{
			if (policy.IndentSwitchBody) {
				curIndent.Push(IndentType.Block);
			}
			
			foreach (CaseLabel label in switchSection.CaseLabels) {
				FixStatementIndentation(label.StartLocation);
				label.AcceptVisitor(this);
			}
			if (policy.IndentCaseBody) {
				curIndent.Push(IndentType.Block);
			}
			
			foreach (var stmt in switchSection.Statements) {
				if (stmt is BreakStatement && !policy.IndentBreakStatements && policy.IndentCaseBody) {
					curIndent.Pop();
					FixStatementIndentation(stmt.StartLocation);
					stmt.AcceptVisitor(this);
					curIndent.Push(IndentType.Block);
					continue;
				}
				FixStatementIndentation(stmt.StartLocation);
				stmt.AcceptVisitor(this);
			}
			if (policy.IndentCaseBody) {
				curIndent.Pop ();
			}
			
			if (policy.IndentSwitchBody) {
				curIndent.Pop ();
			}
		}

		public override void VisitCaseLabel(CaseLabel caseLabel)
		{
			FixSemicolon(caseLabel.ColonToken);
		}

		public override void VisitThrowStatement(ThrowStatement throwStatement)
		{
			VisitChildren(throwStatement);
			FixSemicolon(throwStatement.SemicolonToken);
		}

		public override void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
		{
			if (!tryCatchStatement.TryBlock.IsNull) {
				FixEmbeddedStatment(policy.StatementBraceStyle, BraceForcement.DoNotChange, tryCatchStatement.TryBlock);
			}
			
			foreach (CatchClause clause in tryCatchStatement.CatchClauses) {
				PlaceOnNewLine(policy.CatchNewLinePlacement, clause.CatchToken);
				if (!clause.LParToken.IsNull) {
					ForceSpacesBefore(clause.LParToken, policy.SpaceBeforeCatchParentheses);

					ForceSpacesAfter(clause.LParToken, policy.SpacesWithinCatchParentheses);
					ForceSpacesBefore(clause.RParToken, policy.SpacesWithinCatchParentheses);
				}
				FixEmbeddedStatment(policy.StatementBraceStyle, BraceForcement.DoNotChange, clause.Body);
			}
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				PlaceOnNewLine(policy.FinallyNewLinePlacement, tryCatchStatement.FinallyToken);
				
				FixEmbeddedStatment(policy.StatementBraceStyle, BraceForcement.DoNotChange, tryCatchStatement.FinallyBlock);
			}
			
		}

		public override void VisitCatchClause(CatchClause catchClause)
		{
			// Handled in TryCatchStatement
		}

		public override void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, policy.FixedBraceForcement, uncheckedStatement.Body);
		}

		public override void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			FixEmbeddedStatment(policy.StatementBraceStyle, BraceForcement.DoNotChange, unsafeStatement.Body);
		}

		public override void VisitUsingStatement(UsingStatement usingStatement)
		{
			ForceSpacesBefore(usingStatement.LParToken, policy.SpaceBeforeUsingParentheses);

			ForceSpacesAfter(usingStatement.LParToken, policy.SpacesWithinUsingParentheses);
			ForceSpacesBefore(usingStatement.RParToken, policy.SpacesWithinUsingParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, policy.UsingBraceForcement, usingStatement.EmbeddedStatement);
		}

		public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			if ((variableDeclarationStatement.Modifiers & Modifiers.Const) == Modifiers.Const) {
				ForceSpacesAround(variableDeclarationStatement.Type, true);
			} else {
				ForceSpacesAfter(variableDeclarationStatement.Type, true);
			}
			var lastLoc = variableDeclarationStatement.StartLocation;
			foreach (var initializer in variableDeclarationStatement.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					FixStatementIndentation(initializer.StartLocation);
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}

			FormatCommas(variableDeclarationStatement, policy.SpaceBeforeLocalVariableDeclarationComma, policy.SpaceAfterLocalVariableDeclarationComma);
			FixSemicolon(variableDeclarationStatement.SemicolonToken);
		}

		public override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			PlaceOnNewLine(policy.WhileNewLinePlacement, doWhileStatement.WhileToken);
			FixEmbeddedStatment(policy.StatementBraceStyle, policy.WhileBraceForcement, doWhileStatement.EmbeddedStatement);
		}

		public override void VisitWhileStatement(WhileStatement whileStatement)
		{
			ForceSpacesBefore(whileStatement.LParToken, policy.SpaceBeforeWhileParentheses);

			ForceSpacesAfter(whileStatement.LParToken, policy.SpacesWithinWhileParentheses);
			ForceSpacesBefore(whileStatement.RParToken, policy.SpacesWithinWhileParentheses);

			FixEmbeddedStatment(policy.StatementBraceStyle, policy.WhileBraceForcement, whileStatement.EmbeddedStatement);
		}

		public override void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			FixSemicolon(yieldBreakStatement.SemicolonToken);
		}

		public override void VisitYieldReturnStatement(YieldReturnStatement yieldStatement)
		{
			yieldStatement.Expression.AcceptVisitor(this);
			FixSemicolon(yieldStatement.SemicolonToken);
		}

		public override void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			if (!variableInitializer.AssignToken.IsNull) {
				ForceSpacesAround(variableInitializer.AssignToken, policy.SpaceAroundAssignment);
			}
			if (!variableInitializer.Initializer.IsNull) {
				variableInitializer.Initializer.AcceptVisitor(this);
			}
		}

		#endregion
		
		#region Expressions
		public override void VisitComposedType(ComposedType composedType)
		{
			var spec = composedType.ArraySpecifiers.FirstOrDefault();
			if (spec != null) {
				ForceSpacesBefore(spec.LBracketToken, policy.SpaceBeforeArrayDeclarationBrackets);
			}

			base.VisitComposedType(composedType);
		}

		public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			if (!anonymousMethodExpression.Body.IsNull) {
				EnforceBraceStyle(policy.AnonymousMethodBraceStyle, anonymousMethodExpression.Body.LBraceToken, anonymousMethodExpression.Body.RBraceToken);
				VisitBlockWithoutFixingBraces(anonymousMethodExpression.Body, policy.IndentBlocks);
				return;
			}
			base.VisitAnonymousMethodExpression(anonymousMethodExpression);
		}

		public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			ForceSpacesAround(assignmentExpression.OperatorToken, policy.SpaceAroundAssignment);
			base.VisitAssignmentExpression(assignmentExpression);
		}

		public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			bool forceSpaces = false;
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					forceSpaces = policy.SpaceAroundEqualityOperator;
					break;
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.LessThanOrEqual:
					forceSpaces = policy.SpaceAroundRelationalOperator;
					break;
				case BinaryOperatorType.ConditionalAnd:
				case BinaryOperatorType.ConditionalOr:
					forceSpaces = policy.SpaceAroundLogicalOperator;
					break;
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
				case BinaryOperatorType.ExclusiveOr:
					forceSpaces = policy.SpaceAroundBitwiseOperator;
					break;
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Subtract:
					forceSpaces = policy.SpaceAroundAdditiveOperator;
					break;
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.Modulus:
					forceSpaces = policy.SpaceAroundMultiplicativeOperator;
					break;
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					forceSpaces = policy.SpaceAroundShiftOperator;
					break;
				case BinaryOperatorType.NullCoalescing:
					forceSpaces = policy.SpaceAroundNullCoalescingOperator;
					break;
			}
			ForceSpacesAround(binaryOperatorExpression.OperatorToken, forceSpaces);
			
			base.VisitBinaryOperatorExpression(binaryOperatorExpression);
			// Handle line breaks in binary opeartor expression.
			if (binaryOperatorExpression.Left.EndLocation.Line != binaryOperatorExpression.Right.StartLocation.Line) {
				curIndent.Push(IndentType.Block);
				if (binaryOperatorExpression.OperatorToken.StartLocation.Line == binaryOperatorExpression.Right.StartLocation.Line) {
					FixStatementIndentation(binaryOperatorExpression.OperatorToken.StartLocation);
				} else {
					FixStatementIndentation(binaryOperatorExpression.Right.StartLocation);
				}
				curIndent.Pop ();
			}
		}

		public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
		{
			ForceSpacesBefore(conditionalExpression.QuestionMarkToken, policy.SpaceBeforeConditionalOperatorCondition);
			ForceSpacesAfter(conditionalExpression.QuestionMarkToken, policy.SpaceAfterConditionalOperatorCondition);
			ForceSpacesBefore(conditionalExpression.ColonToken, policy.SpaceBeforeConditionalOperatorSeparator);
			ForceSpacesAfter(conditionalExpression.ColonToken, policy.SpaceAfterConditionalOperatorSeparator);
			base.VisitConditionalExpression(conditionalExpression);
		}

		public override void VisitCastExpression(CastExpression castExpression)
		{
			if (castExpression.RParToken != null) {
				ForceSpacesAfter(castExpression.LParToken, policy.SpacesWithinCastParentheses);
				ForceSpacesBefore(castExpression.RParToken, policy.SpacesWithinCastParentheses);

				ForceSpacesAfter(castExpression.RParToken, policy.SpaceAfterTypecast);
			}
			base.VisitCastExpression(castExpression);
		}

		void ForceSpacesAround(AstNode node, bool forceSpaces)
		{
			if (node.IsNull) {
				return;
			}
			ForceSpacesBefore(node, forceSpaces);
			ForceSpacesAfter(node, forceSpaces);
		}

		void FormatCommas(AstNode parent, bool before, bool after)
		{
			if (parent.IsNull) {
				return;
			}
			foreach (CSharpTokenNode comma in parent.Children.Where (node => node.Role == Roles.Comma)) {
				ForceSpacesAfter(comma, after);
				ForceSpacesBefore(comma, before);
			}
		}

		bool DoWrap (Wrapping wrapping, AstNode wrapNode, int argumentCount)
		{
			return wrapping == Wrapping.WrapAlways || 
				options.WrapLineLength > 0 && argumentCount > 1 && wrapping == Wrapping.WrapIfTooLong && wrapNode.StartLocation.Column >= options.WrapLineLength;
		}

		void FormatArguments(AstNode node)
		{
			Wrapping methodCallArgumentWrapping;
			bool newLineAferMethodCallOpenParentheses;
			bool methodClosingParenthesesOnNewLine;
			bool spaceWithinMethodCallParentheses;
			bool spaceAfterMethodCallParameterComma;
			bool spaceBeforeMethodCallParameterComma;
				
			CSharpTokenNode rParToken;
			AstNodeCollection<Expression> arguments;
			var indexer = node as IndexerExpression;
			if (indexer != null) {
				methodCallArgumentWrapping = policy.IndexerArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferIndexerOpenBracket;
				methodClosingParenthesesOnNewLine = policy.IndexerClosingBracketOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpacesWithinBrackets;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterBracketComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeBracketComma;
			
				rParToken = indexer.RBracketToken;
				arguments = indexer.Arguments;
			} else if (node is ObjectCreateExpression) {
				var oce = node as ObjectCreateExpression;
				methodCallArgumentWrapping = policy.MethodCallArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodCallOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodCallClosingParenthesesOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpacesWithinNewParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterNewParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeNewParameterComma;
			
				rParToken = oce.RParToken;
				arguments = oce.Arguments;
			} else {
				InvocationExpression invocationExpression = node as InvocationExpression;
				methodCallArgumentWrapping = policy.MethodCallArgumentWrapping;
				newLineAferMethodCallOpenParentheses = policy.NewLineAferMethodCallOpenParentheses;
				methodClosingParenthesesOnNewLine = policy.MethodCallClosingParenthesesOnNewLine;
				spaceWithinMethodCallParentheses = policy.SpaceWithinMethodCallParentheses;
				spaceAfterMethodCallParameterComma = policy.SpaceAfterMethodCallParameterComma;
				spaceBeforeMethodCallParameterComma = policy.SpaceBeforeMethodCallParameterComma;
			
				rParToken = invocationExpression.RParToken;
				arguments = invocationExpression.Arguments;
			}

			if (FormattingMode == ICSharpCode.NRefactory.CSharp.FormattingMode.OnTheFly)
				methodCallArgumentWrapping = Wrapping.DoNotChange;

			bool wrapMethodCall = DoWrap(methodCallArgumentWrapping, rParToken, arguments.Count);
			if (wrapMethodCall && arguments.Any()) {
				if (newLineAferMethodCallOpenParentheses) {
					curIndent.Push(IndentType.Continuation);
					foreach (var arg in arguments) {
						FixStatementIndentation(arg.StartLocation);
					}
					curIndent.Pop();
				} else {
					int extraSpaces = arguments.First().StartLocation.Column - 1 - curIndent.IndentString.Length;
					curIndent.ExtraSpaces += extraSpaces;
					foreach (var arg in arguments.Skip(1)) {
						FixStatementIndentation(arg.StartLocation);
					}
					curIndent.ExtraSpaces -= extraSpaces;
				}
				if (!rParToken.IsNull) {
					if (methodClosingParenthesesOnNewLine) {
						FixStatementIndentation(rParToken.StartLocation);
					} else {
						ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
					}
				}
			} else {
				foreach (var arg in arguments) {
					if (arg.PrevSibling != null) {
						if (methodCallArgumentWrapping == Wrapping.DoNotWrap) {
							ForceSpacesBeforeRemoveNewLines(arg, spaceAfterMethodCallParameterComma && arg.PrevSibling.Role == Roles.Comma);
						} else {
							ForceSpacesBefore(arg, spaceAfterMethodCallParameterComma && arg.PrevSibling.Role == Roles.Comma);
						}
					}
					arg.AcceptVisitor(this);
				}
				if (!rParToken.IsNull) {
					if (methodCallArgumentWrapping == Wrapping.DoNotWrap) {
						ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
					} else {
						bool sameLine = rParToken.GetPrevNode().EndLocation.Line == rParToken.StartLocation.Line;
						if (sameLine) {
							ForceSpacesBeforeRemoveNewLines(rParToken, spaceWithinMethodCallParentheses);
						} else {
							FixStatementIndentation(rParToken.StartLocation);
						}
					}
				}
			}
			if (!rParToken.IsNull) {
				foreach (CSharpTokenNode comma in rParToken.Parent.Children.Where(n => n.Role == Roles.Comma)) {
					ForceSpacesBefore(comma, spaceBeforeMethodCallParameterComma);
				}
			}
		}

		public override void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			ForceSpacesBefore(invocationExpression.LParToken, policy.SpaceBeforeMethodCallParentheses);
			if (invocationExpression.Arguments.Any()) {
				ForceSpacesAfter(invocationExpression.LParToken, policy.SpaceWithinMethodCallParentheses);
			} else {
				ForceSpacesAfter(invocationExpression.LParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
				ForceSpacesBefore(invocationExpression.RParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
			}

			if (!invocationExpression.Target.IsNull)
				invocationExpression.Target.AcceptVisitor(this);

			if (invocationExpression.Target is MemberReferenceExpression) {
				var mt = (MemberReferenceExpression)invocationExpression.Target;
				if (mt.Target is InvocationExpression) {
					if (DoWrap(policy.ChainedMethodCallWrapping, mt.DotToken, 2)) {
						curIndent.Push(IndentType.Continuation);
						FixStatementIndentation(mt.DotToken.StartLocation);
						curIndent.Pop();
					} else {
						if (policy.ChainedMethodCallWrapping == Wrapping.DoNotWrap)
							ForceSpacesBeforeRemoveNewLines(mt.DotToken, false);
					}
				}
			}
			FormatArguments(invocationExpression);
		}

		public override void VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			ForceSpacesBefore(indexerExpression.LBracketToken, policy.SpacesBeforeBrackets);
			ForceSpacesAfter(indexerExpression.LBracketToken, policy.SpacesWithinBrackets);

			if (!indexerExpression.Target.IsNull)
				indexerExpression.Target.AcceptVisitor(this);

			FormatArguments(indexerExpression);



		}

		public override void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			ForceSpacesAfter(parenthesizedExpression.LParToken, policy.SpacesWithinParentheses);
			ForceSpacesBefore(parenthesizedExpression.RParToken, policy.SpacesWithinParentheses);
			base.VisitParenthesizedExpression(parenthesizedExpression);
		}

		public override void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			ForceSpacesBefore(sizeOfExpression.LParToken, policy.SpaceBeforeSizeOfParentheses);
			ForceSpacesAfter(sizeOfExpression.LParToken, policy.SpacesWithinSizeOfParentheses);
			ForceSpacesBefore(sizeOfExpression.RParToken, policy.SpacesWithinSizeOfParentheses);
			base.VisitSizeOfExpression(sizeOfExpression);
		}

		public override void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			ForceSpacesBefore(typeOfExpression.LParToken, policy.SpaceBeforeTypeOfParentheses);
			ForceSpacesAfter(typeOfExpression.LParToken, policy.SpacesWithinTypeOfParentheses);
			ForceSpacesBefore(typeOfExpression.RParToken, policy.SpacesWithinTypeOfParentheses);
			base.VisitTypeOfExpression(typeOfExpression);
		}

		public override void VisitCheckedExpression(CheckedExpression checkedExpression)
		{
			ForceSpacesAfter(checkedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBefore(checkedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			base.VisitCheckedExpression(checkedExpression);
		}

		public override void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
		{
			ForceSpacesAfter(uncheckedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBefore(uncheckedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			base.VisitUncheckedExpression(uncheckedExpression);
		}

		public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			ForceSpacesBefore(objectCreateExpression.LParToken, policy.SpaceBeforeNewParentheses);
			
			if (objectCreateExpression.Arguments.Any()) {
				if (!objectCreateExpression.LParToken.IsNull)
					ForceSpacesAfter(objectCreateExpression.LParToken, policy.SpacesWithinNewParentheses);
			} else {
				if (!objectCreateExpression.LParToken.IsNull)
					ForceSpacesAfter(objectCreateExpression.LParToken, policy.SpacesBetweenEmptyNewParentheses);
			}

			if (!objectCreateExpression.Type.IsNull)
				objectCreateExpression.Type.AcceptVisitor(this);

			FormatArguments(objectCreateExpression);

		}

		public override void VisitArrayCreateExpression(ArrayCreateExpression arrayObjectCreateExpression)
		{
			FormatCommas(arrayObjectCreateExpression, policy.SpaceBeforeMethodCallParameterComma, policy.SpaceAfterMethodCallParameterComma);
			base.VisitArrayCreateExpression(arrayObjectCreateExpression);
		}

		public override void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			if (DoWrap(policy.ArrayInitializerWrapping, arrayInitializerExpression.RBraceToken, arrayInitializerExpression.Elements.Count)) {
				EnforceBraceStyle(policy.ArrayInitializerBraceStyle, arrayInitializerExpression.LBraceToken, arrayInitializerExpression.RBraceToken);
				curIndent.Push(IndentType.Block);
				foreach (var init in arrayInitializerExpression.Elements) {
					FixStatementIndentation(init.StartLocation);
					init.AcceptVisitor(this);
				}
				curIndent.Pop();
			} else if (policy.ArrayInitializerWrapping == Wrapping.DoNotWrap) {
				ForceSpacesBeforeRemoveNewLines(arrayInitializerExpression.LBraceToken);
				ForceSpacesBeforeRemoveNewLines(arrayInitializerExpression.RBraceToken);
				foreach (var init in arrayInitializerExpression.Elements) {
					ForceSpacesBeforeRemoveNewLines(init);
					init.AcceptVisitor(this);
				}
			} else {
				base.VisitArrayInitializerExpression(arrayInitializerExpression);
			}
		}

		public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			ForceSpacesAfter(lambdaExpression.ArrowToken, true);
			ForceSpacesBefore(lambdaExpression.ArrowToken, true);

			base.VisitLambdaExpression(lambdaExpression);
		}

		public override void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			ForceSpacesAfter(namedArgumentExpression.ColonToken, policy.SpaceInNamedArgumentAfterDoubleColon);
			base.VisitNamedArgumentExpression(namedArgumentExpression);
		}

		public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			ForceSpacesAfter(memberReferenceExpression.DotToken, false);
			base.VisitMemberReferenceExpression(memberReferenceExpression);
		}

		#endregion
		
		void ForceSpaceBefore(int offset, bool forceSpace)
		{
			bool insertedSpace = false;
			do {
				char ch = document.GetCharAt(offset);
				//Console.WriteLine (ch);
				if (!IsSpacing(ch) && (insertedSpace || !forceSpace)) {
					break;
				}
				if (ch == ' ' && forceSpace) {
					if (insertedSpace) {
						AddChange(offset, 1, null);
					} else {
						insertedSpace = true;
					}
				} else if (forceSpace) {
					if (!insertedSpace) {
						AddChange(offset, IsSpacing(ch) ? 1 : 0, " ");
						insertedSpace = true;
					} else if (IsSpacing(ch)) {
						AddChange(offset, 1, null);
					}
				}

				offset--;
			} while (offset >= 0);
		}

		

		/*
		int GetLastNonWsChar (LineSegment line, int lastColumn)
		{
			int result = -1;
			bool inComment = false;
			for (int i = 0; i < lastColumn; i++) {
				char ch = data.GetCharAt (line.Offset + i);
				if (Char.IsWhiteSpace (ch))
					continue;
				if (ch == '/' && i + 1 < line.EditableLength && data.GetCharAt (line.Offset + i + 1) == '/')
					return result;
				if (ch == '/' && i + 1 < line.EditableLength && data.GetCharAt (line.Offset + i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < line.EditableLength && data.GetCharAt (line.Offset + i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment)
					result = i;
			}
			return result;
		}
		 */
		
		
		public void FixSemicolon(CSharpTokenNode semicolon)
		{
			if (semicolon.IsNull) {
				return;
			}
			int endOffset = document.GetOffset(semicolon.StartLocation);
			int offset = endOffset;
			while (offset - 1 > 0 && char.IsWhiteSpace (document.GetCharAt (offset - 1))) {
				offset--;
			}
			if (offset < endOffset) {
				AddChange(offset, endOffset - offset, null);
			}
		}

		void PlaceOnNewLine(NewLinePlacement newLine, AstNode keywordNode)
		{
			if (keywordNode == null || newLine == NewLinePlacement.DoNotCare) {
				return;
			}

			var prev = keywordNode.GetPrevNode ();
			if (prev is Comment || prev is PreProcessorDirective)
				return;

			int offset = document.GetOffset(keywordNode.StartLocation);
			
			int whitespaceStart = SearchWhitespaceStart(offset);
			string indentString = newLine == NewLinePlacement.NewLine ? this.options.EolMarker + this.curIndent.IndentString : " ";
			AddChange(whitespaceStart, offset - whitespaceStart, indentString);
		}

		string nextStatementIndent = null;

		void FixStatementIndentation(TextLocation location)
		{
			int offset = document.GetOffset(location);
			if (offset <= 0) {
				Console.WriteLine("possible wrong offset");
				Console.WriteLine(Environment.StackTrace);
				return;
			}
			bool isEmpty = IsLineIsEmptyUpToEol(offset);
			int lineStart = SearchWhitespaceLineStart(offset);
			string indentString = nextStatementIndent == null ? (isEmpty ? "" : this.options.EolMarker) + this.curIndent.IndentString : nextStatementIndent;
			nextStatementIndent = null;
			AddChange(lineStart, offset - lineStart, indentString);
		}

		void FixIndentation(TextLocation location)
		{
			FixIndentation(location, 0);
		}

		void FixIndentation(TextLocation location, int relOffset)
		{
			if (location.Line < 1 || location.Line > document.LineCount) {
				Console.WriteLine("Invalid location " + location);
				Console.WriteLine(Environment.StackTrace);
				return;
			}
			
			string lineIndent = GetIndentation(location.Line);
			string indentString = this.curIndent.IndentString;
			if (indentString != lineIndent && location.Column - 1 + relOffset == lineIndent.Length) {
				AddChange(document.GetOffset(location.Line, 1), lineIndent.Length, indentString);
			}
		}

		void FixIndentationForceNewLine(TextLocation location)
		{
			string lineIndent = GetIndentation(location.Line);
			string indentString = this.curIndent.IndentString;
			if (location.Column - 1 == lineIndent.Length) {
				AddChange(document.GetOffset(location.Line, 1), lineIndent.Length, indentString);
			} else {
				int offset = document.GetOffset(location);
				int start = SearchWhitespaceLineStart(offset);
				if (start > 0) {
					char ch = document.GetCharAt(start - 1);
					if (ch == '\n') {
						start--;
						if (start > 1 && document.GetCharAt(start - 1) == '\r') {
							start--;
						}
					} else if (ch == '\r') {
						start--;
					}
					AddChange(start, offset - start, this.options.EolMarker + indentString);
				}
			}
		}
		
		string GetIndentation(int lineNumber)
		{
			IDocumentLine line = document.GetLineByNumber(lineNumber);
			StringBuilder b = new StringBuilder ();
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
	}
}

