// 
// InspectionIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// A code issue marks a region of text with an issue and can provide solution actions for this issue.
	/// </summary>
	public class CodeIssue
	{
		/// <summary>
		/// Gets the description of the issue.
		/// </summary>
		public string Description {
			get;
			private set;
		}

		/// <summary>
		/// Gets the issue start location.
		/// </summary>
		public TextLocation Start {
			get;
			private set;
		}
		
		/// <summary>
		/// Gets the issue end location.
		/// </summary>
		public TextLocation End {
			get;
			private set;
		}

		/// <summary>
		/// Gets a list of potential solutions for the issue.
		/// </summary>
		public IList<CodeAction> Actions {
			get;
			private set;
		}

		List<Type> actionProvider = new List<Type>();
		public List<Type> ActionProvider {
			get {
				return actionProvider;
			}
			set {
				actionProvider = value;
			}
		}

		/// <summary>
		/// Gets or sets the Issue marker which should be used to mark this issue in the editor.
		/// It's up to the editor implementation if and how this info is used.
		/// </summary>
		public IssueMarker IssueMarker {
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(TextLocation start, TextLocation end, string issueDescription)
		{
			if (issueDescription == null)
				throw new ArgumentNullException("issueDescription");
			Description = issueDescription;
			Start = start;
			End = end;
			Actions = EmptyList<CodeAction>.Instance;
			IssueMarker = IssueMarker.WavedLine;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(TextLocation start, TextLocation end, string issueDescription, IEnumerable<CodeAction> actions) : this(start, end, issueDescription)
		{
			if (actions != null)
				Actions = actions.ToArray();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(TextLocation start, TextLocation end, string issueDescription, params CodeAction[] actions) : this(start, end, issueDescription)
		{
			if (actions != null)
				Actions = actions;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(TextLocation start, TextLocation end, string issueDescription, string actionDescription, Action<Script> fix) : this(start, end, issueDescription)
		{
			if (actionDescription == null)
				throw new ArgumentNullException("actionDescription");
			if (fix == null)
				throw new ArgumentNullException("fix");
			this.Actions = new [] { new CodeAction(actionDescription, fix, start, end) };
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(AstNode node, string issueDescription)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (issueDescription == null)
				throw new ArgumentNullException("issueDescription");
			Description = issueDescription;
			Start = node.StartLocation;
			End = node.EndLocation;
			Actions = EmptyList<CodeAction>.Instance;
			IssueMarker = IssueMarker.WavedLine;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(AstNode node, string issueDescription, IEnumerable<CodeAction> actions) : this(node, issueDescription)
		{
			if (actions != null)
				Actions = actions.ToArray();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(AstNode node, string issueDescription, params CodeAction[] actions) : this(node, issueDescription)
		{
			if (actions != null)
				Actions = actions;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		public CodeIssue(AstNode node, string issueDescription, string actionDescription, Action<Script> fix) : this(node, issueDescription)
		{
			if (actionDescription == null)
				throw new ArgumentNullException("actionDescription");
			if (fix == null)
				throw new ArgumentNullException("fix");
			this.Actions = new [] { new CodeAction(actionDescription, fix, node) };
		}

	}
}

