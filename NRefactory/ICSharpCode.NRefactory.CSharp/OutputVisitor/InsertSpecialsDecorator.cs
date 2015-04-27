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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp
{
	class InsertSpecialsDecorator : DecoratingTokenWriter
	{
		readonly Stack<AstNode> positionStack = new Stack<AstNode>();
		int visitorWroteNewLine = 0;
		
		public InsertSpecialsDecorator(TokenWriter writer) : base(writer)
		{
		}
		
		public override void StartNode(AstNode node)
		{
			if (positionStack.Count > 0) {
				WriteSpecialsUpToNode(node);
			}
			positionStack.Push(node.FirstChild);
			base.StartNode(node);
		}
		
		public override void EndNode(AstNode node)
		{
			base.EndNode(node);
			AstNode pos = positionStack.Pop();
			Debug.Assert(pos == null || pos.Parent == node);
			WriteSpecials(pos, null);
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			if (role != null) {
				WriteSpecialsUpToRole(role);
			}
			base.WriteKeyword(role, keyword);
		}
		
		public override void WriteIdentifier(Identifier identifier)
		{
			WriteSpecialsUpToRole(identifier.Role ?? Roles.Identifier);
			base.WriteIdentifier(identifier);
		}
		
		public override void WriteToken(Role role, string token)
		{
			WriteSpecialsUpToRole(role);
			base.WriteToken(role, token);
		}
		
		public override void NewLine()
		{
			if (visitorWroteNewLine >= 0)
				base.NewLine();
			visitorWroteNewLine++;
		}
		
		#region WriteSpecials
		/// <summary>
		/// Writes all specials from start to end (exclusive). Does not touch the positionStack.
		/// </summary>
		void WriteSpecials(AstNode start, AstNode end)
		{
			for (AstNode pos = start; pos != end; pos = pos.NextSibling) {
				if (pos.Role == Roles.Comment) {
					var node = (Comment)pos;
					base.WriteComment(node.CommentType, node.Content);
				}
				// see CSharpOutputVisitor.VisitNewLine()
				//				if (pos.Role == Roles.NewLine) {
				//					if (visitorWroteNewLine <= 0)
				//						base.NewLine();
				//					visitorWroteNewLine--;
				//				}
				if (pos.Role == Roles.PreProcessorDirective) {
					var node = (PreProcessorDirective)pos;
					base.WritePreProcessorDirective(node.Type, node.Argument);
				}
			}
		}
		
		/// <summary>
		/// Writes all specials between the current position (in the positionStack) and the next
		/// node with the specified role. Advances the current position.
		/// </summary>
		void WriteSpecialsUpToRole(Role role)
		{
			WriteSpecialsUpToRole(role, null);
		}
		
		void WriteSpecialsUpToRole(Role role, AstNode nextNode)
		{
			if (positionStack.Count == 0) {
				return;
			}
			// Look for the role between the current position and the nextNode.
			for (AstNode pos = positionStack.Peek(); pos != null && pos != nextNode; pos = pos.NextSibling) {
				if (pos.Role == role) {
					WriteSpecials(positionStack.Pop(), pos);
					// Push the next sibling because the node matching the role is not a special,
					// and should be considered to be already handled.
					positionStack.Push(pos.NextSibling);
					// This is necessary for OptionalComma() to work correctly.
					break;
				}
			}
		}
		
		/// <summary>
		/// Writes all specials between the current position (in the positionStack) and the specified node.
		/// Advances the current position.
		/// </summary>
		void WriteSpecialsUpToNode(AstNode node)
		{
			if (positionStack.Count == 0) {
				return;
			}
			for (AstNode pos = positionStack.Peek(); pos != null; pos = pos.NextSibling) {
				if (pos == node) {
					WriteSpecials(positionStack.Pop(), pos);
					// Push the next sibling because the node itself is not a special,
					// and should be considered to be already handled.
					positionStack.Push(pos.NextSibling);
					// This is necessary for OptionalComma() to work correctly.
					break;
				}
			}
		}
		#endregion
	}
}




