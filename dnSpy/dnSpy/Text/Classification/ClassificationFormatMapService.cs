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
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Classification {
	abstract class ClassificationFormatMapService {
		readonly IThemeService themeService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly IEditorFormatDefinitionService editorFormatDefinitionService;
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly Dictionary<IEditorFormatMap, IClassificationFormatMap> toCategoryMap;

		protected ClassificationFormatMapService(IThemeService themeService, IEditorFormatMapService editorFormatMapService, IEditorFormatDefinitionService editorFormatDefinitionService, IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.themeService = themeService;
			this.editorFormatMapService = editorFormatMapService;
			this.editorFormatDefinitionService = editorFormatDefinitionService;
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.toCategoryMap = new Dictionary<IEditorFormatMap, IClassificationFormatMap>();
		}

		public IClassificationFormatMap GetClassificationFormatMap(string category) {
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			var editorFormatMap = editorFormatMapService.GetEditorFormatMap(category);
			IClassificationFormatMap map;
			if (toCategoryMap.TryGetValue(editorFormatMap, out map))
				return map;
			map = new CategoryClassificationFormatMap(themeService, editorFormatMap, editorFormatDefinitionService, classificationTypeRegistryService);
			toCategoryMap.Add(editorFormatMap, map);
			return map;
		}
	}

	[Export(typeof(IClassificationFormatMapService))]
	sealed class ClassificationFormatMapServiceImpl : ClassificationFormatMapService, IClassificationFormatMapService {
		[ImportingConstructor]
		ClassificationFormatMapServiceImpl(IThemeService themeService, IEditorFormatMapService editorFormatMapService, IEditorFormatDefinitionService editorFormatDefinitionService, IClassificationTypeRegistryService classificationTypeRegistryService)
			: base(themeService, editorFormatMapService, editorFormatDefinitionService, classificationTypeRegistryService) {
		}

		public IClassificationFormatMap GetClassificationFormatMap(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(typeof(ViewClassificationFormatMap), () => CreateViewClassificationFormatMap(textView));
		}

		ViewClassificationFormatMap CreateViewClassificationFormatMap(ITextView textView) {
			textView.Closed += TextView_Closed;
			return new TextViewClassificationFormatMap(this, textView);
		}

		static void TextView_Closed(object sender, EventArgs e) {
			var textView = (ITextView)sender;
			textView.Closed -= TextView_Closed;
			var map = (ViewClassificationFormatMap)textView.Properties[typeof(ViewClassificationFormatMap)];
			textView.Properties.RemoveProperty(typeof(ViewClassificationFormatMap));
			map.Dispose();
		}
	}
}
