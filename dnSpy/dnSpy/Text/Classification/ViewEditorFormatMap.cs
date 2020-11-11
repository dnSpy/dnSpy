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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Classification {
	abstract class ViewEditorFormatMap : IEditorFormatMap {
		public bool IsInBatchUpdate => categoryMap.IsInBatchUpdate;
		public event EventHandler<FormatItemsEventArgs>? FormatMappingChanged;

		readonly EditorFormatMapService editorFormatMapService;
		readonly string appearanceCategoryName;
		IEditorFormatMap categoryMap;
		readonly HashSet<string> viewProps;

		protected ViewEditorFormatMap(EditorFormatMapService editorFormatMapService, string appearanceCategoryName) {
			categoryMap = null!;
			this.editorFormatMapService = editorFormatMapService ?? throw new ArgumentNullException(nameof(editorFormatMapService));
			this.appearanceCategoryName = appearanceCategoryName ?? throw new ArgumentNullException(nameof(appearanceCategoryName));
			viewProps = new HashSet<string>(StringComparer.Ordinal);
		}

		protected void Initialize() => UpdateAppearanceMap();

		protected void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == appearanceCategoryName)
				UpdateAppearanceMap();
		}

		protected abstract string GetAppearanceCategory();

		void UpdateAppearanceMap() {
			var newMap = editorFormatMapService.GetEditorFormatMap(GetAppearanceCategory());
			if (categoryMap == newMap)
				return;

			if (categoryMap is not null)
				categoryMap.FormatMappingChanged -= CategoryMap_FormatMappingChanged;
			categoryMap = newMap;
			categoryMap.FormatMappingChanged += CategoryMap_FormatMappingChanged;
			FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(new ReadOnlyCollection<string>(viewProps.ToArray())));
		}

		void CategoryMap_FormatMappingChanged(object? sender, FormatItemsEventArgs e) =>
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
			if (categoryMap is not null)
				categoryMap.FormatMappingChanged -= CategoryMap_FormatMappingChanged;
			DisposeCore();
		}

		protected abstract void DisposeCore();
	}

	sealed class TextViewEditorFormatMap : ViewEditorFormatMap {
		readonly ITextView textView;

		public TextViewEditorFormatMap(ITextView textView, EditorFormatMapService editorFormatMapService)
			: base(editorFormatMapService, DefaultWpfViewOptions.AppearanceCategoryName) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			textView.Options.OptionChanged += Options_OptionChanged;
			Initialize();
		}

		protected override string GetAppearanceCategory() => textView.Options.AppearanceCategory();
		protected override void DisposeCore() => textView.Options.OptionChanged -= Options_OptionChanged;
	}
}
