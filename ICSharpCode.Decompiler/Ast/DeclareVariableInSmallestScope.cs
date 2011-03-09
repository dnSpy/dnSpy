// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;

namespace ICSharpCode.Decompiler.Ast
{
	/// <summary>
	/// Helper class for declaring variables.
	/// </summary>
	public static class DeclareVariableInSmallestScope
	{
		static readonly ExpressionStatement assignmentPattern = new ExpressionStatement(
			new AssignmentExpression(
				new NamedNode("ident", new IdentifierExpression()),
				new AnyNode("init")
			));
		
		/// <summary>
		/// Declares a variable in the smallest required scope.
		/// </summary>
		/// <param name="node">The root of the subtree being searched for the best insertion position</param>
		/// <param name="type">The type of the new variable</param>
		/// <param name="name">The name of the new variable</param>
		/// <param name="allowPassIntoLoops">Whether the variable is allowed to be placed inside a loop</param>
		public static VariableDeclarationStatement DeclareVariable(AstNode node, AstType type, string name, bool allowPassIntoLoops = true)
		{
			VariableDeclarationStatement result = null;
			AstNode pos = FindInsertPos(node, name, allowPassIntoLoops);
			if (pos != null) {
				Match m = assignmentPattern.Match(pos);
				if (m != null && m.Get<IdentifierExpression>("ident").Single().Identifier == name) {
					result = new VariableDeclarationStatement(type, name, m.Get<Expression>("init").Single().Detach());
					result.Variables.Single().CopyAnnotationsFrom(((ExpressionStatement)pos).Expression);
					result.CopyAnnotationsFrom(pos);
					pos.ReplaceWith(result);
				} else {
					result = new VariableDeclarationStatement(type, name);
					pos.Parent.InsertChildBefore(pos, result, BlockStatement.StatementRole);
				}
			}
			return result;
		}
		
		static AstNode FindInsertPos(AstNode node, string name, bool allowPassIntoLoops)
		{
			AstNode pos = null;
			AstNode withinPos = null;
			while (node != null) {
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null && ident.Identifier == name && ident.TypeArguments.Count == 0)
					return node;
				
				AstNode withinCurrent = FindInsertPos(node.FirstChild, name, allowPassIntoLoops);
				if (withinCurrent != null) {
					if (pos == null) {
						pos = node;
						withinPos = withinCurrent;
					} else {
						return pos;
					}
				}
				node = node.NextSibling;
			}
			if (withinPos != null && withinPos.Role == BlockStatement.StatementRole && AllowPassInto(pos, allowPassIntoLoops))
				return withinPos;
			else
				return pos;
		}
		
		static bool AllowPassInto(AstNode node, bool allowPassIntoLoops)
		{
			if (node is AnonymousMethodExpression || node is LambdaExpression)
				return false;
			if (node is ForStatement || node is ForeachStatement || node is DoWhileStatement || node is WhileStatement)
				return allowPassIntoLoops;
			return true;
		}
	}
}
