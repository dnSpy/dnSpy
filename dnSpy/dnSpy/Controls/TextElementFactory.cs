/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.DnSpy.Text.WPF;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Controls {
	[Export(typeof(ITextElementFactory))]
	sealed class TextElementFactoryImpl : ITextElementFactory {
		public FrameworkElement Create(IClassificationFormatMap classificationFormatMap, string text, IList<TextClassificationTag> tags, TextElementFlags flags) =>
			TextElementFactory.Create(classificationFormatMap, text, tags, flags);
	}

	static class TextElementFactory {
		static string ToString(string s, bool filterOutNewLines) {
			if (!filterOutNewLines)
				return s;
			if (s.IndexOfAny(LineConstants.newLineChars) < 0)
				return s;
			var sb = new StringBuilder(s.Length);
			foreach (var c in s) {
				if (Array.IndexOf(LineConstants.newLineChars, c) >= 0)
					sb.Append(' ');
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		static TextTrimming GetTextTrimming(TextElementFlags flags) {
			switch (flags & TextElementFlags.TrimmingMask) {
			case TextElementFlags.NoTrimming: return TextTrimming.None;
			case TextElementFlags.CharacterEllipsis: return TextTrimming.CharacterEllipsis;
			case TextElementFlags.WordEllipsis: return TextTrimming.WordEllipsis;
			default: throw new ArgumentOutOfRangeException(nameof(flags));
			}
		}

		static TextWrapping GetTextWrapping(TextElementFlags flags) {
			switch (flags & TextElementFlags.WrapMask) {
			case TextElementFlags.WrapWithOverflow: return TextWrapping.WrapWithOverflow;
			case TextElementFlags.NoWrap: return TextWrapping.NoWrap;
			case TextElementFlags.Wrap: return TextWrapping.Wrap;
			default: throw new ArgumentOutOfRangeException(nameof(flags));
			}
		}

		public static FrameworkElement Create(IClassificationFormatMap classificationFormatMap, string text, IList<TextClassificationTag> tags, TextElementFlags flags) {
			bool useFastTextBlock = (flags & (TextElementFlags.TrimmingMask | TextElementFlags.WrapMask | TextElementFlags.FilterOutNewLines)) == (TextElementFlags.NoTrimming | TextElementFlags.NoWrap | TextElementFlags.FilterOutNewLines);
			bool filterOutNewLines = (flags & TextElementFlags.FilterOutNewLines) != 0;
			if (tags.Count != 0) {
				if (useFastTextBlock) {
					return new FastTextBlock((flags & TextElementFlags.NewFormatter) != 0, new TextSrc {
						text = ToString(WpfUnicodeUtils.ReplaceBadChars(text), filterOutNewLines),
						classificationFormatMap = classificationFormatMap,
						tagsList = tags.ToArray(),
					});
				}

				var propsSpans = tags.Select(a => new TextRunPropertiesAndSpan(a.Span, classificationFormatMap.GetTextProperties(a.ClassificationType)));
				var textBlock = TextBlockFactory.Create(text, classificationFormatMap.DefaultTextProperties, propsSpans, TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize | (filterOutNewLines ? TextBlockFactory.Flags.FilterOutNewlines : 0));
				textBlock.TextTrimming = GetTextTrimming(flags);
				textBlock.TextWrapping = GetTextWrapping(flags);
				return textBlock;
			}

			FrameworkElement fwElem;
			if (useFastTextBlock) {
				fwElem = new FastTextBlock((flags & TextElementFlags.NewFormatter) != 0) {
					Text = ToString(WpfUnicodeUtils.ReplaceBadChars(text), filterOutNewLines)
				};
			}
			else {
				fwElem = new TextBlock {
					Text = ToString(WpfUnicodeUtils.ReplaceBadChars(text), filterOutNewLines),
					TextTrimming = GetTextTrimming(flags),
					TextWrapping = GetTextWrapping(flags),
				};
			}
			return InitializeDefault(classificationFormatMap, fwElem);
		}

		static FrameworkElement InitializeDefault(IClassificationFormatMap classificationFormatMap, FrameworkElement fwElem) {
			var defaultProperties = classificationFormatMap.DefaultTextProperties;
			bool setFontFamily = false;//TODO:
			bool setFontSize = false;//TODO:

			if (!defaultProperties.BackgroundBrushEmpty)
				fwElem.SetValue(TextElement.BackgroundProperty, defaultProperties.BackgroundBrush);
			if (!defaultProperties.ForegroundBrushEmpty)
				fwElem.SetValue(TextElement.ForegroundProperty, defaultProperties.ForegroundBrush);
			if (!defaultProperties.BoldEmpty)
				fwElem.SetValue(TextElement.FontWeightProperty, defaultProperties.Bold ? FontWeights.Bold : FontWeights.Normal);
			if (!defaultProperties.ItalicEmpty)
				fwElem.SetValue(TextElement.FontStyleProperty, defaultProperties.Italic ? FontStyles.Italic : FontStyles.Normal);
			if (!defaultProperties.FontRenderingEmSizeEmpty && setFontSize)
				fwElem.SetValue(TextElement.FontSizeProperty, defaultProperties.FontRenderingEmSize);
			if (!defaultProperties.TextDecorationsEmpty)
				fwElem.SetValue(TextBlock.TextDecorationsProperty, defaultProperties.TextDecorations);
			if (!defaultProperties.TextEffectsEmpty)
				fwElem.SetValue(TextElement.TextEffectsProperty, defaultProperties.TextEffects);
			if (!defaultProperties.TypefaceEmpty && setFontFamily)
				fwElem.SetValue(TextElement.FontFamilyProperty, defaultProperties.Typeface.FontFamily);

			return fwElem;
		}

		// Ki's fast TextSource
		sealed class TextSrc : TextSource, FastTextBlock.IFastTextSource {
			FastTextBlock parent;
			internal string text;
			internal IClassificationFormatMap classificationFormatMap;
			internal TextClassificationTag[] tagsList;

			sealed class TextProps : TextRunProperties {
				internal Brush background;
				internal Brush foreground;
				internal Typeface typeface;
				internal double fontSize;
				internal TextDecorationCollection textDecorations;
				internal TextEffectCollection textEffects;

				public override Brush BackgroundBrush => background;
				public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;
				public override double FontHintingEmSize => fontSize;
				public override double FontRenderingEmSize => fontSize;
				public override Brush ForegroundBrush => foreground;
				public override TextDecorationCollection TextDecorations => textDecorations;
				public override TextEffectCollection TextEffects => textEffects;
				public override Typeface Typeface => typeface;
			}

			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) => new TextSpan<CultureSpecificCharacterBufferRange>(0,
					new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));

			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => throw new NotSupportedException();

			public void UpdateParent(FastTextBlock ftb) => parent = ftb;
			public TextSource Source => this;

			TextRunProperties GetDefaultTextRunProperties() => new TextProps {
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

			int GetStartIndex(int position) {
				var list = tagsList;
				int lo = 0, hi = list.Length - 1;
				while (lo <= hi) {
					int index = (lo + hi) / 2;

					var spanData = list[index];
					if (position < spanData.Span.Start)
						hi = index - 1;
					else if (position >= spanData.Span.End)
						lo = index + 1;
					else {
						if (index > 0 && list[index - 1].Span.End == position)
							return index - 1;
						return index;
					}
				}
				if ((uint)hi < (uint)list.Length && list[hi].Span.End == position)
					return hi;
				return lo < list.Length ? lo : -1;
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

				int collIndex = GetStartIndex(index);
				Debug.Assert(collIndex >= 0);
				if (collIndex < 0)
					return new TextCharacters(text.Substring(index), GetDefaultTextRunProperties());

				var info = tagsList[collIndex];
				if (info.Span.End == index) {
					Debug.Assert(collIndex + 1 < tagsList.Length);
					if (collIndex + 1 >= tagsList.Length)
						return new TextCharacters(text.Substring(index), GetDefaultTextRunProperties());
					info = tagsList[collIndex + 1];
				}

				int startIndex = info.Span.Start;
				int endIndex = info.Span.End;

				int nlIndex = text.IndexOfAny(LineConstants.newLineChars, index, endIndex - startIndex);
				if (nlIndex > 0)
					endIndex = nlIndex;

				var props = classificationFormatMap.GetTextProperties(info.ClassificationType);

				var tokenText = text.Substring(index, endIndex - startIndex);

				var textProps = new TextProps();
				textProps.fontSize = TextElement.GetFontSize(parent);

				textProps.foreground = (props.ForegroundBrushEmpty ? null : props.ForegroundBrush) ?? TextElement.GetForeground(parent);
				textProps.background = (props.BackgroundBrushEmpty ? null : props.BackgroundBrush) ?? (Brush)parent.GetValue(TextElement.BackgroundProperty);

				textProps.textEffects = props.TextEffectsEmpty ? null : props.TextEffects;
				textProps.textDecorations = props.TextDecorationsEmpty ? null : props.TextDecorations;

				textProps.typeface = new Typeface(
					TextElement.GetFontFamily(parent),
					(props.ItalicEmpty ? (FontStyle?)null : FontStyles.Italic) ?? TextElement.GetFontStyle(parent),
					(props.BoldEmpty ? (FontWeight?)null : FontWeights.Bold) ?? TextElement.GetFontWeight(parent),
					TextElement.GetFontStretch(parent)
				);

				return new TextCharacters(tokenText.Length == 0 ? " " : tokenText, textProps);
			}
		}
	}
}
