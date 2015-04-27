// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	class InsertMissingTokensDecorator : DecoratingTokenWriter
	{
		readonly Stack<List<AstNode>> nodes = new Stack<List<AstNode>>();
		List<AstNode> currentList;
		readonly ILocatable locationProvider;
		
		public InsertMissingTokensDecorator(TokenWriter writer, ILocatable locationProvider)
			: base(writer)
		{
			this.locationProvider = locationProvider;
			currentList = new List<AstNode>();
		}
		
		public override void StartNode(AstNode node)
		{
			currentList.Add(node);
			nodes.Push(currentList);
			currentList = new List<AstNode>();
			base.StartNode(node);
		}
		
		public override void EndNode(AstNode node)
		{
			System.Diagnostics.Debug.Assert(currentList != null);
			foreach (var removable in node.Children.Where(n => n is CSharpTokenNode)) {
				removable.Remove();
			}
			foreach (var child in currentList) {
				System.Diagnostics.Debug.Assert(child.Parent == null || node == child.Parent);
				child.Remove();
				node.AddChildWithExistingRole(child);
			}
			currentList = nodes.Pop();
			base.EndNode(node);
		}
		
		public override void WriteToken(Role role, string token)
		{
			CSharpTokenNode t = new CSharpTokenNode(locationProvider.Location, (TokenRole)role);
			t.Role = role;
			EmptyStatement node = nodes.Peek().LastOrDefault() as EmptyStatement;
			if (node == null)
				currentList.Add(t);
			else {
				node.Location = locationProvider.Location;
			}
			base.WriteToken(role, token);
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			TextLocation start = locationProvider.Location;
			CSharpTokenNode t = null;
			if (role is TokenRole)
				t = new CSharpTokenNode(start, (TokenRole)role);
			else if (role == EntityDeclaration.ModifierRole)
				t = new CSharpModifierToken(start, CSharpModifierToken.GetModifierValue(keyword));
			else if (keyword == "this") {
				ThisReferenceExpression node = nodes.Peek().LastOrDefault() as ThisReferenceExpression;
				if (node != null)
					node.Location = start;
			} else if (keyword == "base") {
				BaseReferenceExpression node = nodes.Peek().LastOrDefault() as BaseReferenceExpression;
				if (node != null)
					node.Location = start;
			}
			if (t != null) currentList.Add(t);
			base.WriteKeyword(role, keyword);
		}
		
		public override void WriteIdentifier(Identifier identifier)
		{
			if (!identifier.IsNull)
				identifier.SetStartLocation(locationProvider.Location);
			currentList.Add(identifier);
			base.WriteIdentifier(identifier);
		}
		
		public override void WritePrimitiveValue(object value, string literalValue = null)
		{
			Expression node = nodes.Peek().LastOrDefault() as Expression;
			if (node is PrimitiveExpression) {
				((PrimitiveExpression)node).SetStartLocation(locationProvider.Location);
			}
			if (node is NullReferenceExpression) {
				((NullReferenceExpression)node).SetStartLocation(locationProvider.Location);
			}
			base.WritePrimitiveValue(value, literalValue);
		}
		
		public override void WritePrimitiveType(string type)
		{
			PrimitiveType node = nodes.Peek().LastOrDefault() as PrimitiveType;
			if (node != null)
				node.SetStartLocation(locationProvider.Location);
			base.WritePrimitiveType(type);
		}
	}
}
