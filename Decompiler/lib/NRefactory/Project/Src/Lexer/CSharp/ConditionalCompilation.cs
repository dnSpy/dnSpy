// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.Parser.CSharp
{
	public sealed class ConditionalCompilation : AbstractAstVisitor
	{
		static readonly object SymbolDefined = new object();
		Dictionary<string, object> symbols = new Dictionary<string, object>();
		
		public IDictionary<string, object> Symbols { 
			get { return symbols; }
		}
		
		public void Define(string symbol)
		{
			symbols[symbol] = SymbolDefined;
		}
		
		public void Undefine(string symbol)
		{
			symbols.Remove(symbol);
		}
		
		public bool Evaluate(Expression condition)
		{
			return condition.AcceptVisitor(this, null) == SymbolDefined;
		}
		
		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value is bool)
				return (bool)primitiveExpression.Value ? SymbolDefined : null;
			else
				return null;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			return symbols.ContainsKey(identifierExpression.Identifier) ? SymbolDefined : null;
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			if (unaryOperatorExpression.Op == UnaryOperatorType.Not) {
				return unaryOperatorExpression.Expression.AcceptVisitor(this, data) == SymbolDefined ? null : SymbolDefined;
			} else {
				return null;
			}
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			bool lhs = binaryOperatorExpression.Left.AcceptVisitor(this, data) == SymbolDefined;
			bool rhs = binaryOperatorExpression.Right.AcceptVisitor(this, data) == SymbolDefined;
			bool result;
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.LogicalAnd:
					result = lhs && rhs;
					break;
				case BinaryOperatorType.LogicalOr:
					result = lhs || rhs;
					break;
				case BinaryOperatorType.Equality:
					result = lhs == rhs;
					break;
				case BinaryOperatorType.InEquality:
					result = lhs != rhs;
					break;
				default:
					return null;
			}
			return result ? SymbolDefined : null;
		}
		
		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
	}
}
