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

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		/// <param name='description'>
		/// The desription of the issue.
		/// </param>
		/// <param name='start'>
		/// The issue start location.
		/// </param>
		/// <param name='end'>
		/// the issue end location.
		/// </param>
		/// <param name='actions'>
		/// A list of potential solutions for the issue.
		/// </param>
		public CodeIssue(string description, TextLocation start, TextLocation end, IEnumerable<CodeAction> actions = null)
		{
			Description = description;
			Start = start;
			End = end;
			if (actions != null)
				Actions = actions.ToArray();
			else
				Actions = EmptyList<CodeAction>.Instance;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue"/> class.
		/// </summary>
		/// <param name='description'>
		/// The desription of the issue.
		/// </param>
		/// <param name='start'>
		/// The issue start location.
		/// </param>
		/// <param name='end'>
		/// the issue end location.
		/// </param>
		/// <param name='action'>
		/// A potential solution for the issue.
		/// </param>
		public CodeIssue(string description, TextLocation start, TextLocation end, CodeAction action) : this (description, start, end, action != null ? new [] { action } : null)
		{
		}
	}
}

