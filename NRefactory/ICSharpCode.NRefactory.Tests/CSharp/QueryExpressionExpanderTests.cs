using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp.Parser;
using ICSharpCode.NRefactory.CSharp.Resolver;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp {
	[TestFixture]
	public class QueryExpressionExpanderTests {
		private dynamic ElementAt(dynamic d, int index) {
			int i = 0;
			foreach (var o in d) {
				if (i++ == index)
					return o;
			}
			throw new ArgumentException();
		}

		private void AssertCorrect(AstNode actual, string expected) {
			Assert.That(Regex.Replace(actual.GetText(), @"\s+", "").Replace("<>", ""), Is.EqualTo(Regex.Replace(expected, @"\s+", "")));
		}

		private void AssertLookupCorrect<T, U>(IEnumerable<KeyValuePair<T, U>> actual, IList<Tuple<TextLocation, AstNode>> expected) where T : AstNode where U : AstNode {
			var actualList = actual.OrderBy(x => x.Key.StartLocation).ThenBy(x => x.Key.GetType().ToString()).ToList();
			Assert.That(actualList.Select(x => x.Key.StartLocation).ToList(), Is.EqualTo(expected.Select(x => x.Item1).ToList()));
			for (int i = 0; i < actualList.Count; i++) {
				Assert.That(actualList[i].Value, Is.Not.SameAs(actualList[i].Key));
				Assert.That(actualList[i].Value, Is.SameAs(expected[i].Item2));
			}
		}

		[Test]
		public void QueryExpressionWithFromAndSelectWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from a in args select int.Parse(a)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "args.Select(a => int.Parse(a))");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] { Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)) });
			AssertLookupCorrect(actual.Expressions, new[] { Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target), Tuple.Create(new TextLocation(1, 16), actual.AstNode) });
		}

		[Test]
		public void QueryExpressionWithSingleFromAndExplicitTypeWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from object a in args select int.Parse(a)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "args.Cast<object>().Select(a => int.Parse(a))");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] { Tuple.Create(new TextLocation(1, 13), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)) });
			AssertLookupCorrect(actual.Expressions, new[] { Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target), Tuple.Create(new TextLocation(1, 23), actual.AstNode) });
		}

		[Test]
		public void QueryExpressionWithLetWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from a in args let b = int.Parse(a) select a + b.ToString()");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "args.Select(a => new { a, b = int.Parse(a) }).Select(x0 => x0.a + x0.b.ToString())");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 20), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 37), actual.AstNode),
			});
		}

		[Test]
		public void QueryExpressionWithTwoLetsWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from a in args let b = int.Parse(a) let c = b + 1 select a + b.ToString() + c.ToString()");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "args.Select(a => new { a, b = int.Parse(a) }).Select(x0 => new { x0, c = x0.b + 1 }).Select(x1 => x1.x0.a + x1.x0.b.ToString() + x1.c.ToString())");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 20), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
				Tuple.Create(new TextLocation(1, 41), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 37), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 51), actual.AstNode),
			});
		}

		[Test]
		public void TwoFromClausesFollowedBySelectWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 from j in arr2 select i + j");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.SelectMany(i => arr2, (i, j) => i + j)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Arguments, 1).Parameters, 1)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 16), actual.AstNode),
				Tuple.Create(new TextLocation(1, 31), actual.AstNode),
			});
		}

		[Test]
		public void SelectManyFollowedBySelectWorksWhenTheTargetIsTransparentAndTheCollectionsAreCorrelated() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in outer let j = F(i) from k in j.Result select i + j + k");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "outer.Select(i => new { i, j = F(i) }).SelectMany(x0 => x0.j.Result, (x1, k) => x1.i + x1.j + k)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
				Tuple.Create(new TextLocation(1, 35), (AstNode)ElementAt(ElementAt(astNode.Arguments, 1).Parameters, 1)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 17), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 30), actual.AstNode),
				Tuple.Create(new TextLocation(1, 49), actual.AstNode),
			});
		}

		[Test]
		public void SelectManyFollowedByLetWorksWhenTheTargetIsTransparentAndTheCollectionsAreCorrelated() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in outer let j = F(i) from k in j.Result let l = i + j + k select i + j + k + l");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "outer.Select(i => new { i, j = F(i) }).SelectMany(x0 => x0.j.Result, (x1, k) => new { x1, k }).Select(x2 => new { x2, l = x2.x1.i + x2.x1.j + x2.k }).Select(x3 => x3.x2.x1.i + x3.x2.x1.j + x3.x2.k + x3.l)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
				Tuple.Create(new TextLocation(1, 35), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 1).Parameters, 1)),
				Tuple.Create(new TextLocation(1, 53), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 17), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 30), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 49), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 67), (AstNode)astNode),
			});
		}

		[Test]
		public void TwoFromClausesFollowedByLetWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 from j in arr2 let k = i + j select i + j + k");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.SelectMany(i => arr2, (i, j) => new { i, j }).Select(x0 => new { x0, k = x0.i + x0.j }).Select(x1 => x1.x0.i + x1.x0.j + x1.k)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 1).Parameters, 1)),
				Tuple.Create(new TextLocation(1, 35), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 31), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 45), actual.AstNode),
			});
		}

		[Test]
		public void ThreeFromClausesFollowedBySelectWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 from j in arr2 from k in arr3 select i + j + k");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.SelectMany(i => arr2, (i, j) => new { i, j }).SelectMany(x0 => arr3, (x1, k) => x1.i + x1.j + k)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 1).Parameters, 1)),
				Tuple.Create(new TextLocation(1, 36), (AstNode)ElementAt(ElementAt(astNode.Arguments, 1).Parameters, 1)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 31), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 46), (AstNode)astNode),
			});
		}

		[Test]
		public void GroupByWithSimpleValue() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr group i by i.field");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr.GroupBy(i => i.field)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 15), (AstNode)astNode),
			});
		}

		[Test]
		public void GroupByWithProjectedValue() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr group i.something by i.field");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr.GroupBy(i => i.field, i => i.something)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 15), (AstNode)astNode),
			});
		}

		[Test]
		public void GroupByWhenThereIsATransparentIdentifer() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr let j = F(i) group i by i.field");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr.Select(i => new { i, j = F(i) }).GroupBy(x0 => x0.i.field, x1 => x1.i)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 19), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 15), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 28), (AstNode)astNode),
			});
		}

		[Test]
		public void JoinFollowedBySelect() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 join j in arr2 on i.keyi equals j.keyj select i + j");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Join(arr2, i => i.keyi, j => j.keyj, (i, j) => i + j)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 1).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Arguments, 2).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 55), (AstNode)astNode),
			});
		}

		[Test]
		public void JoinFollowedByLet() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 join j in arr2 on i.keyi equals j.keyj let k = i + j select i + j + k");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Join(arr2, i => i.keyi, j => j.keyj, (i, j) => new { i, j }).Select(x0 => new { x0, k = x0.i + x0.j }).Select(x1 => x1.x0.i + x1.x0.j + x1.k)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 1).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 2).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 59), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 55), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 69), (AstNode)astNode),
			});
		}

		[Test]
		public void JoinFollowedBySelectWhenThereIsATransparentIdentifier() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 let j = F(i) join k in arr2 on j.keyj equals k.keyk select i + j + k");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Select(i => new { i, j = F(i) }).Join(arr2, x0 => x0.j.keyj, k => k.keyk, (x1, k) => x1.i + x1.j + k)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 20), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
				Tuple.Create(new TextLocation(1, 34), (AstNode)ElementAt(ElementAt(astNode.Arguments, 2).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 29), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 68), (AstNode)astNode),
			});
		}

		[Test]
		public void GroupJoinFollowedBySelect() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 join j in arr2 on i.keyi equals j.keyj into g select F(i, g)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.GroupJoin(arr2, i => i.keyi, j => j.keyj, (i, g) => F(i, g))");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 1).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 60), (AstNode)ElementAt(ElementAt(astNode.Arguments, 3).Parameters, 1)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 62), (AstNode)astNode),
			});
		}

		[Test]
		public void GroupJoinFollowedByLet() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 join j in arr2 on i.keyi equals j.keyj into g let k = i + g select i + g + k");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.GroupJoin(arr2, i => i.keyi, j => j.keyj, (i, g) => new { i, g }).Select(x0 => new { x0, k = x0.i + x0.g }).Select(x1 => x1.x0.i + x1.x0.g + x1.k)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 1).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 60), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 3).Parameters, 1)),
				Tuple.Create(new TextLocation(1, 66), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 62), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 76), (AstNode)astNode),
			});
		}

		[Test]
		public void GroupJoinFollowedBySelectWhenThereIsATransparentIdentifier() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 let j = F(i) join k in arr2 on j.keyj equals k.keyk into g select F(i, j, g)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Select(i => new { i, j = F(i) }).GroupJoin(arr2, x0 => x0.j.keyj, k => k.keyk, (x1, g) => F(x1.i, x1.j, g))");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 20), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
				Tuple.Create(new TextLocation(1, 73), (AstNode)ElementAt(ElementAt(astNode.Arguments, 3).Parameters, 1)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 29), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 75), (AstNode)astNode),
			});
		}

		[Test]
		public void WhereWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 where i > 5 select i + 1");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Where(i => i > 5).Select(i => i + 1)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 22), (AstNode)astNode.Target.Target),	// This should be the where at location 16, but a parser bug causes 22 to be returned. change this to 16 after fixing the parser bug.
				Tuple.Create(new TextLocation(1, 28), (AstNode)astNode),
			});
		}

		[Test]
		public void WhereWorksWhenThereIsATransparentIdentifier() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 let j = i + 1 where i > j select i + j");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Select(i => new { i, j = i + 1 }).Where(x0 => x0.i > x0.j).Select(x1 => x1.i + x1.j)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 20), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 36), (AstNode)astNode.Target.Target),	// This should be the orderby at location 30, but a parser bug causes 36 to be returned. change this to 30 after fixing the parser bug.
				Tuple.Create(new TextLocation(1, 42), (AstNode)astNode),
			});
		}

		[Test]
		public void TrivialSelectIsEliminatedAfterWhere() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 where i > 5 select i");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Where(i => i > 5)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 22), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 28), (AstNode)astNode),
			});
		}

		[Test]
		public void TrivialSelectIsEliminatedAfterWhereEvenWhenParenthesized() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 where i > 5 select (i)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Where(i => i > 5)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 22), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 28), (AstNode)astNode),
			});
		}

		[Test]
		public void TrivialSelectIsNotEliminatingWhenTheOnlyOperation() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 select i");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Select(i => i)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode),
			});
		}

		[Test]
		public void OrderingWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 orderby i.field1 select i");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.OrderBy(i => i.field1)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 24), (AstNode)astNode),	// This should be the orderby at location 16, but a parser bug causes 24 to be returned. change this to 16 after fixing the parser bug.
				Tuple.Create(new TextLocation(1, 24), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 33), (AstNode)astNode),
			});
		}

		[Test]
		public void OrderingWorksWhenThereIsATransparentIdentifier() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 let j = i + 1 orderby i + j select i");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.Select(i => new { i, j = i + 1 }).OrderBy(x0 => x0.i + x0.j).Select(x1 => x1.i)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 20), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Body.Initializers, 1).NameToken),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 38), (AstNode)astNode.Target.Target),	// This should be the orderby at location 30, but a parser bug causes 38 to be returned. change this to 30 after fixing the parser bug.
				Tuple.Create(new TextLocation(1, 38), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 44), (AstNode)astNode),
			});
		}

		[Test]
		public void ThenByWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 orderby i.field1, i.field2 select i");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.OrderBy(i => i.field1).ThenBy(i => i.field2)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 24), (AstNode)astNode),	// This should be the orderby at location 16, but a parser bug causes 24 to be returned. change this to 16 after fixing the parser bug.
				Tuple.Create(new TextLocation(1, 24), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 34), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 43), (AstNode)astNode),
			});
		}

		[Test]
		public void OrderingDescendingWorks() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 orderby i.field1 descending, i.field2 descending select i");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);

			AssertCorrect(actual.AstNode, "arr1.OrderByDescending(i => i.field1).ThenByDescending(i => i.field2)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 24), (AstNode)astNode),	// This should be the orderby at location 16, but a parser bug causes 24 to be returned. change this to 16 after fixing the parser bug.
				Tuple.Create(new TextLocation(1, 24), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 45), (AstNode)astNode),
				Tuple.Create(new TextLocation(1, 65), (AstNode)astNode),
			});
		}

		[Test]
		public void QueryContinuation() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 from j in arr2 select i + j into a where a > 5 select a + 1");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.SelectMany(i => arr2, (i, j) => i + j).Where(a => a > 5).Select(a => a + 1)");
			dynamic astNode = actual.AstNode;
			AssertLookupCorrect(actual.RangeVariables, new[] {
				Tuple.Create(new TextLocation(1, 6), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 0).Parameters, 0)),
				Tuple.Create(new TextLocation(1, 21), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Target.Target.Arguments, 1).Parameters, 1)),
				Tuple.Create(new TextLocation(1, 49), (AstNode)ElementAt(ElementAt(astNode.Target.Target.Arguments, 0).Parameters, 0)),
			});
			AssertLookupCorrect(actual.Expressions, new[] {
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 1), (AstNode)astNode.Target.Target.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 16), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 31), (AstNode)astNode.Target.Target.Target.Target),
				Tuple.Create(new TextLocation(1, 57), (AstNode)astNode.Target.Target),
				Tuple.Create(new TextLocation(1, 63), (AstNode)astNode),
			});
		}

		[Test]
		public void NestedQueries() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 from j in arr2 let l = new { i, j } group l by l.i into g select new { g.Key, a = from q in g select new { q.i, q.j } }");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.SelectMany(i => arr2, (i, j) => new { i, j }).Select(x0 => new { x0, l = new { x0.i, x0.j } }).GroupBy(x1 => x1.l.i, x2 => x2.l).Select(g => new { g.Key, a = g.Select(q => new { q.i, q.j }) })");
		}

		[Test]
		public void NestedQueryUsingRangeVariableFromOuter() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from i in arr1 from j in arr2 let k = new[] { i, j } select (from l in k let m = l + 1 select l + m + i)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "arr1.SelectMany(i => arr2, (i, j) => new { i, j }).Select(x0 => new { x0, k = new[] { x0.i, x0.j } }).Select(x1 => (x1.k.Select(l => new { l, m = l + 1 }).Select(x2 => x2.l + x2.m + x1.x0.i)))");
		}

		[Test]
		public void RangeVariablesAreNotInScopeInJoinEquals() {
			var node = ParseUtilCSharp.ParseExpression<QueryExpression>("from a in args let a2 = a select (from b in args let b2 = b join c in args on b[0] equals b + a into g select g)");
			var actual = new QueryExpressionExpander().ExpandQueryExpressions(node);
			AssertCorrect(actual.AstNode, "args.Select(a => new { a, a2 = a }).Select(x0 => (args.Select(b => new { b, b2 = b }).GroupJoin(args, x1 => x1.b[0], c => b + x0.a, (x2, g) => g)))");
		}
	}
}
