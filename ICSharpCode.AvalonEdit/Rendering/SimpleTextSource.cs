// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.AvalonEdit.Rendering
{
	sealed class SimpleTextSource : TextSource
	{
		readonly string text;
		readonly TextRunProperties properties;
		
		public SimpleTextSource(string text, TextRunProperties properties)
		{
			this.text = text;
			this.properties = properties;
		}
		
		public override TextRun GetTextRun(int textSourceCharacterIndex)
		{
			if (textSourceCharacterIndex < text.Length)
				return new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, properties);
			else
				return new TextEndOfParagraph(1);
		}
		
		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
		{
			throw new NotImplementedException();
		}
		
		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
		{
			throw new NotImplementedException();
		}
	}
}
