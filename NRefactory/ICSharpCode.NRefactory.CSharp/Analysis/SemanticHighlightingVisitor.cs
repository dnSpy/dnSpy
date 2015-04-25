// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Completion;
using System.Collections.ObjectModel;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// C# Semantic highlighter.
	/// </summary>
	public abstract class SemanticHighlightingVisitor<TColor> : DepthFirstAstVisitor
	{
		protected CancellationToken cancellationToken = default (CancellationToken);
		
		protected TColor defaultTextColor;
		protected TColor referenceTypeColor;
		protected TColor valueTypeColor;
		protected TColor interfaceTypeColor;
		protected TColor enumerationTypeColor;
		protected TColor typeParameterTypeColor;
		protected TColor delegateTypeColor;
		
		protected TColor methodCallColor;
		protected TColor methodDeclarationColor;
		
		protected TColor eventDeclarationColor;
		protected TColor eventAccessColor;
		
		protected TColor propertyDeclarationColor;
		protected TColor propertyAccessColor;
		
		protected TColor fieldDeclarationColor;
		protected TColor fieldAccessColor;
		
		protected TColor variableDeclarationColor;
		protected TColor variableAccessColor;
		
		protected TColor parameterDeclarationColor;
		protected TColor parameterAccessColor;
		
		protected TColor valueKeywordColor;
		protected TColor externAliasKeywordColor;
		protected TColor varKeywordTypeColor;

		/// <summary>
		/// Used for 'in' modifiers on type parameters.
		/// </summary>
		/// <remarks>
		/// 'in' may have a different color when used with 'foreach'.
		/// 'out' is not colored by semantic highlighting, as syntax highlighting can already detect it as a parameter modifier.
		/// </remarks>
		protected TColor parameterModifierColor;
		
		/// <summary>
		/// Used for inactive code (excluded by preprocessor or ConditionalAttribute)
		/// </summary>
		protected TColor inactiveCodeColor;

		protected TColor stringFormatItemColor;


		protected TColor syntaxErrorColor;
		
		protected TextLocation regionStart;
		protected TextLocation regionEnd;
		
		protected CSharpAstResolver resolver;
		protected bool isInAccessorContainingValueParameter;
		
		protected abstract void Colorize(TextLocation start, TextLocation end, TColor color);
		
		#region Colorize helper methods
		protected void Colorize(Identifier identifier, ResolveResult rr)
		{
			if (identifier.IsNull)
				return;
			if (rr.IsError) {
				Colorize(identifier, syntaxErrorColor);
				return;
			}
			if (rr is TypeResolveResult) {
				if (blockDepth > 0 && identifier.Name == "var" && rr.Type.Kind != TypeKind.Null && rr.Type.Name != "var" ) {
					Colorize(identifier, varKeywordTypeColor);
					return;
				}

				TColor color;
				if (TryGetTypeHighlighting (rr.Type.Kind, out color)) {
					Colorize(identifier, color);
				}
				return;
			}
			var mrr = rr as MemberResolveResult;
			if (mrr != null) {
				TColor color;
				if (TryGetMemberColor (mrr.Member, out color)) {
					Colorize(identifier, color);
					return;
				}
			}
			
			if (rr is MethodGroupResolveResult) {
				Colorize (identifier, methodCallColor);
				return;
			}
			
			var localResult = rr as LocalResolveResult;
			if (localResult != null) {
				if (localResult.Variable is IParameter) {
					Colorize (identifier, parameterAccessColor);
				} else {
					Colorize (identifier, variableAccessColor);
				}
			}
			
			
			VisitIdentifier(identifier); // un-colorize contextual keywords
		}
		
		protected void Colorize(AstNode node, TColor color)
		{
			if (node.IsNull)
				return;
			Colorize(node.StartLocation, node.EndLocation, color);
		}
		#endregion
		
		protected override void VisitChildren(AstNode node)
		{
			for (var child = node.FirstChild; child != null; child = child.NextSibling) {
				if (child.StartLocation < regionEnd && child.EndLocation > regionStart)
					child.AcceptVisitor(this);
			}
		}
		
		/// <summary>
		/// Visit all children of <c>node</c> until (but excluding) <c>end</c>.
		/// If <c>end</c> is a null node, nothing will be visited.
		/// </summary>
		protected void VisitChildrenUntil(AstNode node, AstNode end)
		{
			if (end.IsNull)
				return;
			Debug.Assert(node == end.Parent);
			for (var child = node.FirstChild; child != end; child = child.NextSibling) {
				cancellationToken.ThrowIfCancellationRequested();
				if (child.StartLocation < regionEnd && child.EndLocation > regionStart)
					child.AcceptVisitor(this);
			}
		}
		
		/// <summary>
		/// Visit all children of <c>node</c> after (excluding) <c>start</c>.
		/// If <c>start</c> is a null node, all children will be visited.
		/// </summary>
		protected void VisitChildrenAfter(AstNode node, AstNode start)
		{
			Debug.Assert(start.IsNull || start.Parent == node);
			for (var child = (start.IsNull ? node.FirstChild : start.NextSibling); child != null; child = child.NextSibling) {
				cancellationToken.ThrowIfCancellationRequested();
				if (child.StartLocation < regionEnd && child.EndLocation > regionStart)
					child.AcceptVisitor(this);
			}
		}
		
		public override void VisitIdentifier(Identifier identifier)
		{
			switch (identifier.Name) {
				case "add":
				case "async":
				case "await":
				case "get":
				case "partial":
				case "remove":
				case "set":
				case "where":
				case "yield":
				case "from":
				case "select":
				case "group":
				case "into":
				case "orderby":
				case "join":
				case "let":
				case "on":
				case "equals":
				case "by":
				case "ascending":
				case "descending":
				case "dynamic":
				case "var":
					// Reset color of contextual keyword to default if it's used as an identifier.
					// Note that this method does not get called when 'var' or 'dynamic' is used as a type,
					// because types get highlighted with valueTypeColor/referenceTypeColor instead.
					Colorize(identifier, defaultTextColor);
					break;
				case "global":
					// Reset color of 'global' keyword to default unless its used as part of 'global::'.
					MemberType parentMemberType = identifier.Parent as MemberType;
					if (parentMemberType == null || !parentMemberType.IsDoubleColon)
						Colorize(identifier, defaultTextColor);
					break;
			}
			// "value" is handled in VisitIdentifierExpression()
			// "alias" is handled in VisitExternAliasDeclaration()
		}
		
		public override void VisitSimpleType(SimpleType simpleType)
		{
			var identifierToken = simpleType.IdentifierToken;
			VisitChildrenUntil(simpleType, identifierToken);
			Colorize(identifierToken, resolver.Resolve(simpleType, cancellationToken));
			VisitChildrenAfter(simpleType, identifierToken);
		}
		
		public override void VisitMemberType(MemberType memberType)
		{
			var memberNameToken = memberType.MemberNameToken;
			VisitChildrenUntil(memberType, memberNameToken);
			Colorize(memberNameToken, resolver.Resolve(memberType, cancellationToken));
			VisitChildrenAfter(memberType, memberNameToken);
		}
		
		public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			var identifier = identifierExpression.IdentifierToken;
			VisitChildrenUntil(identifierExpression, identifier);
			if (isInAccessorContainingValueParameter && identifierExpression.Identifier == "value") {
				Colorize(identifier, valueKeywordColor);
			} else {
				Colorize(identifier, resolver.Resolve(identifierExpression, cancellationToken));
			}
			VisitChildrenAfter(identifierExpression, identifier);
		}
		
		public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			var memberNameToken = memberReferenceExpression.MemberNameToken;
			VisitChildrenUntil(memberReferenceExpression, memberNameToken);
			ResolveResult rr = resolver.Resolve(memberReferenceExpression, cancellationToken);
			Colorize(memberNameToken, rr);
			VisitChildrenAfter(memberReferenceExpression, memberNameToken);
		}

		void HighlightStringFormatItems(PrimitiveExpression expr)
		{
			if (!(expr.Value is string))
				return;
			int line = expr.StartLocation.Line;
			int col = expr.StartLocation.Column;
			TextLocation start = TextLocation.Empty;
			for (int i = 0; i < expr.LiteralValue.Length; i++) {
				char ch = expr.LiteralValue [i];

				if (NewLine.GetDelimiterType(ch, i + 1 < expr.LiteralValue.Length ? expr.LiteralValue [i + 1] : '\0') != UnicodeNewline.Unknown) {
					line++;
					col = 1;
					continue;
				}


				if (ch == '{' && start.IsEmpty) {
					char next = i + 1 < expr.LiteralValue.Length ? expr.LiteralValue [i + 1] : '\0';
					if (next == '{') {
						i++;
						col += 2;
						continue;
					}
					start = new TextLocation(line, col);
				}
				if (ch == '}' &&!start.IsEmpty) {
					Colorize(start, new TextLocation(line, col + 1), stringFormatItemColor);
					start = TextLocation.Empty;
				}
				col++;
			}

		}

		public override void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			Expression target = invocationExpression.Target;
			if (target is IdentifierExpression || target is MemberReferenceExpression || target is PointerReferenceExpression) {
				var invocationRR = resolver.Resolve(invocationExpression, cancellationToken) as CSharpInvocationResolveResult;
				if (invocationRR != null) {
					if (invocationExpression.Parent is ExpressionStatement && (IsInactiveConditionalMethod(invocationRR.Member) || IsEmptyPartialMethod(invocationRR.Member))) {
						// mark the whole invocation statement as inactive code
						Colorize(invocationExpression.Parent, inactiveCodeColor);
						return;
					}

					Expression fmtArgumets;
					IList<Expression> args;
					if (invocationRR.Arguments.Count > 1 && FormatStringHelper.TryGetFormattingParameters(invocationRR, invocationExpression, out fmtArgumets, out args, null)) {
						var expr = invocationExpression.Arguments.First() as PrimitiveExpression; 
						if (expr != null)
							HighlightStringFormatItems(expr);
					}
				}

				VisitChildrenUntil(invocationExpression, target);
				
				// highlight the method call
				var identifier = target.GetChildByRole(Roles.Identifier);
				VisitChildrenUntil(target, identifier);
				if (invocationRR != null && !invocationRR.IsDelegateInvocation) {
					Colorize(identifier, methodCallColor);
				} else {
					ResolveResult targetRR = resolver.Resolve(target, cancellationToken);
					Colorize(identifier, targetRR);
				}
				VisitChildrenAfter(target, identifier);
				VisitChildrenAfter(invocationExpression, target);
			} else {
				VisitChildren(invocationExpression);
			}
		}
		
		#region IsInactiveConditional helper methods
		bool IsInactiveConditionalMethod(IParameterizedMember member)
		{
			if (member.SymbolKind != SymbolKind.Method || member.ReturnType.Kind != TypeKind.Void)
				return false;
			foreach (var baseMember in InheritanceHelper.GetBaseMembers(member, false)) {
				if (IsInactiveConditional (baseMember.Attributes))
					return true;
			}
			return IsInactiveConditional(member.Attributes);
		}

		static bool IsEmptyPartialMethod(IParameterizedMember member)
		{
			if (member.SymbolKind != SymbolKind.Method || member.ReturnType.Kind != TypeKind.Void)
				return false;
			var method = (IMethod)member;
			return method.IsPartial && !method.HasBody;
		}

		bool IsInactiveConditional(IList<IAttribute> attributes)
		{
			bool hasConditionalAttribute = false;
			foreach (var attr in attributes) {
				if (attr.AttributeType.Name == "ConditionalAttribute" && attr.AttributeType.Namespace == "System.Diagnostics" && attr.PositionalArguments.Count == 1) {
					string symbol = attr.PositionalArguments[0].ConstantValue as string;
					if (symbol != null) {
						hasConditionalAttribute = true;
						var cu = this.resolver.RootNode as SyntaxTree;
						if (cu != null) {
							if (cu.ConditionalSymbols.Contains(symbol))
								return false; // conditional is active
						}
					}
				}
			}

			return hasConditionalAttribute;
		}
		#endregion
		
		public override void VisitExternAliasDeclaration (ExternAliasDeclaration externAliasDeclaration)
		{
			var aliasToken = externAliasDeclaration.AliasToken;
			VisitChildrenUntil(externAliasDeclaration, aliasToken);
			Colorize (aliasToken, externAliasKeywordColor);
			VisitChildrenAfter(externAliasDeclaration, aliasToken);
		}
		
		public override void VisitAccessor(Accessor accessor)
		{
			isInAccessorContainingValueParameter = accessor.Role != PropertyDeclaration.GetterRole;
			try {
				VisitChildren(accessor);
			} finally {
				isInAccessorContainingValueParameter = false;
			}
		}
		
		bool CheckInterfaceImplementation (EntityDeclaration entityDeclaration)
		{
			var result = resolver.Resolve (entityDeclaration, cancellationToken) as MemberResolveResult;
			if (result == null)
				return false;
			if (result.Member.ImplementedInterfaceMembers.Count == 0) {
				Colorize (entityDeclaration.NameToken, syntaxErrorColor);
				return false;
			}
			return true;
		}
		
		public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			var nameToken = methodDeclaration.NameToken;
			VisitChildrenUntil(methodDeclaration, nameToken);
			if (!methodDeclaration.PrivateImplementationType.IsNull) {
				if (!CheckInterfaceImplementation (methodDeclaration)) {
					VisitChildrenAfter(methodDeclaration, nameToken);
					return;
				}
			}
			Colorize(nameToken, methodDeclarationColor);
			VisitChildrenAfter(methodDeclaration, nameToken);
		}
		
		public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			var nameToken = parameterDeclaration.NameToken;
			VisitChildrenUntil(parameterDeclaration, nameToken);
			Colorize(nameToken, parameterDeclarationColor);
			VisitChildrenAfter(parameterDeclaration, nameToken);
		}
		
		public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			var nameToken = eventDeclaration.NameToken;
			VisitChildrenUntil(eventDeclaration, nameToken);
			Colorize(nameToken, eventDeclarationColor);
			VisitChildrenAfter(eventDeclaration, nameToken);
		}
		
		public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			var nameToken = eventDeclaration.NameToken;
			VisitChildrenUntil(eventDeclaration, nameToken);
			if (!eventDeclaration.PrivateImplementationType.IsNull) {
				if (!CheckInterfaceImplementation (eventDeclaration)) {
					VisitChildrenAfter(eventDeclaration, nameToken);
					return;
				}
			}
			Colorize(nameToken, eventDeclarationColor);
			VisitChildrenAfter(eventDeclaration, nameToken);
		}
		
		public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			var nameToken = propertyDeclaration.NameToken;
			VisitChildrenUntil(propertyDeclaration, nameToken);
			if (!propertyDeclaration.PrivateImplementationType.IsNull) {
				if (!CheckInterfaceImplementation (propertyDeclaration)) {
					VisitChildrenAfter(propertyDeclaration, nameToken);
					return;
				}
			}
			Colorize(nameToken, propertyDeclarationColor);
			VisitChildrenAfter(propertyDeclaration, nameToken);
		}
		
		public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			base.VisitIndexerDeclaration(indexerDeclaration);
			if (!indexerDeclaration.PrivateImplementationType.IsNull) {
				CheckInterfaceImplementation (indexerDeclaration);
			}
		}
		
		public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			fieldDeclaration.ReturnType.AcceptVisitor (this);
			foreach (var init in fieldDeclaration.Variables) {
				Colorize (init.NameToken, fieldDeclarationColor);
				init.Initializer.AcceptVisitor (this);
			}
		}
		
		public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			fixedFieldDeclaration.ReturnType.AcceptVisitor (this);
			foreach (var init in fixedFieldDeclaration.Variables) {
				Colorize (init.NameToken, fieldDeclarationColor);
				init.CountExpression.AcceptVisitor (this);
			}
		}
		
		public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			HandleConstructorOrDestructor(constructorDeclaration);
		}
		
		public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			HandleConstructorOrDestructor(destructorDeclaration);
		}
		
		void HandleConstructorOrDestructor(AstNode constructorDeclaration)
		{
			Identifier nameToken = constructorDeclaration.GetChildByRole(Roles.Identifier);
			VisitChildrenUntil(constructorDeclaration, nameToken);
			var currentTypeDef = resolver.GetResolverStateBefore(constructorDeclaration).CurrentTypeDefinition;
			if (currentTypeDef != null && nameToken.Name == currentTypeDef.Name) {
				TColor color;
				if (TryGetTypeHighlighting (currentTypeDef.Kind, out color))
					Colorize(nameToken, color);
			}
			VisitChildrenAfter(constructorDeclaration, nameToken);
		}
		
		bool TryGetMemberColor(IMember member, out TColor color)
		{
			switch (member.SymbolKind) {
				case SymbolKind.Field:
					color = fieldAccessColor;
					return true;
				case SymbolKind.Property:
					color = propertyAccessColor;
					return true;
				case SymbolKind.Event:
					color = eventAccessColor;
					return true;
				case SymbolKind.Method:
					color = methodCallColor;
					return true;
				case SymbolKind.Constructor:
				case SymbolKind.Destructor:
					return TryGetTypeHighlighting (member.DeclaringType.Kind, out color);
				default:
					color = default (TColor);
					return false;
			}
		}
		
		TColor GetTypeHighlighting (ClassType classType)
		{
			switch (classType) {
				case ClassType.Class:
					return referenceTypeColor;
				case ClassType.Struct:
					return valueTypeColor;
				case ClassType.Interface:
					return interfaceTypeColor;
				case ClassType.Enum:
					return enumerationTypeColor;
				default:
					throw new InvalidOperationException ("Unknown class type :" + classType);
			}
		}
		
		bool TryGetTypeHighlighting (TypeKind kind, out TColor color)
		{
			switch (kind) {
				case TypeKind.Class:
					color = referenceTypeColor;
					return true;
				case TypeKind.Struct:
					color = valueTypeColor;
					return true;
				case TypeKind.Interface:
					color = interfaceTypeColor;
					return true;
				case TypeKind.Enum:
					color = enumerationTypeColor;
					return true;
				case TypeKind.TypeParameter:
					color = typeParameterTypeColor;
					return true;
				case TypeKind.Delegate:
					color = delegateTypeColor;
					return true;
				case TypeKind.Unknown:
				case TypeKind.Null:
					color = syntaxErrorColor;
					return true;
				default:
					color = default (TColor);
					return false;
			}
		}
		
		public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			var nameToken = typeDeclaration.NameToken;
			VisitChildrenUntil(typeDeclaration, nameToken);
			Colorize(nameToken, GetTypeHighlighting (typeDeclaration.ClassType));
			VisitChildrenAfter(typeDeclaration, nameToken);
		}
		
		public override void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
		{
			if (typeParameterDeclaration.Variance == VarianceModifier.Contravariant)
				Colorize(typeParameterDeclaration.VarianceToken, parameterModifierColor);
			
			//			bool isValueType = false;
			//			if (typeParameterDeclaration.Parent != null) {
			//				foreach (var constraint in typeParameterDeclaration.Parent.GetChildrenByRole(Roles.Constraint)) {
			//					if (constraint.TypeParameter.Identifier == typeParameterDeclaration.Name) {
			//						isValueType = constraint.BaseTypes.OfType<PrimitiveType>().Any(p => p.Keyword == "struct");
			//					}
			//				}
			//			}
			var nameToken = typeParameterDeclaration.NameToken;
			VisitChildrenUntil(typeParameterDeclaration, nameToken);
			Colorize(nameToken, typeParameterTypeColor);  /*isValueType ? valueTypeColor : referenceTypeColor*/
			VisitChildrenAfter(typeParameterDeclaration, nameToken);
		}
		
		public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			var nameToken = delegateDeclaration.NameToken;
			VisitChildrenUntil(delegateDeclaration, nameToken);
			Colorize(nameToken, delegateTypeColor);
			VisitChildrenAfter(delegateDeclaration, nameToken);
		}
		
		public override void VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			var nameToken = variableInitializer.NameToken;
			VisitChildrenUntil(variableInitializer, nameToken);
			if (variableInitializer.Parent is FieldDeclaration) {
				Colorize(nameToken, fieldDeclarationColor);
			} else if (variableInitializer.Parent is EventDeclaration) {
				Colorize(nameToken, eventDeclarationColor);
			} else {
				Colorize(nameToken, variableDeclarationColor);
			}
			VisitChildrenAfter(variableInitializer, nameToken);
		}
		
		public override void VisitComment(Comment comment)
		{
			if (comment.CommentType == CommentType.InactiveCode) {
				Colorize(comment, inactiveCodeColor);
			}
		}

		public override void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
		{
		}
		
		public override void VisitAttribute(ICSharpCode.NRefactory.CSharp.Attribute attribute)
		{
			ITypeDefinition attrDef = resolver.Resolve(attribute.Type, cancellationToken).Type.GetDefinition();
			if (attrDef != null && IsInactiveConditional(attrDef.Attributes)) {
				Colorize(attribute, inactiveCodeColor);
			} else {
				VisitChildren(attribute);
			}
		}
		
		public override void VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression)
		{
			foreach (var a in arrayInitializerExpression.Elements) {
				var namedElement = a as NamedExpression;
				if (namedElement != null) {
					var result = resolver.Resolve (namedElement, cancellationToken);
					if (result.IsError)
						Colorize (namedElement.NameToken, syntaxErrorColor);
					namedElement.Expression.AcceptVisitor (this);
				} else {
					a.AcceptVisitor (this);
				}
			}
		}

		int blockDepth;
		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			blockDepth++;
			base.VisitBlockStatement(blockStatement);
			blockDepth--;
		}
	}
}
