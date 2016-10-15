/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Themes;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Controls {
	sealed class TextBlockColorOutput : ITextColorWriter {
		readonly CachedTextColorsCollection cachedTextColorsCollection;
		readonly StringBuilder sb;

		public bool IsEmpty => sb.Length == 0;
		public string Text => sb.ToString();

		public TextBlockColorOutput() {
			this.cachedTextColorsCollection = new CachedTextColorsCollection();
			this.sb = new StringBuilder();
		}

		public void Write(TextColor color, string text) => Write(color.Box(), text);
		public void Write(object color, string text) {
			cachedTextColorsCollection.Append(color, text);
			sb.Append(text);
			Debug.Assert(sb.Length == cachedTextColorsCollection.TextLength);
		}

		IEnumerable<KeyValuePair<string, int>> GetLines(string s) {
			var sb = new StringBuilder();
			for (int offs = 0; offs < s.Length;) {
				sb.Clear();
				char c;
				while (offs < s.Length && (c = s[offs]) != '\r' && c != '\n' && c != '\u0085' && c != '\u2028' && c != '\u2029')
					sb.Append(s[offs++]);
				int nlLen;
				if (offs >= s.Length)
					nlLen = 0;
				else if (s[offs] == '\r' && offs + 1 < s.Length && s[offs + 1] == '\n')
					nlLen = 2;
				else
					nlLen = 1;
				yield return new KeyValuePair<string, int>(sb.ToString(), nlLen);
				offs += nlLen;
			}
		}

		public FrameworkElement Create(bool useNewFormatter, bool useEllipsis, bool filterOutNewLines, TextWrapping textWrapping) {
			var textBlockText = sb.ToString();

			if (!useEllipsis && filterOutNewLines) {
				return new FastTextBlock(useNewFormatter, new TextSrc {
					text = textBlockText,
					cachedTextColorsCollection = cachedTextColorsCollection,
				});
			}

			var textBlock = new TextBlock();

			int offs = 0;
			foreach (var line in GetLines(textBlockText)) {
				if (offs != 0 && !filterOutNewLines)
					textBlock.Inlines.Add(new LineBreak());
				int endOffs = offs + line.Key.Length;
				Debug.Assert(offs <= textBlockText.Length);

				foreach (var spanData in GetColors(offs, endOffs)) {
					var color = spanData.Data;
					var hlColor = GetTextColor(themeService.Theme, color);
					var text = textBlockText.Substring(offs, spanData.Span.Length);
					var elem = new Run(text);
					if (hlColor.FontStyle != null)
						elem.FontStyle = hlColor.FontStyle.Value;
					if (hlColor.FontWeight != null)
						elem.FontWeight = hlColor.FontWeight.Value;
					if (hlColor.Foreground != null)
						elem.Foreground = hlColor.Foreground;
					if (hlColor.Background != null)
						elem.Background = hlColor.Background;
					textBlock.Inlines.Add(elem);
					offs += spanData.Span.Length;
				}
				Debug.Assert(offs == endOffs);
				offs += line.Value;
				Debug.Assert(offs <= textBlockText.Length);
			}

			if (useEllipsis)
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			textBlock.TextWrapping = textWrapping;
			return textBlock;
		}

		IEnumerable<SpanData<object>> GetColors(int offs, int endOffs) {
			var coll = cachedTextColorsCollection;
			int index = coll.GetStartIndex(offs);
			if (index < 0)
				yield break;

			int count = coll.Count;
			while (index < count) {
				var info = coll[index];
				if (info.Span.Start > endOffs)
					break;

				int start = Math.Max(offs, info.Span.Start);
				int end = Math.Min(endOffs, info.Span.End);
				if (start < end)
					yield return new SpanData<object>(VST.Span.FromBounds(start, end), info.Data);
				index++;
			}
		}

		static IThemeColor GetTextColor(ITheme theme, object data) =>
			theme.GetTextColor((data as TextColor? ?? TextColor.Text).ToColorType());

		public override string ToString() => Text;

		[ExportAutoLoaded]
		sealed class ThemeServiceLoader : IAutoLoaded {
			[ImportingConstructor]
			ThemeServiceLoader(IThemeService themeService) {
				TextBlockColorOutput.themeService = themeService;
			}
		}
		static IThemeService themeService;

		sealed class TextSrc : TextSource, FastTextBlock.IFastTextSource {
			FastTextBlock parent;
			internal string text;
			internal CachedTextColorsCollection cachedTextColorsCollection;

			sealed class TextProps : TextRunProperties {
				internal Brush background;
				internal Brush foreground;
				internal Typeface typeface;
				internal double fontSize;

				public override Brush BackgroundBrush => background;
				public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;
				public override double FontHintingEmSize => fontSize;
				public override double FontRenderingEmSize => fontSize;
				public override Brush ForegroundBrush => foreground;
				public override TextDecorationCollection TextDecorations => null;
				public override TextEffectCollection TextEffects => null;
				public override Typeface Typeface => typeface;
			}

			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) {
				return new TextSpan<CultureSpecificCharacterBufferRange>(0,
					new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
			}

			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) {
				throw new NotSupportedException();
			}

			public void UpdateParent(FastTextBlock ftb) => parent = ftb;
			public TextSource Source => this;

			TextRunProperties GetDefaultTextRunProperties() {
				return new TextProps {
					background = (Brush)parent.GetValue(TextElement.BackgroundProperty),
					foreground = TextElement.GetForeground(parent),
					typeface = new Typeface(
											TextElement.GetFontFamily(parent),
											TextElement.GetFontStyle(parent),
											TextElement.GetFontWeight(parent),
											TextElement.GetFontStretch(parent)
										),
					fontSize = TextElement.GetFontSize(parent),
				};
			}

			public override TextRun GetTextRun(int textSourceCharacterIndex) {
				var index = textSourceCharacterIndex;

				if (index >= text.Length)
					return new TextEndOfParagraph(1);

				char c = text[index];
				if (c == '\r' || c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029') {
					int nlLen = c == '\r' && index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;
					return new TextEndOfParagraph(nlLen);
				}

				int collIndex = cachedTextColorsCollection.GetStartIndex(index);
				Debug.Assert(collIndex >= 0);
				if (collIndex < 0)
					return new TextCharacters(text.Substring(index), GetDefaultTextRunProperties());

				var info = cachedTextColorsCollection[collIndex];
				if (info.Span.End == index) {
					Debug.Assert(collIndex + 1 < cachedTextColorsCollection.Count);
					if (collIndex + 1 >= cachedTextColorsCollection.Count)
						return new TextCharacters(text.Substring(index), GetDefaultTextRunProperties());
					info = cachedTextColorsCollection[collIndex + 1];
				}

				int startIndex = info.Span.Start;
				int endIndex = info.Span.End;

				int nlIndex = text.IndexOfAny(LineConstants.newLineChars, index, endIndex - startIndex);
				if (nlIndex > 0)
					endIndex = nlIndex;

				var tc = GetTextColor(themeService.Theme, info.Data);
				var tokenText = text.Substring(index, endIndex - startIndex);

				var textProps = new TextProps();
				textProps.fontSize = TextElement.GetFontSize(parent);

				textProps.foreground = tc.Foreground ?? TextElement.GetForeground(parent);
				textProps.background = tc.Background ?? (Brush)parent.GetValue(TextElement.BackgroundProperty);

				textProps.typeface = new Typeface(
					TextElement.GetFontFamily(parent),
					tc.FontStyle ?? TextElement.GetFontStyle(parent),
					tc.FontWeight ?? TextElement.GetFontWeight(parent),
					TextElement.GetFontStretch(parent)
				);

				return new TextCharacters(tokenText.Length == 0 ? " " : tokenText, textProps);
			}
		}
	}
}
