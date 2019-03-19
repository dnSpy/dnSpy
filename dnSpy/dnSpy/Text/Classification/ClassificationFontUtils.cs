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

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Classification {
	static class ClassificationFontUtils {
		public static void CopyTo(ResourceDictionary dict, TextFormattingRunProperties textProps) {
			dict.Clear();
			if (!textProps.TypefaceEmpty)
				dict[ClassificationFormatDefinition.TypefaceId] = textProps.Typeface;
			if (!textProps.CultureInfoEmpty)
				dict[ClassificationFormatDefinition.CultureInfoId] = textProps.CultureInfo;
			if (!textProps.FontHintingEmSizeEmpty)
				dict[ClassificationFormatDefinition.FontHintingSizeId] = textProps.FontHintingEmSize;
			if (!textProps.FontRenderingEmSizeEmpty)
				dict[ClassificationFormatDefinition.FontRenderingSizeId] = textProps.FontRenderingEmSize;
			if (!textProps.BackgroundBrushEmpty) {
				dict[EditorFormatDefinition.BackgroundBrushId] = textProps.BackgroundBrush;
				if (textProps.BackgroundBrush.Opacity < 1)
					dict[ClassificationFormatDefinition.BackgroundOpacityId] = textProps.BackgroundBrush.Opacity;
			}
			if (!textProps.BackgroundOpacityEmpty)
				dict[ClassificationFormatDefinition.BackgroundOpacityId] = textProps.BackgroundOpacity;
			if (!textProps.ForegroundBrushEmpty) {
				dict[EditorFormatDefinition.ForegroundBrushId] = textProps.ForegroundBrush;
				if (textProps.ForegroundBrush.Opacity < 1)
					dict[ClassificationFormatDefinition.ForegroundOpacityId] = textProps.ForegroundBrush.Opacity;
			}
			if (!textProps.ForegroundOpacityEmpty)
				dict[ClassificationFormatDefinition.ForegroundOpacityId] = textProps.ForegroundOpacity;
			if (!textProps.BoldEmpty)
				dict[ClassificationFormatDefinition.IsBoldId] = textProps.Bold;
			if (!textProps.ItalicEmpty)
				dict[ClassificationFormatDefinition.IsItalicId] = textProps.Italic;
			if (!textProps.TextDecorationsEmpty)
				dict[ClassificationFormatDefinition.TextDecorationsId] = textProps.TextDecorations;
			if (!textProps.TextEffectsEmpty)
				dict[ClassificationFormatDefinition.TextEffectsId] = textProps.TextEffects;
		}

		public static void CopyTo(ResourceDictionary target, ResourceDictionary source) {
			target.Clear();
			foreach (var key in source.Keys)
				target[key] = source[key];
		}

		public static TextFormattingRunProperties Create(ResourceDictionary dict) {
			var foreground = GetBrush(dict, EditorFormatDefinition.ForegroundBrushId, EditorFormatDefinition.ForegroundColorId, ClassificationFormatDefinition.ForegroundOpacityId, null, SystemColors.WindowTextBrush);
			var background = GetBrush(dict, EditorFormatDefinition.BackgroundBrushId, EditorFormatDefinition.BackgroundColorId, ClassificationFormatDefinition.BackgroundOpacityId, ClassificationFormatDefinition.DefaultBackgroundOpacity, null);
			Typeface typeface = GetTypeface(dict);
			double? size = dict[ClassificationFormatDefinition.FontRenderingSizeId] as double? ?? 16;
			double? hintingSize = dict[ClassificationFormatDefinition.FontHintingSizeId] as double?;
			var textDecorations = dict[ClassificationFormatDefinition.TextDecorationsId] as TextDecorationCollection;
			var textEffects = dict[ClassificationFormatDefinition.TextEffectsId] as TextEffectCollection;
			var cultureInfo = dict[ClassificationFormatDefinition.CultureInfoId] as CultureInfo;
			var textRunProps = TextFormattingRunProperties.CreateTextFormattingRunProperties(foreground, background, typeface, size, hintingSize, textDecorations, textEffects, cultureInfo);
			var isItalic = dict[ClassificationFormatDefinition.IsItalicId] as bool?;
			if (isItalic != null)
				textRunProps = textRunProps.SetItalic(isItalic.Value);
			var isBold = dict[ClassificationFormatDefinition.IsBoldId] as bool?;
			if (isBold != null)
				textRunProps = textRunProps.SetBold(isBold.Value);
			double? opacity;
			if (foreground == null && (opacity = dict[ClassificationFormatDefinition.ForegroundOpacityId] as double?) != null)
				textRunProps = textRunProps.SetForegroundOpacity(opacity.Value);
			if (background == null && (opacity = dict[ClassificationFormatDefinition.BackgroundOpacityId] as double?) != null)
				textRunProps = textRunProps.SetBackgroundOpacity(opacity.Value);
			return textRunProps;
		}

		static Brush GetBrush(ResourceDictionary dict, string brushId, string colorId, string opacityId, double? defaultOpacity, Brush defaultBrush) {
			var brush = dict[brushId] as Brush;
			if (brush == null) {
				var color = dict[colorId] as Color?;
				if (color != null)
					brush = new SolidColorBrush(color.Value);
			}
			if (brush == null)
				brush = defaultBrush;
			if (brush == null)
				return brush;

			var opacity = dict[opacityId] as double? ?? defaultOpacity;
			if (opacity != null) {
				brush = brush.Clone();
				brush.Opacity = opacity.Value;
			}

			if (brush.CanFreeze)
				brush.Freeze();
			return brush;
		}

		static Typeface GetTypeface(ResourceDictionary dict) {
			var typeface = dict[ClassificationFormatDefinition.TypefaceId] as Typeface;
			if (typeface == null)
				typeface = new Typeface(defaultFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, DefaultFallbackFontFamily);
			var isBold = dict[ClassificationFormatDefinition.IsBoldId] as bool?;
			var isItalic = dict[ClassificationFormatDefinition.IsItalicId] as bool?;
			var fontStyle = isItalic ?? false ? FontStyles.Italic : FontStyles.Normal;
			var fontWeight = isBold ?? false ? FontWeights.Bold : FontWeights.Normal;
			if (typeface.Style != fontStyle || typeface.Weight != fontWeight)
				typeface = new Typeface(typeface.FontFamily, fontStyle, fontWeight, typeface.Stretch, DefaultFallbackFontFamily);
			return typeface;
		}
		static readonly FontFamily defaultFontFamily = new FontFamily("Consolas");
		public static FontFamily DefaultFallbackFontFamily { get; } = new FontFamily("Global Monospace, Global User Interface");

		public static ResourceDictionary CreateResourceDictionary(ResourceDictionary dict, string[] propsToCopy) {
			var res = new ResourceDictionary();
			foreach (var key in propsToCopy) {
				if (dict.Contains(key))
					res[key] = dict[key];
			}
			return res;
		}

		public static void Merge(ResourceDictionary lowPrioSource, ResourceDictionary hiPrioDest) {
			foreach (var key in lowPrioSource.Keys) {
				if (!hiPrioDest.Contains(key))
					hiPrioDest[key] = lowPrioSource[key];
			}
		}
	}
}
