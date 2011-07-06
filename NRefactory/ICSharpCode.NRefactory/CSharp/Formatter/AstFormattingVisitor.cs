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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp
{
	public class AstFormattingVisitor : DepthFirstAstVisitor<object, object>
	{
		CSharpFormattingOptions policy;
		ITextEditorAdapter data;
		IActionFactory factory;
		List<TextReplaceAction> changes = new List<TextReplaceAction> ();
		Indent curIndent = new Indent ();

		public int IndentLevel {
			get {
				return curIndent.Level;
			}
			set {
				curIndent.Level = value;
			}
		}

		public int CurrentSpaceIndents {
			get;
			set;
		}

		public List<TextReplaceAction> Changes {
			get { return this.changes; }
		}

		public bool CorrectBlankLines {
			get;
			set;
		}

		public bool HadErrors {
			get;
			set;
		}

		public AstFormattingVisitor (CSharpFormattingOptions policy, ITextEditorAdapter data, IActionFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");
			this.policy = policy;
			this.data = data;
			this.curIndent.TabsToSpaces = this.data.TabsToSpaces;
			this.curIndent.TabSize = this.data.TabSize;
			this.factory = factory;
			CorrectBlankLines = true;
		}

		public override object VisitCompilationUnit (CompilationUnit unit, object data)
		{
			base.VisitCompilationUnit (unit, data);
			return null;
		}

		public void EnsureBlankLinesAfter (AstNode node, int blankLines)
		{
			if (!CorrectBlankLines)
				return;
			var loc = node.EndLocation;
			int line = loc.Line;
			do {
				line++;
			} while (line < data.LineCount && data.GetEditableLength (line) == data.GetIndentation (line).Length);
			var start = data.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
			
			int foundBlankLines = line - loc.Line - 1;
			
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < blankLines - foundBlankLines; i++)
				sb.Append (data.EolMarker);
			
			int ws = start;
			while (ws < data.Length && IsSpacing (data.GetCharAt (ws)))
				ws++;
			int removedChars = ws - start;
			if (foundBlankLines > blankLines)
				removedChars += data.GetLineEndOffset (loc.Line + foundBlankLines - blankLines) - data.GetLineEndOffset (loc.Line);
			AddChange (start, removedChars, sb.ToString ());
		}

		public void EnsureBlankLinesBefore (AstNode node, int blankLines)
		{
			if (!CorrectBlankLines)
				return;
			var loc = node.StartLocation;
			int line = loc.Line;
			do {
				line--;
			} while (line > 0 && data.GetEditableLength (line) == data.GetIndentation (line).Length);
			int end = data.GetLineOffset (loc.Line);
			int start = line >= 1 ? data.GetLineEndOffset (line) : 0;
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < blankLines; i++)
				sb.Append (data.EolMarker);
			AddChange (start, end - start, sb.ToString ());
		}

		public override object VisitUsingDeclaration (UsingDeclaration usingDeclaration, object data)
		{
			if (!(usingDeclaration.PrevSibling is UsingDeclaration || usingDeclaration.PrevSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesBefore (usingDeclaration, policy.BlankLinesBeforeUsings);
			if (!(usingDeclaration.NextSibling is UsingDeclaration || usingDeclaration.NextSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesAfter (usingDeclaration, policy.BlankLinesAfterUsings);

			return null;
		}

		public override object VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration, object data)
		{
			if (!(usingDeclaration.PrevSibling is UsingDeclaration || usingDeclaration.PrevSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesBefore (usingDeclaration, policy.BlankLinesBeforeUsings);
			if (!(usingDeclaration.NextSibling is UsingDeclaration || usingDeclaration.NextSibling  is UsingAliasDeclaration)) 
				EnsureBlankLinesAfter (usingDeclaration, policy.BlankLinesAfterUsings);
			return null;
		}

		public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
		{
			var firstNsMember = namespaceDeclaration.Members.FirstOrDefault ();
			if (firstNsMember != null)
				EnsureBlankLinesBefore (firstNsMember, policy.BlankLinesBeforeFirstDeclaration);
			FixIndentationForceNewLine (namespaceDeclaration.StartLocation);
			EnforceBraceStyle (policy.NamespaceBraceStyle, namespaceDeclaration.LBraceToken, namespaceDeclaration.RBraceToken);
			if (policy.IndentNamespaceBody)
				IndentLevel++;
			object result = base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			if (policy.IndentNamespaceBody)
				IndentLevel--;
			FixIndentation (namespaceDeclaration.RBraceToken.StartLocation);
			return result;
		}

		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			FixIndentationForceNewLine (typeDeclaration.StartLocation);
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
				throw new InvalidOperationException ("unsupported class type : " + typeDeclaration.ClassType);
			}
			EnforceBraceStyle (braceStyle, typeDeclaration.LBraceToken, typeDeclaration.RBraceToken);
			
			if (indentBody)
				IndentLevel++;
			object result = base.VisitTypeDeclaration (typeDeclaration, data);
			if (indentBody)
				IndentLevel--;
			
			if (typeDeclaration.NextSibling is TypeDeclaration || typeDeclaration.NextSibling is DelegateDeclaration)
				EnsureBlankLinesAfter (typeDeclaration, policy.BlankLinesBetweenTypes);
			return result;
		}

		bool IsSimpleAccessor (Accessor accessor)
		{
			if (accessor.IsNull || accessor.Body.IsNull || accessor.Body.FirstChild == null)
				return true;
			if (accessor.Body.Statements.Count () != 1)
				return false;
			return !(accessor.Body.Statements.FirstOrDefault () is BlockStatement);
			
		}

		bool IsSpacing (char ch)
		{
			return ch == ' ' || ch == '\t';
		}

		int SearchLastNonWsChar (int startOffset, int endOffset)
		{
			startOffset = System.Math.Max (0, startOffset);
			endOffset = System.Math.Max (startOffset, endOffset);
			if (startOffset >= endOffset)
				return startOffset;
			int result = -1;
			bool inComment = false;
			
			for (int i = startOffset; i < endOffset && i < data.Length; i++) {
				char ch = data.GetCharAt (i);
				if (IsSpacing (ch))
					continue;
				if (ch == '/' && i + 1 < data.Length && data.GetCharAt (i + 1) == '/')
					return result;
				if (ch == '/' && i + 1 < data.Length && data.GetCharAt (i + 1) == '*') {
					inComment = true;
					i++;
					continue;
				}
				if (inComment && ch == '*' && i + 1 < data.Length && data.GetCharAt (i + 1) == '/') {
					inComment = false;
					i++;
					continue;
				}
				if (!inComment)
					result = i;
			}
			return result;
		}

		void ForceSpace (int startOffset, int endOffset, bool forceSpace)
		{
			int lastNonWs = SearchLastNonWsChar (startOffset, endOffset);
			AddChange (lastNonWs + 1, System.Math.Max (0, endOffset - lastNonWs - 1), forceSpace ? " " : "");
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
		
		void ForceSpacesAfter (AstNode n, bool forceSpaces)
		{
			if (n == null)
				return;
			AstLocation location = n.EndLocation;
			int offset = data.LocationToOffset (location.Line, location.Column);
			if (location.Column > data.GetEditableLength (location.Line))
				return;
			int i = offset;
			while (i < data.Length && IsSpacing (data.GetCharAt (i))) {
				i++;
			}
			ForceSpace (offset - 1, i, forceSpaces);
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
		
		int ForceSpacesBefore (AstNode n, bool forceSpaces)
		{
			if (n == null || n.IsNull)
				return 0;
			AstLocation location = n.StartLocation;
			// respect manual line breaks.
			if (location.Column <= 1 || data.GetIndentation (location.Line).Length == location.Column - 1)
				return 0;
	
			int offset = data.LocationToOffset (location.Line, location.Column);
			int i = offset - 1;
			while (i >= 0 && IsSpacing (data.GetCharAt (i))) {
				i--;
			}
			ForceSpace (i, offset, forceSpaces);
			return i;
		}

		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			FixIndentationForceNewLine (propertyDeclaration.StartLocation);
			bool oneLine = false;
			switch (policy.PropertyFormatting) {
			case PropertyFormatting.AllowOneLine:
				bool isSimple = IsSimpleAccessor (propertyDeclaration.Getter) && IsSimpleAccessor (propertyDeclaration.Setter);
				if (!isSimple || propertyDeclaration.LBraceToken.StartLocation.Line != propertyDeclaration.RBraceToken.StartLocation.Line) {
					EnforceBraceStyle (policy.PropertyBraceStyle, propertyDeclaration.LBraceToken, propertyDeclaration.RBraceToken);
				} else {
					ForceSpacesBefore (propertyDeclaration.Getter, true);
					ForceSpacesBefore (propertyDeclaration.Setter, true);
					ForceSpacesBefore (propertyDeclaration.RBraceToken, true);
					oneLine = true;
				}
				break;
			case PropertyFormatting.ForceNewLine:
				EnforceBraceStyle (policy.PropertyBraceStyle, propertyDeclaration.LBraceToken, propertyDeclaration.RBraceToken);
				break;
			case PropertyFormatting.ForceOneLine:
				isSimple = IsSimpleAccessor (propertyDeclaration.Getter) && IsSimpleAccessor (propertyDeclaration.Setter);
				if (isSimple) {
					int offset = this.data.LocationToOffset (propertyDeclaration.LBraceToken.StartLocation.Line, propertyDeclaration.LBraceToken.StartLocation.Column);
					
					int start = SearchWhitespaceStart (offset);
					int end = SearchWhitespaceEnd (offset);
					AddChange (start, offset - start, " ");
					AddChange (offset + 1, end - offset - 2, " ");
					
					offset = this.data.LocationToOffset (propertyDeclaration.RBraceToken.StartLocation.Line, propertyDeclaration.RBraceToken.StartLocation.Column);
					start = SearchWhitespaceStart (offset);
					AddChange (start, offset - start, " ");
					oneLine = true;
				
				} else {
					EnforceBraceStyle (policy.PropertyBraceStyle, propertyDeclaration.LBraceToken, propertyDeclaration.RBraceToken);
				}
				break;
			}
			if (policy.IndentPropertyBody)
				IndentLevel++;
			///System.Console.WriteLine ("one line: " + oneLine);
			if (!propertyDeclaration.Getter.IsNull) {
				if (!oneLine) {
					if (!IsLineIsEmptyUpToEol (propertyDeclaration.Getter.StartLocation)) {
						int offset = this.data.LocationToOffset (propertyDeclaration.Getter.StartLocation.Line, propertyDeclaration.Getter.StartLocation.Column);
						int start = SearchWhitespaceStart (offset);
						string indentString = this.curIndent.IndentString;
						AddChange (start, offset - start, this.data.EolMarker + indentString);
					} else {
						FixIndentation (propertyDeclaration.Getter.StartLocation);
					}
				} else {
					int offset = this.data.LocationToOffset (propertyDeclaration.Getter.StartLocation.Line, propertyDeclaration.Getter.StartLocation.Column);
					int start = SearchWhitespaceStart (offset);
					AddChange (start, offset - start, " ");
					
					ForceSpacesBefore (propertyDeclaration.Getter.Body.LBraceToken, true);
					ForceSpacesBefore (propertyDeclaration.Getter.Body.RBraceToken, true);
				}
				if (!propertyDeclaration.Getter.Body.IsNull) {
					if (!policy.AllowPropertyGetBlockInline || propertyDeclaration.Getter.Body.LBraceToken.StartLocation.Line != propertyDeclaration.Getter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertyGetBraceStyle, propertyDeclaration.Getter.Body.LBraceToken, propertyDeclaration.Getter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (propertyDeclaration.Getter.Body, policy.IndentBlocks, data);
				}
			}
			
			if (!propertyDeclaration.Setter.IsNull) {
				if (!oneLine) {
					if (!IsLineIsEmptyUpToEol (propertyDeclaration.Setter.StartLocation)) {
						int offset = this.data.LocationToOffset (propertyDeclaration.Setter.StartLocation.Line, propertyDeclaration.Setter.StartLocation.Column);
						int start = SearchWhitespaceStart (offset);
						string indentString = this.curIndent.IndentString;
						AddChange (start, offset - start, this.data.EolMarker + indentString);
					} else {
						FixIndentation (propertyDeclaration.Setter.StartLocation);
					}
				} else {
					int offset = this.data.LocationToOffset (propertyDeclaration.Setter.StartLocation.Line, propertyDeclaration.Setter.StartLocation.Column);
					int start = SearchWhitespaceStart (offset);
					AddChange (start, offset - start, " ");
					
					ForceSpacesBefore (propertyDeclaration.Setter.Body.LBraceToken, true);
					ForceSpacesBefore (propertyDeclaration.Setter.Body.RBraceToken, true);
				}
				if (!propertyDeclaration.Setter.Body.IsNull) {
					if (!policy.AllowPropertySetBlockInline || propertyDeclaration.Setter.Body.LBraceToken.StartLocation.Line != propertyDeclaration.Setter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertySetBraceStyle, propertyDeclaration.Setter.Body.LBraceToken, propertyDeclaration.Setter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (propertyDeclaration.Setter.Body, policy.IndentBlocks, data);
				}
			}
			
			if (policy.IndentPropertyBody)
				IndentLevel--;
			if (IsMember (propertyDeclaration.NextSibling))
				EnsureBlankLinesAfter (propertyDeclaration, policy.BlankLinesBetweenMembers);
			return null;
		}

		public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			ForceSpacesBefore (indexerDeclaration.LBracketToken, policy.SpaceBeforeIndexerDeclarationBracket);
			ForceSpacesAfter (indexerDeclaration.LBracketToken, policy.SpaceWithinIndexerDeclarationBracket);
			ForceSpacesBefore (indexerDeclaration.RBracketToken, policy.SpaceWithinIndexerDeclarationBracket);

			FormatCommas (indexerDeclaration, policy.SpaceBeforeIndexerDeclarationParameterComma, policy.SpaceAfterIndexerDeclarationParameterComma);

			
			FixIndentationForceNewLine (indexerDeclaration.StartLocation);
			EnforceBraceStyle (policy.PropertyBraceStyle, indexerDeclaration.LBraceToken, indexerDeclaration.RBraceToken);
			if (policy.IndentPropertyBody)
				IndentLevel++;
			
			if (!indexerDeclaration.Getter.IsNull) {
				FixIndentation (indexerDeclaration.Getter.StartLocation);
				if (!indexerDeclaration.Getter.Body.IsNull) {
					if (!policy.AllowPropertyGetBlockInline || indexerDeclaration.Getter.Body.LBraceToken.StartLocation.Line != indexerDeclaration.Getter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertyGetBraceStyle, indexerDeclaration.Getter.Body.LBraceToken, indexerDeclaration.Getter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (indexerDeclaration.Getter.Body, policy.IndentBlocks, data);
				}
			}
			
			if (!indexerDeclaration.Setter.IsNull) {
				FixIndentation (indexerDeclaration.Setter.StartLocation);
				if (!indexerDeclaration.Setter.Body.IsNull) {
					if (!policy.AllowPropertySetBlockInline || indexerDeclaration.Setter.Body.LBraceToken.StartLocation.Line != indexerDeclaration.Setter.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle (policy.PropertySetBraceStyle, indexerDeclaration.Setter.Body.LBraceToken, indexerDeclaration.Setter.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (indexerDeclaration.Setter.Body, policy.IndentBlocks, data);
				}
			}
			if (policy.IndentPropertyBody)
				IndentLevel--;
			if (IsMember (indexerDeclaration.NextSibling))
				EnsureBlankLinesAfter (indexerDeclaration, policy.BlankLinesBetweenMembers);
			return null;
		}

		static bool IsSimpleEvent (AstNode node)
		{
			return node is EventDeclaration;
		}

		public override object VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, object data)
		{
			FixIndentationForceNewLine (eventDeclaration.StartLocation);
			EnforceBraceStyle (policy.EventBraceStyle, eventDeclaration.LBraceToken, eventDeclaration.RBraceToken);
			if (policy.IndentEventBody)
				IndentLevel++;
			
			if (!eventDeclaration.AddAccessor.IsNull) {
				FixIndentation (eventDeclaration.AddAccessor.StartLocation);
				if (!eventDeclaration.AddAccessor.Body.IsNull) {
					if (!policy.AllowEventAddBlockInline || eventDeclaration.AddAccessor.Body.LBraceToken.StartLocation.Line != eventDeclaration.AddAccessor.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle (policy.EventAddBraceStyle, eventDeclaration.AddAccessor.Body.LBraceToken, eventDeclaration.AddAccessor.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					
					VisitBlockWithoutFixIndentation (eventDeclaration.AddAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (!eventDeclaration.RemoveAccessor.IsNull) {
				FixIndentation (eventDeclaration.RemoveAccessor.StartLocation);
				if (!eventDeclaration.RemoveAccessor.Body.IsNull) {
					if (!policy.AllowEventRemoveBlockInline || eventDeclaration.RemoveAccessor.Body.LBraceToken.StartLocation.Line != eventDeclaration.RemoveAccessor.Body.RBraceToken.StartLocation.Line) {
						EnforceBraceStyle (policy.EventRemoveBraceStyle, eventDeclaration.RemoveAccessor.Body.LBraceToken, eventDeclaration.RemoveAccessor.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
					}
					VisitBlockWithoutFixIndentation (eventDeclaration.RemoveAccessor.Body, policy.IndentBlocks, data);
				}
			}
			
			if (policy.IndentEventBody)
				IndentLevel--;
			
			if (eventDeclaration.NextSibling is EventDeclaration && IsSimpleEvent (eventDeclaration) && IsSimpleEvent (eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (eventDeclaration, policy.BlankLinesBetweenEventFields);
			} else if (IsMember (eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (eventDeclaration, policy.BlankLinesBetweenMembers);
			}
			return null;
		}

		public override object VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			FixIndentationForceNewLine (eventDeclaration.StartLocation);
			if (eventDeclaration.NextSibling is EventDeclaration && IsSimpleEvent (eventDeclaration) && IsSimpleEvent (eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (eventDeclaration, policy.BlankLinesBetweenEventFields);
			} else if (IsMember (eventDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (eventDeclaration, policy.BlankLinesBetweenMembers);
			}
			return null;
		}

		public override object VisitAccessor (Accessor accessor, object data)
		{
			FixIndentationForceNewLine (accessor.StartLocation);
			object result = base.VisitAccessor (accessor, data);
			return result;
		}

		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			FixIndentationForceNewLine (fieldDeclaration.StartLocation);
			FormatCommas (fieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);
			if (fieldDeclaration.NextSibling is FieldDeclaration || fieldDeclaration.NextSibling is FixedFieldDeclaration) {
				EnsureBlankLinesAfter (fieldDeclaration, policy.BlankLinesBetweenFields);
			} else if (IsMember (fieldDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (fieldDeclaration, policy.BlankLinesBetweenMembers);
			}
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}

		public override object VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			FixIndentationForceNewLine (fixedFieldDeclaration.StartLocation);
			FormatCommas (fixedFieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);
			if (fixedFieldDeclaration.NextSibling is FieldDeclaration || fixedFieldDeclaration.NextSibling is FixedFieldDeclaration) {
				EnsureBlankLinesAfter (fixedFieldDeclaration, policy.BlankLinesBetweenFields);
			} else if (IsMember (fixedFieldDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (fixedFieldDeclaration, policy.BlankLinesBetweenMembers);
			}
			return base.VisitFixedFieldDeclaration (fixedFieldDeclaration, data);
		}

		public override object VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			FixIndentationForceNewLine (enumMemberDeclaration.StartLocation);
			return base.VisitEnumMemberDeclaration (enumMemberDeclaration, data);
		}

		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			FixIndentation (delegateDeclaration.StartLocation);
			
			ForceSpacesBefore (delegateDeclaration.LParToken, policy.SpaceBeforeDelegateDeclarationParentheses);
			if (delegateDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (delegateDeclaration.LParToken, policy.SpaceWithinDelegateDeclarationParentheses);
				ForceSpacesBefore (delegateDeclaration.RParToken, policy.SpaceWithinDelegateDeclarationParentheses);
			} else {
				ForceSpacesAfter (delegateDeclaration.LParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
				ForceSpacesBefore (delegateDeclaration.RParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
			}
			FormatCommas (delegateDeclaration, policy.SpaceBeforeDelegateDeclarationParameterComma, policy.SpaceAfterDelegateDeclarationParameterComma);

			if (delegateDeclaration.NextSibling is TypeDeclaration || delegateDeclaration.NextSibling is DelegateDeclaration) {
				EnsureBlankLinesAfter (delegateDeclaration, policy.BlankLinesBetweenTypes);
			} else if (IsMember (delegateDeclaration.NextSibling)) {
				EnsureBlankLinesAfter (delegateDeclaration, policy.BlankLinesBetweenMembers);
			}

			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}

		static bool IsMember (AstNode nextSibling)
		{
			return nextSibling != null && nextSibling.NodeType == NodeType.Member;
		}

		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			FixIndentationForceNewLine (methodDeclaration.StartLocation);
			
			ForceSpacesBefore (methodDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (methodDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (methodDeclaration.LParToken, policy.SpaceWithinMethodDeclarationParentheses);
				ForceSpacesBefore (methodDeclaration.RParToken, policy.SpaceWithinMethodDeclarationParentheses);
			} else {
				ForceSpacesAfter (methodDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore (methodDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}
			FormatCommas (methodDeclaration, policy.SpaceBeforeMethodDeclarationParameterComma, policy.SpaceAfterMethodDeclarationParameterComma);

			if (!methodDeclaration.Body.IsNull) {
				EnforceBraceStyle (policy.MethodBraceStyle, methodDeclaration.Body.LBraceToken, methodDeclaration.Body.RBraceToken);
				if (policy.IndentMethodBody)
					IndentLevel++;
				base.VisitBlockStatement (methodDeclaration.Body, data);
				if (policy.IndentMethodBody)
					IndentLevel--;
			}
			if (IsMember (methodDeclaration.NextSibling))
				EnsureBlankLinesAfter (methodDeclaration, policy.BlankLinesBetweenMembers);

			return null;
		}

		public override object VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
		{
			FixIndentationForceNewLine (operatorDeclaration.StartLocation);
			
			ForceSpacesBefore (operatorDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (operatorDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (operatorDeclaration.LParToken, policy.SpaceWithinMethodDeclarationParentheses);
				ForceSpacesBefore (operatorDeclaration.RParToken, policy.SpaceWithinMethodDeclarationParentheses);
			} else {
				ForceSpacesAfter (operatorDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore (operatorDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}
			FormatCommas (operatorDeclaration, policy.SpaceBeforeMethodDeclarationParameterComma, policy.SpaceAfterMethodDeclarationParameterComma);

			if (!operatorDeclaration.Body.IsNull) {
				EnforceBraceStyle (policy.MethodBraceStyle, operatorDeclaration.Body.LBraceToken, operatorDeclaration.Body.RBraceToken);
				if (policy.IndentMethodBody)
					IndentLevel++;
				base.VisitBlockStatement (operatorDeclaration.Body, data);
				if (policy.IndentMethodBody)
					IndentLevel--;
			}
			if (IsMember (operatorDeclaration.NextSibling))
				EnsureBlankLinesAfter (operatorDeclaration, policy.BlankLinesBetweenMembers);
			
			return null;
		}

		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			FixIndentationForceNewLine (constructorDeclaration.StartLocation);
			
			ForceSpacesBefore (constructorDeclaration.LParToken, policy.SpaceBeforeConstructorDeclarationParentheses);
			if (constructorDeclaration.Parameters.Any ()) {
				ForceSpacesAfter (constructorDeclaration.LParToken, policy.SpaceWithinConstructorDeclarationParentheses);
				ForceSpacesBefore (constructorDeclaration.RParToken, policy.SpaceWithinConstructorDeclarationParentheses);
			} else {
				ForceSpacesAfter (constructorDeclaration.LParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
				ForceSpacesBefore (constructorDeclaration.RParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
			}
			FormatCommas (constructorDeclaration, policy.SpaceBeforeConstructorDeclarationParameterComma, policy.SpaceAfterConstructorDeclarationParameterComma);
		
			object result = null;
			if (!constructorDeclaration.Body.IsNull) {
				EnforceBraceStyle (policy.ConstructorBraceStyle, constructorDeclaration.Body.LBraceToken, constructorDeclaration.Body.RBraceToken);
				if (policy.IndentMethodBody)
					IndentLevel++;
				result = base.VisitBlockStatement (constructorDeclaration.Body, data);
				if (policy.IndentMethodBody)
					IndentLevel--;
			}
			if (IsMember (constructorDeclaration.NextSibling))
				EnsureBlankLinesAfter (constructorDeclaration, policy.BlankLinesBetweenMembers);
			return result;
		}

		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			FixIndentationForceNewLine (destructorDeclaration.StartLocation);
			
			CSharpTokenNode lParen = destructorDeclaration.LParToken;
			int offset = this.data.LocationToOffset (lParen.StartLocation.Line, lParen.StartLocation.Column);
			ForceSpaceBefore (offset, policy.SpaceBeforeConstructorDeclarationParentheses);
			
			object result = null;
			if (!destructorDeclaration.Body.IsNull) {
				EnforceBraceStyle (policy.DestructorBraceStyle, destructorDeclaration.Body.LBraceToken, destructorDeclaration.Body.RBraceToken);
				if (policy.IndentMethodBody)
					IndentLevel++;
				result = base.VisitBlockStatement (destructorDeclaration.Body, data);
				if (policy.IndentMethodBody)
					IndentLevel--;
			}
			if (IsMember (destructorDeclaration.NextSibling))
				EnsureBlankLinesAfter (destructorDeclaration, policy.BlankLinesBetweenMembers);
			return result;
		}

		#region Statements
		public override object VisitExpressionStatement (ExpressionStatement expressionStatement, object data)
		{
			FixStatementIndentation (expressionStatement.StartLocation);
			FixSemicolon (expressionStatement.SemicolonToken);
			return base.VisitExpressionStatement (expressionStatement, data);
		}

		object VisitBlockWithoutFixIndentation (BlockStatement blockStatement, bool indent, object data)
		{
			if (indent)
				IndentLevel++;
			object result = base.VisitBlockStatement (blockStatement, data);
			if (indent)
				IndentLevel--;
			return result;
		}

		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			FixIndentation (blockStatement.StartLocation);
			object result = VisitBlockWithoutFixIndentation (blockStatement, policy.IndentBlocks, data);
			FixIndentation (blockStatement.EndLocation, -1);
			return result;
		}

		public override object VisitComment (Comment comment, object data)
		{
			if (comment.StartsLine && !HadErrors && comment.StartLocation.Column > 1)
				FixIndentation (comment.StartLocation);
			return null;
		}

		public override object VisitBreakStatement (BreakStatement breakStatement, object data)
		{
			FixStatementIndentation (breakStatement.StartLocation);
			return null;
		}

		public override object VisitCheckedStatement (CheckedStatement checkedStatement, object data)
		{
			FixStatementIndentation (checkedStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, checkedStatement.Body);
		}

		public override object VisitContinueStatement (ContinueStatement continueStatement, object data)
		{
			FixStatementIndentation (continueStatement.StartLocation);
			return null;
		}

		public override object VisitEmptyStatement (EmptyStatement emptyStatement, object data)
		{
			FixStatementIndentation (emptyStatement.StartLocation);
			return null;
		}

		public override object VisitFixedStatement (FixedStatement fixedStatement, object data)
		{
			FixStatementIndentation (fixedStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, fixedStatement.EmbeddedStatement);
		}

		public override object VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			FixStatementIndentation (foreachStatement.StartLocation);
			ForceSpacesBefore (foreachStatement.LParToken, policy.SpaceBeforeForeachParentheses);

			ForceSpacesAfter (foreachStatement.LParToken, policy.SpacesWithinForeachParentheses);
			ForceSpacesBefore (foreachStatement.RParToken, policy.SpacesWithinForeachParentheses);

			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.ForEachBraceForcement, foreachStatement.EmbeddedStatement);
		}

		object FixEmbeddedStatment (BraceStyle braceStyle, BraceForcement braceForcement, AstNode node)
		{
			return FixEmbeddedStatment (braceStyle, braceForcement, null, false, node);
		}

		object FixEmbeddedStatment (BraceStyle braceStyle, BraceForcement braceForcement, CSharpTokenNode token, bool allowInLine, AstNode node)
		{
			if (node == null)
				return null;
			int originalLevel = curIndent.Level;
			bool isBlock = node is BlockStatement;
			switch (braceForcement) {
			case BraceForcement.DoNotChange:
				//nothing
				break;
			case BraceForcement.AddBraces:
				if (!isBlock) {
					AstNode n = node.Parent.GetCSharpNodeBefore (node);
					int start = data.LocationToOffset (n.EndLocation.Line, n.EndLocation.Column);
					var next = n.GetNextNode ();
					int offset = data.LocationToOffset (next.StartLocation.Line, next.StartLocation.Column);
					string startBrace = "";
					switch (braceStyle) {
					case BraceStyle.EndOfLineWithoutSpace:
						startBrace = "{";
						break;
					case BraceStyle.EndOfLine:
						startBrace = " {";
						break;
					case BraceStyle.NextLine:
						startBrace = data.EolMarker + curIndent.IndentString + "{";
						break;
					case BraceStyle.NextLineShifted2:
					case BraceStyle.NextLineShifted:
						startBrace = data.EolMarker + curIndent.IndentString + curIndent.SingleIndent + "{";
						break;
					}
					if (IsLineIsEmptyUpToEol (data.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column)))
						startBrace += data.EolMarker + data.GetIndentation (node.StartLocation.Line);
					AddChange (start, offset - start, startBrace);
				}
				break;
			case BraceForcement.RemoveBraces:
				if (isBlock) {
					BlockStatement block = node as BlockStatement;
					if (block.Statements.Count () == 1) {
						int offset1 = data.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
						int start = SearchWhitespaceStart (offset1);
						
						int offset2 = data.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
						int end = SearchWhitespaceStart (offset2 - 1);
						
						AddChange (start, offset1 - start + 1, null);
						AddChange (end + 1, offset2 - end, null);
						node = block.FirstChild;
						isBlock = false;
					}
				}
				break;
			}
			if (isBlock) {
				BlockStatement block = node as BlockStatement;
				if (allowInLine && block.StartLocation.Line == block.EndLocation.Line && block.Statements.Count () <= 1) {
					if (block.Statements.Count () == 1)
						nextStatementIndent = " ";
				} else {
					EnforceBraceStyle (braceStyle, block.LBraceToken, block.RBraceToken);
				}
				if (braceStyle == BraceStyle.NextLineShifted2)
					curIndent.Level++;
			} else {
				if (allowInLine && token.StartLocation.Line == node.EndLocation.Line) {
					nextStatementIndent = " ";
				}
			}
			if (!(policy.AlignEmbeddedIfStatements && node is IfElseStatement && node.Parent is IfElseStatement || 
				policy.AlignEmbeddedUsingStatements && node is UsingStatement && node.Parent is UsingStatement)) 
				curIndent.Level++;
			object result = isBlock ? base.VisitBlockStatement ((BlockStatement)node, null) : node.AcceptVisitor (this, null);
			curIndent.Level = originalLevel;
			switch (braceForcement) {
			case BraceForcement.DoNotChange:
				break;
			case BraceForcement.AddBraces:
				if (!isBlock) {
					int offset = data.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
					if (!char.IsWhiteSpace (data.GetCharAt (offset)))
						offset++;
					string startBrace = "";
					switch (braceStyle) {
					case BraceStyle.DoNotChange:
						startBrace = null;
						break;
					case BraceStyle.EndOfLineWithoutSpace:
						startBrace = data.EolMarker + curIndent.IndentString + "}";
						break;
					case BraceStyle.EndOfLine:
						startBrace = data.EolMarker + curIndent.IndentString + "}";
						break;
					case BraceStyle.NextLine:
						startBrace = data.EolMarker + curIndent.IndentString + "}";
						break;
					case BraceStyle.NextLineShifted2:
					case BraceStyle.NextLineShifted:
						startBrace = data.EolMarker + curIndent.IndentString + curIndent.SingleIndent + "}";
						break;
					}
					if (startBrace != null)
						AddChange (offset, 0, startBrace);
				}
				break;
			}
			return result;
		}

		void EnforceBraceStyle (BraceStyle braceStyle, AstNode lbrace, AstNode rbrace)
		{
			if (lbrace.IsNull || rbrace.IsNull)
				return;
			
//			LineSegment lbraceLineSegment = data.Document.GetLine (lbrace.StartLocation.Line);
			int lbraceOffset = data.LocationToOffset (lbrace.StartLocation.Line, lbrace.StartLocation.Column);
			
//			LineSegment rbraceLineSegment = data.Document.GetLine (rbrace.StartLocation.Line);
			int rbraceOffset = data.LocationToOffset (rbrace.StartLocation.Line, rbrace.StartLocation.Column);
			int whitespaceStart = SearchWhitespaceStart (lbraceOffset);
			int whitespaceEnd = SearchWhitespaceLineStart (rbraceOffset);
			string startIndent = "";
			string endIndent = "";
			switch (braceStyle) {
			case BraceStyle.DoNotChange:
				startIndent = endIndent = null;
				break;
			case BraceStyle.EndOfLineWithoutSpace:
				startIndent = "";
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString : data.EolMarker + curIndent.IndentString;
				break;
			case BraceStyle.EndOfLine:
				var prevNode = lbrace.GetPrevNode ();
				if (prevNode is Comment) {
					// delete old bracket
					AddChange (whitespaceStart, lbraceOffset - whitespaceStart + 1, "");
					
					while (prevNode is Comment) {
						prevNode = prevNode.GetPrevNode ();
					}
					whitespaceStart = data.LocationToOffset (prevNode.EndLocation.Line, prevNode.EndLocation.Column);
					lbraceOffset = whitespaceStart;
					startIndent = " {";
				} else {
					startIndent = " ";
				}
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString : data.EolMarker + curIndent.IndentString;
				break;
			case BraceStyle.NextLine:
				startIndent = data.EolMarker + curIndent.IndentString;
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString : data.EolMarker + curIndent.IndentString;
				break;
			case BraceStyle.NextLineShifted2:
			case BraceStyle.NextLineShifted:
				startIndent = data.EolMarker + curIndent.IndentString + curIndent.SingleIndent;
				endIndent = IsLineIsEmptyUpToEol (rbraceOffset) ? curIndent.IndentString + curIndent.SingleIndent : data.EolMarker + curIndent.IndentString + curIndent.SingleIndent;
				break;
			}
			
			if (lbraceOffset > 0 && startIndent != null)
				AddChange (whitespaceStart, lbraceOffset - whitespaceStart, startIndent);
			if (rbraceOffset > 0 && endIndent != null)
				AddChange (whitespaceEnd, rbraceOffset - whitespaceEnd, endIndent);
		}

		void AddChange (int offset, int removedChars, string insertedText)
		{
			if (changes.Any (c => c.Offset == offset && c.RemovedChars == removedChars 
				&& c.InsertedText == insertedText))
				return;
			string currentText = data.GetTextAt (offset, removedChars);
			if (currentText == insertedText)
				return;
			if (currentText.Any (c => !(char.IsWhiteSpace (c) || c == '\r' || c == '\t' || c == '{' || c == '}')))
				throw new InvalidOperationException ("Tried to remove non ws chars: '" + currentText + "'");
			foreach (var change in changes) {
				if (change.Offset == offset) {
					if (removedChars > 0 && insertedText == change.InsertedText) {
						change.RemovedChars = removedChars;
//						change.InsertedText = insertedText;
						return;
					}
					if (!string.IsNullOrEmpty (change.InsertedText)) {
						change.InsertedText += insertedText;
					} else {
						change.InsertedText = insertedText;
					}
					change.RemovedChars = System.Math.Max (removedChars, change.RemovedChars);
					return;
				}
			}
			//Console.WriteLine ("offset={0}, removedChars={1}, insertedText={2}", offset, removedChars, insertedText == null ? "<null>" : insertedText.Replace ("\n", "\\n").Replace ("\t", "\\t").Replace (" ", "."));
			//Console.WriteLine (Environment.StackTrace);
			
			changes.Add (factory.CreateTextReplaceAction (offset, removedChars, insertedText));
		}

		public bool IsLineIsEmptyUpToEol (AstLocation startLocation)
		{
			return IsLineIsEmptyUpToEol (data.LocationToOffset (startLocation.Line, startLocation.Column) - 1);
		}

		bool IsLineIsEmptyUpToEol (int startOffset)
		{
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = data.GetCharAt (offset);
				if (ch != ' ' && ch != '\t')
					return ch == '\n' || ch == '\r';
			}
			return true;
		}

		int SearchWhitespaceStart (int startOffset)
		{
			if (startOffset < 0)
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = data.GetCharAt (offset);
				if (!Char.IsWhiteSpace (ch)) {
					return offset + 1;
				}
			}
			return 0;
		}

		int SearchWhitespaceEnd (int startOffset)
		{
			if (startOffset > data.Length)
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			for (int offset = startOffset + 1; offset < data.Length; offset++) {
				char ch = data.GetCharAt (offset);
				if (!Char.IsWhiteSpace (ch)) {
					return offset + 1;
				}
			}
			return data.Length - 1;
		}

		int SearchWhitespaceLineStart (int startOffset)
		{
			if (startOffset < 0)
				throw new ArgumentOutOfRangeException ("startoffset", "value : " + startOffset);
			for (int offset = startOffset - 1; offset >= 0; offset--) {
				char ch = data.GetCharAt (offset);
				if (ch != ' ' && ch != '\t') {
					return offset + 1;
				}
			}
			return 0;
		}

		public override object VisitForStatement (ForStatement forStatement, object data)
		{
			FixStatementIndentation (forStatement.StartLocation);
			foreach (AstNode node in forStatement.Children) {
				if (node.Role == ForStatement.Roles.Semicolon) {
					if (node.NextSibling is CSharpTokenNode || node.NextSibling is EmptyStatement)
						continue;
					ForceSpacesBefore (node, policy.SpaceBeforeForSemicolon);
					ForceSpacesAfter (node, policy.SpaceAfterForSemicolon);
				}
			}

			ForceSpacesBefore (forStatement.LParToken, policy.SpaceBeforeForParentheses);

			ForceSpacesAfter (forStatement.LParToken, policy.SpacesWithinForParentheses);
			ForceSpacesBefore (forStatement.RParToken, policy.SpacesWithinForParentheses);

			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.ForBraceForcement, forStatement.EmbeddedStatement);
		}

		public override object VisitGotoStatement (GotoStatement gotoStatement, object data)
		{
			FixStatementIndentation (gotoStatement.StartLocation);
			return VisitChildren (gotoStatement, data);
		}

		public override object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
		{
			ForceSpacesBefore (ifElseStatement.LParToken, policy.SpaceBeforeIfParentheses);

			ForceSpacesAfter (ifElseStatement.LParToken, policy.SpacesWithinIfParentheses);
			ForceSpacesBefore (ifElseStatement.RParToken, policy.SpacesWithinIfParentheses);

			if (!(ifElseStatement.Parent is IfElseStatement && ((IfElseStatement)ifElseStatement.Parent).FalseStatement == ifElseStatement))
				FixStatementIndentation (ifElseStatement.StartLocation);
			
			if (!ifElseStatement.Condition.IsNull)
				ifElseStatement.Condition.AcceptVisitor (this, data);
			
			if (!ifElseStatement.TrueStatement.IsNull)
				FixEmbeddedStatment (policy.StatementBraceStyle, policy.IfElseBraceForcement, ifElseStatement.IfToken, policy.AllowIfBlockInline, ifElseStatement.TrueStatement);
			
			if (!ifElseStatement.FalseStatement.IsNull) {
				PlaceOnNewLine (policy.PlaceElseOnNewLine || !(ifElseStatement.TrueStatement is BlockStatement) && policy.IfElseBraceForcement != BraceForcement.AddBraces, ifElseStatement.ElseToken);
				var forcement = policy.IfElseBraceForcement;
				if (ifElseStatement.FalseStatement is IfElseStatement) {
					forcement = BraceForcement.DoNotChange;
					PlaceOnNewLine (policy.PlaceElseIfOnNewLine, ((IfElseStatement)ifElseStatement.FalseStatement).IfToken);
				}
				FixEmbeddedStatment (policy.StatementBraceStyle, forcement, ifElseStatement.ElseToken, policy.AllowIfBlockInline, ifElseStatement.FalseStatement);
			}
			
			return null;
		}

		public override object VisitLabelStatement (LabelStatement labelStatement, object data)
		{
			// TODO
			return VisitChildren (labelStatement, data);
		}

		public override object VisitLockStatement (LockStatement lockStatement, object data)
		{
			FixStatementIndentation (lockStatement.StartLocation);
			ForceSpacesBefore (lockStatement.LParToken, policy.SpaceBeforeLockParentheses);

			ForceSpacesAfter (lockStatement.LParToken, policy.SpacesWithinLockParentheses);
			ForceSpacesBefore (lockStatement.RParToken, policy.SpacesWithinLockParentheses);

			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, lockStatement.EmbeddedStatement);
		}

		public override object VisitReturnStatement (ReturnStatement returnStatement, object data)
		{
			FixStatementIndentation (returnStatement.StartLocation);
			return VisitChildren (returnStatement, data);
		}

		public override object VisitSwitchStatement (SwitchStatement switchStatement, object data)
		{
			FixStatementIndentation (switchStatement.StartLocation);
			ForceSpacesBefore (switchStatement.LParToken, policy.SpaceBeforeSwitchParentheses);

			ForceSpacesAfter (switchStatement.LParToken, policy.SpacesWithinSwitchParentheses);
			ForceSpacesBefore (switchStatement.RParToken, policy.SpacesWithinSwitchParentheses);

			EnforceBraceStyle (policy.StatementBraceStyle, switchStatement.LBraceToken, switchStatement.RBraceToken);
			object result = VisitChildren (switchStatement, data);
			return result;
		}

		public override object VisitSwitchSection (SwitchSection switchSection, object data)
		{
			if (policy.IndentSwitchBody)
				curIndent.Level++;
			
			foreach (CaseLabel label in switchSection.CaseLabels) {
				FixStatementIndentation (label.StartLocation);
			}
			if (policy.IndentCaseBody)
				curIndent.Level++;
			
			foreach (var stmt in switchSection.Statements) {
				stmt.AcceptVisitor (this, null);
			}
			if (policy.IndentCaseBody)
				curIndent.Level--;
				
			if (policy.IndentSwitchBody)
				curIndent.Level--;
			return null;
		}

		public override object VisitCaseLabel (CaseLabel caseLabel, object data)
		{
			// handled in switchsection
			return null;
		}

		public override object VisitThrowStatement (ThrowStatement throwStatement, object data)
		{
			FixStatementIndentation (throwStatement.StartLocation);
			return VisitChildren (throwStatement, data);
		}

		public override object VisitTryCatchStatement (TryCatchStatement tryCatchStatement, object data)
		{
			FixStatementIndentation (tryCatchStatement.StartLocation);
			
			if (!tryCatchStatement.TryBlock.IsNull)
				FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, tryCatchStatement.TryBlock);
			
			foreach (CatchClause clause in tryCatchStatement.CatchClauses) {
				PlaceOnNewLine (policy.PlaceCatchOnNewLine, clause.CatchToken);
				if (!clause.LParToken.IsNull) {
					ForceSpacesBefore (clause.LParToken, policy.SpaceBeforeCatchParentheses);

					ForceSpacesAfter (clause.LParToken, policy.SpacesWithinCatchParentheses);
					ForceSpacesBefore (clause.RParToken, policy.SpacesWithinCatchParentheses);
				}
				FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, clause.Body);
			}
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				PlaceOnNewLine (policy.PlaceFinallyOnNewLine, tryCatchStatement.FinallyToken);
				
				FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, tryCatchStatement.FinallyBlock);
			}
			
			return VisitChildren (tryCatchStatement, data);
		}

		public override object VisitCatchClause (CatchClause catchClause, object data)
		{
			// Handled in TryCatchStatement
			return null;
		}

		public override object VisitUncheckedStatement (UncheckedStatement uncheckedStatement, object data)
		{
			FixStatementIndentation (uncheckedStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.FixedBraceForcement, uncheckedStatement.Body);
		}

		public override object VisitUnsafeStatement (UnsafeStatement unsafeStatement, object data)
		{
			FixStatementIndentation (unsafeStatement.StartLocation);
			return FixEmbeddedStatment (policy.StatementBraceStyle, BraceForcement.DoNotChange, unsafeStatement.Body);
		}

		public override object VisitUsingStatement (UsingStatement usingStatement, object data)
		{
			FixStatementIndentation (usingStatement.StartLocation);
			ForceSpacesBefore (usingStatement.LParToken, policy.SpaceBeforeUsingParentheses);

			ForceSpacesAfter (usingStatement.LParToken, policy.SpacesWithinUsingParentheses);
			ForceSpacesBefore (usingStatement.RParToken, policy.SpacesWithinUsingParentheses);

			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.UsingBraceForcement, usingStatement.EmbeddedStatement);
		}

		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			if (!variableDeclarationStatement.SemicolonToken.IsNull)
				FixStatementIndentation (variableDeclarationStatement.StartLocation);
			
			if ((variableDeclarationStatement.Modifiers & Modifiers.Const) == Modifiers.Const) {
				ForceSpacesAround (variableDeclarationStatement.Type, true);
			} else {
				ForceSpacesAfter (variableDeclarationStatement.Type, true);
			}
			foreach (var initializer in variableDeclarationStatement.Variables) {
				initializer.AcceptVisitor (this, data);
			}
			FormatCommas (variableDeclarationStatement, policy.SpaceBeforeLocalVariableDeclarationComma, policy.SpaceAfterLocalVariableDeclarationComma);
			FixSemicolon (variableDeclarationStatement.SemicolonToken);
			return null;
		}

		public override object VisitDoWhileStatement (DoWhileStatement doWhileStatement, object data)
		{
			FixStatementIndentation (doWhileStatement.StartLocation);
			PlaceOnNewLine (policy.PlaceWhileOnNewLine, doWhileStatement.WhileToken);
			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.WhileBraceForcement, doWhileStatement.EmbeddedStatement);
		}

		public override object VisitWhileStatement (WhileStatement whileStatement, object data)
		{
			FixStatementIndentation (whileStatement.StartLocation);
			ForceSpacesBefore (whileStatement.LParToken, policy.SpaceBeforeWhileParentheses);

			ForceSpacesAfter (whileStatement.LParToken, policy.SpacesWithinWhileParentheses);
			ForceSpacesBefore (whileStatement.RParToken, policy.SpacesWithinWhileParentheses);

			return FixEmbeddedStatment (policy.StatementBraceStyle, policy.WhileBraceForcement, whileStatement.EmbeddedStatement);
		}

		public override object VisitYieldBreakStatement (YieldBreakStatement yieldBreakStatement, object data)
		{
			FixStatementIndentation (yieldBreakStatement.StartLocation);
			return null;
		}

		public override object VisitYieldStatement (YieldStatement yieldStatement, object data)
		{
			FixStatementIndentation (yieldStatement.StartLocation);
			return null;
		}

		public override object VisitVariableInitializer (VariableInitializer variableInitializer, object data)
		{
			if (!variableInitializer.AssignToken.IsNull)
				ForceSpacesAround (variableInitializer.AssignToken, policy.SpaceAroundAssignment);
			if (!variableInitializer.Initializer.IsNull)
				variableInitializer.Initializer.AcceptVisitor (this, data);
			return data;
		}

		#endregion
		
		#region Expressions
		public override object VisitComposedType (ComposedType composedType, object data)
		{
			var spec = composedType.ArraySpecifiers.FirstOrDefault ();
			if (spec != null)
				ForceSpacesBefore (spec.LBracketToken, policy.SpaceBeforeArrayDeclarationBrackets);

			return base.VisitComposedType (composedType, data);
		}

		public override object VisitAssignmentExpression (AssignmentExpression assignmentExpression, object data)
		{
			ForceSpacesAround (assignmentExpression.OperatorToken, policy.SpaceAroundAssignment);
			return base.VisitAssignmentExpression (assignmentExpression, data);
		}

		public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
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
			ForceSpacesAround (binaryOperatorExpression.OperatorToken, forceSpaces);

			return base.VisitBinaryOperatorExpression (binaryOperatorExpression, data);
		}

		public override object VisitConditionalExpression (ConditionalExpression conditionalExpression, object data)
		{
			ForceSpacesBefore (conditionalExpression.QuestionMarkToken, policy.SpaceBeforeConditionalOperatorCondition);
			ForceSpacesAfter (conditionalExpression.QuestionMarkToken, policy.SpaceAfterConditionalOperatorCondition);
			ForceSpacesBefore (conditionalExpression.ColonToken, policy.SpaceBeforeConditionalOperatorSeparator);
			ForceSpacesAfter (conditionalExpression.ColonToken, policy.SpaceAfterConditionalOperatorSeparator);
			return base.VisitConditionalExpression (conditionalExpression, data);
		}

		public override object VisitCastExpression (CastExpression castExpression, object data)
		{
			if (castExpression.RParToken != null) {
				ForceSpacesAfter (castExpression.LParToken, policy.SpacesWithinCastParentheses);
				ForceSpacesBefore (castExpression.RParToken, policy.SpacesWithinCastParentheses);

				ForceSpacesAfter (castExpression.RParToken, policy.SpaceAfterTypecast);
			}
			return base.VisitCastExpression (castExpression, data);
		}

		void ForceSpacesAround (AstNode node, bool forceSpaces)
		{
			if (node.IsNull)
				return;
			ForceSpacesBefore (node, forceSpaces);
			ForceSpacesAfter (node, forceSpaces);
		}

		void FormatCommas (AstNode parent, bool before, bool after)
		{
			if (parent.IsNull)
				return;
			foreach (CSharpTokenNode comma in parent.Children.Where (node => node.Role == FieldDeclaration.Roles.Comma)) {
				ForceSpacesAfter (comma, after);
				ForceSpacesBefore (comma, before);
			}
		}

		public override object VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			ForceSpacesBefore (invocationExpression.LParToken, policy.SpaceBeforeMethodCallParentheses);
			if (invocationExpression.Arguments.Any ()) {
				ForceSpacesAfter (invocationExpression.LParToken, policy.SpaceWithinMethodCallParentheses);
				ForceSpacesBefore (invocationExpression.RParToken, policy.SpaceWithinMethodCallParentheses);
			} else {
				ForceSpacesAfter (invocationExpression.LParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
				ForceSpacesBefore (invocationExpression.RParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
			}
			FormatCommas (invocationExpression, policy.SpaceBeforeMethodCallParameterComma, policy.SpaceAfterMethodCallParameterComma);

			return base.VisitInvocationExpression (invocationExpression, data);
		}

		public override object VisitIndexerExpression (IndexerExpression indexerExpression, object data)
		{
			ForceSpacesBefore (indexerExpression.LBracketToken, policy.SpacesBeforeBrackets);
			ForceSpacesAfter (indexerExpression.LBracketToken, policy.SpacesWithinBrackets);
			ForceSpacesBefore (indexerExpression.RBracketToken, policy.SpacesWithinBrackets);
			FormatCommas (indexerExpression, policy.SpaceBeforeBracketComma, policy.SpaceAfterBracketComma);

			return base.VisitIndexerExpression (indexerExpression, data);
		}

		public override object VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, object data)
		{
			ForceSpacesAfter (parenthesizedExpression.LParToken, policy.SpacesWithinParentheses);
			ForceSpacesBefore (parenthesizedExpression.RParToken, policy.SpacesWithinParentheses);
			return base.VisitParenthesizedExpression (parenthesizedExpression, data);
		}

		public override object VisitSizeOfExpression (SizeOfExpression sizeOfExpression, object data)
		{
			ForceSpacesBefore (sizeOfExpression.LParToken, policy.SpaceBeforeSizeOfParentheses);
			ForceSpacesAfter (sizeOfExpression.LParToken, policy.SpacesWithinSizeOfParentheses);
			ForceSpacesBefore (sizeOfExpression.RParToken, policy.SpacesWithinSizeOfParentheses);
			return base.VisitSizeOfExpression (sizeOfExpression, data);
		}

		public override object VisitTypeOfExpression (TypeOfExpression typeOfExpression, object data)
		{
			ForceSpacesBefore (typeOfExpression.LParToken, policy.SpaceBeforeTypeOfParentheses);
			ForceSpacesAfter (typeOfExpression.LParToken, policy.SpacesWithinTypeOfParentheses);
			ForceSpacesBefore (typeOfExpression.RParToken, policy.SpacesWithinTypeOfParentheses);
			return base.VisitTypeOfExpression (typeOfExpression, data);
		}

		public override object VisitCheckedExpression (CheckedExpression checkedExpression, object data)
		{
			ForceSpacesAfter (checkedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBefore (checkedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			return base.VisitCheckedExpression (checkedExpression, data);
		}

		public override object VisitUncheckedExpression (UncheckedExpression uncheckedExpression, object data)
		{
			ForceSpacesAfter (uncheckedExpression.LParToken, policy.SpacesWithinCheckedExpressionParantheses);
			ForceSpacesBefore (uncheckedExpression.RParToken, policy.SpacesWithinCheckedExpressionParantheses);
			return base.VisitUncheckedExpression (uncheckedExpression, data);
		}

		public override object VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, object data)
		{
			ForceSpacesBefore (objectCreateExpression.LParToken, policy.SpaceBeforeNewParentheses);
			
			if (objectCreateExpression.Arguments.Any ()) {
				if (!objectCreateExpression.LParToken.IsNull)
					ForceSpacesAfter (objectCreateExpression.LParToken, policy.SpacesWithinNewParentheses);
				if (!objectCreateExpression.RParToken.IsNull)
					ForceSpacesBefore (objectCreateExpression.RParToken, policy.SpacesWithinNewParentheses);
			} else {
				if (!objectCreateExpression.LParToken.IsNull)
					ForceSpacesAfter (objectCreateExpression.LParToken, policy.SpacesBetweenEmptyNewParentheses);
				if (!objectCreateExpression.RParToken.IsNull)
					ForceSpacesBefore (objectCreateExpression.RParToken, policy.SpacesBetweenEmptyNewParentheses);
			}
			FormatCommas (objectCreateExpression, policy.SpaceBeforeNewParameterComma, policy.SpaceAfterNewParameterComma);
			
			return base.VisitObjectCreateExpression (objectCreateExpression, data);
		}

		public override object VisitArrayCreateExpression (ArrayCreateExpression arrayObjectCreateExpression, object data)
		{
			FormatCommas (arrayObjectCreateExpression, policy.SpaceBeforeMethodCallParameterComma, policy.SpaceAfterMethodCallParameterComma);
			return base.VisitArrayCreateExpression (arrayObjectCreateExpression, data);
		}

		public override object VisitLambdaExpression (LambdaExpression lambdaExpression, object data)
		{
			ForceSpacesAfter (lambdaExpression.ArrowToken, true);
			ForceSpacesBefore (lambdaExpression.ArrowToken, true);

			return base.VisitLambdaExpression (lambdaExpression, data);
		}

		#endregion
		
		void ForceSpaceBefore (int offset, bool forceSpace)
		{
			bool insertedSpace = false;
			do {
				char ch = data.GetCharAt (offset);
				//Console.WriteLine (ch);
				if (!IsSpacing (ch) && (insertedSpace || !forceSpace))
					break;
				if (ch == ' ' && forceSpace) {
					if (insertedSpace) {
						AddChange (offset, 1, null);
					} else {
						insertedSpace = true;
					}
				} else if (forceSpace) {
					if (!insertedSpace) {
						AddChange (offset, IsSpacing (ch) ? 1 : 0, " ");
						insertedSpace = true;
					} else if (IsSpacing (ch)) {
						AddChange (offset, 1, null);
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
		
		
		public void FixSemicolon (CSharpTokenNode semicolon)
		{
			if (semicolon.IsNull)
				return;
			int endOffset = data.LocationToOffset (semicolon.StartLocation.Line, semicolon.StartLocation.Column);
			int offset = endOffset;
			while (offset - 1 > 0 && char.IsWhiteSpace (data.GetCharAt (offset - 1))) {
				offset--;
			}
			if (offset < endOffset) {
				AddChange (offset, endOffset - offset, null);
			}
		}	

		void PlaceOnNewLine (bool newLine, AstNode keywordNode)
		{
			if (keywordNode == null)
				return;
			int offset = data.LocationToOffset (keywordNode.StartLocation.Line, keywordNode.StartLocation.Column);
			
			int whitespaceStart = SearchWhitespaceStart (offset);
			string indentString = newLine ? data.EolMarker + this.curIndent.IndentString : " ";
			AddChange (whitespaceStart, offset - whitespaceStart, indentString);
		}

		string nextStatementIndent = null;

		void FixStatementIndentation (AstLocation location)
		{
			int offset = data.LocationToOffset (location.Line, location.Column);
			if (offset <= 0) {
				Console.WriteLine ("possible wrong offset");
				Console.WriteLine (Environment.StackTrace);
				return;
			}
			bool isEmpty = IsLineIsEmptyUpToEol (offset);
			int lineStart = SearchWhitespaceLineStart (offset);
			string indentString = nextStatementIndent == null ? (isEmpty ? "" : data.EolMarker) + this.curIndent.IndentString : nextStatementIndent;
			nextStatementIndent = null;
			AddChange (lineStart, offset - lineStart, indentString);
		}

		void FixIndentation (AstLocation location)
		{
			FixIndentation (location, 0);
		}

		void FixIndentation (AstLocation location, int relOffset)
		{
			if (location.Line < 1 || location.Line > data.LineCount) {
				Console.WriteLine ("Invalid location " + location);
				Console.WriteLine (Environment.StackTrace);
				return;
			}
		
			string lineIndent = data.GetIndentation (location.Line);
			string indentString = this.curIndent.IndentString;
			if (indentString != lineIndent && location.Column - 1 + relOffset == lineIndent.Length) {
				AddChange (data.GetLineOffset (location.Line), lineIndent.Length, indentString);
			}
		}

		void FixIndentationForceNewLine (AstLocation location)
		{
			string lineIndent = data.GetIndentation (location.Line);
			string indentString = this.curIndent.IndentString;
			if (location.Column - 1 == lineIndent.Length) {
				AddChange (data.GetLineOffset (location.Line), lineIndent.Length, indentString);
			} else { 
				int offset = data.LocationToOffset (location.Line, location.Column);
				int start = SearchWhitespaceLineStart (offset);
				if (start > 0) { 
					char ch = data.GetCharAt (start - 1);
					if (ch == '\n') {
						start--;
						if (start > 1 && data.GetCharAt (start - 1) == '\r')
							start--;
					} else if (ch == '\r') {
						start--;
					}
					AddChange (start, offset - start, data.EolMarker + indentString);
				}
			}
		}
	}
}
