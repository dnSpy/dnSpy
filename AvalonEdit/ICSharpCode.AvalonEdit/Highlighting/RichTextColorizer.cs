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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A colorizer that applies the highlighting from a <see cref="RichTextModel"/> to the editor.
	/// </summary>
	public class RichTextColorizer : DocumentColorizingTransformer
	{
		readonly RichTextModel richTextModel;
		
		/// <summary>
		/// Creates a new RichTextColorizer instance.
		/// </summary>
		public RichTextColorizer(RichTextModel richTextModel)
		{
			if (richTextModel == null)
				throw new ArgumentNullException("richTextModel");
			this.richTextModel = richTextModel;
		}
		
		/// <inheritdoc/>
		protected override void ColorizeLine(DocumentLine line)
		{
			var sections = richTextModel.GetHighlightedSections(line.Offset, line.Length);
			foreach (HighlightedSection section in sections) {
				if (HighlightingColorizer.IsEmptyColor(section.Color))
					continue;
				ChangeLinePart(section.Offset, section.Offset + section.Length,
				               visualLineElement => HighlightingColorizer.ApplyColorToElement(visualLineElement, section.Color, CurrentContext));
			}
		}
	}
}
