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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Documents.Tabs.DocViewer {
	[ExportDocumentViewerReferenceEnablerProvider(PredefinedSpanReferenceIds.HighlightRelatedKeywords)]
	sealed class HighlightRelatedKeywordsDocumentViewerReferenceEnablerProvider : IDocumentViewerReferenceEnablerProvider {
		public IDocumentViewerReferenceEnabler Create(IDocumentViewer documentViewer) =>
			new HighlightRelatedKeywordsDocumentViewerReferenceEnabler(documentViewer);
	}

	sealed class HighlightRelatedKeywordsDocumentViewerReferenceEnabler : IDocumentViewerReferenceEnabler {
		public bool IsEnabled { get; private set; }
		public event EventHandler IsEnabledChanged;

		readonly IDocumentViewer documentViewer;

		public HighlightRelatedKeywordsDocumentViewerReferenceEnabler(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			this.documentViewer = documentViewer;
			IsEnabled = documentViewer.TextView.Options.IsHighlightRelatedKeywordsEnabled();
			documentViewer.TextView.Options.OptionChanged += Options_OptionChanged;
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultDsTextViewOptions.HighlightRelatedKeywordsName) {
				IsEnabled = documentViewer.TextView.Options.IsHighlightRelatedKeywordsEnabled();
				IsEnabledChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public void Dispose() => documentViewer.TextView.Options.OptionChanged -= Options_OptionChanged;
	}
}
