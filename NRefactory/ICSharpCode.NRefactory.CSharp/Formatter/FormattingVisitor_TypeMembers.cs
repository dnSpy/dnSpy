//
// AstFormattingVisitor_TypeMembers.cs
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
		public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			FixAttributesAndDocComment(propertyDeclaration);
			bool oneLine = false;
			bool fixClosingBrace = false;
			PropertyFormatting propertyFormatting;

			if ((propertyDeclaration.Getter.IsNull || propertyDeclaration.Getter.Body.IsNull) &&
			    (propertyDeclaration.Setter.IsNull || propertyDeclaration.Setter.Body.IsNull)) {
				propertyFormatting = policy.AutoPropertyFormatting;
			} else {
				propertyFormatting = policy.SimplePropertyFormatting;
			}

			switch (propertyFormatting) {
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
					if (isSimple && 
					    Math.Min(propertyDeclaration.Getter.StartLocation.Line, propertyDeclaration.Setter.StartLocation.Line) == propertyDeclaration.LBraceToken.StartLocation.Line &&
				        propertyDeclaration.Getter.StartLocation.Line != propertyDeclaration.Setter.StartLocation.Line)
						goto case PropertyFormatting.ForceOneLine;
					if (!isSimple || propertyDeclaration.LBraceToken.StartLocation.Line != accessorLine) {
						fixClosingBrace = true;
						FixOpenBrace(policy.PropertyBraceStyle, propertyDeclaration.LBraceToken);
					} else {
						ForceSpacesBefore(propertyDeclaration.Getter, true);
						ForceSpacesBefore(propertyDeclaration.Setter, true);
						ForceSpacesBeforeRemoveNewLines(propertyDeclaration.RBraceToken, true);
						oneLine = true;
					}
					break;
				case PropertyFormatting.ForceNewLine:
					fixClosingBrace = true;
					FixOpenBrace(policy.PropertyBraceStyle, propertyDeclaration.LBraceToken);
					break;
				case PropertyFormatting.ForceOneLine:
					isSimple = IsSimpleAccessor(propertyDeclaration.Getter) && IsSimpleAccessor(propertyDeclaration.Setter);
					if (isSimple) {
						var lBraceToken = propertyDeclaration.LBraceToken;
						var rBraceToken = propertyDeclaration.RBraceToken;
						ForceSpacesBeforeRemoveNewLines(lBraceToken, true);
						if (!propertyDeclaration.Getter.IsNull)
							ForceSpacesBeforeRemoveNewLines(propertyDeclaration.Getter, true);
						if (!propertyDeclaration.Setter.IsNull)
							ForceSpacesBeforeRemoveNewLines(propertyDeclaration.Setter, true);

						ForceSpacesBeforeRemoveNewLines(rBraceToken, true);
						oneLine = true;
					} else {
						fixClosingBrace = true;
						FixOpenBrace(policy.PropertyBraceStyle, propertyDeclaration.LBraceToken);
					}
					break;
			}
			if (policy.IndentPropertyBody)
				curIndent.Push(IndentType.Block);

			FormatAccessor(propertyDeclaration.Getter, policy.PropertyGetBraceStyle, policy.SimpleGetBlockFormatting, oneLine);
			FormatAccessor(propertyDeclaration.Setter, policy.PropertySetBraceStyle, policy.SimpleSetBlockFormatting, oneLine);

			if (policy.IndentPropertyBody) {
				curIndent.Pop();
			}
			if (fixClosingBrace)
				FixClosingBrace(policy.PropertyBraceStyle, propertyDeclaration.RBraceToken);

		}

		void FormatAccessor(Accessor accessor, BraceStyle braceStyle, PropertyFormatting blockFormatting, bool oneLine)
		{
			if (accessor.IsNull)
				return;
			if (!oneLine) {
				if (!IsLineIsEmptyUpToEol(accessor.StartLocation)) {
					int offset = this.document.GetOffset(accessor.StartLocation);
					int start = SearchWhitespaceStart(offset);
					string indentString = this.curIndent.IndentString;
					AddChange(start, offset - start, this.options.EolMarker + indentString);
				} else {
					FixIndentation(accessor);
				}
			} else {
				blockFormatting = PropertyFormatting.ForceOneLine;
				if (!accessor.Body.IsNull) {
					ForceSpacesBeforeRemoveNewLines(accessor.Body.LBraceToken, true);
					ForceSpacesBeforeRemoveNewLines(accessor.Body.RBraceToken, true);
				}
			}

		
			if (!accessor.IsNull) {
				if (!accessor.Body.IsNull) {
					if (IsSimpleAccessor (accessor)) {
						switch (blockFormatting) {
							case PropertyFormatting.AllowOneLine:
								if (accessor.Body.LBraceToken.StartLocation.Line != accessor.Body.RBraceToken.StartLocation.Line)
									goto case PropertyFormatting.ForceNewLine;
								nextStatementIndent = " ";
								VisitBlockWithoutFixingBraces(accessor.Body, policy.IndentBlocks);
								nextStatementIndent = null;
								if (!oneLine)
									ForceSpacesBeforeRemoveNewLines(accessor.Body.RBraceToken, true);
								break;
							case PropertyFormatting.ForceOneLine:
								FixOpenBrace(BraceStyle.EndOfLine, accessor.Body.LBraceToken);


								var statement = accessor.Body.Statements.FirstOrDefault();
								if (statement != null) {
									ForceSpacesBeforeRemoveNewLines(statement, true);
									statement.AcceptVisitor(this);
								}
								if (!oneLine)
									ForceSpacesBeforeRemoveNewLines(accessor.Body.RBraceToken, true);
								break;
							case PropertyFormatting.ForceNewLine:
								FixOpenBrace(braceStyle, accessor.Body.LBraceToken);
								VisitBlockWithoutFixingBraces(accessor.Body, policy.IndentBlocks);
								if (!oneLine)
									FixClosingBrace(braceStyle, accessor.Body.RBraceToken);
								break;
						}
					} else {
						FixOpenBrace(braceStyle, accessor.Body.LBraceToken);
						VisitBlockWithoutFixingBraces(accessor.Body, policy.IndentBlocks);
						FixClosingBrace(braceStyle, accessor.Body.RBraceToken);
					}
				} 
			}
		}

		public override void VisitAccessor(Accessor accessor)
		{
			FixAttributesAndDocComment(accessor);

			base.VisitAccessor(accessor);
		}

		public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			FixAttributesAndDocComment(indexerDeclaration);

			ForceSpacesBefore(indexerDeclaration.LBracketToken, policy.SpaceBeforeIndexerDeclarationBracket);
			ForceSpacesAfter(indexerDeclaration.LBracketToken, policy.SpaceWithinIndexerDeclarationBracket);

			FormatArguments(indexerDeclaration);

			bool oneLine = false;
			bool fixClosingBrace = false;
			switch (policy.SimplePropertyFormatting) {
				case PropertyFormatting.AllowOneLine:
					bool isSimple = IsSimpleAccessor(indexerDeclaration.Getter) && IsSimpleAccessor(indexerDeclaration.Setter);
					int accessorLine = indexerDeclaration.RBraceToken.StartLocation.Line;
					if (!indexerDeclaration.Getter.IsNull && indexerDeclaration.Setter.IsNull) {
						accessorLine = indexerDeclaration.Getter.StartLocation.Line;
					} else if (indexerDeclaration.Getter.IsNull && !indexerDeclaration.Setter.IsNull) {
						accessorLine = indexerDeclaration.Setter.StartLocation.Line;
					} else {
						var acc = indexerDeclaration.Getter.StartLocation < indexerDeclaration.Setter.StartLocation ?
							indexerDeclaration.Getter : indexerDeclaration.Setter;
						accessorLine = acc.StartLocation.Line;
					}
					if (!isSimple || indexerDeclaration.LBraceToken.StartLocation.Line != accessorLine) {
						fixClosingBrace = true;
						FixOpenBrace(policy.PropertyBraceStyle, indexerDeclaration.LBraceToken);
					} else {
						ForceSpacesBefore(indexerDeclaration.Getter, true);
						ForceSpacesBefore(indexerDeclaration.Setter, true);
						ForceSpacesBeforeRemoveNewLines(indexerDeclaration.RBraceToken, true);
						oneLine = true;
					}
					break;
				case PropertyFormatting.ForceNewLine:
					fixClosingBrace = true;
					FixOpenBrace(policy.PropertyBraceStyle, indexerDeclaration.LBraceToken);
					break;
				case PropertyFormatting.ForceOneLine:
					isSimple = IsSimpleAccessor(indexerDeclaration.Getter) && IsSimpleAccessor(indexerDeclaration.Setter);
					if (isSimple) {
						int offset = this.document.GetOffset(indexerDeclaration.LBraceToken.StartLocation);

						int start = SearchWhitespaceStart(offset);
						int end = SearchWhitespaceEnd(offset);
						AddChange(start, offset - start, " ");
						AddChange(offset + 1, end - offset - 2, " ");

						offset = this.document.GetOffset(indexerDeclaration.RBraceToken.StartLocation);
						start = SearchWhitespaceStart(offset);
						AddChange(start, offset - start, " ");
						oneLine = true;

					} else {
						fixClosingBrace = true;
						FixOpenBrace(policy.PropertyBraceStyle, indexerDeclaration.LBraceToken);
					}
					break;
			}

			if (policy.IndentPropertyBody)
				curIndent.Push(IndentType.Block);

			FormatAccessor(indexerDeclaration.Getter, policy.PropertyGetBraceStyle, policy.SimpleGetBlockFormatting, oneLine);
			FormatAccessor(indexerDeclaration.Setter, policy.PropertySetBraceStyle, policy.SimpleSetBlockFormatting, oneLine);
			if (policy.IndentPropertyBody)
				curIndent.Pop();

			if (fixClosingBrace)
				FixClosingBrace(policy.PropertyBraceStyle, indexerDeclaration.RBraceToken);
		}

		static bool IsSimpleEvent(AstNode node)
		{
			return node is EventDeclaration;
		}

		public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			FixAttributesAndDocComment(eventDeclaration);

			FixOpenBrace(policy.EventBraceStyle, eventDeclaration.LBraceToken);
			if (policy.IndentEventBody)
				curIndent.Push(IndentType.Block);

			if (!eventDeclaration.AddAccessor.IsNull) {
				FixIndentation(eventDeclaration.AddAccessor);
				if (!eventDeclaration.AddAccessor.Body.IsNull) {
					if (!policy.AllowEventAddBlockInline || eventDeclaration.AddAccessor.Body.LBraceToken.StartLocation.Line != eventDeclaration.AddAccessor.Body.RBraceToken.StartLocation.Line) {
						FixOpenBrace(policy.EventAddBraceStyle, eventDeclaration.AddAccessor.Body.LBraceToken);
						VisitBlockWithoutFixingBraces(eventDeclaration.AddAccessor.Body, policy.IndentBlocks);
						FixClosingBrace(policy.EventAddBraceStyle, eventDeclaration.AddAccessor.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
						VisitBlockWithoutFixingBraces(eventDeclaration.AddAccessor.Body, policy.IndentBlocks);
						nextStatementIndent = null;
					}
				}
			}

			if (!eventDeclaration.RemoveAccessor.IsNull) {
				FixIndentation(eventDeclaration.RemoveAccessor);
				if (!eventDeclaration.RemoveAccessor.Body.IsNull) {
					if (!policy.AllowEventRemoveBlockInline || eventDeclaration.RemoveAccessor.Body.LBraceToken.StartLocation.Line != eventDeclaration.RemoveAccessor.Body.RBraceToken.StartLocation.Line) {
						FixOpenBrace(policy.EventRemoveBraceStyle, eventDeclaration.RemoveAccessor.Body.LBraceToken);
						VisitBlockWithoutFixingBraces(eventDeclaration.RemoveAccessor.Body, policy.IndentBlocks);
						FixClosingBrace(policy.EventRemoveBraceStyle, eventDeclaration.RemoveAccessor.Body.RBraceToken);
					} else {
						nextStatementIndent = " ";
						VisitBlockWithoutFixingBraces(eventDeclaration.RemoveAccessor.Body, policy.IndentBlocks);
						nextStatementIndent = null;
					}
				}
			}

			if (policy.IndentEventBody)
				curIndent.Pop();

			FixClosingBrace(policy.EventBraceStyle, eventDeclaration.RBraceToken);
		}

		public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			FixAttributesAndDocComment(eventDeclaration);

			foreach (var m in eventDeclaration.ModifierTokens) {
				ForceSpacesAfter(m, true);
			}

			ForceSpacesBeforeRemoveNewLines(eventDeclaration.EventToken.GetNextSibling(NoWhitespacePredicate), true);
			eventDeclaration.ReturnType.AcceptVisitor(this);
			ForceSpacesAfter(eventDeclaration.ReturnType, true);
			/*
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
			*/
			FixSemicolon(eventDeclaration.SemicolonToken);
		}

		public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			FixAttributesAndDocComment(fieldDeclaration);

			fieldDeclaration.ReturnType.AcceptVisitor(this);
			ForceSpacesAfter(fieldDeclaration.ReturnType, true);

			FormatCommas(fieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);

			var lastLoc = fieldDeclaration.ReturnType.StartLocation;
			foreach (var initializer in fieldDeclaration.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					curIndent.Push(IndentType.Block);
					FixStatementIndentation(initializer.StartLocation);
					curIndent.Pop();
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}
			FixSemicolon(fieldDeclaration.SemicolonToken);
		}

		public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			FixAttributesAndDocComment(fixedFieldDeclaration);

			FormatCommas(fixedFieldDeclaration, policy.SpaceBeforeFieldDeclarationComma, policy.SpaceAfterFieldDeclarationComma);

			var lastLoc = fixedFieldDeclaration.StartLocation;
			curIndent.Push(IndentType.Block);
			foreach (var initializer in fixedFieldDeclaration.Variables) {
				if (lastLoc.Line != initializer.StartLocation.Line) {
					FixStatementIndentation(initializer.StartLocation);
					lastLoc = initializer.StartLocation;
				}
				initializer.AcceptVisitor(this);
			}
			curIndent.Pop();
			FixSemicolon(fixedFieldDeclaration.SemicolonToken);
		}

		public override void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			FixAttributesAndDocComment(enumMemberDeclaration);
			var initializer = enumMemberDeclaration.Initializer;
			if (!initializer.IsNull) {
				ForceSpacesAround(enumMemberDeclaration.AssignToken, policy.SpaceAroundAssignment);
				initializer.AcceptVisitor(this);
			}
		}

		public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			FixAttributesAndDocComment(methodDeclaration);

			ForceSpacesBefore(methodDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (methodDeclaration.Parameters.Any()) {
				ForceSpacesAfter(methodDeclaration.LParToken, policy.SpaceWithinMethodDeclarationParentheses);
				FormatArguments(methodDeclaration);
			} else {
				ForceSpacesAfter(methodDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore(methodDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}
			
			foreach (var constraint in methodDeclaration.Constraints)
				constraint.AcceptVisitor(this);

			if (!methodDeclaration.Body.IsNull) {
				FixOpenBrace(policy.MethodBraceStyle, methodDeclaration.Body.LBraceToken);
				VisitBlockWithoutFixingBraces(methodDeclaration.Body, policy.IndentMethodBody);
				FixClosingBrace(policy.MethodBraceStyle, methodDeclaration.Body.RBraceToken);
			}
		}

		public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			FixAttributesAndDocComment(operatorDeclaration);

			ForceSpacesBefore(operatorDeclaration.LParToken, policy.SpaceBeforeMethodDeclarationParentheses);
			if (operatorDeclaration.Parameters.Any()) {
				ForceSpacesAfter(operatorDeclaration.LParToken, policy.SpaceWithinMethodDeclarationParentheses);
				FormatArguments(operatorDeclaration);
			} else {
				ForceSpacesAfter(operatorDeclaration.LParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
				ForceSpacesBefore(operatorDeclaration.RParToken, policy.SpaceBetweenEmptyMethodDeclarationParentheses);
			}

			if (!operatorDeclaration.Body.IsNull) {
				FixOpenBrace(policy.MethodBraceStyle, operatorDeclaration.Body.LBraceToken);
				VisitBlockWithoutFixingBraces(operatorDeclaration.Body, policy.IndentMethodBody);
				FixClosingBrace(policy.MethodBraceStyle, operatorDeclaration.Body.RBraceToken);
			}
		}

		public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			FixAttributesAndDocComment(constructorDeclaration);

			ForceSpacesBefore(constructorDeclaration.LParToken, policy.SpaceBeforeConstructorDeclarationParentheses);
			if (constructorDeclaration.Parameters.Any()) {
				ForceSpacesAfter(constructorDeclaration.LParToken, policy.SpaceWithinConstructorDeclarationParentheses);
				FormatArguments(constructorDeclaration);
			} else {
				ForceSpacesAfter(constructorDeclaration.LParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
				ForceSpacesBefore(constructorDeclaration.RParToken, policy.SpaceBetweenEmptyConstructorDeclarationParentheses);
			}

			var initializer = constructorDeclaration.Initializer;
			if (!initializer.IsNull) {
				curIndent.Push(IndentType.Block);
				PlaceOnNewLine(policy.NewLineBeforeConstructorInitializerColon, constructorDeclaration.ColonToken);
				PlaceOnNewLine(policy.NewLineAfterConstructorInitializerColon, initializer);
				initializer.AcceptVisitor(this);
				curIndent.Pop();
			}
			if (!constructorDeclaration.Body.IsNull) {
				FixOpenBrace(policy.ConstructorBraceStyle, constructorDeclaration.Body.LBraceToken);
				VisitBlockWithoutFixingBraces(constructorDeclaration.Body, policy.IndentMethodBody);
				FixClosingBrace(policy.ConstructorBraceStyle, constructorDeclaration.Body.RBraceToken);
			}
		}
		public override void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
		{
			ForceSpacesBefore(constructorInitializer.LParToken, policy.SpaceBeforeMethodCallParentheses);
			if (constructorInitializer.Arguments.Any()) {
				ForceSpacesAfter(constructorInitializer.LParToken, policy.SpaceWithinMethodCallParentheses);
			} else {
				ForceSpacesAfter(constructorInitializer.LParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
				ForceSpacesBefore(constructorInitializer.RParToken, policy.SpaceBetweenEmptyMethodCallParentheses);
			}

			FormatArguments(constructorInitializer);

		}
		public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			FixAttributesAndDocComment(destructorDeclaration);

			CSharpTokenNode lParen = destructorDeclaration.LParToken;
			ForceSpaceBefore(lParen, policy.SpaceBeforeConstructorDeclarationParentheses);

			if (!destructorDeclaration.Body.IsNull) {
				FixOpenBrace(policy.DestructorBraceStyle, destructorDeclaration.Body.LBraceToken);
				VisitBlockWithoutFixingBraces(destructorDeclaration.Body, policy.IndentMethodBody);
				FixClosingBrace(policy.DestructorBraceStyle, destructorDeclaration.Body.RBraceToken);
			}
		}
	}
}

