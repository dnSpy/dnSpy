// 
// TextEditorOptions.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// The text editor options class holds basic information about the text editor settings that influences code generation and formatting beside
	/// the CSharpFormattingOptions.
	/// </summary>
	public class TextEditorOptions
	{
		public static readonly TextEditorOptions Default = new TextEditorOptions ();

		/// <summary>
		/// Gets or sets a value indicating if tabs need to be replaced by spaces. If that is true, all indenting will be done with spaces only,
		/// otherwise the indenting will start with tabs.
		/// </summary>
		public bool TabsToSpaces {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the size of the tab chacter as spaces.
		/// </summary>
		public int TabSize {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the size of a single indent as spaces.
		/// </summary>
		public int IndentSize {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the continuation indent. A continuation indent is the indent that will be put after an embedded statement that is no block.
		/// </summary>
		public int ContinuationIndent {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the label indent. A label indent is the indent that will be put before an label.
		/// (Note: it may be negative -IndentSize would cause that labels are unindented)
		/// </summary>
		public int LabelIndent {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the eol marker.
		/// </summary>
		public string EolMarker {
			get;
			set;
		}

		/// <summary>
		/// If true blank lines will be indented up to the indent level, otherwise blank lines will have the length 0.
		/// </summary>
		public bool IndentBlankLines {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the length of the desired line length. The formatting engine will wrap at wrap points set to Wrapping.WrapIfTooLong if the line length is too long.
		/// 0 means do not wrap.
		/// </summary>
		public int WrapLineLength {
			get;
			set;
		}

		public TextEditorOptions()
		{
			TabsToSpaces = false;
			TabSize = 4;
			IndentSize = 4;
			ContinuationIndent = 4;
			WrapLineLength = 0;
			EolMarker = Environment.NewLine;
		}
	}
}
