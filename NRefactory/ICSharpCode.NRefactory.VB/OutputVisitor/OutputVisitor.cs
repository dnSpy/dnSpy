// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
			StartNode(attribute);
			
			if (!attribute.Target.IsNull) {
				attribute.Target.AcceptVisitor(this, data);
				WriteToken(":", VB.Ast.Attribute.TargetRole);
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
			Indent();
			isFirst = true;
			foreach (var member in namespaceDeclaration.Members) {
				if (isFirst) {
					isFirst = false;
				} else {
					NewLine();
				}
				member.AcceptVisitor(this, data);
			}
			Unindent();
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
			NewLine();
			
			WriteKeyword("End");
			WriteClassTypeKeyword(typeDeclaration);
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
			throw new NotImplementedException();
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
		
		public object VisitAddressOfExpression(AddressOfExpression addressOfExpression, object data)
		{
			StartNode(addressOfExpression);
			
			WriteKeyword("AddressOf");
			addressOfExpression.Expression.AcceptVisitor(this, data);
			
			return EndNode(addressOfExpression);
		}
		
		#region TypeName
		public object VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}
		
		public object VisitArraySpecifier(ArraySpecifier arraySpecifier, object data)
		{
			throw new NotImplementedException();
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
				formatter.WriteToken("@");
			} else if (lastWritten == LastWritten.KeywordOrIdentifier) {
				formatter.Space(); // this space is strictly required, so we directly call the formatter
			}
			formatter.WriteIdentifier(identifier);
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
		
		void WriteMethodBody(BlockStatement body)
		{
			if (body.IsNull)
				NewLine();
			else
				VisitBlockStatement(body, null);
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
		#endregion
	}
}
