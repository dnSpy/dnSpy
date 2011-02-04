// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	sealed class TextViewCachedElements : IDisposable
	{
		TextFormatter formatter;
		Dictionary<string, TextLine> nonPrintableCharacterTexts;
		
		public TextLine GetTextForNonPrintableCharacter(string text, ITextRunConstructionContext context)
		{
			if (nonPrintableCharacterTexts == null)
				nonPrintableCharacterTexts = new Dictionary<string, TextLine>();
			TextLine textLine;
			if (!nonPrintableCharacterTexts.TryGetValue(text, out textLine)) {
				var p = new VisualLineElementTextRunProperties(context.GlobalTextRunProperties);
				p.SetForegroundBrush(context.TextView.NonPrintableCharacterBrush);
				if (formatter == null)
					formatter = TextFormatterFactory.Create(context.TextView);
				textLine = FormattedTextElement.PrepareText(formatter, text, p);
				nonPrintableCharacterTexts[text] = textLine;
			}
			return textLine;
		}
		
		public void Dispose()
		{
			if (nonPrintableCharacterTexts != null) {
				foreach (TextLine line in nonPrintableCharacterTexts.Values)
					line.Dispose();
			}
			if (formatter != null)
				formatter.Dispose();
		}
	}
}
