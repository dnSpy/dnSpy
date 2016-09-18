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
using System.Collections.Immutable;
using System.Text;
using System.Windows.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Internal.QuickInfo;
using dnSpy.Roslyn.Shared.Glyphs;
using dnSpy.Roslyn.Shared.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Roslyn.Shared.Intellisense.QuickInfo {
	sealed class InformationQuickInfoContentVM {
		public object SymbolImageSource { get; }
		public bool HasSymbolImageSource => SymbolImageSource != null;
		public object WarningImageSource { get; }
		public bool HasWarningImageSource => WarningImageSource != null;
		public object MainDescriptionObject { get; }
		public object DocumentationObject { get; }
		public bool HasDocumentationObject => DocumentationObject != null;
		public object UsageObject { get; }
		public bool HasUsageObject => UsageObject != null;
		public object TypeParameterMapObject { get; }
		public bool HasTypeParameterMapObject => TypeParameterMapObject != null;
		public object AnonymousTypesObject { get; }
		public bool HasAnonymousTypesObject => AnonymousTypesObject != null;
		public object ExceptionObject { get; }
		public bool HasExceptionObject => ExceptionObject != null;

		public InformationQuickInfoContentVM(InformationQuickInfoContent content, IRoslynGlyphService roslynGlyphService, IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if (roslynGlyphService == null)
				throw new ArgumentNullException(nameof(roslynGlyphService));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			var sb = new StringBuilder();
			if (content.SymbolGlyph != null)
				SymbolImageSource = roslynGlyphService.GetImage(content.SymbolGlyph.Value, BackgroundType.QuickInfo);
			if (content.WarningGlyph != null)
				WarningImageSource = roslynGlyphService.GetImage(content.WarningGlyph.Value, BackgroundType.QuickInfo);
			MainDescriptionObject = TryCreateObject(sb, content.MainDescription, classificationFormatMap, themeClassificationTypeService);
			DocumentationObject = TryCreateObject(sb, content.Documentation, classificationFormatMap, themeClassificationTypeService);
			UsageObject = TryCreateObject(sb, content.UsageText, classificationFormatMap, themeClassificationTypeService);
			TypeParameterMapObject = TryCreateObject(sb, content.TypeParameterMap, classificationFormatMap, themeClassificationTypeService);
			AnonymousTypesObject = TryCreateObject(sb, content.AnonymousTypes, classificationFormatMap, themeClassificationTypeService);
			ExceptionObject = TryCreateObject(sb, content.ExceptionText, classificationFormatMap, themeClassificationTypeService);
		}

		TextBlock TryCreateObject(StringBuilder sb, ImmutableArray<TaggedText> taggedParts, IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService) {
			if (taggedParts.IsDefaultOrEmpty)
				return null;
			var text = ToString(sb, taggedParts);
			var propsSpans = CreateTextRunPropertiesAndSpans(taggedParts, classificationFormatMap, themeClassificationTypeService);
			return TextBlockFactory.Create(text, classificationFormatMap.DefaultTextProperties, propsSpans, TextBlockFactory.Flags.DisableSetTextBlockFontFamily);
		}

		IEnumerable<TextRunPropertiesAndSpan> CreateTextRunPropertiesAndSpans(ImmutableArray<TaggedText> taggedParts, IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService) {
			int pos = 0;
			foreach (var part in taggedParts) {
				var color = TextTagsHelper.ToTextColor(part.Tag);
				var classificationType = themeClassificationTypeService.GetClassificationType(color);
				yield return new TextRunPropertiesAndSpan(new Span(pos, part.Text.Length), classificationFormatMap.GetTextProperties(classificationType));
				pos += part.Text.Length;
			}
		}

		static string ToString(StringBuilder sb, ImmutableArray<TaggedText> taggedParts) {
			sb.Clear();
			foreach (var part in taggedParts)
				sb.Append(part.Text);
			return sb.ToString();
		}
	}
}
