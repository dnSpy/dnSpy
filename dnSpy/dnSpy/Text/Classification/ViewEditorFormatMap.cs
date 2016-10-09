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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Classification {
	sealed class ViewEditorFormatMap : IEditorFormatMap {
		public bool IsInBatchUpdate => categoryMap.IsInBatchUpdate;
		public event EventHandler<FormatItemsEventArgs> FormatMappingChanged;

		readonly ITextView textView;
		readonly IEditorFormatMapService editorFormatMapService;
		IEditorFormatMap categoryMap;
		readonly HashSet<string> viewProps;

		public ViewEditorFormatMap(ITextView textView, IEditorFormatMapService editorFormatMapService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (editorFormatMapService == null)
				throw new ArgumentNullException(nameof(editorFormatMapService));
			this.textView = textView;
			this.editorFormatMapService = editorFormatMapService;
			this.viewProps = new HashSet<string>();
			textView.Options.OptionChanged += Options_OptionChanged;
			UpdateAppearanceMap();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.AppearanceCategoryName)
				UpdateAppearanceMap();
		}

		void UpdateAppearanceMap() {
			var newMap = editorFormatMapService.GetEditorFormatMap(textView.Options.AppearanceCategory());
			if (categoryMap == newMap)
				return;

			if (categoryMap != null)
				categoryMap.FormatMappingChanged -= CategoryMap_FormatMappingChanged;
			categoryMap = newMap;
			categoryMap.FormatMappingChanged += CategoryMap_FormatMappingChanged;
			FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(new ReadOnlyCollection<string>(viewProps.ToArray())));
		}

		void CategoryMap_FormatMappingChanged(object sender, FormatItemsEventArgs e) =>
			FormatMappingChanged?.Invoke(this, e);

		public void BeginBatchUpdate() => categoryMap.BeginBatchUpdate();
		public void EndBatchUpdate() => categoryMap.EndBatchUpdate();

		public void AddProperties(string key, ResourceDictionary properties) {
			viewProps.Add(key);
			categoryMap.AddProperties(key, properties);
		}

		public ResourceDictionary GetProperties(string key) {
			viewProps.Add(key);
			return categoryMap.GetProperties(key);
		}

		public void SetProperties(string key, ResourceDictionary properties) {
			viewProps.Add(key);
			categoryMap.SetProperties(key, properties);
		}

		public void Dispose() {
			if (categoryMap != null)
				categoryMap.FormatMappingChanged -= CategoryMap_FormatMappingChanged;
			textView.Options.OptionChanged -= Options_OptionChanged;
		}
	}
}
