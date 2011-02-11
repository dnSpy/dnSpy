// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore]
	public class QueryExpressionTests
	{
		[Test]
		public void SimpleExpression()
		{
			/* 
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(
				"from c in customers where c.City == \"London\" select c"
			);
			Assert.AreEqual("c", qe.FromClause.Sources.First().Identifier);
			Assert.AreEqual("customers", ((IdentifierExpression)qe.FromClause.Sources.First().Expression).Identifier);
			Assert.AreEqual(1, qe.MiddleClauses.Count);
			Assert.IsInstanceOf(typeof(QueryExpressionWhereClause), qe.MiddleClauses[0]);
			QueryExpressionWhereClause wc = (QueryExpressionWhereClause)qe.MiddleClauses[0];
			Assert.IsInstanceOf(typeof(BinaryOperatorExpression), wc.Condition);
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);*/
			throw new NotImplementedException();
		}
		
		/* TODO port unit tests
		[Test]
		public void ExpressionWithType1()
		{
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(
				"from Customer c in customers select c"
			);
			Assert.AreEqual("c", qe.FromClause.Sources.First().Identifier);
			Assert.AreEqual("Customer", qe.FromClause.Sources.First().Type.ToString());
			Assert.AreEqual("customers", ((IdentifierExpression)qe.FromClause.Sources.First().Expression).Identifier);
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);
		}
		
		[Test]
		public void ExpressionWithType2()
		{
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(
				"from int c in customers select c"
			);
			Assert.AreEqual("c", qe.FromClause.Sources.First().Identifier);
			Assert.AreEqual("System.Int32", qe.FromClause.Sources.First().Type.Type);
			Assert.AreEqual("customers", ((IdentifierExpression)qe.FromClause.Sources.First().Expression).Identifier);
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);
		}
		
		
		[Test]
		public void ExpressionWithType3()
		{
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(
				"from S<int[]>? c in customers select c"
			);
			Assert.AreEqual("c", qe.FromClause.Sources.First().Identifier);
			Assert.AreEqual("System.Nullable<S<System.Int32[]>>", qe.FromClause.Sources.First().Type.ToString());
			Assert.AreEqual("customers", ((IdentifierExpression)qe.FromClause.Sources.First().Expression).Identifier);
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);
		}
		
		[Test]
		public void MultipleGenerators()
		{
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(@"
from c in customers
where c.City == ""London""
from o in c.Orders
where o.OrderDate.Year == 2005
select new { c.Name, o.OrderID, o.Total }");
			Assert.AreEqual(3, qe.MiddleClauses.Count);
			Assert.IsInstanceOf(typeof(QueryExpressionWhereClause), qe.MiddleClauses[0]);
			Assert.IsInstanceOf(typeof(QueryExpressionFromClause), qe.MiddleClauses[1]);
			Assert.IsInstanceOf(typeof(QueryExpressionWhereClause), qe.MiddleClauses[2]);
			
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);
		}
		
		[Test]
		public void ExpressionWithOrderBy()
		{
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(
				"from c in customers orderby c.Name select c"
			);
			Assert.AreEqual("c", qe.FromClause.Sources.First().Identifier);
			Assert.AreEqual("customers", ((IdentifierExpression)qe.FromClause.Sources.First().Expression).Identifier);
			Assert.IsInstanceOf(typeof(QueryExpressionOrderClause), qe.MiddleClauses[0]);
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);
		}
		
		[Test]
		public void ExpressionWithOrderByAndLet()
		{
			QueryExpression qe = ParseUtilCSharp.ParseExpression<QueryExpression>(
				"from c in customers orderby c.Name let x = c select x"
			);
			Assert.AreEqual("c", qe.FromClause.Sources.First().Identifier);
			Assert.AreEqual("customers", ((IdentifierExpression)qe.FromClause.Sources.First().Expression).Identifier);
			Assert.IsInstanceOf(typeof(QueryExpressionOrderClause), qe.MiddleClauses[0]);
			Assert.IsInstanceOf(typeof(QueryExpressionLetClause), qe.MiddleClauses[1]);
			Assert.IsInstanceOf(typeof(QueryExpressionSelectClause), qe.SelectOrGroupClause);
		}*/
	}
}
