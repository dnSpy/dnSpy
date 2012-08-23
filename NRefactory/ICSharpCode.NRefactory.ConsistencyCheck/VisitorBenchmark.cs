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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	/// <summary>
	/// Determines the fastest way to retrieve a List{IdentifierExpression} of all identifiers
	/// in a syntax tree.
	/// </summary>
	public class VisitorBenchmark
	{
		public static void Run(IEnumerable<SyntaxTree> files)
		{
			files = files.ToList();
			
			RunTest("recursive method using for", files, (syntaxTree, list) => WalkTreeFor(syntaxTree, list));
			RunTest("recursive method using foreach", files, (syntaxTree, list) => WalkTreeForEach(syntaxTree, list));
			RunTest("non-recursive loop", files, (syntaxTree, list) => WalkTreeNonRecursive(syntaxTree, list));
			RunTest("foreach over Descendants.OfType()", files, (syntaxTree, list) => {
			        	foreach (var node in syntaxTree.Descendants.OfType<IdentifierExpression>()) {
			        		list.Add(node);
			        	}
			        });
			RunTest("DepthFirstAstVisitor", files, (syntaxTree, list) => syntaxTree.AcceptVisitor(new DepthFirst(list)));
			RunTest("DepthFirstAstVisitor<object>", files, (syntaxTree, list) => syntaxTree.AcceptVisitor(new DepthFirst<object>(list)));
			RunTest("DepthFirstAstVisitor<object, object>", files, (syntaxTree, list) => syntaxTree.AcceptVisitor(new DepthFirst<object, object>(list), null));
			RunTest("ObservableAstVisitor", files, (syntaxTree, list) => {
			        	var visitor = new ObservableAstVisitor();
			        	visitor.EnterIdentifierExpression += list.Add;
			        	syntaxTree.AcceptVisitor(visitor);
			        });
		}
		
		static void WalkTreeForEach(AstNode node, List<IdentifierExpression> list)
		{
			foreach (AstNode child in node.Children) {
				IdentifierExpression id = child as IdentifierExpression;
				if (id != null) {
					list.Add(id);
				}
				WalkTreeForEach(child, list);
			}
		}
		
		static void WalkTreeFor(AstNode node, List<IdentifierExpression> list)
		{
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				IdentifierExpression id = child as IdentifierExpression;
				if (id != null) {
					list.Add(id);
				}
				WalkTreeFor(child, list);
			}
		}
		
		static void WalkTreeNonRecursive(AstNode root, List<IdentifierExpression> list)
		{
			AstNode pos = root;
			while (pos != null) {
				{
					IdentifierExpression id = pos as IdentifierExpression;
					if (id != null) {
						list.Add(id);
					}
				}
				if (pos.FirstChild != null) {
					pos = pos.FirstChild;
				} else {
					pos = pos.GetNextNode();
				}
			}
		}
		
		class DepthFirst : DepthFirstAstVisitor {
			readonly List<IdentifierExpression> list;
			public DepthFirst(List<IdentifierExpression> list) { this.list = list; }
			public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				list.Add(identifierExpression);
				base.VisitIdentifierExpression(identifierExpression);
			}
		}
		class DepthFirst<T> : DepthFirstAstVisitor<T> {
			readonly List<IdentifierExpression> list;
			public DepthFirst(List<IdentifierExpression> list) { this.list = list; }
			public override T VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				list.Add(identifierExpression);
				return base.VisitIdentifierExpression(identifierExpression);
			}
		}
		class DepthFirst<T, S> : DepthFirstAstVisitor<T, S> {
			readonly List<IdentifierExpression> list;
			public DepthFirst(List<IdentifierExpression> list) { this.list = list; }
			public override S VisitIdentifierExpression(IdentifierExpression identifierExpression, T data)
			{
				list.Add(identifierExpression);
				return base.VisitIdentifierExpression(identifierExpression, data);
			}
		}
		
		static void RunTest(string text, IEnumerable<SyntaxTree> files, Action<SyntaxTree, List<IdentifierExpression>> action)
		{
			// validation:
			var list = new List<IdentifierExpression>();
			foreach (var file in files) {
				list.Clear();
				action(file, list);
				if (!list.SequenceEqual(file.Descendants.OfType<IdentifierExpression>()))
					throw new InvalidOperationException();
			}
			Stopwatch w = Stopwatch.StartNew();
			foreach (var file in files) {
				for (int i = 0; i < 20; i++) {
					list.Clear();
					action(file, list);
				}
			}
			w.Stop();
			Console.WriteLine(text.PadRight(40) + ": " + w.Elapsed);
		}
	}
}
