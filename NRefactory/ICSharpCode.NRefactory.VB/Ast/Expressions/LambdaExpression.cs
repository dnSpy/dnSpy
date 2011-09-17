// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public abstract class LambdaExpression : Expression
	{
		public static readonly Role<VBModifierToken> ModifierRole = AttributedNode.ModifierRole;
		
		public LambdaExpressionModifiers Modifiers {
			get { return GetModifiers(this); }
			set { SetModifiers(this, value); }
		}
		
		public AstNodeCollection<VBModifierToken> ModifierTokens {
			get { return GetChildrenByRole (ModifierRole); }
		}
		
		internal static LambdaExpressionModifiers GetModifiers(AstNode node)
		{
			LambdaExpressionModifiers m = 0;
			foreach (VBModifierToken t in node.GetChildrenByRole (ModifierRole)) {
				m |= (LambdaExpressionModifiers)t.Modifier;
			}
			return m;
		}
		
		internal static void SetModifiers(AstNode node, LambdaExpressionModifiers newValue)
		{
			LambdaExpressionModifiers oldValue = GetModifiers(node);
			AstNode insertionPos = null;
			foreach (Modifiers m in VBModifierToken.AllModifiers) {
				if ((m & (Modifiers)newValue) != 0) {
					if ((m & (Modifiers)oldValue) == 0) {
						// Modifier was added
						var newToken = new VBModifierToken(TextLocation.Empty, m);
						node.InsertChildAfter(insertionPos, newToken, ModifierRole);
						insertionPos = newToken;
					} else {
						// Modifier already exists
						insertionPos = node.GetChildrenByRole(ModifierRole).First(t => t.Modifier == m);
					}
				} else {
					if ((m & (Modifiers)oldValue) != 0) {
						// Modifier was removed
						node.GetChildrenByRole (ModifierRole).First(t => t.Modifier == m).Remove();
					}
				}
			}
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole(Roles.Parameter); }
		}
	}
	
	public class SingleLineSubLambdaExpression : LambdaExpression
	{
		public static readonly Role<Statement> StatementRole = BlockStatement.StatementRole;
		
		public Statement EmbeddedStatement {
			get { return GetChildByRole(StatementRole); }
			set { SetChildByRole(StatementRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSingleLineSubLambdaExpression(this, data);
		}
	}
	
	public class SingleLineFunctionLambdaExpression : LambdaExpression
	{
		public Expression EmbeddedExpression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSingleLineFunctionLambdaExpression(this, data);
		}
	}
	
	public class MultiLineLambdaExpression : LambdaExpression
	{
		public bool IsSub { get; set; }
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitMultiLineLambdaExpression(this, data);
		}
	}
	
	public enum LambdaExpressionModifiers
	{
		Async = Modifiers.Async,
		Iterator = Modifiers.Iterator
	}
}
