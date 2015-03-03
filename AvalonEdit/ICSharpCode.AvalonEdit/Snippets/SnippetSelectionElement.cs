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
using System.Text;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// Inserts the previously selected text at the selection marker.
	/// </summary>
	[Serializable]
	public class SnippetSelectionElement : SnippetElement
	{
		/// <summary>
		/// Gets/Sets the new indentation of the selected text.
		/// </summary>
		public int Indentation { get; set; }
		
		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			StringBuilder tabString = new StringBuilder();
			
			for (int i = 0; i < Indentation; i++) {
				tabString.Append(context.Tab);
			}
			
			string indent = tabString.ToString();
			
			string text = context.SelectedText.TrimStart(' ', '\t');
			
			text = text.Replace(context.LineTerminator,
			                             context.LineTerminator + indent);
			
			context.Document.Insert(context.InsertionPosition, text);
			context.InsertionPosition += text.Length;
			
			if (string.IsNullOrEmpty(context.SelectedText))
				SnippetCaretElement.SetCaret(context);
		}
	}
}
