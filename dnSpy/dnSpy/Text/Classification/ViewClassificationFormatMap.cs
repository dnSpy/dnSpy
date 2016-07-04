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
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Classification {
	sealed class ViewClassificationFormatMap : IClassificationFormatMap {
		public ReadOnlyCollection<IClassificationType> CurrentPriorityOrder => categoryMap.CurrentPriorityOrder;
		public bool IsInBatchUpdate => categoryMap.IsInBatchUpdate;

		public TextFormattingRunProperties DefaultTextProperties {
			get { return categoryMap.DefaultTextProperties; }
			set { categoryMap.DefaultTextProperties = value; }
		}

		public event EventHandler<EventArgs> ClassificationFormatMappingChanged;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ITextView textView;
		IClassificationFormatMap categoryMap;

		public ViewClassificationFormatMap(IClassificationFormatMapService classificationFormatMapService, ITextView textView) {
			if (classificationFormatMapService == null)
				throw new ArgumentNullException(nameof(classificationFormatMapService));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.classificationFormatMapService = classificationFormatMapService;
			this.textView = textView;
			textView.Options.OptionChanged += Options_OptionChanged;
			UpdateAppearanceMap();
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.AppearanceCategory.Name)
				UpdateAppearanceMap();
		}

		void UpdateAppearanceMap() {
			var newMap = classificationFormatMapService.GetClassificationFormatMap(textView.Options.AppearanceCategory());
			if (categoryMap == newMap)
				return;

			if (categoryMap != null)
				categoryMap.ClassificationFormatMappingChanged -= CategoryMap_ClassificationFormatMappingChanged;
			categoryMap = newMap;
			categoryMap.ClassificationFormatMappingChanged += CategoryMap_ClassificationFormatMappingChanged;
			ClassificationFormatMappingChanged?.Invoke(this, EventArgs.Empty);
		}

		void CategoryMap_ClassificationFormatMappingChanged(object sender, EventArgs e) =>
			ClassificationFormatMappingChanged?.Invoke(this, EventArgs.Empty);

		public TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType) =>
			categoryMap.GetExplicitTextProperties(classificationType);

		public TextFormattingRunProperties GetTextProperties(IClassificationType classificationType) =>
			categoryMap.GetTextProperties(classificationType);

		public string GetEditorFormatMapKey(IClassificationType classificationType) =>
			categoryMap.GetEditorFormatMapKey(classificationType);

		public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties) =>
			categoryMap.AddExplicitTextProperties(classificationType, properties);

		public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties, IClassificationType priority) =>
			categoryMap.AddExplicitTextProperties(classificationType, properties, priority);

		public void SetTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties) =>
			categoryMap.SetTextProperties(classificationType, properties);

		public void SetExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties) =>
			categoryMap.SetExplicitTextProperties(classificationType, properties);

		public void SwapPriorities(IClassificationType firstType, IClassificationType secondType) =>
			categoryMap.SwapPriorities(firstType, secondType);

		public void BeginBatchUpdate() => categoryMap.BeginBatchUpdate();
		public void EndBatchUpdate() => categoryMap.EndBatchUpdate();

		public void Dispose() {
			if (categoryMap != null)
				categoryMap.ClassificationFormatMappingChanged -= CategoryMap_ClassificationFormatMappingChanged;
			textView.Options.OptionChanged -= Options_OptionChanged;
		}
	}
}
