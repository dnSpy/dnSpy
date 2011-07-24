// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB
{
	/// <summary>
	/// Description of OutputVisitor.
	/// </summary>
	public class OutputVisitor : IAstVisitor<object, object>, IPatternAstVisitor<object, object>
	{
		readonly IOutputFormatter formatter;
		readonly VBFormattingOptions policy;
		
		readonly Stack<AstNode> containerStack = new Stack<AstNode>();
		readonly Stack<AstNode> positionStack = new Stack<AstNode>();
		
		/// <summary>
		/// Used to insert the minimal amount of spaces so that the lexer recognizes the tokens that were written.
		/// </summary>
		LastWritten lastWritten;
		
		enum LastWritten
		{
			Whitespace,
			Other,
			KeywordOrIdentifier
		}
		
		public OutputVisitor(TextWriter textWriter, VBFormattingOptions formattingPolicy)
		{
			if (textWriter == null)
				throw new ArgumentNullException("textWriter");
			if (formattingPolicy == null)
				throw new ArgumentNullException("formattingPolicy");
			this.formatter = new TextWriterOutputFormatter(textWriter);
			this.policy = formattingPolicy;
		}
		
		public OutputVisitor(IOutputFormatter formatter, VBFormattingOptions formattingPolicy)
		{
			if (formatter == null)
				throw new ArgumentNullException("formatter");
			if (formattingPolicy == null)
				throw new ArgumentNullException("formattingPolicy");
			this.formatter = formatter;
			this.policy = formattingPolicy;
		}
		
		public object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			// don't do node tracking as we visit all children directly
			foreach (AstNode node in compilationUnit.Children)
				node.AcceptVisitor(this, data);
			return null;
		}
		
		public object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			StartNode(blockStatement);
			foreach (var stmt in blockStatement) {
				stmt.AcceptVisitor(this, data);
				NewLine();
			}
			return EndNode(blockStatement);
		}
		
		public object VisitPatternPlaceholder(AstNode placeholder, Pattern pattern, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			StartNode(typeParameterDeclaration);
			
			switch (typeParameterDeclaration.Variance) {
				case ICSharpCode.NRefactory.TypeSystem.VarianceModifier.Invariant:
					break;
				case ICSharpCode.NRefactory.TypeSystem.VarianceModifier.Covariant:
					WriteKeyword("Out");
					break;
				case ICSharpCode.NRefactory.TypeSystem.VarianceModifier.Contravariant:
					WriteKeyword("In");
					break;
				default:
					throw new Exception("Invalid value for VarianceModifier");
			}
			
			WriteIdentifier(typeParameterDeclaration.Name);
			if (typeParameterDeclaration.Constraints.Any()) {
				WriteKeyword("As");
				if (typeParameterDeclaration.Constraints.Count > 1)
					WriteToken("{", TypeParameterDeclaration.Roles.LBrace);
				WriteCommaSeparatedList(typeParameterDeclaration.Constraints);
				if (typeParameterDeclaration.Constraints.Count > 1)
					WriteToken("}", TypeParameterDeclaration.Roles.RBrace);
			}
			
			return EndNode(typeParameterDeclaration);
		}
		
		public object VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			StartNode(parameterDeclaration);
			WriteAttributes(parameterDeclaration.Attributes);
			WriteModifiers(parameterDeclaration.ModifierTokens);
			WriteIdentifier(parameterDeclaration.Name.Name);
			if (!parameterDeclaration.Type.IsNull) {
				WriteKeyword("As");
				parameterDeclaration.Type.AcceptVisitor(this, data);
			}
			if (!parameterDeclaration.OptionalValue.IsNull) {
				WriteToken("=", ParameterDeclaration.Roles.Assign);
				parameterDeclaration.OptionalValue.AcceptVisitor(this, data);
			}
			return EndNode(parameterDeclaration);
		}
		
		public object VisitVBTokenNode(VBTokenNode vBTokenNode, object data)
		{
			var mod = vBTokenNode as VBModifierToken;
			if (mod != null) {
				StartNode(vBTokenNode);
				WriteKeyword(VBModifierToken.GetModifierName(mod.Modifier));
				return EndNode(vBTokenNode);
			} else {
				throw new NotSupportedException("Should never visit individual tokens");
			}
		}
		
		public object VisitAliasImportsClause(AliasImportsClause aliasImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitAttribute(ICSharpCode.NRefactory.VB.Ast.Attribute attribute, object data)
		{
			StartNode(attribute);
			
			if (attribute.Target != AttributeTarget.None) {
				switch (attribute.Target) {
					case AttributeTarget.None:
						break;
					case AttributeTarget.Assembly:
						WriteKeyword("Assembly");
						break;
					case AttributeTarget.Module:
						WriteKeyword("Module");
						break;
					default:
						throw new Exception("Invalid value for AttributeTarget");
				}
				WriteToken(":", Ast.Attribute.Roles.Colon);
				Space();
			}
			attribute.Type.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(attribute.Arguments, false);
			
			return EndNode(attribute);
		}
		
		public object VisitAttributeBlock(AttributeBlock attributeBlock, object data)
		{
			StartNode(attributeBlock);
			
			WriteToken("<", AttributeBlock.Roles.LChevron);
			WriteCommaSeparatedList(attributeBlock.Attributes);
			WriteToken(">", AttributeBlock.Roles.RChevron);
			if (attributeBlock.Parent is ParameterDeclaration)
				Space();
			else
				NewLine();
			
			return EndNode(attributeBlock);
		}
		
		public object VisitImportsStatement(ImportsStatement importsStatement, object data)
		{
			StartNode(importsStatement);
			
			WriteKeyword("Imports", AstNode.Roles.Keyword);
			Space();
			WriteCommaSeparatedList(importsStatement.ImportsClauses);
			NewLine();
			
			return EndNode(importsStatement);
		}
		
		public object VisitMemberImportsClause(MemberImportsClause memberImportsClause, object data)
		{
			StartNode(memberImportsClause);
			memberImportsClause.Member.AcceptVisitor(this, data);
			return EndNode(memberImportsClause);
		}
		
		public object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			StartNode(namespaceDeclaration);
			NewLine();
			WriteKeyword("Namespace");
			bool isFirst = true;
			foreach (Identifier node in namespaceDeclaration.Identifiers) {
				if (isFirst) {
					isFirst = false;
				} else {
					WriteToken(".", NamespaceDeclaration.Roles.Dot);
				}
				node.AcceptVisitor(this, null);
			}
			NewLine();
			WriteMembers(namespaceDeclaration.Members);
			WriteKeyword("End");
			WriteKeyword("Namespace");
			NewLine();
			return EndNode(namespaceDeclaration);
		}
		
		public object VisitOptionStatement(OptionStatement optionStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			StartNode(typeDeclaration);
			WriteAttributes(typeDeclaration.Attributes);
			WriteModifiers(typeDeclaration.ModifierTokens);
			WriteClassTypeKeyword(typeDeclaration);
			WriteIdentifier(typeDeclaration.Name.Name);
			MarkFoldStart();
			NewLine();
			
			if (!typeDeclaration.InheritsType.IsNull) {
				Indent();
				WriteKeyword("Inherits");
				typeDeclaration.InheritsType.AcceptVisitor(this, data);
				Unindent();
				NewLine();
			}
			if (typeDeclaration.ImplementsTypes.Any()) {
				Indent();
				WriteImplementsClause(typeDeclaration.ImplementsTypes);
				Unindent();
				NewLine();
			}
			
			if (!typeDeclaration.InheritsType.IsNull || typeDeclaration.ImplementsTypes.Any())
			NewLine();
			
			WriteMembers(typeDeclaration.Members);
			
			WriteKeyword("End");
			WriteClassTypeKeyword(typeDeclaration);
			MarkFoldEnd();
			NewLine();
			return EndNode(typeDeclaration);
		}

		void WriteClassTypeKeyword(TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.ClassType) {
				case ICSharpCode.NRefactory.TypeSystem.ClassType.Class:
					WriteKeyword("Class");
					break;
				case ICSharpCode.NRefactory.TypeSystem.ClassType.Enum:
					break;
				case ICSharpCode.NRefactory.TypeSystem.ClassType.Interface:
					WriteKeyword("Interface");
					break;
				case ICSharpCode.NRefactory.TypeSystem.ClassType.Struct:
					WriteKeyword("Structure");
					break;
				case ICSharpCode.NRefactory.TypeSystem.ClassType.Delegate:
					break;
				case ICSharpCode.NRefactory.TypeSystem.ClassType.Module:
					WriteKeyword("Module");
					break;
				default:
					throw new Exception("Invalid value for ClassType");
			}
		}
		
		public object VisitXmlNamespaceImportsClause(XmlNamespaceImportsClause xmlNamespaceImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitEnumDeclaration(EnumDeclaration enumDeclaration, object data)
		{
			StartNode(enumDeclaration);
			
			WriteAttributes(enumDeclaration.Attributes);
			WriteModifiers(enumDeclaration.ModifierTokens);
			WriteKeyword("Enum");
			WriteIdentifier(enumDeclaration.Name.Name);
			if (!enumDeclaration.UnderlyingType.IsNull) {
				Space();
				WriteKeyword("As");
				enumDeclaration.UnderlyingType.AcceptVisitor(this, data);
			}
			MarkFoldStart();
			NewLine();
			
			Indent();
			foreach (var member in enumDeclaration.Members) {
				member.AcceptVisitor(this, null);
			}
			Unindent();
			
			WriteKeyword("End");
			WriteKeyword("Enum");
			MarkFoldEnd();
			NewLine();
			
			return EndNode(enumDeclaration);
		}
		
		public object VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			StartNode(enumMemberDeclaration);
			
			WriteAttributes(enumMemberDeclaration.Attributes);
			WriteIdentifier(enumMemberDeclaration.Name.Name);
			
			if (!enumMemberDeclaration.Value.IsNull) {
				Space();
				WriteToken("=", EnumMemberDeclaration.Roles.Assign);
				Space();
				enumMemberDeclaration.Value.AcceptVisitor(this, data);
			}
			NewLine();
			
			return EndNode(enumMemberDeclaration);
		}
		
		public object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			StartNode(delegateDeclaration);
			
			WriteAttributes(delegateDeclaration.Attributes);
			WriteModifiers(delegateDeclaration.ModifierTokens);
			WriteKeyword("Delegate");
			if (delegateDeclaration.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			WriteIdentifier(delegateDeclaration.Name.Name);
			WriteTypeParameters(delegateDeclaration.TypeParameters);
			WriteCommaSeparatedListInParenthesis(delegateDeclaration.Parameters, false);
			if (!delegateDeclaration.IsSub) {
				Space();
				WriteKeyword("As");
				WriteAttributes(delegateDeclaration.ReturnTypeAttributes);
				delegateDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			NewLine();
			
			return EndNode(delegateDeclaration);
		}
		
		public object VisitIdentifier(Identifier identifier, object data)
		{
			StartNode(identifier);
			WriteIdentifier(identifier.Name);
			WriteTypeCharacter(identifier.TypeCharacter);
			return EndNode(identifier);
		}
		
		public object VisitXmlIdentifier(XmlIdentifier xmlIdentifier, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitXmlLiteralString(XmlLiteralString xmlLiteralString, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitSimpleNameExpression(SimpleNameExpression simpleNameExpression, object data)
		{
			StartNode(simpleNameExpression);
			
			simpleNameExpression.Identifier.AcceptVisitor(this, data);
			WriteTypeArguments(simpleNameExpression.TypeArguments);
			
			return EndNode(simpleNameExpression);
		}
		
		public object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			StartNode(primitiveExpression);
			
			WritePrimitiveValue(primitiveExpression.Value);
			
			return EndNode(primitiveExpression);
		}
		
		public object VisitInstanceExpression(InstanceExpression instanceExpression, object data)
		{
			StartNode(instanceExpression);
			
			switch (instanceExpression.Type) {
				case InstanceExpressionType.Me:
					WriteKeyword("Me");
					break;
				case InstanceExpressionType.MyBase:
					WriteKeyword("MyBase");
					break;
				case InstanceExpressionType.MyClass:
					WriteKeyword("MyClass");
					break;
				default:
					throw new Exception("Invalid value for InstanceExpressionType");
			}
			
			return EndNode(instanceExpression);
		}
		
		public object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			StartNode(parenthesizedExpression);
			
			LPar();
			parenthesizedExpression.Expression.AcceptVisitor(this, data);
			RPar();
			
			return EndNode(parenthesizedExpression);
		}
		
		public object VisitGetTypeExpression(GetTypeExpression getTypeExpression, object data)
		{
			StartNode(getTypeExpression);
			
			WriteKeyword("GetType");
			LPar();
			getTypeExpression.Type.AcceptVisitor(this, data);
			RPar();
			
			return EndNode(getTypeExpression);
		}
		
		public object VisitTypeOfIsExpression(TypeOfIsExpression typeOfIsExpression, object data)
		{
			StartNode(typeOfIsExpression);
			
			WriteKeyword("TypeOf");
			typeOfIsExpression.TypeOfExpression.AcceptVisitor(this, data);
			WriteKeyword("Is");
			typeOfIsExpression.Type.AcceptVisitor(this, data);
			
			return EndNode(typeOfIsExpression);
		}
		
		public object VisitGetXmlNamespaceExpression(GetXmlNamespaceExpression getXmlNamespaceExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitMemberAccessExpression(MemberAccessExpression memberAccessExpression, object data)
		{
			StartNode(memberAccessExpression);
			
			memberAccessExpression.Target.AcceptVisitor(this, data);
			WriteToken(".", MemberAccessExpression.Roles.Dot);
			memberAccessExpression.MemberName.AcceptVisitor(this, data);
			WriteTypeArguments(memberAccessExpression.TypeArguments);
			
			return EndNode(memberAccessExpression);
		}
		
		public object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			StartNode(typeReferenceExpression);
			
			typeReferenceExpression.Type.AcceptVisitor(this, data);
			
			return EndNode(typeReferenceExpression);
		}
		
		public object VisitEventMemberSpecifier(EventMemberSpecifier eventMemberSpecifier, object data)
		{
			StartNode(eventMemberSpecifier);
			
			eventMemberSpecifier.Target.AcceptVisitor(this, data);
			WriteToken(".", EventMemberSpecifier.Roles.Dot);
			eventMemberSpecifier.Member.AcceptVisitor(this, data);
			
			return EndNode(eventMemberSpecifier);
		}
		
		public object VisitInterfaceMemberSpecifier(InterfaceMemberSpecifier interfaceMemberSpecifier, object data)
		{
			StartNode(interfaceMemberSpecifier);
			
			interfaceMemberSpecifier.Target.AcceptVisitor(this, data);
			WriteToken(".", EventMemberSpecifier.Roles.Dot);
			interfaceMemberSpecifier.Member.AcceptVisitor(this, data);
			
			return EndNode(interfaceMemberSpecifier);
		}
		
		#region TypeMembers
		public object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			StartNode(constructorDeclaration);
			
			WriteAttributes(constructorDeclaration.Attributes);
			WriteModifiers(constructorDeclaration.ModifierTokens);
			WriteKeyword("Sub");
			WriteKeyword("New");
			WriteCommaSeparatedListInParenthesis(constructorDeclaration.Parameters, false);
			MarkFoldStart();
			NewLine();
			
			Indent();
			WriteBlock(constructorDeclaration.Body);
			Unindent();
			
			WriteKeyword("End");
			WriteKeyword("Sub");
			MarkFoldEnd();
			NewLine();
			
			return EndNode(constructorDeclaration);
		}
		
		public object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			StartNode(methodDeclaration);
			
			WriteAttributes(methodDeclaration.Attributes);
			WriteModifiers(methodDeclaration.ModifierTokens);
			if (methodDeclaration.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			methodDeclaration.Name.AcceptVisitor(this, data);
			WriteTypeParameters(methodDeclaration.TypeParameters);
			WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, false);
			if (!methodDeclaration.IsSub && !methodDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				WriteAttributes(methodDeclaration.ReturnTypeAttributes);
				methodDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			WriteHandlesClause(methodDeclaration.HandlesClause);
			WriteImplementsClause(methodDeclaration.ImplementsClause);
			if (!methodDeclaration.Body.IsNull) {
				MarkFoldStart();
				NewLine();
				Indent();
				WriteBlock(methodDeclaration.Body);
				Unindent();
				WriteKeyword("End");
				if (methodDeclaration.IsSub)
					WriteKeyword("Sub");
				else
					WriteKeyword("Function");
				MarkFoldEnd();
			}
			NewLine();
			
			return EndNode(methodDeclaration);
		}

		public object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			StartNode(fieldDeclaration);
			
			WriteAttributes(fieldDeclaration.Attributes);
			WriteModifiers(fieldDeclaration.ModifierTokens);
			WriteCommaSeparatedList(fieldDeclaration.Variables);
			NewLine();
			
			return EndNode(fieldDeclaration);
		}
		
		public object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			StartNode(propertyDeclaration);
			
			WriteAttributes(propertyDeclaration.Attributes);
			WriteModifiers(propertyDeclaration.ModifierTokens);
			WriteKeyword("Property");
			WriteIdentifier(propertyDeclaration.Name.Name);
			WriteCommaSeparatedListInParenthesis(propertyDeclaration.Parameters, false);
			if (!propertyDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				WriteAttributes(propertyDeclaration.ReturnTypeAttributes);
				propertyDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			
			bool needsBody = !propertyDeclaration.Getter.Body.IsNull || !propertyDeclaration.Setter.Body.IsNull;
			
			if (needsBody) {
				MarkFoldStart();
				NewLine();
				Indent();
				
				if (!propertyDeclaration.Getter.Body.IsNull) {
					propertyDeclaration.Getter.AcceptVisitor(this, data);
				}
				
				if (!propertyDeclaration.Setter.Body.IsNull) {
					propertyDeclaration.Setter.AcceptVisitor(this, data);
				}
				Unindent();
				
				WriteKeyword("End");
				WriteKeyword("Property");
				MarkFoldEnd();
			}
			NewLine();
			
			return EndNode(propertyDeclaration);
		}
		#endregion
		
		#region TypeName
		public object VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			StartNode(primitiveType);
			
			WriteKeyword(primitiveType.Keyword);
			
			return EndNode(primitiveType);
		}
		
		public object VisitQualifiedType(QualifiedType qualifiedType, object data)
		{
			StartNode(qualifiedType);
			
			qualifiedType.Target.AcceptVisitor(this, data);
			WriteToken(".", AstNode.Roles.Dot);
			WriteIdentifier(qualifiedType.Name);
			WriteTypeArguments(qualifiedType.TypeArguments);
			
			return EndNode(qualifiedType);
		}
		
		public object VisitComposedType(ComposedType composedType, object data)
		{
			StartNode(composedType);
			
			composedType.BaseType.AcceptVisitor(this, data);
			if (composedType.HasNullableSpecifier)
				WriteToken("?", ComposedType.Roles.QuestionMark);
			WriteArraySpecifiers(composedType.ArraySpecifiers);
			
			return EndNode(composedType);
		}
		
		public object VisitArraySpecifier(ArraySpecifier arraySpecifier, object data)
		{
			StartNode(arraySpecifier);
			
			LPar();
			for (int i = 0; i < arraySpecifier.Dimensions - 1; i++) {
				WriteToken(",", ArraySpecifier.Roles.Comma);
			}
			RPar();
			
			return EndNode(arraySpecifier);
		}
		
		public object VisitSimpleType(SimpleType simpleType, object data)
		{
			StartNode(simpleType);
			
			WriteIdentifier(simpleType.Identifier);
			WriteTypeArguments(simpleType.TypeArguments);
			
			return EndNode(simpleType);
		}
		#endregion
		
		#region Pattern Matching
		public object VisitAnyNode(AnyNode anyNode, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitBackreference(Backreference backreference, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitChoice(Choice choice, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitNamedNode(NamedNode namedNode, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitRepeat(Repeat repeat, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitOptionalNode(OptionalNode optionalNode, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitIdentifierExpressionBackreference(IdentifierExpressionBackreference identifierExpressionBackreference, object data)
		{
			throw new NotImplementedException();
		}
		#endregion
		
		#region StartNode/EndNode
		void StartNode(AstNode node)
		{
			// Ensure that nodes are visited in the proper nested order.
			// Jumps to different subtrees are allowed only for the child of a placeholder node.
			Debug.Assert(containerStack.Count == 0 || node.Parent == containerStack.Peek());
			if (positionStack.Count > 0)
				WriteSpecialsUpToNode(node);
			containerStack.Push(node);
			positionStack.Push(node.FirstChild);
			formatter.StartNode(node);
		}
		
		object EndNode(AstNode node)
		{
			Debug.Assert(node == containerStack.Peek());
			AstNode pos = positionStack.Pop();
			Debug.Assert(pos == null || pos.Parent == node);
			WriteSpecials(pos, null);
			containerStack.Pop();
			formatter.EndNode(node);
			return null;
		}
		#endregion
		
		#region WriteSpecials
		/// <summary>
		/// Writes all specials from start to end (exclusive). Does not touch the positionStack.
		/// </summary>
		void WriteSpecials(AstNode start, AstNode end)
		{
			for (AstNode pos = start; pos != end; pos = pos.NextSibling) {
				if (pos.Role == AstNode.Roles.Comment) {
					pos.AcceptVisitor(this, null);
				}
			}
		}
		
		/// <summary>
		/// Writes all specials between the current position (in the positionStack) and the next
		/// node with the specified role. Advances the current position.
		/// </summary>
		void WriteSpecialsUpToRole(Role role)
		{
			for (AstNode pos = positionStack.Peek(); pos != null; pos = pos.NextSibling) {
				if (pos.Role == role) {
					WriteSpecials(positionStack.Pop(), pos);
					positionStack.Push(pos);
					break;
				}
			}
		}
		
		/// <summary>
		/// Writes all specials between the current position (in the positionStack) and the specified node.
		/// Advances the current position.
		/// </summary>
		void WriteSpecialsUpToNode(AstNode node)
		{
			for (AstNode pos = positionStack.Peek(); pos != null; pos = pos.NextSibling) {
				if (pos == node) {
					WriteSpecials(positionStack.Pop(), pos);
					positionStack.Push(pos);
					break;
				}
			}
		}
		
		void WriteSpecialsUpToRole(Role role, AstNode nextNode)
		{
			// Look for the role between the current position and the nextNode.
			for (AstNode pos = positionStack.Peek(); pos != null && pos != nextNode; pos = pos.NextSibling) {
				if (pos.Role == AstNode.Roles.Comma) {
					WriteSpecials(positionStack.Pop(), pos);
					positionStack.Push(pos);
					break;
				}
			}
		}
		#endregion
		
		#region Comma
		/// <summary>
		/// Writes a comma.
		/// </summary>
		/// <param name="nextNode">The next node after the comma.</param>
		/// <param name="noSpacesAfterComma">When set prevents printing a space after comma.</param>
		void Comma(AstNode nextNode, bool noSpaceAfterComma = false)
		{
			WriteSpecialsUpToRole(AstNode.Roles.Comma, nextNode);
			formatter.WriteToken(",");
			lastWritten = LastWritten.Other;
			Space(!noSpaceAfterComma); // TODO: Comma policy has changed.
		}
		
		void WriteCommaSeparatedList(IEnumerable<AstNode> list)
		{
			bool isFirst = true;
			foreach (AstNode node in list) {
				if (isFirst) {
					isFirst = false;
				} else {
					Comma(node);
				}
				node.AcceptVisitor(this, null);
			}
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<AstNode> list, bool spaceWithin)
		{
			LPar();
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			RPar();
		}
		
		#if DOTNET35
		void WriteCommaSeparatedList(IEnumerable<VariableInitializer> list)
		{
			WriteCommaSeparatedList(list);
		}
		
		void WriteCommaSeparatedList(IEnumerable<AstType> list)
		{
			WriteCommaSeparatedList(list);
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<Expression> list, bool spaceWithin)
		{
			WriteCommaSeparatedListInParenthesis(list.SafeCast<Expression, AstNode>(), spaceWithin);
		}
		
		void WriteCommaSeparatedListInParenthesis(IEnumerable<ParameterDeclaration> list, bool spaceWithin)
		{
			WriteCommaSeparatedListInParenthesis(list.SafeCast<ParameterDeclaration, AstNode>(), spaceWithin);
		}

		#endif

		void WriteCommaSeparatedListInBrackets(IEnumerable<ParameterDeclaration> list, bool spaceWithin)
		{
			WriteToken("[", AstNode.Roles.LBracket);
			if (list.Any()) {
				Space(spaceWithin);
				WriteCommaSeparatedList(list);
				Space(spaceWithin);
			}
			WriteToken("]", AstNode.Roles.RBracket);
		}

		void WriteCommaSeparatedListInBrackets(IEnumerable<Expression> list)
		{
			WriteToken ("[", AstNode.Roles.LBracket);
			if (list.Any ()) {
				Space();
				WriteCommaSeparatedList(list);
				Space();
			}
			WriteToken ("]", AstNode.Roles.RBracket);
		}
		#endregion
		
		#region Write tokens
		/// <summary>
		/// Writes a keyword, and all specials up to
		/// </summary>
		void WriteKeyword(string keyword, Role<VBTokenNode> tokenRole = null)
		{
			WriteSpecialsUpToRole(tokenRole ?? AstNode.Roles.Keyword);
			if (lastWritten == LastWritten.KeywordOrIdentifier)
				formatter.Space();
			formatter.WriteKeyword(keyword);
			lastWritten = LastWritten.KeywordOrIdentifier;
		}
		
		void WriteIdentifier(string identifier, Role<Identifier> identifierRole = null)
		{
			WriteSpecialsUpToRole(identifierRole ?? AstNode.Roles.Identifier);
			if (IsKeyword(identifier, containerStack.Peek())) {
				if (lastWritten == LastWritten.KeywordOrIdentifier)
					Space(); // this space is not strictly required, so we call Space()
				formatter.WriteToken("[");
			} else if (lastWritten == LastWritten.KeywordOrIdentifier) {
				formatter.Space(); // this space is strictly required, so we directly call the formatter
			}
			formatter.WriteIdentifier(identifier);
			if (IsKeyword(identifier, containerStack.Peek())) {
				formatter.WriteToken("]");
			}
			lastWritten = LastWritten.KeywordOrIdentifier;
		}
		
		void WriteToken(string token, Role<VBTokenNode> tokenRole)
		{
			WriteSpecialsUpToRole(tokenRole);
			// Avoid that two +, - or ? tokens are combined into a ++, -- or ?? token.
			// Note that we don't need to handle tokens like = because there's no valid
			// C# program that contains the single token twice in a row.
			// (for +, - and &, this can happen with unary operators;
			// for ?, this can happen in "a is int? ? b : c" or "a as int? ?? 0";
			// and for /, this can happen with "1/ *ptr" or "1/ //comment".)
//			if (lastWritten == LastWritten.Plus && token[0] == '+'
//			    || lastWritten == LastWritten.Minus && token[0] == '-'
//			    || lastWritten == LastWritten.Ampersand && token[0] == '&'
//			    || lastWritten == LastWritten.QuestionMark && token[0] == '?'
//			    || lastWritten == LastWritten.Division && token[0] == '*')
//			{
//				formatter.Space();
//			}
			formatter.WriteToken(token);
//			if (token == "+")
//				lastWritten = LastWritten.Plus;
//			else if (token == "-")
//				lastWritten = LastWritten.Minus;
//			else if (token == "&")
//				lastWritten = LastWritten.Ampersand;
//			else if (token == "?")
//				lastWritten = LastWritten.QuestionMark;
//			else if (token == "/")
//				lastWritten = LastWritten.Division;
//			else
			lastWritten = LastWritten.Other;
		}
		
		void WriteTypeCharacter(TypeCode typeCharacter)
		{
			switch (typeCharacter) {
				case TypeCode.Empty:
				case TypeCode.Object:
				case TypeCode.DBNull:
				case TypeCode.Boolean:
				case TypeCode.Char:
					
					break;
				case TypeCode.SByte:
					
					break;
				case TypeCode.Byte:
					
					break;
				case TypeCode.Int16:
					
					break;
				case TypeCode.UInt16:
					
					break;
				case TypeCode.Int32:
					WriteToken("%", null);
					break;
				case TypeCode.UInt32:
					
					break;
				case TypeCode.Int64:
					WriteToken("&", null);
					break;
				case TypeCode.UInt64:
					
					break;
				case TypeCode.Single:
					WriteToken("!", null);
					break;
				case TypeCode.Double:
					WriteToken("#", null);
					break;
				case TypeCode.Decimal:
					WriteToken("@", null);
					break;
				case TypeCode.DateTime:
					
					break;
				case TypeCode.String:
					WriteToken("$", null);
					break;
				default:
					throw new Exception("Invalid value for TypeCode");
			}
		}
		
		void LPar()
		{
			WriteToken("(", AstNode.Roles.LPar);
		}
		
		void RPar()
		{
			WriteToken(")", AstNode.Roles.LPar);
		}
		
		/// <summary>
		/// Writes a space depending on policy.
		/// </summary>
		void Space(bool addSpace = true)
		{
			if (addSpace) {
				formatter.Space();
				lastWritten = LastWritten.Whitespace;
			}
		}
		
		void NewLine()
		{
			formatter.NewLine();
			lastWritten = LastWritten.Whitespace;
		}
		
		void Indent()
		{
			formatter.Indent();
		}
		
		void Unindent()
		{
			formatter.Unindent();
		}
		
		void MarkFoldStart()
		{
			formatter.MarkFoldStart();
		}
		
		void MarkFoldEnd()
		{
			formatter.MarkFoldEnd();
		}
		#endregion
		
		#region IsKeyword Test
		static readonly HashSet<string> unconditionalKeywords = new HashSet<string> {
			"AddHandler", "AddressOf", "Alias", "And", "AndAlso", "As", "Boolean", "ByRef", "Byte",
			"ByVal", "Call", "Case", "Catch", "CBool", "CByte", "CChar", "CInt", "Class", "CLng",
			"CObj", "Const", "Continue", "CSByte", "CShort", "CSng", "CStr", "CType", "CUInt",
			"CULng", "CUShort", "Date", "Decimal", "Declare", "Default", "Delegate", "Dim",
			"DirectCast", "Do", "Double", "Each", "Else", "ElseIf", "End", "EndIf", "Enum", "Erase",
			"Error", "Event", "Exit", "False", "Finally", "For", "Friend", "Function", "Get",
			"GetType", "GetXmlNamespace", "Global", "GoSub", "GoTo", "Handles", "If", "Implements",
			"Imports", "In", "Inherits", "Integer", "Interface", "Is", "IsNot", "Let", "Lib", "Like",
			"Long", "Loop", "Me", "Mod", "Module", "MustInherit", "MustOverride", "MyBase", "MyClass",
			"Namespace", "Narrowing", "New", "Next", "Not", "Nothing", "NotInheritable", "NotOverridable",
			"Object", "Of", "On", "Operator", "Option", "Optional", "Or", "OrElse", "Overloads",
			"Overridable", "Overrides", "ParamArray", "Partial", "Private", "Property", "Protected",
			"Public", "RaiseEvent", "ReadOnly", "ReDim", "REM", "RemoveHandler", "Resume", "Return",
			"SByte", "Select", "Set", "Shadows", "Shared", "Short", "Single", "Static", "Step", "Stop",
			"String", "Structure", "Sub", "SyncLock", "Then", "Throw", "To", "True", "Try", "TryCast",
			"TypeOf", "UInteger", "ULong", "UShort", "Using", "Variant", "Wend", "When", "While",
			"Widening", "With", "WithEvents", "WriteOnly", "Xor"
		};
		
		static readonly HashSet<string> queryKeywords = new HashSet<string> {
			
		};
		
		/// <summary>
		/// Determines whether the specified identifier is a keyword in the given context.
		/// </summary>
		public static bool IsKeyword(string identifier, AstNode context)
		{
			if (unconditionalKeywords.Contains(identifier))
				return true;
//			if (context.Ancestors.Any(a => a is QueryExpression)) {
//				if (queryKeywords.Contains(identifier))
//					return true;
//			}
			return false;
		}
		#endregion
		
		#region Write constructs
		void WriteTypeArguments(IEnumerable<AstType> typeArguments)
		{
			if (typeArguments.Any()) {
				LPar();
				WriteKeyword("Of");
				WriteCommaSeparatedList(typeArguments);
				RPar();
			}
		}
		
		void WriteTypeParameters(IEnumerable<TypeParameterDeclaration> typeParameters)
		{
			if (typeParameters.Any()) {
				LPar();
				WriteKeyword("Of");
				WriteCommaSeparatedList(typeParameters);
				RPar();
			}
		}
		
		void WriteModifiers(IEnumerable<VBModifierToken> modifierTokens)
		{
			foreach (VBModifierToken modifier in modifierTokens) {
				modifier.AcceptVisitor(this, null);
			}
		}
		
		void WriteArraySpecifiers(IEnumerable<ArraySpecifier> arraySpecifiers)
		{
			foreach (ArraySpecifier specifier in arraySpecifiers) {
				specifier.AcceptVisitor(this, null);
			}
		}
		
		void WriteQualifiedIdentifier(IEnumerable<Identifier> identifiers)
		{
			bool first = true;
			foreach (Identifier ident in identifiers) {
				if (first) {
					first = false;
					if (lastWritten == LastWritten.KeywordOrIdentifier)
						formatter.Space();
				} else {
					WriteSpecialsUpToRole(AstNode.Roles.Dot, ident);
					formatter.WriteToken(".");
					lastWritten = LastWritten.Other;
				}
				WriteSpecialsUpToNode(ident);
				formatter.WriteIdentifier(ident.Name);
				lastWritten = LastWritten.KeywordOrIdentifier;
			}
		}
		
		void WriteEmbeddedStatement(Statement embeddedStatement)
		{
			if (embeddedStatement.IsNull)
				return;
			BlockStatement block = embeddedStatement as BlockStatement;
			if (block != null)
				VisitBlockStatement(block, null);
			else
				embeddedStatement.AcceptVisitor(this, null);
		}
		
		void WriteBlock(BlockStatement body)
		{
			if (body.IsNull)
				NewLine();
			else
				VisitBlockStatement(body, null);
		}
		
		void WriteMembers(IEnumerable<AstNode> members)
		{
			Indent();
			bool isFirst = true;
			foreach (var member in members) {
				if (isFirst) {
					isFirst = false;
				} else {
					NewLine();
				}
				member.AcceptVisitor(this, null);
			}
			Unindent();
		}
		
		void WriteAttributes(IEnumerable<AttributeBlock> attributes)
		{
			foreach (AttributeBlock attr in attributes) {
				attr.AcceptVisitor(this, null);
			}
		}
		
		void WritePrivateImplementationType(AstType privateImplementationType)
		{
			if (!privateImplementationType.IsNull) {
				privateImplementationType.AcceptVisitor(this, null);
				WriteToken(".", AstNode.Roles.Dot);
			}
		}
		
		void WriteImplementsClause(AstNodeCollection<InterfaceMemberSpecifier> implementsClause)
		{
			if (implementsClause.Any()) {
				Space();
				WriteKeyword("Implements");
				WriteCommaSeparatedList(implementsClause);
			}
		}
		
		void WriteImplementsClause(AstNodeCollection<AstType> implementsClause)
		{
			if (implementsClause.Any()) {
				WriteKeyword("Implements");
				WriteCommaSeparatedList(implementsClause);
			}
		}
		
		void WriteHandlesClause(AstNodeCollection<EventMemberSpecifier> handlesClause)
		{
			if (handlesClause.Any()) {
				Space();
				WriteKeyword("Handles");
				WriteCommaSeparatedList(handlesClause);
			}
		}
		
		void WritePrimitiveValue(object val)
		{
			if (val == null) {
				WriteKeyword("Nothing");
				return;
			}
			
			if (val is bool) {
				if ((bool)val) {
					WriteKeyword("True");
				} else {
					WriteKeyword("False");
				}
				return;
			}
			
			if (val is string) {
				formatter.WriteToken("\"" + ConvertString(val.ToString()) + "\"");
				lastWritten = LastWritten.Other;
			} else if (val is char) {
				formatter.WriteToken("\"" + ConvertCharLiteral((char)val) + "\"c");
				lastWritten = LastWritten.Other;
			} else if (val is decimal) {
				formatter.WriteToken(((decimal)val).ToString(NumberFormatInfo.InvariantInfo) + "D");
				lastWritten = LastWritten.Other;
			} else if (val is float) {
				float f = (float)val;
				if (float.IsInfinity(f) || float.IsNaN(f)) {
					// Strictly speaking, these aren't PrimitiveExpressions;
					// but we still support writing these to make life easier for code generators.
					WriteKeyword("Single");
					WriteToken(".", AstNode.Roles.Dot);
					if (float.IsPositiveInfinity(f))
						WriteIdentifier("PositiveInfinity");
					else if (float.IsNegativeInfinity(f))
						WriteIdentifier("NegativeInfinity");
					else
						WriteIdentifier("NaN");
					return;
				}
				formatter.WriteToken(f.ToString("R", NumberFormatInfo.InvariantInfo) + "F");
				lastWritten = LastWritten.Other;
			} else if (val is double) {
				double f = (double)val;
				if (double.IsInfinity(f) || double.IsNaN(f)) {
					// Strictly speaking, these aren't PrimitiveExpressions;
					// but we still support writing these to make life easier for code generators.
					WriteKeyword("Double");
					WriteToken(".", AstNode.Roles.Dot);
					if (double.IsPositiveInfinity(f))
						WriteIdentifier("PositiveInfinity");
					else if (double.IsNegativeInfinity(f))
						WriteIdentifier("NegativeInfinity");
					else
						WriteIdentifier("NaN");
					return;
				}
				string number = f.ToString("R", NumberFormatInfo.InvariantInfo);
				if (number.IndexOf('.') < 0 && number.IndexOf('E') < 0)
					number += ".0";
				formatter.WriteToken(number);
				// needs space if identifier follows number; this avoids mistaking the following identifier as type suffix
				lastWritten = LastWritten.KeywordOrIdentifier;
			} else if (val is IFormattable) {
				StringBuilder b = new StringBuilder();
//				if (primitiveExpression.LiteralFormat == LiteralFormat.HexadecimalNumber) {
//					b.Append("0x");
//					b.Append(((IFormattable)val).ToString("x", NumberFormatInfo.InvariantInfo));
//				} else {
				b.Append(((IFormattable)val).ToString(null, NumberFormatInfo.InvariantInfo));
//				}
				if (val is uint || val is ulong) {
					b.Append("U");
				}
				if (val is long || val is ulong) {
					b.Append("L");
				}
				formatter.WriteToken(b.ToString());
				// needs space if identifier follows number; this avoids mistaking the following identifier as type suffix
				lastWritten = LastWritten.KeywordOrIdentifier;
			} else {
				formatter.WriteToken(val.ToString());
				lastWritten = LastWritten.Other;
			}
		}
		#endregion
		
		#region ConvertLiteral
		static string ConvertCharLiteral(char ch)
		{
			if (ch == '"') return "\"\"";
			return ch.ToString();
		}
		
		static string ConvertString(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in str) {
				sb.Append(ConvertCharLiteral(ch));
			}
			return sb.ToString();
		}
		#endregion
		
		public object VisitVariableIdentifier(VariableIdentifier variableIdentifier, object data)
		{
			StartNode(variableIdentifier);
			
			WriteIdentifier(variableIdentifier.Name.Name);
			if (variableIdentifier.HasNullableSpecifier)
				WriteToken("?", VariableIdentifier.Roles.QuestionMark);
			WriteArraySpecifiers(variableIdentifier.ArraySpecifiers);
			
			return EndNode(variableIdentifier);
		}
		
		public object VisitAccessor(Accessor accessor, object data)
		{
			StartNode(accessor);
			WriteAttributes(accessor.Attributes);
			WriteModifiers(accessor.ModifierTokens);
			if (accessor.Role == PropertyDeclaration.GetterRole) {
				WriteKeyword("Get");
			} else if (accessor.Role == PropertyDeclaration.SetterRole) {
				WriteKeyword("Set");
			} else if (accessor.Role == EventDeclaration.AddHandlerRole) {
				WriteKeyword("AddHandler");
			} else if (accessor.Role == EventDeclaration.RemoveHandlerRole) {
				WriteKeyword("RemoveHandler");
			} else if (accessor.Role == EventDeclaration.RaiseEventRole) {
				WriteKeyword("RaiseEvent");
			}
			if (accessor.Parameters.Any())
				WriteCommaSeparatedListInParenthesis(accessor.Parameters, false);
			NewLine();
			Indent();
			WriteBlock(accessor.Body);
			Unindent();
			WriteKeyword("End");

			if (accessor.Role == PropertyDeclaration.GetterRole) {
				WriteKeyword("Get");
			} else if (accessor.Role == PropertyDeclaration.SetterRole) {
				WriteKeyword("Set");
			} else if (accessor.Role == EventDeclaration.AddHandlerRole) {
				WriteKeyword("AddHandler");
			} else if (accessor.Role == EventDeclaration.RemoveHandlerRole) {
				WriteKeyword("RemoveHandler");
			} else if (accessor.Role == EventDeclaration.RaiseEventRole) {
				WriteKeyword("RaiseEvent");
			}
			NewLine();
			
			return EndNode(accessor);
		}

		
		public object VisitLabelDeclarationStatement(LabelDeclarationStatement labelDeclarationStatement, object data)
		{
			StartNode(labelDeclarationStatement);
			
			labelDeclarationStatement.Label.AcceptVisitor(this, data);
			WriteToken(":", LabelDeclarationStatement.Roles.Colon);
			
			return EndNode(labelDeclarationStatement);
		}
		
		public object VisitLocalDeclarationStatement(LocalDeclarationStatement localDeclarationStatement, object data)
		{
			StartNode(localDeclarationStatement);
			
			if (localDeclarationStatement.ModifierToken != null && !localDeclarationStatement.ModifierToken.IsNull)
				WriteModifiers(new [] { localDeclarationStatement.ModifierToken });
			WriteCommaSeparatedList(localDeclarationStatement.Variables);
			
			return EndNode(localDeclarationStatement);
		}
		
		public object VisitWithStatement(WithStatement withStatement, object data)
		{
			StartNode(withStatement);
			WriteKeyword("With");
			withStatement.Expression.AcceptVisitor(this, data);
			NewLine();
			Indent();
			withStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("End");
			WriteKeyword("With");
			return EndNode(withStatement);
		}
		
		public object VisitSyncLockStatement(SyncLockStatement syncLockStatement, object data)
		{
			StartNode(syncLockStatement);
			WriteKeyword("SyncLock");
			syncLockStatement.Expression.AcceptVisitor(this, data);
			NewLine();
			Indent();
			syncLockStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("End");
			WriteKeyword("SyncLock");
			return EndNode(syncLockStatement);
		}
		
		public object VisitTryStatement(TryStatement tryStatement, object data)
		{
			StartNode(tryStatement);
			WriteKeyword("Try");
			NewLine();
			Indent();
			tryStatement.Body.AcceptVisitor(this, data);
			Unindent();
			foreach (var clause in tryStatement.CatchBlocks) {
				clause.AcceptVisitor(this, data);
			}
			if (!tryStatement.FinallyBlock.IsNull) {
				WriteKeyword("Finally");
				NewLine();
				Indent();
				tryStatement.FinallyBlock.AcceptVisitor(this, data);
				Unindent();
			}
			WriteKeyword("End");
			WriteKeyword("Try");
			return EndNode(tryStatement);
		}
		
		public object VisitCatchBlock(CatchBlock catchBlock, object data)
		{
			StartNode(catchBlock);
			WriteKeyword("Catch");
			catchBlock.ExceptionVariable.AcceptVisitor(this, data);
			if (!catchBlock.ExceptionType.IsNull) {
				WriteKeyword("As");
				catchBlock.ExceptionType.AcceptVisitor(this, data);
			}
			NewLine();
			Indent();
			foreach (var stmt in catchBlock) {
				stmt.AcceptVisitor(this, data);
				NewLine();
			}
			Unindent();
			return EndNode(catchBlock);
		}
		
		public object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			StartNode(expressionStatement);
			expressionStatement.Expression.AcceptVisitor(this, data);
			return EndNode(expressionStatement);
		}
		
		public object VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			StartNode(throwStatement);
			
			WriteKeyword("Throw");
			throwStatement.Expression.AcceptVisitor(this, data);
			
			return EndNode(throwStatement);
		}
		
		public object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			StartNode(ifElseStatement);
			WriteKeyword("If");
			ifElseStatement.Condition.AcceptVisitor(this, data);
			Space();
			WriteKeyword("Then");
			NewLine();
			Indent();
			ifElseStatement.Body.AcceptVisitor(this, data);
			Unindent();
			if (!ifElseStatement.ElseBlock.IsNull) {
				WriteKeyword("Else");
				NewLine();
				Indent();
				ifElseStatement.ElseBlock.AcceptVisitor(this, data);
				Unindent();
			}
			WriteKeyword("End");
			WriteKeyword("If");
			return EndNode(ifElseStatement);
		}
		
		public object VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			StartNode(returnStatement);
			WriteKeyword("Return");
			returnStatement.Expression.AcceptVisitor(this, data);
			return EndNode(returnStatement);
		}
		
		public object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			StartNode(binaryOperatorExpression);
			binaryOperatorExpression.Left.AcceptVisitor(this, data);
			Space();
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.BitwiseAnd:
					WriteKeyword("And");
					break;
				case BinaryOperatorType.BitwiseOr:
					WriteKeyword("Or");
					break;
				case BinaryOperatorType.LogicalAnd:
					WriteKeyword("AndAlso");
					break;
				case BinaryOperatorType.LogicalOr:
					WriteKeyword("OrElse");
					break;
				case BinaryOperatorType.ExclusiveOr:
					WriteKeyword("Xor");
					break;
				case BinaryOperatorType.GreaterThan:
					WriteToken(">", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					WriteToken(">=", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Equality:
					WriteToken("=", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.InEquality:
					WriteToken("<>", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.LessThan:
					WriteToken("<", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.LessThanOrEqual:
					WriteToken("<=", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Add:
					WriteToken("+", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Subtract:
					WriteToken("-", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Multiply:
					WriteToken("*", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Divide:
					WriteToken("/", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Modulus:
					WriteKeyword("Mod");
					break;
				case BinaryOperatorType.DivideInteger:
					WriteToken("\\", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Power:
					WriteToken("*", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.Concat:
					WriteToken("&", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.ShiftLeft:
					WriteToken("<<", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.ShiftRight:
					WriteToken(">>", BinaryOperatorExpression.OperatorRole);
					break;
				case BinaryOperatorType.ReferenceEquality:
					WriteKeyword("Is");
					break;
				case BinaryOperatorType.ReferenceInequality:
					WriteKeyword("IsNot");
					break;
				case BinaryOperatorType.Like:
					WriteKeyword("Like");
					break;
				case BinaryOperatorType.DictionaryAccess:
					WriteToken("!", BinaryOperatorExpression.OperatorRole);
					break;
				default:
					throw new Exception("Invalid value for BinaryOperatorType: " + binaryOperatorExpression.Operator);
			}
			Space();
			binaryOperatorExpression.Right.AcceptVisitor(this, data);
			return EndNode(binaryOperatorExpression);
		}
		
		public object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			StartNode(identifierExpression);
			identifierExpression.Identifier.AcceptVisitor(this, data);
			WriteTypeArguments(identifierExpression.TypeArguments);
			return EndNode(identifierExpression);
		}
		
		public object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			StartNode(assignmentExpression);
			assignmentExpression.Left.AcceptVisitor(this, data);
			Space();
			switch (assignmentExpression.Operator) {
				case AssignmentOperatorType.Assign:
					WriteToken("=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.Add:
					WriteToken("+=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.Subtract:
					WriteToken("-=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.Multiply:
					WriteToken("*=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.Divide:
					WriteToken("/=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.Power:
					WriteToken("^=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.DivideInteger:
					WriteToken("\\=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.ConcatString:
					WriteToken("&=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.ShiftLeft:
					WriteToken("<<=", AssignmentExpression.OperatorRole);
					break;
				case AssignmentOperatorType.ShiftRight:
					WriteToken(">>=", AssignmentExpression.OperatorRole);
					break;
				default:
					throw new Exception("Invalid value for AssignmentOperatorType: " + assignmentExpression.Operator);
			}
			Space();
			assignmentExpression.Right.AcceptVisitor(this, data);
			return EndNode(assignmentExpression);
		}
		
		public object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			StartNode(invocationExpression);
			invocationExpression.Target.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(invocationExpression.Arguments, false);
			return EndNode(invocationExpression);
		}
		
		public object VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			StartNode(arrayInitializerExpression);
			WriteToken("{", ArrayInitializerExpression.Roles.LBrace);
			Space();
			WriteCommaSeparatedList(arrayInitializerExpression.Elements);
			Space();
			WriteToken("}", ArrayInitializerExpression.Roles.RBrace);
			return EndNode(arrayInitializerExpression);
		}
		
		public object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			StartNode(arrayCreateExpression);
			WriteKeyword("New");
			Space();
			arrayCreateExpression.Type.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(arrayCreateExpression.Arguments, false);
			foreach (var specifier in arrayCreateExpression.AdditionalArraySpecifiers) {
				specifier.AcceptVisitor(this, data);
			}
			if (!arrayCreateExpression.Initializer.IsNull) {
				Space();
				WriteToken("=", ArrayCreateExpression.Roles.Assign);
				Space();
				arrayCreateExpression.Initializer.AcceptVisitor(this, data);
			}
			return EndNode(arrayCreateExpression);
		}
		
		public object VisitObjectCreationExpression(ObjectCreationExpression objectCreationExpression, object data)
		{
			StartNode(objectCreationExpression);
			
			WriteKeyword("New");
			objectCreationExpression.Type.AcceptVisitor(this, data);
			WriteCommaSeparatedListInParenthesis(objectCreationExpression.Arguments, false);
			if (!objectCreationExpression.Initializer.IsNull) {
				Space();
				objectCreationExpression.Initializer.AcceptVisitor(this, data);
			}
			
			return EndNode(objectCreationExpression);
		}
		
		public object VisitCastExpression(CastExpression castExpression, object data)
		{
			StartNode(castExpression);
			
			switch (castExpression.CastType) {
				case CastType.DirectCast:
					WriteKeyword("DirectCast");
					break;
				case CastType.TryCast:
					WriteKeyword("TryCast");
					break;
				case CastType.CType:
					WriteKeyword("CType");
					break;
				case CastType.CBool:
					WriteKeyword("CBool");
					break;
				case CastType.CByte:
					WriteKeyword("CByte");
					break;
				case CastType.CChar:
					WriteKeyword("CChar");
					break;
				case CastType.CDate:
					WriteKeyword("CDate");
					break;
				case CastType.CDec:
					WriteKeyword("CType");
					break;
				case CastType.CDbl:
					WriteKeyword("CDec");
					break;
				case CastType.CInt:
					WriteKeyword("CInt");
					break;
				case CastType.CLng:
					WriteKeyword("CLng");
					break;
				case CastType.CObj:
					WriteKeyword("CObj");
					break;
				case CastType.CSByte:
					WriteKeyword("CSByte");
					break;
				case CastType.CShort:
					WriteKeyword("CShort");
					break;
				case CastType.CSng:
					WriteKeyword("CSng");
					break;
				case CastType.CStr:
					WriteKeyword("CStr");
					break;
				case CastType.CUInt:
					WriteKeyword("CUInt");
					break;
				case CastType.CULng:
					WriteKeyword("CULng");
					break;
				case CastType.CUShort:
					WriteKeyword("CUShort");
					break;
				default:
					throw new Exception("Invalid value for CastType");
			}
			
			WriteToken("(", CastExpression.Roles.LPar);
			castExpression.Expression.AcceptVisitor(this, data);
			
			if (castExpression.CastType == CastType.CType ||
			    castExpression.CastType == CastType.DirectCast ||
			    castExpression.CastType == CastType.TryCast) {
				WriteToken(",", CastExpression.Roles.Comma);
				Space();
				castExpression.Type.AcceptVisitor(this, data);
			}
			
			WriteToken(")", CastExpression.Roles.RPar);
			
			return EndNode(castExpression);
		}
		
		public object VisitComment(Comment comment, object data)
		{
			formatter.WriteComment(comment.IsDocumentationComment, comment.Content);
			return null;
		}
		
		public object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			StartNode(eventDeclaration);
			
			WriteAttributes(eventDeclaration.Attributes);
			WriteModifiers(eventDeclaration.ModifierTokens);
			if (eventDeclaration.IsCustom)
				WriteKeyword("Custom");
			WriteKeyword("Event");
			WriteIdentifier(eventDeclaration.Name.Name);
			if (!eventDeclaration.IsCustom && eventDeclaration.ReturnType.IsNull)
				WriteCommaSeparatedListInParenthesis(eventDeclaration.Parameters, false);
			if (!eventDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				eventDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			WriteImplementsClause(eventDeclaration.ImplementsClause);
			
			if (eventDeclaration.IsCustom) {
				MarkFoldStart();
				NewLine();
				Indent();
				
				eventDeclaration.AddHandlerBlock.AcceptVisitor(this, data);
				eventDeclaration.RemoveHandlerBlock.AcceptVisitor(this, data);
				eventDeclaration.RaiseEventBlock.AcceptVisitor(this, data);
				
				Unindent();
				WriteKeyword("End");
				WriteKeyword("Event");
				MarkFoldEnd();
			}
			NewLine();
			
			return EndNode(eventDeclaration);
		}
		
		public object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			StartNode(unaryOperatorExpression);
			
			switch (unaryOperatorExpression.Operator) {
				case UnaryOperatorType.Not:
					WriteKeyword("Not");
					break;
				case UnaryOperatorType.Minus:
					WriteToken("-", UnaryOperatorExpression.OperatorRole);
					break;
				case UnaryOperatorType.Plus:
					WriteToken("+", UnaryOperatorExpression.OperatorRole);
					break;
				case UnaryOperatorType.AddressOf:
					WriteKeyword("AddressOf");
					break;
				case UnaryOperatorType.Await:
					WriteKeyword("Await");
					break;
				default:
					throw new Exception("Invalid value for UnaryOperatorType");
			}
			
			unaryOperatorExpression.Expression.AcceptVisitor(this, data);
			
			return EndNode(unaryOperatorExpression);
		}
		
		public object VisitFieldInitializerExpression(FieldInitializerExpression fieldInitializerExpression, object data)
		{
			StartNode(fieldInitializerExpression);
			
			if (fieldInitializerExpression.IsKey) {
				WriteKeyword("Key");
				Space();
			}
			
			WriteToken(".", FieldInitializerExpression.Roles.Dot);
			fieldInitializerExpression.Identifier.AcceptVisitor(this, data);
			
			Space();
			WriteToken("=", FieldInitializerExpression.Roles.Assign);
			Space();
			fieldInitializerExpression.Expression.AcceptVisitor(this, data);
			
			return EndNode(fieldInitializerExpression);
		}
		
		public object VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			StartNode(conditionalExpression);
			
			WriteKeyword("If");
			WriteToken("(", ConditionalExpression.Roles.LPar);
			
			conditionalExpression.ConditionExpression.AcceptVisitor(this, data);
			WriteToken(",", ConditionalExpression.Roles.Comma);
			Space();
			
			if (!conditionalExpression.TrueExpression.IsNull) {
				conditionalExpression.TrueExpression.AcceptVisitor(this, data);
				WriteToken(",", ConditionalExpression.Roles.Comma);
				Space();
			}
			
			conditionalExpression.FalseExpression.AcceptVisitor(this, data);
			
			WriteToken(")", ConditionalExpression.Roles.RPar);
			
			return EndNode(conditionalExpression);
		}
		
		public object VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			StartNode(whileStatement);
			
			WriteKeyword("While");
			Space();
			whileStatement.Condition.AcceptVisitor(this, data);
			NewLine();
			Indent();
			whileStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("End");
			WriteKeyword("While");
			
			return EndNode(whileStatement);
		}
		
		public object VisitExitStatement(ExitStatement exitStatement, object data)
		{
			StartNode(exitStatement);
			
			WriteKeyword("Exit");
			
			switch (exitStatement.ExitKind) {
				case ExitKind.Sub:
					WriteKeyword("Sub");
					break;
				case ExitKind.Function:
					WriteKeyword("Function");
					break;
				case ExitKind.Property:
					WriteKeyword("Property");
					break;
				case ExitKind.Do:
					WriteKeyword("Do");
					break;
				case ExitKind.For:
					WriteKeyword("For");
					break;
				case ExitKind.While:
					WriteKeyword("While");
					break;
				case ExitKind.Select:
					WriteKeyword("Select");
					break;
				case ExitKind.Try:
					WriteKeyword("Try");
					break;
				default:
					throw new Exception("Invalid value for ExitKind");
			}
			
			return EndNode(exitStatement);
		}
		
		public object VisitForStatement(ForStatement forStatement, object data)
		{
			StartNode(forStatement);
			
			WriteKeyword("For");
			forStatement.Variable.AcceptVisitor(this, data);
			WriteKeyword("To");
			forStatement.ToExpression.AcceptVisitor(this, data);
			if (!forStatement.StepExpression.IsNull) {
				WriteKeyword("Step");
				Space();
				forStatement.StepExpression.AcceptVisitor(this, data);
			}
			NewLine();
			Indent();
			forStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("Next");
			
			return EndNode(forStatement);
		}
		
		public object VisitForEachStatement(ForEachStatement forEachStatement, object data)
		{
			StartNode(forEachStatement);
			
			WriteKeyword("For");
			WriteKeyword("Each");
			forEachStatement.Variable.AcceptVisitor(this, data);
			WriteKeyword("In");
			forEachStatement.InExpression.AcceptVisitor(this, data);
			NewLine();
			Indent();
			forEachStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("Next");
			
			return EndNode(forEachStatement);
		}
		
		public object VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			StartNode(operatorDeclaration);
			
			WriteAttributes(operatorDeclaration.Attributes);
			WriteModifiers(operatorDeclaration.ModifierTokens);
			WriteKeyword("Operator");
			switch (operatorDeclaration.Operator) {
				case OverloadableOperatorType.Add:
				case OverloadableOperatorType.UnaryPlus:
					WriteToken("+", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.Subtract:
				case OverloadableOperatorType.UnaryMinus:
					WriteToken("-", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.Multiply:
					WriteToken("*", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.Divide:
					WriteToken("/", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.Modulus:
					WriteKeyword("Mod");
					break;
				case OverloadableOperatorType.Concat:
					WriteToken("&", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.Not:
					WriteKeyword("Not");
					break;
				case OverloadableOperatorType.BitwiseAnd:
					WriteKeyword("And");
					break;
				case OverloadableOperatorType.BitwiseOr:
					WriteKeyword("Or");
					break;
				case OverloadableOperatorType.ExclusiveOr:
					WriteKeyword("Xor");
					break;
				case OverloadableOperatorType.ShiftLeft:
					WriteToken("<<", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.ShiftRight:
					WriteToken(">>", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.GreaterThan:
					WriteToken(">", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.GreaterThanOrEqual:
					WriteToken(">=", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.Equality:
					WriteToken("=", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.InEquality:
					WriteToken("<>", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.LessThan:
					WriteToken("<", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.LessThanOrEqual:
					WriteToken("<=", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.IsTrue:
					WriteKeyword("IsTrue");
					break;
				case OverloadableOperatorType.IsFalse:
					WriteKeyword("IsFalse");
					break;
				case OverloadableOperatorType.Like:
					WriteKeyword("Like");
					break;
				case OverloadableOperatorType.Power:
					WriteToken("^", OperatorDeclaration.Roles.Keyword);
					break;
				case OverloadableOperatorType.CType:
					WriteKeyword("CType");
					break;
				case OverloadableOperatorType.DivideInteger:
					WriteToken("\\", OperatorDeclaration.Roles.Keyword);
					break;
				default:
					throw new Exception("Invalid value for OverloadableOperatorType");
			}
			WriteCommaSeparatedListInParenthesis(operatorDeclaration.Parameters, false);
			if (!operatorDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				WriteAttributes(operatorDeclaration.ReturnTypeAttributes);
				operatorDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			if (!operatorDeclaration.Body.IsNull) {
				MarkFoldStart();
				NewLine();
				Indent();
				WriteBlock(operatorDeclaration.Body);
				Unindent();
				WriteKeyword("End");
				WriteKeyword("Operator");
				MarkFoldEnd();
			}
			NewLine();
			
			return EndNode(operatorDeclaration);
		}
		
		public object VisitSelectStatement(SelectStatement selectStatement, object data)
		{
			StartNode(selectStatement);
			
			WriteKeyword("Select");
			WriteKeyword("Case");
			selectStatement.Expression.AcceptVisitor(this, data);
			NewLine();
			Indent();
			
			foreach (CaseStatement stmt in selectStatement.Cases) {
				stmt.AcceptVisitor(this, data);
			}
			
			Unindent();
			WriteKeyword("End");
			WriteKeyword("Select");
			
			return EndNode(selectStatement);
		}
		
		public object VisitCaseStatement(CaseStatement caseStatement, object data)
		{
			StartNode(caseStatement);
			
			WriteKeyword("Case");
			if (caseStatement.Clauses.Count == 1 && caseStatement.Clauses.First().Expression.IsNull)
				WriteKeyword("Else");
			else {
				Space();
				WriteCommaSeparatedList(caseStatement.Clauses);
			}
			NewLine();
			Indent();
			caseStatement.Body.AcceptVisitor(this, data);
			Unindent();
			
			return EndNode(caseStatement);
		}
		
		public object VisitSimpleCaseClause(SimpleCaseClause simpleCaseClause, object data)
		{
			StartNode(simpleCaseClause);
			simpleCaseClause.Expression.AcceptVisitor(this, data);
			return EndNode(simpleCaseClause);
		}
		
		public object VisitRangeCaseClause(RangeCaseClause rangeCaseClause, object data)
		{
			StartNode(rangeCaseClause);
			rangeCaseClause.Expression.AcceptVisitor(this, data);
			WriteKeyword("To");
			rangeCaseClause.ToExpression.AcceptVisitor(this, data);
			return EndNode(rangeCaseClause);
		}
		
		public object VisitComparisonCaseClause(ComparisonCaseClause comparisonCaseClause, object data)
		{
			StartNode(comparisonCaseClause);
			switch (comparisonCaseClause.Operator) {
				case ComparisonOperator.Equality:
					WriteToken("=", ComparisonCaseClause.OperatorRole);
					break;
				case ComparisonOperator.InEquality:
					WriteToken("<>", ComparisonCaseClause.OperatorRole);
					break;
				case ComparisonOperator.LessThan:
					WriteToken("<", ComparisonCaseClause.OperatorRole);
					break;
				case ComparisonOperator.GreaterThan:
					WriteToken(">", ComparisonCaseClause.OperatorRole);
					break;
				case ComparisonOperator.LessThanOrEqual:
					WriteToken("<=", ComparisonCaseClause.OperatorRole);
					break;
				case ComparisonOperator.GreaterThanOrEqual:
					WriteToken(">=", ComparisonCaseClause.OperatorRole);
					break;
				default:
					throw new Exception("Invalid value for ComparisonOperator");
			}
			Space();
			comparisonCaseClause.Expression.AcceptVisitor(this, data);
			return EndNode(comparisonCaseClause);
		}

		
		public object VisitYieldStatement(YieldStatement yieldStatement, object data)
		{
			StartNode(yieldStatement);
			WriteKeyword("Yield");
			yieldStatement.Expression.AcceptVisitor(this, data);
			return EndNode(yieldStatement);
		}
		
		public object VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			StartNode(variableInitializer);
			
			variableInitializer.Identifier.AcceptVisitor(this, data);
			if (!variableInitializer.Type.IsNull) {
				WriteKeyword("As");
				variableInitializer.Type.AcceptVisitor(this, data);
			}
			if (!variableInitializer.Expression.IsNull) {
				Space();
				WriteToken("=", VariableInitializer.Roles.Assign);
				Space();
				variableInitializer.Expression.AcceptVisitor(this, data);
			}
			
			return EndNode(variableInitializer);
		}
		
		public object VisitVariableDeclaratorWithTypeAndInitializer(VariableDeclaratorWithTypeAndInitializer variableDeclaratorWithTypeAndInitializer, object data)
		{
			StartNode(variableDeclaratorWithTypeAndInitializer);
			
			WriteCommaSeparatedList(variableDeclaratorWithTypeAndInitializer.Identifiers);
			WriteKeyword("As");
			variableDeclaratorWithTypeAndInitializer.Type.AcceptVisitor(this, data);
			if (!variableDeclaratorWithTypeAndInitializer.Initializer.IsNull) {
				Space();
				WriteToken("=", VariableDeclarator.Roles.Assign);
				Space();
				variableDeclaratorWithTypeAndInitializer.Initializer.AcceptVisitor(this, data);
			}
			
			return EndNode(variableDeclaratorWithTypeAndInitializer);
		}
		
		public object VisitVariableDeclaratorWithObjectCreation(VariableDeclaratorWithObjectCreation variableDeclaratorWithObjectCreation, object data)
		{
			StartNode(variableDeclaratorWithObjectCreation);
			
			WriteCommaSeparatedList(variableDeclaratorWithObjectCreation.Identifiers);
			WriteKeyword("As");
			variableDeclaratorWithObjectCreation.Initializer.AcceptVisitor(this, data);
			
			return EndNode(variableDeclaratorWithObjectCreation);
		}
		
		public object VisitDoLoopStatement(DoLoopStatement doLoopStatement, object data)
		{
			StartNode(doLoopStatement);
			
			WriteKeyword("Do");
			if (doLoopStatement.ConditionType == ConditionType.DoUntil) {
				WriteKeyword("Until");
				doLoopStatement.Expression.AcceptVisitor(this, data);
			}
			if (doLoopStatement.ConditionType == ConditionType.DoWhile) {
				WriteKeyword("While");
				doLoopStatement.Expression.AcceptVisitor(this, data);
			}
			NewLine();
			Indent();
			doLoopStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("Loop");
			if (doLoopStatement.ConditionType == ConditionType.LoopUntil) {
				WriteKeyword("Until");
				doLoopStatement.Expression.AcceptVisitor(this, data);
			}
			if (doLoopStatement.ConditionType == ConditionType.LoopWhile) {
				WriteKeyword("While");
				doLoopStatement.Expression.AcceptVisitor(this, data);
			}
			
			return EndNode(doLoopStatement);
		}
		
		public object VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			StartNode(usingStatement);
			
			WriteKeyword("Using");
			WriteCommaSeparatedList(usingStatement.Resources);
			NewLine();
			Indent();
			usingStatement.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("End");
			WriteKeyword("Using");
			
			return EndNode(usingStatement);
		}
		
		public object VisitGoToStatement(GoToStatement goToStatement, object data)
		{
			StartNode(goToStatement);
			
			WriteKeyword("GoTo");
			goToStatement.Label.AcceptVisitor(this, data);
			
			return EndNode(goToStatement);
		}
		
		public object VisitSingleLineSubLambdaExpression(SingleLineSubLambdaExpression singleLineSubLambdaExpression, object data)
		{
			StartNode(singleLineSubLambdaExpression);
			
			WriteModifiers(singleLineSubLambdaExpression.ModifierTokens);
			WriteKeyword("Sub");
			WriteCommaSeparatedListInParenthesis(singleLineSubLambdaExpression.Parameters, false);
			Space();
			singleLineSubLambdaExpression.EmbeddedStatement.AcceptVisitor(this, data);
			
			return EndNode(singleLineSubLambdaExpression);
		}
		
		public object VisitSingleLineFunctionLambdaExpression(SingleLineFunctionLambdaExpression singleLineFunctionLambdaExpression, object data)
		{
			StartNode(singleLineFunctionLambdaExpression);
			
			WriteModifiers(singleLineFunctionLambdaExpression.ModifierTokens);
			WriteKeyword("Function");
			WriteCommaSeparatedListInParenthesis(singleLineFunctionLambdaExpression.Parameters, false);
			Space();
			singleLineFunctionLambdaExpression.EmbeddedExpression.AcceptVisitor(this, data);
			
			return EndNode(singleLineFunctionLambdaExpression);
		}
		
		public object VisitMultiLineLambdaExpression(MultiLineLambdaExpression multiLineLambdaExpression, object data)
		{
			StartNode(multiLineLambdaExpression);
			
			WriteModifiers(multiLineLambdaExpression.ModifierTokens);
			if (multiLineLambdaExpression.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			WriteCommaSeparatedListInParenthesis(multiLineLambdaExpression.Parameters, false);
			NewLine();
			Indent();
			multiLineLambdaExpression.Body.AcceptVisitor(this, data);
			Unindent();
			WriteKeyword("End");
			if (multiLineLambdaExpression.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			
			return EndNode(multiLineLambdaExpression);
		}
		
		public object VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			StartNode(queryExpression);
			
			foreach (var op in queryExpression.QueryOperators) {
				op.AcceptVisitor(this, data);
			}
			
			return EndNode(queryExpression);
		}
		
		public object VisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			StartNode(continueStatement);
			
			WriteKeyword("Continue");
			
			switch (continueStatement.ContinueKind) {
				case ContinueKind.Do:
					WriteKeyword("Do");
					break;
				case ContinueKind.For:
					WriteKeyword("For");
					break;
				case ContinueKind.While:
					WriteKeyword("While");
					break;
				default:
					throw new Exception("Invalid value for ContinueKind");
			}
			
			return EndNode(continueStatement);
		}
		
		public object VisitExternalMethodDeclaration(ExternalMethodDeclaration externalMethodDeclaration, object data)
		{
			StartNode(externalMethodDeclaration);
			
			WriteAttributes(externalMethodDeclaration.Attributes);
			WriteModifiers(externalMethodDeclaration.ModifierTokens);
			WriteKeyword("Declare");
			switch (externalMethodDeclaration.CharsetModifier) {
				case CharsetModifier.None:
					break;
				case CharsetModifier.Auto:
					WriteKeyword("Auto");
					break;
				case CharsetModifier.Unicode:
					WriteKeyword("Unicode");
					break;
				case CharsetModifier.Ansi:
					WriteKeyword("Ansi");
					break;
				default:
					throw new Exception("Invalid value for CharsetModifier");
			}
			if (externalMethodDeclaration.IsSub)
				WriteKeyword("Sub");
			else
				WriteKeyword("Function");
			externalMethodDeclaration.Name.AcceptVisitor(this, data);
			WriteKeyword("Lib");
			Space();
			WritePrimitiveValue(externalMethodDeclaration.Library);
			Space();
			if (externalMethodDeclaration.Alias != null) {
				WriteKeyword("Alias");
				Space();
				WritePrimitiveValue(externalMethodDeclaration.Alias);
				Space();
			}
			WriteCommaSeparatedListInParenthesis(externalMethodDeclaration.Parameters, false);
			if (!externalMethodDeclaration.IsSub && !externalMethodDeclaration.ReturnType.IsNull) {
				Space();
				WriteKeyword("As");
				WriteAttributes(externalMethodDeclaration.ReturnTypeAttributes);
				externalMethodDeclaration.ReturnType.AcceptVisitor(this, data);
			}
			NewLine();
			
			return EndNode(externalMethodDeclaration);
		}
		
		public static string ToVBNetString(PrimitiveExpression primitiveExpression)
		{
			var writer = new StringWriter();
			new OutputVisitor(writer, new VBFormattingOptions()).WritePrimitiveValue(primitiveExpression.Value);
			return writer.ToString();
		}
		
		public object VisitEmptyExpression(EmptyExpression emptyExpression, object data)
		{
			StartNode(emptyExpression);
			
			return EndNode(emptyExpression);
		}
		
		public object VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpression anonymousObjectCreationExpression, object data)
		{
			StartNode(anonymousObjectCreationExpression);
			
			WriteKeyword("New");
			WriteKeyword("With");
			
			WriteToken("{", AnonymousObjectCreationExpression.Roles.LBrace);
			Space();
			WriteCommaSeparatedList(anonymousObjectCreationExpression.Initializer);
			Space();
			WriteToken("}", AnonymousObjectCreationExpression.Roles.RBrace);
			
			return EndNode(anonymousObjectCreationExpression);
		}
		
		public object VisitCollectionRangeVariableDeclaration(CollectionRangeVariableDeclaration collectionRangeVariableDeclaration, object data)
		{
			StartNode(collectionRangeVariableDeclaration);
			
			collectionRangeVariableDeclaration.Identifier.AcceptVisitor(this, data);
			if (!collectionRangeVariableDeclaration.Type.IsNull) {
				WriteKeyword("As");
				collectionRangeVariableDeclaration.Type.AcceptVisitor(this, data);
			}
			WriteKeyword("In");
			collectionRangeVariableDeclaration.Expression.AcceptVisitor(this, data);
			
			return EndNode(collectionRangeVariableDeclaration);
		}
		
		public object VisitFromQueryOperator(FromQueryOperator fromQueryOperator, object data)
		{
			StartNode(fromQueryOperator);
			
			WriteKeyword("From");
			WriteCommaSeparatedList(fromQueryOperator.Variables);
			
			return EndNode(fromQueryOperator);
		}
		
		public object VisitAggregateQueryOperator(AggregateQueryOperator aggregateQueryOperator, object data)
		{
			StartNode(aggregateQueryOperator);
			
			WriteKeyword("Aggregate");
			aggregateQueryOperator.Variable.AcceptVisitor(this, data);
			
			foreach (var operators in aggregateQueryOperator.SubQueryOperators) {
				operators.AcceptVisitor(this, data);
			}
			
			WriteKeyword("Into");
			WriteCommaSeparatedList(aggregateQueryOperator.IntoExpressions);
			
			return EndNode(aggregateQueryOperator);
		}
		
		public object VisitSelectQueryOperator(SelectQueryOperator selectQueryOperator, object data)
		{
			StartNode(selectQueryOperator);
			
			WriteKeyword("Select");
			WriteCommaSeparatedList(selectQueryOperator.Variables);
			
			return EndNode(selectQueryOperator);
		}
		
		public object VisitDistinctQueryOperator(DistinctQueryOperator distinctQueryOperator, object data)
		{
			StartNode(distinctQueryOperator);
			
			WriteKeyword("Distinct");
			
			return EndNode(distinctQueryOperator);
		}
		
		public object VisitWhereQueryOperator(WhereQueryOperator whereQueryOperator, object data)
		{
			StartNode(whereQueryOperator);
			
			WriteKeyword("Where");
			whereQueryOperator.Condition.AcceptVisitor(this, data);
			
			return EndNode(whereQueryOperator);
		}
		
		public object VisitPartitionQueryOperator(PartitionQueryOperator partitionQueryOperator, object data)
		{
			StartNode(partitionQueryOperator);
			
			switch (partitionQueryOperator.Kind) {
				case PartitionKind.Take:
					WriteKeyword("Take");
					break;
				case PartitionKind.TakeWhile:
					WriteKeyword("Take");
					WriteKeyword("While");
					break;
				case PartitionKind.Skip:
					WriteKeyword("Skip");
					break;
				case PartitionKind.SkipWhile:
					WriteKeyword("Skip");
					WriteKeyword("While");
					break;
				default:
					throw new Exception("Invalid value for PartitionKind");
			}
			
			partitionQueryOperator.Expression.AcceptVisitor(this, data);
			
			return EndNode(partitionQueryOperator);
		}
		
		public object VisitOrderExpression(OrderExpression orderExpression, object data)
		{
			StartNode(orderExpression);
			
			orderExpression.Expression.AcceptVisitor(this, data);
			
			switch (orderExpression.Direction) {
				case QueryOrderingDirection.None:
					break;
				case QueryOrderingDirection.Ascending:
					WriteKeyword("Ascending");
					break;
				case QueryOrderingDirection.Descending:
					WriteKeyword("Descending");
					break;
				default:
					throw new Exception("Invalid value for QueryExpressionOrderingDirection");
			}
			
			return EndNode(orderExpression);
		}
		
		public object VisitOrderByQueryOperator(OrderByQueryOperator orderByQueryOperator, object data)
		{
			StartNode(orderByQueryOperator);
			
			WriteKeyword("Order");
			WriteKeyword("By");
			WriteCommaSeparatedList(orderByQueryOperator.Expressions);
			
			return EndNode(orderByQueryOperator);
		}
		
		public object VisitLetQueryOperator(LetQueryOperator letQueryOperator, object data)
		{
			StartNode(letQueryOperator);
			
			WriteKeyword("Let");
			WriteCommaSeparatedList(letQueryOperator.Variables);
			
			return EndNode(letQueryOperator);
		}
		
		public object VisitGroupByQueryOperator(GroupByQueryOperator groupByQueryOperator, object data)
		{
			StartNode(groupByQueryOperator);
			
			WriteKeyword("Group");
			WriteCommaSeparatedList(groupByQueryOperator.GroupExpressions);
			WriteKeyword("By");
			WriteCommaSeparatedList(groupByQueryOperator.ByExpressions);
			WriteKeyword("Into");
			WriteCommaSeparatedList(groupByQueryOperator.IntoExpressions);
			
			return EndNode(groupByQueryOperator);
		}
		
		public object VisitJoinQueryOperator(JoinQueryOperator joinQueryOperator, object data)
		{
			StartNode(joinQueryOperator);
			
			WriteKeyword("Join");
			joinQueryOperator.JoinVariable.AcceptVisitor(this, data);
			if (!joinQueryOperator.SubJoinQuery.IsNull) {
				joinQueryOperator.SubJoinQuery.AcceptVisitor(this, data);
			}
			WriteKeyword("On");
			bool first = true;
			foreach (var cond in joinQueryOperator.JoinConditions) {
				if (first)
					first = false;
				else
					WriteKeyword("And");
				cond.AcceptVisitor(this, data);
			}
			
			return EndNode(joinQueryOperator);
		}
		
		public object VisitJoinCondition(JoinCondition joinCondition, object data)
		{
			StartNode(joinCondition);
			
			joinCondition.Left.AcceptVisitor(this, data);
			WriteKeyword("Equals");
			joinCondition.Right.AcceptVisitor(this, data);
			
			return EndNode(joinCondition);
		}
		
		public object VisitGroupJoinQueryOperator(GroupJoinQueryOperator groupJoinQueryOperator, object data)
		{
			StartNode(groupJoinQueryOperator);
			
			WriteKeyword("Group");
			WriteKeyword("Join");
			groupJoinQueryOperator.JoinVariable.AcceptVisitor(this, data);
			if (!groupJoinQueryOperator.SubJoinQuery.IsNull) {
				groupJoinQueryOperator.SubJoinQuery.AcceptVisitor(this, data);
			}
			WriteKeyword("On");
			bool first = true;
			foreach (var cond in groupJoinQueryOperator.JoinConditions) {
				if (first)
					first = false;
				else
					WriteKeyword("And");
				cond.AcceptVisitor(this, data);
			}
			WriteKeyword("Into");
			WriteCommaSeparatedList(groupJoinQueryOperator.IntoExpressions);
			
			return EndNode(groupJoinQueryOperator);
		}
	}
}
