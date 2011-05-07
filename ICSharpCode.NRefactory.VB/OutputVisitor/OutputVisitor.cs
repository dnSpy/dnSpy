// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;

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
		
		public object VisitCompilationUnit(ICSharpCode.NRefactory.VB.Ast.CompilationUnit compilationUnit, object data)
		{
			// don't do node tracking as we visit all children directly
			foreach (AstNode node in compilationUnit.Children)
				node.AcceptVisitor(this, data);
			return null;
		}
		
		public object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitPatternPlaceholder(AstNode placeholder, Pattern pattern, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitVBTokenNode(VBTokenNode vBTokenNode, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitAliasImportsClause(AliasImportsClause aliasImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitAttribute(ICSharpCode.NRefactory.VB.Ast.Attribute attribute, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitAttributeBlock(AttributeBlock attributeBlock, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitImportsStatement(ImportsStatement importsStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitMembersImportsClause(MemberImportsClause membersImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitOptionStatement(OptionStatement optionStatement, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitXmlNamespaceImportsClause(XmlNamespaceImportsClause xmlNamespaceImportsClause, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitEnumDeclaration(EnumDeclaration enumDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitIdentifier(Identifier identifier, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitXmlIdentifier(XmlIdentifier xmlIdentifier, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitXmlLiteralString(XmlLiteralString xmlLiteralString, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitSimpleNameExpression(SimpleNameExpression identifierExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitQualifiedType(QualifiedType qualifiedType, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitComposedType(ComposedType composedType, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitArraySpecifier(ArraySpecifier arraySpecifier, object data)
		{
			throw new NotImplementedException();
		}
		
		public object VisitSimpleType(SimpleType simpleType, object data)
		{
			throw new NotImplementedException();
		}
		
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
	}
}
