//
// AstFormattingVisitor_Query.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	partial class FormattingVisitor
	{
		int GetUpdatedStartLocation(QueryExpression queryExpression)
		{
			//TODO
			return queryExpression.StartLocation.Column;
		}

		public override void VisitQueryExpression(QueryExpression queryExpression)
		{
			var oldIndent = curIndent.Clone();

			var column = GetUpdatedStartLocation(queryExpression);

			int extraSpaces = column - 1 - (curIndent.CurIndent / options.TabSize);
			if (extraSpaces < 0) {
				//This check should probably be removed in the future, when GetUpdatedStartLocation is implemented
				extraSpaces = 0;
			}

			curIndent.ExtraSpaces = extraSpaces;
			VisitChildren(queryExpression);

			curIndent = oldIndent;
		}

		public override void VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			FixClauseIndentation(queryFromClause, queryFromClause.FromKeyword);
		}

		public override void VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			VisitChildren(queryContinuationClause);
		}

		public override void VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			FixClauseIndentation(queryGroupClause, queryGroupClause.GroupKeyword);
		}

		public override void VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			FixClauseIndentation(queryJoinClause, queryJoinClause.JoinKeyword);
		}

		public override void VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			FixClauseIndentation(queryLetClause, queryLetClause.LetKeyword);
		}

		public override void VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			FixClauseIndentation(querySelectClause, querySelectClause.SelectKeyword);
		}

		public override void VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			FixClauseIndentation(queryOrderClause, queryOrderClause.OrderbyToken);
		}

		public override void VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			FixClauseIndentation(queryWhereClause, queryWhereClause.WhereKeyword);
		}

		void FixClauseIndentation(QueryClause clause, AstNode keyword) {
			var parentExpression = clause.GetParent<QueryExpression>();
			bool isFirstClause = parentExpression.Clauses.First() == clause;
			if (!isFirstClause) {
				PlaceOnNewLine(policy.NewLineBeforeNewQueryClause, keyword);
			}

			int extraSpaces = options.IndentSize;
			curIndent.ExtraSpaces += extraSpaces;
			foreach (var child in clause.Children) {
				var expression = child as Expression;
				if (expression != null) {
					FixIndentation(child);
					child.AcceptVisitor(this);
				}

				var tokenNode = child as CSharpTokenNode;
				if (tokenNode != null) {
					if (tokenNode.GetNextSibling(NoWhitespacePredicate).StartLocation.Line != tokenNode.EndLocation.Line) {
						ForceSpacesAfter(tokenNode, false);
					}
				}
			}
			curIndent.ExtraSpaces -= extraSpaces;
		}
	}
}

