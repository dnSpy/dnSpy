//
// AstFormattingVisitor_Global.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	partial class FormattingVisitor : DepthFirstAstVisitor
	{
		int GetGlobalNewLinesFor(AstNode child)
		{
			if (child.NextSibling == null)
				// last node in the document => no extra newlines
				return 0;
			if (child.NextSibling.Role == Roles.RBrace)
				// Last node in a block => no extra newlines, it's handled later by FixClosingBrace()
				return 0;

			int newLines = 1;
			var nextSibling = child.GetNextSibling(NoWhitespacePredicate);
			if (nextSibling is PreProcessorDirective) {
				var directive = (PreProcessorDirective)nextSibling;
				if (directive.Type == PreProcessorDirectiveType.Endif)
					return -1;
				if (directive.Type == PreProcessorDirectiveType.Undef)
					return -1;
			}
			if ((child is UsingDeclaration || child is UsingAliasDeclaration) && !(nextSibling is UsingDeclaration || nextSibling is UsingAliasDeclaration)) {
				newLines += policy.MinimumBlankLinesAfterUsings;
			} else if ((child is TypeDeclaration) && (nextSibling is TypeDeclaration)) {
				newLines += policy.MinimumBlankLinesBetweenTypes;
			}

			return newLines;
		}

		public override void VisitSyntaxTree(SyntaxTree unit)
		{
			bool first = true;
			VisitChildrenToFormat(unit, child => {
				if (first && (child is UsingDeclaration || child is UsingAliasDeclaration)) {
					EnsureMinimumBlankLinesBefore(child, policy.MinimumBlankLinesBeforeUsings);
					first = false;
				}
				if (NoWhitespacePredicate(child))
					FixIndentation(child);
				child.AcceptVisitor(this);
				if (NoWhitespacePredicate(child) && !first)
					EnsureMinimumNewLinesAfter(child, GetGlobalNewLinesFor(child));
			});
		}

		public override void VisitAttributeSection(AttributeSection attributeSection)
		{
			VisitChildrenToFormat(attributeSection, child => {
				child.AcceptVisitor(this);
				if (child.NextSibling != null && child.NextSibling.Role == Roles.RBracket) {
					ForceSpacesAfter(child, false);
				}
			});
		}

		public override void VisitAttribute(Attribute attribute)
		{
			if (attribute.HasArgumentList) {
				ForceSpacesBefore(attribute.LParToken, policy.SpaceBeforeMethodCallParentheses);
				if (attribute.Arguments.Any()) {
					ForceSpacesAfter(attribute.LParToken, policy.SpaceWithinMethodCallParentheses);
				} else {
					ForceSpacesAfter(attribute.LParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
					ForceSpacesBefore(attribute.RParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
				}
				FormatArguments(attribute);
			}
		}

		public override void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			ForceSpacesAfter(usingDeclaration.UsingToken, true);
			FixSemicolon(usingDeclaration.SemicolonToken);
		}

		public override void VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration)
		{
			ForceSpacesAfter(usingDeclaration.UsingToken, true);
			ForceSpacesAround(usingDeclaration.AssignToken, policy.SpaceAroundAssignment);
			FixSemicolon(usingDeclaration.SemicolonToken);
		}

		public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			FixOpenBrace(policy.NamespaceBraceStyle, namespaceDeclaration.LBraceToken);
			if (policy.IndentNamespaceBody)
				curIndent.Push(IndentType.Block);

			bool first = true;
			bool startFormat = false;
			VisitChildrenToFormat(namespaceDeclaration, child => {
				if (first) {
					startFormat = child.StartLocation > namespaceDeclaration.LBraceToken.StartLocation;
				}
				if (child.Role == Roles.LBrace) {
					var next = child.GetNextSibling(NoWhitespacePredicate);
					var blankLines = 1;
					if (next is UsingDeclaration || next is UsingAliasDeclaration) {
						blankLines += policy.MinimumBlankLinesBeforeUsings;
					} else {
						blankLines += policy.MinimumBlankLinesBeforeFirstDeclaration;
					}
					EnsureMinimumNewLinesAfter(child, blankLines);
					startFormat = true;
					return;
				}
				if (child.Role == Roles.RBrace) {
					startFormat = false;
					return;
				}
				if (!startFormat || !NoWhitespacePredicate (child))
					return;
				if (first && (child is UsingDeclaration || child is UsingAliasDeclaration)) {
					// TODO: policy.BlankLinesBeforeUsings
					first = false;
				}
				if (NoWhitespacePredicate(child))
					FixIndentationForceNewLine(child);
				child.AcceptVisitor(this);
				if (NoWhitespacePredicate(child))
					EnsureMinimumNewLinesAfter(child, GetGlobalNewLinesFor(child));
			});

			if (policy.IndentNamespaceBody)
				curIndent.Pop();

			FixClosingBrace(policy.NamespaceBraceStyle, namespaceDeclaration.RBraceToken);
		}

		void FixAttributesAndDocComment(EntityDeclaration entity)
		{
			var node = entity.FirstChild;
			while (node != null && node.Role == Roles.Comment) {
				node = node.GetNextSibling(NoWhitespacePredicate);
				FixIndentation(node);
			}
			if (entity.Attributes.Count > 0) {
				AstNode n = null;
				entity.Attributes.First().AcceptVisitor(this);
				foreach (var attr in entity.Attributes.Skip (1)) {
					FixIndentation(attr);
					attr.AcceptVisitor(this);
					n = attr;
				}
				if (n != null) {
					FixIndentation(n.GetNextNode(NoWhitespacePredicate));
				} else {
					FixIndentation(entity.Attributes.First().GetNextNode(NoWhitespacePredicate));
				}
			}
		}

		public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			FixAttributesAndDocComment(typeDeclaration);
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
			
			foreach (var constraint in typeDeclaration.Constraints)
				constraint.AcceptVisitor (this);

			FixOpenBrace(braceStyle, typeDeclaration.LBraceToken);

			if (indentBody)
				curIndent.Push(IndentType.Block);
			bool startFormat = true;
			bool first = true;
			VisitChildrenToFormat(typeDeclaration, child => {
				if (first) {
					startFormat = child.StartLocation > typeDeclaration.LBraceToken.StartLocation;
					first = false;
				}
				if (child.Role == Roles.LBrace) {
					startFormat = true;
					if (braceStyle != BraceStyle.DoNotChange)
						EnsureMinimumNewLinesAfter(child, GetTypeLevelNewLinesFor(child));
					return;
				}
				if (child.Role == Roles.RBrace) {
					startFormat = false;
					return;
				}
				if (!startFormat || !NoWhitespacePredicate (child))
					return;
				if (child.Role == Roles.Comma) {
					ForceSpacesBeforeRemoveNewLines (child, false);
					EnsureMinimumNewLinesAfter(child, 1);
					return;
				} 
				if (NoWhitespacePredicate(child))
					FixIndentationForceNewLine(child);
				child.AcceptVisitor(this);
				if (NoWhitespacePredicate(child) && child.GetNextSibling (NoWhitespacePredicate).Role != Roles.Comma)
					EnsureMinimumNewLinesAfter(child, GetTypeLevelNewLinesFor(child));
			});

			if (indentBody)
				curIndent.Pop();

			FixClosingBrace(braceStyle, typeDeclaration.RBraceToken);
		}

		int GetTypeLevelNewLinesFor(AstNode child)
		{
			var blankLines = 1;
			var nextSibling = child.GetNextSibling(NoWhitespacePredicate);
			if (child is PreProcessorDirective) {
				var directive = (PreProcessorDirective)child;
				if (directive.Type == PreProcessorDirectiveType.Region) {
					blankLines += policy.MinimumBlankLinesInsideRegion;
				}
				if (directive.Type == PreProcessorDirectiveType.Endregion) {
					if (child.GetNextSibling(NoWhitespacePredicate) is CSharpTokenNode)
						return 1;
					blankLines += policy.MinimumBlankLinesAroundRegion;
				}
				return blankLines;
			}

			if (nextSibling is PreProcessorDirective) {
				var directive = (PreProcessorDirective)nextSibling;
				if (directive.Type == PreProcessorDirectiveType.Region) {
					if (child is CSharpTokenNode)
						return 1;
					blankLines += policy.MinimumBlankLinesAroundRegion;
				}
				if (directive.Type == PreProcessorDirectiveType.Endregion)
					blankLines += policy.MinimumBlankLinesInsideRegion;
				if (directive.Type == PreProcessorDirectiveType.Endif)
					return -1;
				if (directive.Type == PreProcessorDirectiveType.Undef)
					return -1;
				return blankLines;
			}
			if (child.Role == Roles.LBrace)
				return 1;
			if (child is Comment)
				return 1;
			if (child is EventDeclaration) {
				if (nextSibling is EventDeclaration) {
					blankLines += policy.MinimumBlankLinesBetweenEventFields;
					return blankLines;
				}
			}

			if (child is FieldDeclaration || child is FixedFieldDeclaration) {
				if (nextSibling is FieldDeclaration || nextSibling is FixedFieldDeclaration) {
					blankLines += policy.MinimumBlankLinesBetweenFields;
					return blankLines;
				}
			}
			
			if (child is TypeDeclaration) {
				if (nextSibling is TypeDeclaration) {
					blankLines += policy.MinimumBlankLinesBetweenTypes;
					return blankLines;
				}
			}

			if (nextSibling.Role == Roles.TypeMemberRole)
				blankLines += policy.MinimumBlankLinesBetweenMembers;
			return blankLines;
		}

		public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			ForceSpacesBefore(delegateDeclaration.LParToken, policy.SpaceBeforeDelegateDeclarationParentheses);
			if (delegateDeclaration.Parameters.Any()) {
				ForceSpacesAfter(delegateDeclaration.LParToken, policy.SpaceWithinDelegateDeclarationParentheses);
				ForceSpacesBefore(delegateDeclaration.RParToken, policy.SpaceWithinDelegateDeclarationParentheses);
			} else {
				ForceSpacesAfter(delegateDeclaration.LParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
				ForceSpacesBefore(delegateDeclaration.RParToken, policy.SpaceBetweenEmptyDelegateDeclarationParentheses);
			}
			FormatCommas(delegateDeclaration, policy.SpaceBeforeDelegateDeclarationParameterComma, policy.SpaceAfterDelegateDeclarationParameterComma);

			base.VisitDelegateDeclaration(delegateDeclaration);
		}

		public override void VisitComment(Comment comment)
		{
			if (comment.StartsLine && !HadErrors && (!policy.KeepCommentsAtFirstColumn || comment.StartLocation.Column > 1))
				FixIndentation(comment);
		}

		public override void VisitConstraint(Constraint constraint)
		{
			VisitChildrenToFormat (constraint, node => {
				if (node is AstType) {
					node.AcceptVisitor (this);
				} else if (node.Role == Roles.LPar) {
					ForceSpacesBefore (node, false);
					ForceSpacesAfter (node, false);
				} else if (node.Role ==Roles.Comma) {
					ForceSpacesBefore (node, false);
					ForceSpacesAfter (node, true);
				}
			});
		}
	}
}