// 
// Severity.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory.Refactoring
{
	/// <summary>
	/// The severity influences how the task bar reacts on found issues.
	/// </summary>
	public enum Severity
	{
		/// <summary>
		/// None means that the task bar doesn't show the issue.
		/// </summary>
		None,

		/// <summary>
		/// Errors are shown in red and that the task bar is in error state if 1 error is found.
		/// </summary>
		Error,

		/// <summary>
		/// Warnings are shown in yellow and set the task bar to warning state (if no error is found).
		/// </summary>
		Warning,

		/// <summary>
		/// Suggestions are shown in green and doesn't influence the task bar state
		/// </summary>
		Suggestion,

		/// <summary>
		/// Hints are shown in blue and doesn't influence the task bar state
		/// </summary>
		Hint
	}
}

