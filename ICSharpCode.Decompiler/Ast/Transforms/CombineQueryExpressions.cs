// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Combines query expressions and removes transparent identifiers.
	/// </summary>
	public class CombineQueryExpressions : IAstTransform
	{
		readonly DecompilerContext context;
		
		public CombineQueryExpressions(DecompilerContext context)
		{
			this.context = context;
		}
		
		public void Run(AstNode compilationUnit)
		{
			if (!context.Settings.QueryExpressions)
				return;
			CombineQueries(compilationUnit);
		}
		
		static readonly InvocationExpression castPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new AnyNode("inExpr"),
				MemberName = "Cast",
				TypeArguments = { new AnyNode("targetType") }
			}};
		
		void CombineQueries(AstNode node)
		{
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				CombineQueries(child);
			}
			QueryExpression query = node as QueryExpression;
			if (query != null) {
				QueryFromClause fromClause = (QueryFromClause)query.Clauses.First();
				QueryExpression innerQuery = fromClause.Expression as QueryExpression;
				if (innerQuery != null) {
					if (TryRemoveTransparentIdentifier(query, fromClause, innerQuery)) {
						RemoveTransparentIdentifierReferences(query);
					} else {
						QueryContinuationClause continuation = new QueryContinuationClause();
						continuation.PrecedingQuery = innerQuery.Detach();
						continuation.Identifier = fromClause.Identifier;
						fromClause.ReplaceWith(continuation);
					}
				} else {
					Match m = castPattern.Match(fromClause.Expression);
					if (m.Success) {
						fromClause.Type = m.Get<AstType>("targetType").Single().Detach();
						fromClause.Expression = m.Get<Expression>("inExpr").Single().Detach();
					}
				}
			}
		}
		
		static readonly QuerySelectClause selectTransparentIdentifierPattern = new QuerySelectClause {
			Expression = new Choice {
				new AnonymousTypeCreateExpression {
					Initializers = {
						new NamedExpression {
							Name = Pattern.AnyString,
							Expression = new IdentifierExpression(Pattern.AnyString)
						}.WithName("nae1"),
						new NamedExpression {
							Name = Pattern.AnyString,
							Expression = new AnyNode("nae2Expr")
						}.WithName("nae2")
					}
				},
				new AnonymousTypeCreateExpression {
					Initializers = {
						new NamedNode("identifier", new IdentifierExpression(Pattern.AnyString)),
						new AnyNode("nae2Expr")
					}
				}
			}};
		
		bool IsTransparentIdentifier(string identifier)
		{
			return identifier.StartsWith("<>", StringComparison.Ordinal) && identifier.Contains("TransparentIdentifier");
		}
		
		bool TryRemoveTransparentIdentifier(QueryExpression query, QueryFromClause fromClause, QueryExpression innerQuery)
		{
			if (!IsTransparentIdentifier(fromClause.Identifier))
				return false;
			Match match = selectTransparentIdentifierPattern.Match(innerQuery.Clauses.Last());
			if (!match.Success)
				return false;
			QuerySelectClause selectClause = (QuerySelectClause)innerQuery.Clauses.Last();
			NamedExpression nae1 = match.Get<NamedExpression>("nae1").SingleOrDefault();
			NamedExpression nae2 = match.Get<NamedExpression>("nae2").SingleOrDefault();
			if (nae1 != null && nae1.Name != ((IdentifierExpression)nae1.Expression).Identifier)
				return false;
			Expression nae2Expr = match.Get<Expression>("nae2Expr").Single();
			IdentifierExpression nae2IdentExpr = nae2Expr as IdentifierExpression;
			if (nae2IdentExpr != null && (nae2 == null || nae2.Name == nae2IdentExpr.Identifier)) {
				// from * in (from x in ... select new { x = x, y = y }) ...
				// =>
				// from x in ... ...
				fromClause.Remove();
				selectClause.Remove();
				// Move clauses from innerQuery to query
				QueryClause insertionPos = null;
				foreach (var clause in innerQuery.Clauses) {
					query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
				}
			} else {
				// from * in (from x in ... select new { x = x, y = expr }) ...
				// =>
				// from x in ... let y = expr ...
				fromClause.Remove();
				selectClause.Remove();
				// Move clauses from innerQuery to query
				QueryClause insertionPos = null;
				foreach (var clause in innerQuery.Clauses) {
					query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
				}
				string ident;
				if (nae2 != null)
					ident = nae2.Name;
				else if (nae2Expr is IdentifierExpression)
					ident = ((IdentifierExpression)nae2Expr).Identifier;
				else if (nae2Expr is MemberReferenceExpression)
					ident = ((MemberReferenceExpression)nae2Expr).MemberName;
				else
					throw new InvalidOperationException("Could not infer name from initializer in AnonymousTypeCreateExpression");
				query.Clauses.InsertAfter(insertionPos, new QueryLetClause { Identifier = ident, Expression = nae2Expr.Detach() });
			}
			return true;
		}
		
		/// <summary>
		/// Removes all occurrences of transparent identifiers
		/// </summary>
		void RemoveTransparentIdentifierReferences(AstNode node)
		{
			foreach (AstNode child in node.Children) {
				RemoveTransparentIdentifierReferences(child);
			}
			MemberReferenceExpression mre = node as MemberReferenceExpression;
			if (mre != null) {
				IdentifierExpression ident = mre.Target as IdentifierExpression;
				if (ident != null && IsTransparentIdentifier(ident.Identifier)) {
					IdentifierExpression newIdent = new IdentifierExpression(mre.MemberName);
					mre.TypeArguments.MoveTo(newIdent.TypeArguments);
					newIdent.CopyAnnotationsFrom(mre);
					newIdent.RemoveAnnotations<PropertyDeclaration>(); // remove the reference to the property of the anonymous type
					mre.ReplaceWith(newIdent);
					return;
				}
			}
		}
	}
}
