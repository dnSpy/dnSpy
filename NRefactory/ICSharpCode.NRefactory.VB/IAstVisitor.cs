// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using Attribute = ICSharpCode.NRefactory.VB.Ast.Attribute;

namespace ICSharpCode.NRefactory.VB {
	public interface IAstVisitor<in T, out S>
	{
		S VisitBlockStatement(BlockStatement blockStatement, T data);
		S VisitCompilationUnit(CompilationUnit compilationUnit, T data);
		S VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern, T data);
		S VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, T data);
		S VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, T data);
		S VisitVBTokenNode(VBTokenNode vBTokenNode, T data);
		
		// Global scope
		S VisitAliasImportsClause(AliasImportsClause aliasImportsClause, T data);
		S VisitAttribute(Attribute attribute, T data);
		S VisitAttributeBlock(AttributeBlock attributeBlock, T data);
		S VisitImportsStatement(ImportsStatement importsStatement, T data);
		S VisitMembersImportsClause(MemberImportsClause membersImportsClause, T data);
		S VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, T data);
		S VisitOptionStatement(OptionStatement optionStatement, T data);
		S VisitTypeDeclaration(TypeDeclaration typeDeclaration, T data);
		S VisitXmlNamespaceImportsClause(XmlNamespaceImportsClause xmlNamespaceImportsClause, T data);
		S VisitEnumDeclaration(EnumDeclaration enumDeclaration, T data);
		S VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, T data);
		S VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, T data);
		
		// Expression scope
		S VisitIdentifier(Identifier identifier, T data);
		S VisitXmlIdentifier(XmlIdentifier xmlIdentifier, T data);
		S VisitXmlLiteralString(XmlLiteralString xmlLiteralString, T data);
		S VisitSimpleNameExpression(SimpleNameExpression identifierExpression, T data);
		S VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, T data);
		
		// TypeName
		S VisitPrimitiveType(PrimitiveType primitiveType, T data);
		S VisitQualifiedType(QualifiedType qualifiedType, T data);
		S VisitComposedType(ComposedType composedType, T data);
		S VisitArraySpecifier(ArraySpecifier arraySpecifier, T data);
		S VisitSimpleType(SimpleType simpleType, T data);
	}
}
