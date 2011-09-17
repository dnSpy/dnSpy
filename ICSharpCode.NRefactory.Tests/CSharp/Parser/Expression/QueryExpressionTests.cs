// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Expression
{
	[TestFixture]
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
								Left = new IdentifierExpression("o").Member("OrderDate").Member("Year"),
								Operator = BinaryOperatorType.Equality,
								Right = new PrimitiveExpression(2005)
							}
						},
						new QuerySelectClause {
							Expression = new AnonymousTypeCreateExpression {
								Initializers = {
									new IdentifierExpression("c").Member("Name"),
									new IdentifierExpression("o").Member("OrderID"),
									new IdentifierExpression("o").Member("Total")
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
		
		
		[Test]
		public void QueryContinuationWithMultipleFrom()
		{
			ParseUtilCSharp.AssertExpression(
				"from a in b from c in d select e into f select g",
				new QueryExpression {
					Clauses = {
						new QueryContinuationClause {
							PrecedingQuery = new QueryExpression {
								Clauses = {
									new QueryFromClause {
										Identifier = "a",
										Expression = new IdentifierExpression("b")
									},
									new QueryFromClause {
										Identifier = "c",
										Expression = new IdentifierExpression("d")
									},
									new QuerySelectClause { Expression = new IdentifierExpression("e") }
								}
							},
							Identifier = "f"
						},
						new QuerySelectClause { Expression = new IdentifierExpression("g") }
					}
				}
			);
		}
		
		[Test]
		public void MultipleQueryContinuation()
		{
			ParseUtilCSharp.AssertExpression(
				"from a in b select c into d select e into f select g",
				new QueryExpression {
					Clauses = {
						new QueryContinuationClause {
							PrecedingQuery = new QueryExpression {
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
							},
							Identifier = "f"
						},
						new QuerySelectClause { Expression = new IdentifierExpression("g") }
					}});
		}
		
		[Test]
		public void QueryWithGroupBy()
		{
			ParseUtilCSharp.AssertExpression(
				"from a in b group c by d",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "a",
							Expression = new IdentifierExpression("b")
						},
						new QueryGroupClause {
							Projection = new IdentifierExpression("c"),
							Key = new IdentifierExpression("d")
						}
					}});
		}
		
		[Test]
		public void QueryWithJoin()
		{
			ParseUtilCSharp.AssertExpression(
				"from a in b join c in d on e equals f select g",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "a",
							Expression = new IdentifierExpression("b")
						},
						new QueryJoinClause {
							JoinIdentifier = "c",
							InExpression = new IdentifierExpression("d"),
							OnExpression = new IdentifierExpression("e"),
							EqualsExpression = new IdentifierExpression("f")
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("g")
						}
					}});
		}
		
		[Test]
		public void QueryWithGroupJoin()
		{
			ParseUtilCSharp.AssertExpression(
				"from a in b join c in d on e equals f into g select h",
				new QueryExpression {
					Clauses = {
						new QueryFromClause {
							Identifier = "a",
							Expression = new IdentifierExpression("b")
						},
						new QueryJoinClause {
							JoinIdentifier = "c",
							InExpression = new IdentifierExpression("d"),
							OnExpression = new IdentifierExpression("e"),
							EqualsExpression = new IdentifierExpression("f"),
							IntoIdentifier = "g"
						},
						new QuerySelectClause {
							Expression = new IdentifierExpression("h")
						}
					}});
		}
	}
}
