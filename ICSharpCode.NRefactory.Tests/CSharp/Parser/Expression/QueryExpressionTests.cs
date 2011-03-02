// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture, Ignore("Query expressions not yet implemented")]
	public class QueryExpressionTests
	{
		[Test]
		public void SimpleExpression()
		{
			ParseUtilCSharp.AssertExpression(
				"from c in customers where c.City == \"London\" select c",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QueryWhereClause {
							Condition = new BinaryOperatorExpression {
								Left = new IdentifierExpression("c").Member("City"),
								Operator = BinaryOperatorType.Equality,
								Right = new PrimitiveExpression("London")
							}
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("c")
						}
					}});
		}
		
		[Test]
		public void ExpressionWithType1()
		{
			ParseUtilCSharp.AssertExpression(
				"from Customer c in customers select c",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Type = new SimpleType("Customer"),
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("c")
						}
					}});
		}
		
		[Test]
		public void ExpressionWithType2()
		{
			ParseUtilCSharp.AssertExpression(
				"from int c in customers select c",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Type = new PrimitiveType("int"),
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("c")
						}
					}});
		}
		
		
		[Test]
		public void ExpressionWithType3()
		{
			ParseUtilCSharp.AssertExpression(
				"from S<int[]>? c in customers select c",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Type = new ComposedType {
								BaseType = new SimpleType {
									Identifier = "S",
									TypeArguments = {
										new PrimitiveType("int").MakeArrayType()
									}
								},
								HasNullableSpecifier = true
							},
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("c")
						}
					}});
		}
		
		[Test]
		public void MultipleGenerators()
		{
			ParseUtilCSharp.AssertExpression(
				@"
from c in customers
where c.City == ""London""
from o in c.Orders
where o.OrderDate.Year == 2005
select new { c.Name, o.OrderID, o.Total }",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QueryWhereClause {
							Condition = new BinaryOperatorExpression {
								Left = new IdentifierExpression("c").Member("City"),
								Operator = BinaryOperatorType.Equality,
								Right = new PrimitiveExpression("London")
							}
						},
						new QueryFromClause {
							Identifier = "o",
							Expression = new IdentifierExpression("c").Member("Orders")
						},
						new QueryWhereClause {
							Condition = new BinaryOperatorExpression {
								Left = new IdentifierExpression("c").Member("OrderDate").Member("Year"),
								Operator = BinaryOperatorType.Equality,
								Right = new PrimitiveExpression(2005)
							}
						},
						new QuerySelectClause {
							Expression = new ObjectCreateExpression {
								Initializer = new ArrayInitializerExpression {
									Elements = {
										new IdentifierExpression("c").Member("Name"),
										new IdentifierExpression("o").Member("OrderID"),
										new IdentifierExpression("o").Member("Total")
									}
								}
							}
						}
					}});
		}
		
		[Test]
		public void ExpressionWithOrderBy()
		{
			ParseUtilCSharp.AssertExpression(
				"from c in customers orderby c.Name select c",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QueryOrderClause {
							Orderings = {
								new QueryOrdering {
									Expression = new IdentifierExpression("c").Member("Name")
								}
							}
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("c")
						}
					}});
		}
		
		[Test]
		public void ExpressionWithOrderByAndLet()
		{
			ParseUtilCSharp.AssertExpression(
				"from c in customers orderby c.Name descending let x = c select x",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "c",
							Expression = new IdentifierExpression("customers")
						},
						new QueryOrderClause {
							Orderings = {
								new QueryOrdering {
									Expression = new IdentifierExpression("c").Member("Name"),
									Direction = QueryOrderingDirection.Descending
								}
							}
						},
						new QueryLetClause {
							Identifier = "x",
							Expression = new IdentifierExpression("c")
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("x")
						}
					}});
		}
		
		[Test]
		public void QueryContinuation()
		{
			ParseUtilCSharp.AssertExpression(
				"from a in b select c into d select e",
				new QueryExpression {
					Clauses = {
						new QueryContinuationClause {
							PrecedingQuery = new QueryExpression {
								Clauses = {
									new QueryFromClause {
										Identifier = "a",
										Expression = new IdentifierExpression("b")
									},
									new QuerySelectClause { Expression = new IdentifierExpression("c") }
								}
							},
							Identifier = "d"
						},
						new QuerySelectClause { Expression = new IdentifierExpression("e") }
					}
				}
			);
		}
	}
}
