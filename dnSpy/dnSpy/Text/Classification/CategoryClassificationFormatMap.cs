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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Classification {
	sealed class CategoryClassificationFormatMap : IClassificationFormatMap {
		public TextFormattingRunProperties DefaultTextProperties { get; private set; }
		public event EventHandler<EventArgs> ClassificationFormatMappingChanged;

		readonly IThemeManager themeManager;
		readonly ITextEditorSettings textEditorSettings;
		readonly Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata>[] editorFormatDefinitions;
		readonly Dictionary<IClassificationType, ClassificationInfo> toClassificationInfo;
		readonly Dictionary<IClassificationType, Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata>> toEditorFormatDefinition;
		readonly Dictionary<IClassificationType, int> toClassificationTypeOrder;

		sealed class ClassificationInfo {
			public TextFormattingRunProperties ExplicitTextProperties { get; set; }
			public TextFormattingRunProperties InheritedTextProperties { get; set; }
			public Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata> Lazy { get; }
			public IClassificationType ClassificationType { get; }

			public ClassificationInfo(Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata> lazy, IClassificationType classificationType) {
				Lazy = lazy;
				ClassificationType = classificationType;
			}
		}

		public CategoryClassificationFormatMap(IThemeManager themeManager, ITextEditorSettings textEditorSettings, Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata>[] editorFormatDefinitions, IClassificationTypeRegistryService classificationTypeRegistryService) {
			if (themeManager == null)
				throw new ArgumentNullException(nameof(themeManager));
			if (textEditorSettings == null)
				throw new ArgumentNullException(nameof(textEditorSettings));
			if (editorFormatDefinitions == null)
				throw new ArgumentNullException(nameof(editorFormatDefinitions));
			this.themeManager = themeManager;
			this.textEditorSettings = textEditorSettings;
			this.editorFormatDefinitions = editorFormatDefinitions;
			this.toClassificationInfo = new Dictionary<IClassificationType, ClassificationInfo>();
			this.toEditorFormatDefinition = new Dictionary<IClassificationType, Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata>>(editorFormatDefinitions.Length);
			this.toClassificationTypeOrder = new Dictionary<IClassificationType, int>();

			for (int i = 0; i < editorFormatDefinitions.Length; i++) {
				var e = editorFormatDefinitions[i];
				var classificationType = classificationTypeRegistryService.GetClassificationType(e.Metadata.ClassificationTypeName);
				Debug.Assert(classificationType != null);
				if (classificationType == null)
					continue;
				Debug.Assert(!toEditorFormatDefinition.ContainsKey(classificationType));
				if (!toEditorFormatDefinition.ContainsKey(classificationType)) {
					toClassificationTypeOrder.Add(classificationType, i);
					toEditorFormatDefinition.Add(classificationType, e);
				}
			}

			themeManager.ThemeChangedHighPriority += ThemeManager_ThemeChangedHighPriority;
			textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;
			ReinitializeCache();
		}

		void TextEditorSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(textEditorSettings.FontFamily))
				ClearCacheAndNotifyListeners();
			else if (e.PropertyName == nameof(textEditorSettings.FontSize))
				ClearCacheAndNotifyListeners();
		}

		void ThemeManager_ThemeChangedHighPriority(object sender, ThemeChangedEventArgs e) => ClearCacheAndNotifyListeners();

		void ReinitializeCache() {
			toClassificationInfo.Clear();
			DefaultTextProperties = CreateTextFormattingRunProperties(themeManager.Theme, textEditorSettings.FontFamily, textEditorSettings.FontSize);
		}

		sealed class DefaultEditorFormatDefinition : ThemeClassificationFormatDefinition {
			readonly FontFamily fontFamily;
			readonly double fontSize;

			public DefaultEditorFormatDefinition(FontFamily fontFamily, double fontSize)
				: base(ColorType.Text) {
				if (fontFamily == null)
					throw new ArgumentNullException(nameof(fontFamily));
				this.fontFamily = fontFamily;
				this.fontSize = fontSize;
			}

			public override Typeface GetTypeface(ITheme theme) {
				var tc = theme.GetTextColor(ColorType.Text);
				return new Typeface(fontFamily, tc.FontStyle ?? FontStyles.Normal, tc.FontWeight ?? FontWeights.Normal, FontStretches.Normal);
			}

			public override double? GetFontRenderingEmSize(ITheme theme) => fontSize;
		}

		TextFormattingRunProperties CreateTextFormattingRunProperties(ITheme theme, FontFamily fontFamily, double fontSize) {
			var def = new DefaultEditorFormatDefinition(fontFamily, fontSize);
			return def.CreateTextFormattingRunProperties(theme);
		}

		void ClearCacheAndNotifyListeners() {
			ReinitializeCache();
			ClassificationFormatMappingChanged?.Invoke(this, EventArgs.Empty);
		}

		ClassificationInfo TryGetClassificationInfo(IClassificationType classificationType) {
			ClassificationInfo info;
			if (!toClassificationInfo.TryGetValue(classificationType, out info)) {
				Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata> lazy;
				if (!toEditorFormatDefinition.TryGetValue(classificationType, out lazy))
					return null;
				toClassificationInfo.Add(classificationType, info = new ClassificationInfo(lazy, classificationType));
			}
			return info;
		}

		public TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType) {
			if (classificationType == null)
				throw new ArgumentNullException(nameof(classificationType));
			var info = TryGetClassificationInfo(classificationType);
			if (info == null)
				return TextFormattingRunProperties.CreateTextFormattingRunProperties();
			if (info.ExplicitTextProperties == null)
				CreateExplicitTextProperties(info);
			Debug.Assert(info.ExplicitTextProperties != null);
			return info.ExplicitTextProperties;
		}

		public TextFormattingRunProperties GetTextProperties(IClassificationType classificationType) {
			if (classificationType == null)
				throw new ArgumentNullException(nameof(classificationType));
			var info = TryGetClassificationInfo(classificationType);
			if (info == null)
				return TextFormattingRunProperties.CreateTextFormattingRunProperties();
			if (info.InheritedTextProperties == null)
				CreateInheritedTextProperties(info);
			Debug.Assert(info.InheritedTextProperties != null);
			return info.InheritedTextProperties;
		}

		void CreateExplicitTextProperties(ClassificationInfo info) =>
			info.ExplicitTextProperties = info.Lazy.Value.CreateTextFormattingRunProperties(themeManager.Theme);

		void CreateInheritedTextProperties(ClassificationInfo info) {
			var list = new List<IClassificationType>();
			AddBaseTypes(list, info.ClassificationType);
			info.InheritedTextProperties = CreateInheritedTextProperties(DefaultTextProperties, list);
		}

		TextFormattingRunProperties CreateInheritedTextProperties(TextFormattingRunProperties p, List<IClassificationType> types) {
			for (int i = types.Count - 1; i >= 0; i--)
				p = Merge(p, GetExplicitTextProperties(types[i]));
			return p;
		}

		static TextFormattingRunProperties Merge(TextFormattingRunProperties lowPrio, TextFormattingRunProperties hiPrio) {
			var p = hiPrio;

			if (p.TypefaceEmpty && !lowPrio.TypefaceEmpty)
				p = p.SetTypeface(lowPrio.Typeface);
			if (p.BoldEmpty && !lowPrio.BoldEmpty)
				p = p.SetBold(lowPrio.Bold);
			if (p.ItalicEmpty && !lowPrio.ItalicEmpty)
				p = p.SetItalic(lowPrio.Italic);
			if (p.ForegroundBrushEmpty && !lowPrio.ForegroundBrushEmpty)
				p = p.SetForegroundBrush(lowPrio.ForegroundBrush);
			if (p.BackgroundBrushEmpty && !lowPrio.BackgroundBrushEmpty)
				p = p.SetBackgroundBrush(lowPrio.BackgroundBrush);
			if (p.ForegroundOpacityEmpty && !lowPrio.ForegroundOpacityEmpty)
				p = p.SetForegroundOpacity(lowPrio.ForegroundOpacity);
			if (p.BackgroundOpacityEmpty && !lowPrio.BackgroundOpacityEmpty)
				p = p.SetBackgroundOpacity(lowPrio.BackgroundOpacity);
			if (p.CultureInfoEmpty && !lowPrio.CultureInfoEmpty)
				p = p.SetCultureInfo(lowPrio.CultureInfo);
			if (p.FontHintingEmSizeEmpty && !lowPrio.FontHintingEmSizeEmpty)
				p = p.SetFontHintingEmSize(lowPrio.FontHintingEmSize);
			if (p.FontRenderingEmSizeEmpty && !lowPrio.FontRenderingEmSizeEmpty)
				p = p.SetFontRenderingEmSize(lowPrio.FontRenderingEmSize);
			if (p.TextDecorationsEmpty && !lowPrio.TextDecorationsEmpty)
				p = p.SetTextDecorations(lowPrio.TextDecorations);
			if (p.TextEffectsEmpty && !lowPrio.TextEffectsEmpty)
				p = p.SetTextEffects(lowPrio.TextEffects);

			return p;
		}

		void AddBaseTypes(List<IClassificationType> list, IClassificationType classificationType) {
			if (list.Contains(classificationType))
				return;
			list.Add(classificationType);
			var baseTypes = classificationType.BaseTypes.ToArray();
			if (baseTypes.Length > 1)
				Array.Sort(baseTypes, classificationTypeComparer ?? (classificationTypeComparer = new ClassificationTypeComparer(this)));
			foreach (var bt in baseTypes)
				AddBaseTypes(list, bt);
		}
		ClassificationTypeComparer classificationTypeComparer;

		sealed class ClassificationTypeComparer : IComparer<IClassificationType> {
			readonly CategoryClassificationFormatMap owner;

			public ClassificationTypeComparer(CategoryClassificationFormatMap owner) {
				this.owner = owner;
			}

			public int Compare(IClassificationType x, IClassificationType y) => GetOrder(y) - GetOrder(x);

			int GetOrder(IClassificationType a) {
				if (a == null)
					return -1;
				int order;
				if (owner.toClassificationTypeOrder.TryGetValue(a, out order))
					return order;
				return -1;
			}
		}
	}
}
