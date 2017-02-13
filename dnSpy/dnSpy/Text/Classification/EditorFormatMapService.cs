/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows.Threading;
using dnSpy.Contracts.Themes;
using dnSpy.Settings.AppearanceCategory;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Classification {
	abstract class EditorFormatMapService {
		readonly IThemeService themeService;
		readonly ITextAppearanceCategoryService textAppearanceCategoryService;
		readonly IEditorFormatDefinitionService editorFormatDefinitionService;
		readonly Dictionary<ITextAppearanceCategory, IEditorFormatMap> toCategoryMap;
		readonly List<CategoryEditorFormatMapUpdater> cachedUpdaters;
		readonly Dispatcher dispatcher;

		protected EditorFormatMapService(IThemeService themeService, ITextAppearanceCategoryService textAppearanceCategoryService, IEditorFormatDefinitionService editorFormatDefinitionService) {
			this.themeService = themeService;
			this.textAppearanceCategoryService = textAppearanceCategoryService;
			this.editorFormatDefinitionService = editorFormatDefinitionService;
			toCategoryMap = new Dictionary<ITextAppearanceCategory, IEditorFormatMap>();
			cachedUpdaters = new List<CategoryEditorFormatMapUpdater>();
			dispatcher = Dispatcher.CurrentDispatcher;
		}

		public IEditorFormatMap GetEditorFormatMap(string category) {
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			var textAppearanceCategory = textAppearanceCategoryService.GetSettings(category);
			if (toCategoryMap.TryGetValue(textAppearanceCategory, out var map))
				return map;
			map = new CategoryEditorFormatMap(dispatcher, editorFormatDefinitionService);
			var updater = new CategoryEditorFormatMapUpdater(themeService, textAppearanceCategory, editorFormatDefinitionService, map);
			cachedUpdaters.Add(updater);
			toCategoryMap.Add(textAppearanceCategory, map);
			return map;
		}
	}

	[Export(typeof(IEditorFormatMapService))]
	sealed class EditorFormatMapServiceImpl : EditorFormatMapService, IEditorFormatMapService {
		[ImportingConstructor]
		public EditorFormatMapServiceImpl(IThemeService themeService, ITextAppearanceCategoryService textAppearanceCategoryService, IEditorFormatDefinitionService editorFormatDefinitionService)
			: base(themeService, textAppearanceCategoryService, editorFormatDefinitionService) {
		}

		public IEditorFormatMap GetEditorFormatMap(ITextView view) {
			if (view == null)
				throw new ArgumentNullException(nameof(view));
			return view.Properties.GetOrCreateSingletonProperty(typeof(ViewEditorFormatMap), () => CreateViewEditorFormatMap(view));
		}

		ViewEditorFormatMap CreateViewEditorFormatMap(ITextView textView) {
			textView.Closed += TextView_Closed;
			return new TextViewEditorFormatMap(textView, this);
		}

		void TextView_Closed(object sender, EventArgs e) {
			var textView = (ITextView)sender;
			textView.Closed -= TextView_Closed;
			var map = (ViewEditorFormatMap)textView.Properties[typeof(ViewEditorFormatMap)];
			textView.Properties.RemoveProperty(typeof(ViewEditorFormatMap));
			map.Dispose();
		}
	}
}
