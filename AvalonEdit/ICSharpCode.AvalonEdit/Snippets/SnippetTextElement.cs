// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// Represents a text element in a snippet.
	/// </summary>
	[Serializable]
	public class SnippetTextElement : SnippetElement
	{
		string text;
		
		/// <summary>
		/// The text to be inserted.
		/// </summary>
		public string Text {
			get { return text; }
			set { text = value; }
		}
		
		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			if (text != null)
				context.InsertText(text);
		}
		
		/// <inheritdoc/>
		public override Inline ToTextRun()
		{
			return new Run(text ?? string.Empty);
		}
	}
}
