// 
// Script.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class Script : IDisposable
	{
		public RefactoringContext Context {
			get;
			private set;
		}

		protected readonly List<Action> changes = new List<Action> ();

		public IEnumerable<Action> Actions {
			get {
				return changes;
			}
		}
		
		public Script (RefactoringContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			this.Context = context;
		}
		
		public void Queue (Action change)
		{
			changes.Add (change);
		}
		
		public void InsertText (int offset, string text)
		{
			Queue (Context.CreateTextReplaceAction (offset, 0, text));
		}
		
		public void InsertBefore (AstNode node, AstNode insertNode)
		{
			var startOffset = Context.GetOffset (node.StartLocation.Line, 1);
			var output = OutputNode (GetIndentLevelAt (startOffset), insertNode);
			
			if (!(insertNode is Expression || insertNode is AstType))
				output.Text += Context.EolMarker;
			
			Queue (Context.CreateNodeOutputAction (startOffset, 0, output));
		}

		public void AddTo (BlockStatement bodyStatement, AstNode insertNode)
		{
			var startOffset = Context.GetOffset (bodyStatement.LBraceToken.StartLocation) + 1;
			var output = OutputNode (GetIndentLevelAt (startOffset), insertNode, true);
			Queue (Context.CreateNodeOutputAction (startOffset, 0, output));
		}
		
		public void Link (params AstNode[] nodes)
		{
			Queue (Context.CreateLinkAction (nodes));
		}
		
		public void Link (IEnumerable<AstNode> nodes)
		{
			Queue (Context.CreateLinkAction (nodes));
		}
		
		public void Remove (AstNode node)
		{
			var startOffset = Context.GetOffset (node.StartLocation);
			var endOffset = Context.GetOffset (node.EndLocation);
			Remove (startOffset, endOffset - startOffset);
		}
		
		void Remove (int offset, int length)
		{
			Queue (Context.CreateTextReplaceAction (offset, length, null));
		}
		
		void Replace (int offset, int length, string text)
		{
			Queue (Context.CreateTextReplaceAction (offset, length, text));
		}
		
		public void Replace (AstNode node, AstNode replaceWith)
		{
			var startOffset = Context.GetOffset (node.StartLocation);
			var endOffset = Context.GetOffset (node.EndLocation);
			int level = 0;
			if (!(replaceWith is Expression) && !(replaceWith is AstType))
				level = GetIndentLevelAt (startOffset);
			NodeOutput output = OutputNode (level, replaceWith);
			output.Trim ();
			Queue (Context.CreateNodeOutputAction (startOffset, endOffset - startOffset, output));
		}

		public void FormatText (Func<RefactoringContext, AstNode> callback)
		{
			Queue (Context.CreateFormatTextAction (callback));
		}
		
		public void Select (AstNode node)
		{
			Queue (Context.CreateNodeSelectionAction (node));
		}
		
		public enum InsertPosition {
			Start,
			Before,
			After,
			End
		}
		
		public abstract void InsertWithCursor (string operation, AstNode node, InsertPosition defaultPosition);

		protected int GetIndentLevelAt (int offset)
		{
			var node = Context.Unit.GetNodeAt (Context.GetLocation (offset));
			int level = 0;
			while (node != null) {
					if (node is BlockStatement || node is TypeDeclaration || node is NamespaceDeclaration)
						level++;
					node = node.Parent;
				}
			return level;
		}
		
		protected NodeOutput OutputNode (int indentLevel, AstNode node, bool startWithNewLine = false)
		{
			var result = new NodeOutput ();
			var stringWriter = new System.IO.StringWriter ();
			var formatter = new TextWriterOutputFormatter (stringWriter);
			formatter.Indentation = indentLevel;
			stringWriter.NewLine = Context.EolMarker;
			if (startWithNewLine)
				formatter.NewLine ();
			var visitor = new OutputVisitor (formatter, Context.FormattingOptions);
			visitor.OutputStarted += (sender, e) => {
				result.NodeSegments [e.AstNode] = new NodeOutput.Segment (stringWriter.GetStringBuilder ().Length);
			};
			visitor.OutputFinished += (sender, e) => {
				result.NodeSegments [e.AstNode].EndOffset = stringWriter.GetStringBuilder ().Length;
			};
			node.AcceptVisitor (visitor, null);
			result.Text = stringWriter.ToString ().TrimEnd ();
			if (node is FieldDeclaration)
				result.Text += Context.EolMarker;

			return result;
		}

		#region IDisposable implementation
		public abstract void Dispose ();
		#endregion
	}
}

