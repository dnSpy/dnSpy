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
using System.Linq;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Classification {
	[Export(typeof(IClassificationFormatMapService))]
	sealed class ClassificationFormatMapService : IClassificationFormatMapService {
		readonly IThemeManager themeManager;
		readonly ITextEditorFontSettingsService textEditorFontSettingsService;
		readonly Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata>[] editorFormatDefinitions;
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly Dictionary<ITextEditorFontSettings, IClassificationFormatMap> toCategoryMap;

		[ImportingConstructor]
		ClassificationFormatMapService(IThemeManager themeManager, ITextEditorFontSettingsService textEditorFontSettingsService, [ImportMany] IEnumerable<Lazy<ClassificationFormatDefinition, IClassificationFormatDefinitionMetadata>> editorFormatDefinitions, IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.themeManager = themeManager;
			this.textEditorFontSettingsService = textEditorFontSettingsService;
			this.editorFormatDefinitions = editorFormatDefinitions.OrderBy(a => a.Metadata.Order).ToArray();
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.toCategoryMap = new Dictionary<ITextEditorFontSettings, IClassificationFormatMap>();
		}

		public IClassificationFormatMap GetClassificationFormatMap(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(typeof(ViewClassificationFormatMap), () => CreateViewClassificationFormatMap(textView));
		}

		ViewClassificationFormatMap CreateViewClassificationFormatMap(ITextView textView) {
			textView.Closed += TextView_Closed;
			return new ViewClassificationFormatMap(this, textView);
		}

		static void TextView_Closed(object sender, EventArgs e) {
			var textView = (ITextView)sender;
			textView.Closed -= TextView_Closed;
			var map = (ViewClassificationFormatMap)textView.Properties[typeof(ViewClassificationFormatMap)];
			textView.Properties.RemoveProperty(typeof(ViewClassificationFormatMap));
			map.Dispose();
		}

		public IClassificationFormatMap GetClassificationFormatMap(string category) {
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			var textEditorFontSettings = textEditorFontSettingsService.GetSettings(category);
			IClassificationFormatMap map;
			if (toCategoryMap.TryGetValue(textEditorFontSettings, out map))
				return map;
			map = new CategoryClassificationFormatMap(themeManager, textEditorFontSettings, editorFormatDefinitions, classificationTypeRegistryService);
			toCategoryMap.Add(textEditorFontSettings, map);
			return map;
		}
	}
}
