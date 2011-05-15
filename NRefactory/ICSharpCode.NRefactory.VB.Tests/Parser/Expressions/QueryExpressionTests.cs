// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class QueryExpressionTests
	{
		void RunTest(string expression, int expectedCount, Action<QueryExpression> constraint, params Type[] expectedTypes)
		{
			var expr = ParseUtil.ParseExpression<QueryExpression>(expression);
			
			Assert.AreEqual(expectedCount, expr.Clauses.Count);
			
			for (int i = 0; i < expectedTypes.Length; i++) {
				Assert.IsTrue(expectedTypes[i] == expr.Clauses[i].GetType());
			}
			
			constraint(expr);
		}
		
		[Test]
		public void SimpleQueryTest()
		{
			RunTest("From o In db.Orders Select o.OrderID", 2,
			        expr => {
			        	var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
			        	var selectClause = expr.Clauses[1] as QueryExpressionSelectVBClause;
			        	
			        	Assert.AreEqual(1, fromClause.Sources.Count);
			        	
			        	var var1 = fromClause.Sources.First();
			        	
			        	Assert.AreEqual("o", var1.Identifier);
			        	Assert.IsTrue(var1.Expression is MemberReferenceExpression);
			        	var inExpr = var1.Expression as MemberReferenceExpression;
			        	Assert.IsTrue(inExpr.MemberName == "Orders" && inExpr.TargetObject is IdentifierExpression && (inExpr.TargetObject as IdentifierExpression).Identifier == "db");
			        	
			        	Assert.AreEqual(1, selectClause.Variables.Count);
			        	Assert.IsTrue(selectClause.Variables[0].Expression is MemberReferenceExpression);
			        	var member = selectClause.Variables[0].Expression as MemberReferenceExpression;
			        	
			        	Assert.IsTrue(member.MemberName == "OrderID" && member.TargetObject is IdentifierExpression && (member.TargetObject as IdentifierExpression).Identifier == "o");
			        },
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionSelectVBClause)
			       );
		}
		
		[Test]
		public void SkipTakeQueryTest()
		{
			RunTest("From o In db.Orders Select o.OrderID Skip 10 Take 5", 4,
			        expr => {
			        	var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
			        	var selectClause = expr.Clauses[1] as QueryExpressionSelectVBClause;
			        	var skipClause = expr.Clauses[2] as QueryExpressionPartitionVBClause;
			        	var takeClause = expr.Clauses[3] as QueryExpressionPartitionVBClause;
			        	
			        	Assert.AreEqual(1, fromClause.Sources.Count);
			        	
			        	var var1 = fromClause.Sources.First();
			        	
			        	Assert.AreEqual("o", var1.Identifier);
			        	Assert.IsTrue(var1.Expression is MemberReferenceExpression);
			        	var inExpr = var1.Expression as MemberReferenceExpression;
			        	Assert.IsTrue(inExpr.MemberName == "Orders" && inExpr.TargetObject is IdentifierExpression && (inExpr.TargetObject as IdentifierExpression).Identifier == "db");
			        	
			        	Assert.AreEqual(1, selectClause.Variables.Count);
			        	Assert.IsTrue(selectClause.Variables[0].Expression is MemberReferenceExpression);
			        	var member = selectClause.Variables[0].Expression as MemberReferenceExpression;
			        	
			        	Assert.IsTrue(member.MemberName == "OrderID" && member.TargetObject is IdentifierExpression && (member.TargetObject as IdentifierExpression).Identifier == "o");
			        	
			        	Assert.AreEqual(QueryExpressionPartitionType.Skip, skipClause.PartitionType);
			        	Assert.IsTrue(skipClause.Expression is PrimitiveExpression &&
			        	              (skipClause.Expression as PrimitiveExpression).StringValue == "10");
			        	
			        	Assert.AreEqual(QueryExpressionPartitionType.Take, takeClause.PartitionType);
			        	Assert.IsTrue(takeClause.Expression is PrimitiveExpression &&
			        	              (takeClause.Expression as PrimitiveExpression).StringValue == "5");
			        },
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionSelectVBClause),
			        typeof(QueryExpressionPartitionVBClause), typeof(QueryExpressionPartitionVBClause)
			       );
		}
		
		[Test]
		public void SkipWhileTakeWhileQueryTest()
		{
			RunTest("From o In db.Orders Select o.OrderID Skip While o.OrderId > 2 Take While o.OrderId < 5", 4,
			        expr => {
			        	var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
			        	var selectClause = expr.Clauses[1] as QueryExpressionSelectVBClause;
			        	var skipClause = expr.Clauses[2] as QueryExpressionPartitionVBClause;
			        	var takeClause = expr.Clauses[3] as QueryExpressionPartitionVBClause;
			        	
			        	Assert.AreEqual(1, fromClause.Sources.Count);
			        	
			        	var var1 = fromClause.Sources.First();
			        	
			        	Assert.AreEqual("o", var1.Identifier);
			        	Assert.IsTrue(var1.Expression is MemberReferenceExpression);
			        	var inExpr = var1.Expression as MemberReferenceExpression;
			        	Assert.IsTrue(inExpr.MemberName == "Orders" && inExpr.TargetObject is IdentifierExpression && (inExpr.TargetObject as IdentifierExpression).Identifier == "db");
			        	
			        	Assert.AreEqual(1, selectClause.Variables.Count);
			        	Assert.IsTrue(selectClause.Variables[0].Expression is MemberReferenceExpression);
			        	var member = selectClause.Variables[0].Expression as MemberReferenceExpression;
			        	
			        	Assert.IsTrue(member.MemberName == "OrderID" && member.TargetObject is IdentifierExpression && (member.TargetObject as IdentifierExpression).Identifier == "o");
			        	
			        	Assert.AreEqual(QueryExpressionPartitionType.SkipWhile, skipClause.PartitionType);
			        	Assert.IsTrue(skipClause.Expression is BinaryOperatorExpression);
			        	
			        	Assert.AreEqual(QueryExpressionPartitionType.TakeWhile, takeClause.PartitionType);
			        	Assert.IsTrue(takeClause.Expression is BinaryOperatorExpression);
			        },
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionSelectVBClause),
			        typeof(QueryExpressionPartitionVBClause), typeof(QueryExpressionPartitionVBClause)
			       );
		}
		
		[Test]
		public void MultipleValuesSelectTest()
		{
			RunTest(@"From i In list Select i, x2 = i^2",
			        2, expr => {
			        	var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
			        	var selectClause = expr.Clauses[1] as QueryExpressionSelectVBClause;
			        	
			        	Assert.AreEqual(1, fromClause.Sources.Count);
			        	
			        	var var1 = fromClause.Sources.First();
			        	
			        	Assert.AreEqual("i", var1.Identifier);
			        	Assert.IsTrue(var1.Expression is IdentifierExpression);
			        	Assert.IsTrue((var1.Expression as IdentifierExpression).Identifier == "list");
			        	
			        	Assert.AreEqual(2, selectClause.Variables.Count);
			        	
			        	var selectExpr1 = selectClause.Variables[0];
			        	var selectExpr2 = selectClause.Variables[1];
			        	
			        	Assert.IsEmpty(selectExpr1.Identifier);
			        	Assert.IsTrue(selectExpr1.Expression is IdentifierExpression &&
			        	              (selectExpr1.Expression as IdentifierExpression).Identifier == "i");
			        	
			        	Assert.AreEqual("x2", selectExpr2.Identifier);
			        	Assert.IsTrue(selectExpr2.Type.IsNull);
			        	Assert.IsTrue(selectExpr2.Expression is BinaryOperatorExpression);
			        	
			        	var binOp = selectExpr2.Expression as BinaryOperatorExpression;
			        	
			        	Assert.AreEqual(BinaryOperatorType.Power, binOp.Op);
			        	Assert.IsTrue(binOp.Left is IdentifierExpression && (binOp.Left as IdentifierExpression).Identifier == "i");
			        	Assert.IsTrue(binOp.Right is PrimitiveExpression && (binOp.Right as PrimitiveExpression).StringValue == "2");
			        },
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionSelectVBClause)
			       );
		}
		
		[Test]
		public void GroupTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var groupClause = expr.Clauses[1] as QueryExpressionGroupVBClause;
				var selectClause = expr.Clauses[2] as QueryExpressionSelectVBClause;
				
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var fromVar1 = fromClause.Sources.First();
				
				Assert.AreEqual("p", fromVar1.Identifier);
				Assert.IsTrue(fromVar1.Expression is IdentifierExpression);
				Assert.IsTrue((fromVar1.Expression as IdentifierExpression).Identifier == "products");
				
				Assert.AreEqual(1, groupClause.GroupVariables.Count);
				Assert.AreEqual(1, groupClause.ByVariables.Count);
				Assert.AreEqual(1, groupClause.IntoVariables.Count);
				
				var gv = groupClause.GroupVariables.First();
				var bv = groupClause.ByVariables.First();
				var iv = groupClause.IntoVariables.First();
				
				Assert.IsTrue(gv.Expression is IdentifierExpression && (gv.Expression as IdentifierExpression).Identifier == "p");
				Assert.IsTrue(bv.Expression is MemberReferenceExpression &&
				              (bv.Expression as MemberReferenceExpression).MemberName == "Category");
				Assert.IsTrue((bv.Expression as MemberReferenceExpression).TargetObject is IdentifierExpression &&
				              ((bv.Expression as MemberReferenceExpression).TargetObject as IdentifierExpression).Identifier == "p");
				Assert.IsTrue(iv.Expression is IdentifierExpression &&
				              (iv.Expression as IdentifierExpression).Identifier == "Group");
				
				Assert.AreEqual(2, selectClause.Variables.Count);
				
				var var1 = selectClause.Variables.First();
				var var2 = selectClause.Variables.Skip(1).First();
				
				Assert.IsTrue(var1.Expression is IdentifierExpression &&
				              (var1.Expression as IdentifierExpression).Identifier == "Category");
				Assert.IsTrue(var2.Expression is InvocationExpression &&
				              (var2.Expression as InvocationExpression).TargetObject is MemberReferenceExpression &&
				              ((var2.Expression as InvocationExpression).TargetObject as MemberReferenceExpression).MemberName == "Average" &&
				              ((var2.Expression as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject is IdentifierExpression &&
				              (((var2.Expression as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject as IdentifierExpression).Identifier == "Group");
			};
			
			RunTest(@"From p In products _
            Group p By p.Category Into Group _
            Select Category, AveragePrice = Group.Average(Function(p) p.UnitPrice)", 3, constraint,
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionGroupVBClause), typeof(QueryExpressionSelectVBClause));
		}
		
		[Test]
		public void LetTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var groupClause = expr.Clauses[1] as QueryExpressionGroupVBClause;
				var letClause = expr.Clauses[2] as QueryExpressionLetClause;
				var selectClause = expr.Clauses[3] as QueryExpressionSelectVBClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var fromVar1 = fromClause.Sources.First();
				
				Assert.AreEqual("p", fromVar1.Identifier);
				Assert.IsTrue(fromVar1.Expression is IdentifierExpression);
				Assert.IsTrue((fromVar1.Expression as IdentifierExpression).Identifier == "products");
				
				// Group By Into
				Assert.AreEqual(1, groupClause.GroupVariables.Count);
				Assert.AreEqual(1, groupClause.ByVariables.Count);
				Assert.AreEqual(1, groupClause.IntoVariables.Count);
				
				var gv = groupClause.GroupVariables.First();
				var bv = groupClause.ByVariables.First();
				var iv = groupClause.IntoVariables.First();
				
				Assert.IsTrue(gv.Expression is IdentifierExpression && (gv.Expression as IdentifierExpression).Identifier == "p");
				CheckMemberReferenceExpression(bv.Expression, "Category", "p");
				Assert.IsTrue(iv.Expression is IdentifierExpression &&
				              (iv.Expression as IdentifierExpression).Identifier == "Group");
				
				// Let
				Assert.AreEqual(1, letClause.Variables.Count);
				
				var letVariable = letClause.Variables.First();
				
				Assert.AreEqual("minPrice", letVariable.Identifier);
				Assert.IsTrue(letVariable.Expression is InvocationExpression);
				CheckMemberReferenceExpression((letVariable.Expression as InvocationExpression).TargetObject, "Min", "Group");

				// Select
				Assert.AreEqual(2, selectClause.Variables.Count);
				
				var var1 = selectClause.Variables.First();
				var var2 = selectClause.Variables.Skip(1).First();
				
				Assert.IsTrue(var1.Expression is IdentifierExpression &&
				              (var1.Expression as IdentifierExpression).Identifier == "Category");
				Assert.IsTrue(var2.Expression is InvocationExpression);
				CheckMemberReferenceExpression((var2.Expression as InvocationExpression).TargetObject, "Where", "Group");
			};
			
			RunTest(@"From p In products _
            Group p By p.Category Into Group _
            Let minPrice = Group.Min(Function(p) p.UnitPrice) _
            Select Category, CheapestProducts = Group.Where(Function(p) p.UnitPrice = minPrice)", 4, constraint,
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionGroupVBClause), typeof(QueryExpressionLetClause), typeof(QueryExpressionSelectVBClause));
		}
		
		[Test]
		public void CrossJoinTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var joinClause = expr.Clauses[1] as QueryExpressionJoinVBClause;
				var selectClause = expr.Clauses[2] as QueryExpressionSelectVBClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var fromVar1 = fromClause.Sources.First();
				
				Assert.AreEqual("c", fromVar1.Identifier);
				Assert.IsTrue(fromVar1.Expression is IdentifierExpression);
				Assert.IsTrue((fromVar1.Expression as IdentifierExpression).Identifier == "categories");
				
				// Join In On Equals
				var inClause = joinClause.JoinVariable as CollectionRangeVariable;
				
				Assert.AreEqual("p", inClause.Identifier);
				Assert.IsTrue(inClause.Expression is IdentifierExpression &&
				              (inClause.Expression as IdentifierExpression).Identifier == "products");
				
				Assert.IsTrue(joinClause.SubJoin.IsNull);
				
				Assert.AreEqual(1, joinClause.Conditions.Count);
				
				var condition1 = joinClause.Conditions.First();
				
				Assert.IsTrue(condition1.LeftSide is IdentifierExpression && (condition1.LeftSide as IdentifierExpression).Identifier == "c");
				
				CheckMemberReferenceExpression(condition1.RightSide, "Category", "p");
				
				// Select
				Assert.AreEqual(2, selectClause.Variables.Count);
				
				var var1 = selectClause.Variables.First();
				var var2 = selectClause.Variables.Skip(1).First();
				
				Assert.AreEqual("Category", var1.Identifier);
				Assert.IsEmpty(var2.Identifier);
				
				Assert.IsTrue(var1.Expression is IdentifierExpression &&
				              (var1.Expression as IdentifierExpression).Identifier == "c");
				CheckMemberReferenceExpression(var2.Expression, "ProductName", "p");
			};
			
			RunTest(@"From c In categories _
                Join p In products On c Equals p.Category _
                Select Category = c, p.ProductName", 3, constraint,
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionJoinVBClause), typeof(QueryExpressionSelectVBClause));
		}
		
		[Test]
		public void OrderByTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var orderClause = expr.Clauses[1] as QueryExpressionOrderClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var var1 = fromClause.Sources.First();
				
				Assert.AreEqual("i", var1.Identifier);
				Assert.IsTrue(var1.Expression is IdentifierExpression);
				Assert.IsTrue((var1.Expression as IdentifierExpression).Identifier == "list");
				
				// Order By
				Assert.AreEqual(1, orderClause.Orderings.Count);
				
				var ordering1 = orderClause.Orderings.First();
				
				Assert.IsTrue(ordering1.Criteria is IdentifierExpression &&
				              (ordering1.Criteria as IdentifierExpression).Identifier == "i");
				Assert.AreEqual(QueryExpressionOrderingDirection.None, ordering1.Direction);
			};
			
			RunTest(@"From i In list Order By i", 2, constraint, typeof(QueryExpressionFromClause), typeof(QueryExpressionOrderClause));
		}
		
		[Test]
		public void OrderByTest2()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var orderClause = expr.Clauses[1] as QueryExpressionOrderClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var var1 = fromClause.Sources.First();
				
				Assert.AreEqual("i", var1.Identifier);
				Assert.IsTrue(var1.Expression is IdentifierExpression);
				Assert.IsTrue((var1.Expression as IdentifierExpression).Identifier == "list");
				
				// Order By
				Assert.AreEqual(1, orderClause.Orderings.Count);
				
				var ordering1 = orderClause.Orderings.First();
				
				Assert.IsTrue(ordering1.Criteria is IdentifierExpression &&
				              (ordering1.Criteria as IdentifierExpression).Identifier == "i");
				Assert.AreEqual(QueryExpressionOrderingDirection.Ascending, ordering1.Direction);
			};
			
			RunTest(@"From i In list Order By i Ascending", 2, constraint, typeof(QueryExpressionFromClause), typeof(QueryExpressionOrderClause));
		}
		
		[Test]
		public void OrderByTest3()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var orderClause = expr.Clauses[1] as QueryExpressionOrderClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var var1 = fromClause.Sources.First();
				
				Assert.AreEqual("i", var1.Identifier);
				Assert.IsTrue(var1.Expression is IdentifierExpression);
				Assert.IsTrue((var1.Expression as IdentifierExpression).Identifier == "list");
				
				// Order By
				Assert.AreEqual(1, orderClause.Orderings.Count);
				
				var ordering1 = orderClause.Orderings.First();
				
				Assert.IsTrue(ordering1.Criteria is IdentifierExpression &&
				              (ordering1.Criteria as IdentifierExpression).Identifier == "i");
				Assert.AreEqual(QueryExpressionOrderingDirection.Descending, ordering1.Direction);
			};
			
			RunTest(@"From i In list Order By i Descending", 2, constraint, typeof(QueryExpressionFromClause), typeof(QueryExpressionOrderClause));
		}
		
		[Test]
		public void OrderByThenByTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var orderClause = expr.Clauses[1] as QueryExpressionOrderClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var var1 = fromClause.Sources.First();
				
				Assert.AreEqual("d", var1.Identifier);
				Assert.IsTrue(var1.Expression is IdentifierExpression);
				Assert.IsTrue((var1.Expression as IdentifierExpression).Identifier == "digits");
				
				// Order By
				Assert.AreEqual(2, orderClause.Orderings.Count);
				
				var ordering1 = orderClause.Orderings.First();
				var ordering2 = orderClause.Orderings.Skip(1).First();
				
				CheckMemberReferenceExpression(ordering1.Criteria, "Length", "d");

				Assert.IsTrue(ordering2.Criteria is IdentifierExpression &&
				              (ordering2.Criteria as IdentifierExpression).Identifier == "d");
				
				Assert.AreEqual(QueryExpressionOrderingDirection.None, ordering1.Direction);
				Assert.AreEqual(QueryExpressionOrderingDirection.None, ordering2.Direction);

			};
			
			RunTest(@"From d In digits _
        Order By d.Length, d", 2, constraint,
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionOrderClause));
		}
		
		[Test]
		public void DistinctTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				
				// From
				Assert.AreEqual(1, fromClause.Sources.Count);
				
				var var1 = fromClause.Sources.First();
				
				Assert.AreEqual("d", var1.Identifier);
				Assert.IsTrue(var1.Expression is IdentifierExpression);
				Assert.IsTrue((var1.Expression as IdentifierExpression).Identifier == "digits");
			};
			
			RunTest(@"From d In digits Distinct", 2, constraint,
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionDistinctClause));
		}
		
		[Test]
		public void AggregateTest()
		{
			Action<QueryExpression> constraint = expr => {
				var clause = expr.Clauses[0] as QueryExpressionAggregateClause;
				
				Assert.AreEqual("p", clause.Source.Identifier);
				CheckMemberReferenceExpression(clause.Source.Expression, "GetProcesses", "Process");
				
				Assert.AreEqual(1, clause.IntoVariables.Count);
				
				var into1 = clause.IntoVariables.First();
				
				Assert.AreEqual("virtualMemory", into1.Identifier);
				
				Assert.IsTrue(into1.Expression is InvocationExpression &&
				              (into1.Expression as InvocationExpression).TargetObject is IdentifierExpression &&
				              ((into1.Expression as InvocationExpression).TargetObject as IdentifierExpression).Identifier == "Sum");
				Assert.AreEqual(1, (into1.Expression as InvocationExpression).Arguments.Count);
				CheckMemberReferenceExpression((into1.Expression as InvocationExpression).Arguments.First(), "VirtualMemorySize64", "p");
			};
			
			RunTest(@"Aggregate p In Process.GetProcesses _
			Into virtualMemory = Sum(p.VirtualMemorySize64)", 1, constraint, typeof(QueryExpressionAggregateClause));
		}
		
		[Test]
		public void GroupJoinTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause1 = expr.Clauses[0] as QueryExpressionFromClause;
				var groupJoinClause = expr.Clauses[1] as QueryExpressionGroupJoinVBClause;
				var fromClause2 = expr.Clauses[2] as QueryExpressionFromClause;
				var selectClause = expr.Clauses[3] as QueryExpressionSelectVBClause;
				
				// From 1
				Assert.AreEqual(1, fromClause1.Sources.Count);
				
				var var1 = fromClause1.Sources.First();
				
				Assert.AreEqual("s", var1.Identifier);
				Assert.IsTrue(var1.Expression is IdentifierExpression &&
				              (var1.Expression as IdentifierExpression).Identifier == "fileList");
				
				// From 2
				Assert.AreEqual(1, fromClause2.Sources.Count);
				
				var var2 = fromClause2.Sources.First();
				
				Assert.AreEqual("p", var2.Identifier);
				Assert.IsTrue(var2.Expression is IdentifierExpression &&
				              (var2.Expression as IdentifierExpression).Identifier == "Group");
				
				// Select
				Assert.AreEqual(1, selectClause.Variables.Count);
				
				var var3 = selectClause.Variables.First();
				
				Assert.IsEmpty(var3.Identifier);
				Assert.IsTrue(var3.Expression is IdentifierExpression &&
				              (var3.Expression as IdentifierExpression).Identifier == "s");
				
				// Group Join
				var joinClause = groupJoinClause.JoinClause;
				
				// Join In On Equals
				var inClause = joinClause.JoinVariable as CollectionRangeVariable;
				
				Assert.AreEqual("p", inClause.Identifier);
				Assert.IsTrue(inClause.Expression is IdentifierExpression &&
				              (inClause.Expression as IdentifierExpression).Identifier == "IMAGES");
				
				Assert.IsTrue(joinClause.SubJoin.IsNull);
				
				Assert.AreEqual(1, joinClause.Conditions.Count);
				
				var condition1 = joinClause.Conditions.First();
				
				Assert.IsTrue(condition1.LeftSide is InvocationExpression);
				Assert.IsTrue((condition1.LeftSide as InvocationExpression).TargetObject is MemberReferenceExpression);
				Assert.IsTrue(((condition1.LeftSide as InvocationExpression).TargetObject as MemberReferenceExpression).MemberName == "ToUpper");
				Assert.IsTrue(((condition1.LeftSide as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject is MemberReferenceExpression);
				Assert.IsTrue((((condition1.LeftSide as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject as MemberReferenceExpression).MemberName == "Extension");
				Assert.IsTrue((((condition1.LeftSide as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject as MemberReferenceExpression).TargetObject is IdentifierExpression);
				Assert.IsTrue(((((condition1.LeftSide as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject as MemberReferenceExpression).TargetObject as IdentifierExpression).Identifier == "s");
				
				Assert.IsTrue(condition1.RightSide is InvocationExpression);
				Assert.IsTrue((condition1.RightSide as InvocationExpression).TargetObject is MemberReferenceExpression);
				Assert.IsTrue(((condition1.RightSide as InvocationExpression).TargetObject as MemberReferenceExpression).MemberName == "ToUpper");
				Assert.IsTrue(((condition1.RightSide as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject is IdentifierExpression);
				Assert.IsTrue((((condition1.RightSide as InvocationExpression).TargetObject as MemberReferenceExpression).TargetObject as IdentifierExpression).Identifier == "p");
			};
			
			RunTest(@"From s In fileList _
Group Join p In IMAGES On s.Extension.ToUpper() Equals p.ToUpper() Into Group _
From p In Group _
Select s", 4, constraint,
			        typeof(QueryExpressionFromClause), typeof(QueryExpressionGroupJoinVBClause), typeof(QueryExpressionFromClause), typeof(QueryExpressionSelectVBClause));
		}
		
		[Test]
		public void SelectManyTest()
		{
			Action<QueryExpression> constraint = expr => {
				var fromClause = expr.Clauses[0] as QueryExpressionFromClause;
				var whereClause = expr.Clauses[1] as QueryExpressionWhereClause;
				var selectClause = expr.Clauses[2] as QueryExpressionSelectVBClause;
				
				// From
				Assert.AreEqual(2, fromClause.Sources.Count);
				
				var fromVar1 = fromClause.Sources.First();
				var fromVar2 = fromClause.Sources.Skip(1).First();
				
				Assert.AreEqual("c", fromVar1.Identifier);
				Assert.IsTrue(fromVar1.Expression is IdentifierExpression);
				Assert.IsTrue((fromVar1.Expression as IdentifierExpression).Identifier == "customers");
				
				Assert.AreEqual("o", fromVar2.Identifier);
				CheckMemberReferenceExpression(fromVar2.Expression, "Orders", "c");
				
				// Where
				Assert.IsTrue(whereClause.Condition is BinaryOperatorExpression);
				Assert.IsTrue((whereClause.Condition as BinaryOperatorExpression).Op == BinaryOperatorType.LessThan);
				CheckMemberReferenceExpression((whereClause.Condition as BinaryOperatorExpression).Left, "Total", "o");
				Assert.IsTrue((whereClause.Condition as BinaryOperatorExpression).Right is PrimitiveExpression);
				Assert.IsTrue((double)((whereClause.Condition as BinaryOperatorExpression).Right as PrimitiveExpression).Value == 500.0);
				
				// Select
				foreach (var v in selectClause.Variables) {
					Assert.IsEmpty(v.Identifier);
				}
				
				var var1 = selectClause.Variables.First();
				var var2 = selectClause.Variables.Skip(1).First();
				var var3 = selectClause.Variables.Skip(2).First();
				
				CheckMemberReferenceExpression(var1.Expression, "CustomerID", "c");
				CheckMemberReferenceExpression(var2.Expression, "OrderID", "o");
				CheckMemberReferenceExpression(var3.Expression, "Total", "o");
			};
			
			RunTest(@"From c In customers, o In c.Orders _
        Where o.Total < 500.0 _
        Select c.CustomerID, o.OrderID, o.Total", 3, constraint, typeof(QueryExpressionFromClause), typeof(QueryExpressionWhereClause), typeof(QueryExpressionSelectVBClause));
		}
		
		void CheckMemberReferenceExpression(Expression expr, string memberName, string targetObjectIdentifier)
		{
			Assert.IsTrue(expr is MemberReferenceExpression);
			Assert.IsTrue((expr as MemberReferenceExpression).MemberName == memberName &&
			              (expr as MemberReferenceExpression).TargetObject is IdentifierExpression &&
			              ((expr as MemberReferenceExpression).TargetObject as IdentifierExpression).Identifier == targetObjectIdentifier);
		}
	}
}
