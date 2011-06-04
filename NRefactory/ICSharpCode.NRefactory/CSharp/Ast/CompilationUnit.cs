// 
// CompilationUnit.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp
{
	public class CompilationUnit : AstNode 
	{
		public static readonly Role<AstNode> MemberRole = new Role<AstNode>("Member", AstNode.Null);
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public CompilationUnit ()
		{
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CompilationUnit o = other as CompilationUnit;
			return o != null && GetChildrenByRole(MemberRole).DoMatch(o.GetChildrenByRole(MemberRole), match);
		}
		
		public AstNode GetNodeAt (int line, int column)
		{
			return GetNodeAt (new AstLocation (line, column));
		}
		
		public AstNode GetNodeAt (AstLocation location)
		{
			AstNode node = this;
			while (node.FirstChild != null) {
				var child = node.FirstChild;
				while (child != null) {
					if (child.StartLocation <= location && location < child.EndLocation) {
						node = child;
						break;
					}
					child = child.NextSibling;
				}
				// found no better child node - therefore the parent is the right one.
				if (child == null)
					break;
			}
			return node;
		}
		
		public IEnumerable<AstNode> GetNodesBetween (int startLine, int startColumn, int endLine, int endColumn)
		{
			return GetNodesBetween (new AstLocation (startLine, startColumn), new AstLocation (endLine, endColumn));
		}
		
		public IEnumerable<AstNode> GetNodesBetween (AstLocation start, AstLocation end)
		{
			AstNode node = this;
			while (node != null) {
				AstNode next;
				if (start <= node.StartLocation && node.EndLocation <= end) {
					// Remember next before yielding node.
					// This allows iteration to continue when the caller removes/replaces the node.
					next = node.NextSibling;
					yield return node;
				} else {
					if (node.EndLocation < start) {
						next = node.NextSibling; 
					} else {
						next = node.FirstChild;
					}
				}
				
				if (next != null && next.StartLocation > end)
					yield break;
				node = next;
			}
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCompilationUnit (this, data);
		}
	}
}

