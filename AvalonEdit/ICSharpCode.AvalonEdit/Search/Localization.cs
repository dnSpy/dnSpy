// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.ComponentModel;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Holds default texts for buttons and labels in the SearchPanel. Override properties to add other languages.
	/// </summary>
	public class Localization
	{
		/// <summary>
		/// Default: 'Match case'
		/// </summary>
		public virtual string MatchCaseText {
			get { return "Match case"; }
		}
		
		/// <summary>
		/// Default: 'Match whole words'
		/// </summary>
		public virtual string MatchWholeWordsText {
			get { return "Match whole words"; }
		}
		
		
		/// <summary>
		/// Default: 'Use regular expressions'
		/// </summary>
		public virtual string UseRegexText {
			get { return "Use regular expressions"; }
		}
		
		/// <summary>
		/// Default: 'Find next (F3)'
		/// </summary>
		public virtual string FindNextText {
			get { return "Find next (F3)"; }
		}
		
		/// <summary>
		/// Default: 'Find previous (Shift+F3)'
		/// </summary>
		public virtual string FindPreviousText {
			get { return "Find previous (Shift+F3)"; }
		}
		
		/// <summary>
		/// Default: 'Error: '
		/// </summary>
		public virtual string ErrorText {
			get { return "Error: "; }
		}
		
		/// <summary>
		/// Default: 'No matches found!'
		/// </summary>
		public virtual string NoMatchesFoundText {
			get { return "No matches found!"; }
		}
	}
}
