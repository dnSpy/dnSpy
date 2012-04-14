// 
// Roles.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

namespace ICSharpCode.NRefactory.CSharp
{
	public static class Roles
	{
		public static readonly Role<AstNode> Root = AstNode.RootRole;
		
		// some pre defined constants for common roles
		public static readonly Role<Identifier> Identifier = new Role<Identifier> ("Identifier", CSharp.Identifier.Null);
		public static readonly Role<BlockStatement> Body = new Role<BlockStatement> ("Body", CSharp.BlockStatement.Null);
		public static readonly Role<ParameterDeclaration> Parameter = new Role<ParameterDeclaration> ("Parameter");
		public static readonly Role<Expression> Argument = new Role<Expression> ("Argument", CSharp.Expression.Null);
		public static readonly Role<AstType> Type = new Role<AstType> ("Type", CSharp.AstType.Null);
		public static readonly Role<Expression> Expression = new Role<Expression> ("Expression", CSharp.Expression.Null);
		public static readonly Role<Expression> TargetExpression = new Role<Expression> ("Target", CSharp.Expression.Null);
		public readonly static Role<Expression> Condition = new Role<Expression> ("Condition", CSharp.Expression.Null);
		public static readonly Role<TypeParameterDeclaration> TypeParameter = new Role<TypeParameterDeclaration> ("TypeParameter");
		public static readonly Role<AstType> TypeArgument = new Role<AstType> ("TypeArgument", CSharp.AstType.Null);
		public readonly static Role<Constraint> Constraint = new Role<Constraint> ("Constraint");
		public static readonly Role<VariableInitializer> Variable = new Role<VariableInitializer> ("Variable", VariableInitializer.Null);
		public static readonly Role<Statement> EmbeddedStatement = new Role<Statement> ("EmbeddedStatement", CSharp.Statement.Null);
		public readonly static Role<EntityDeclaration> TypeMemberRole = new Role<EntityDeclaration> ("TypeMember");
		

		//			public static readonly TokenRole Keyword = new TokenRole ("Keyword", CSharpTokenNode.Null);
//			public static readonly TokenRole InKeyword = new TokenRole ("InKeyword", CSharpTokenNode.Null);
			
		// some pre defined constants for most used punctuation
		public static readonly TokenRole LPar = new TokenRole ("(");
		public static readonly TokenRole RPar = new TokenRole (")");
		public static readonly TokenRole LBracket = new TokenRole ("[");
		public static readonly TokenRole RBracket = new TokenRole ("]");
		public static readonly TokenRole LBrace = new TokenRole ("{");
		public static readonly TokenRole RBrace = new TokenRole ("}");
		public static readonly TokenRole LChevron = new TokenRole ("<");
		public static readonly TokenRole RChevron = new TokenRole (">");
		public static readonly TokenRole Comma = new TokenRole (",");
		public static readonly TokenRole Dot = new TokenRole (".");
		public static readonly TokenRole Semicolon = new TokenRole (";");
		public static readonly TokenRole Assign = new TokenRole ("=");
		public static readonly TokenRole Colon = new TokenRole (":");
		public static readonly TokenRole DoubleColon = new TokenRole ("::");
		public static readonly Role<Comment> Comment = new Role<Comment> ("Comment");
		public static readonly Role<NewLineNode> NewLine = new Role<NewLineNode> ("NewLine");
		public static readonly Role<WhitespaceNode> Whitespace = new Role<WhitespaceNode> ("Whitespace");
		public static readonly Role<TextNode> Text = new Role<TextNode> ("Text");
		public static readonly Role<PreProcessorDirective> PreProcessorDirective = new Role<PreProcessorDirective> ("PreProcessorDirective");
		public static readonly Role<ErrorNode> Error = new Role<ErrorNode> ("Error");
			
		public readonly static Role<AstType> BaseType = new Role<AstType> ("BaseType", AstType.Null);

		public static readonly Role<Attribute> Attribute = new Role<Attribute> ("Attribute");
		public static readonly Role<CSharpTokenNode> AttributeTargetRole = new Role<CSharpTokenNode> ("AttributeTarget", CSharpTokenNode.Null);

		public readonly static TokenRole WhereKeyword = new TokenRole ("where");
		public readonly static Role<SimpleType> ConstraintTypeParameter = new Role<SimpleType> ("TypeParameter", SimpleType.Null);
		public readonly static TokenRole DelegateKeyword = new TokenRole ("delegate");
		public static readonly TokenRole ExternKeyword = new TokenRole ("extern");
		public static readonly TokenRole AliasKeyword = new TokenRole ("alias");
		public static readonly TokenRole NamespaceKeyword = new TokenRole ("namespace");

		public static readonly TokenRole EnumKeyword = new TokenRole ("enum");
		public static readonly TokenRole InterfaceKeyword = new TokenRole ("interface");
		public static readonly TokenRole StructKeyword = new TokenRole ("struct");
		public static readonly TokenRole ClassKeyword = new TokenRole ("class");

	}
}

