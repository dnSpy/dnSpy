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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor.Search {
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TagType(typeof(ITextMarkerTag))]
	sealed class TextMarkerTaggerProvider : IViewTaggerProvider {
		readonly ISearchServiceProvider searchServiceProvider;

		[ImportingConstructor]
		TextMarkerTaggerProvider(ISearchServiceProvider searchServiceProvider) {
			this.searchServiceProvider = searchServiceProvider;
		}

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			var wpfTextView = textView as IWpfTextView;
			if (wpfTextView == null)
				return null;
			if (textView.TextBuffer != buffer)
				return null;
			return wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(TextMarkerTagger), () => new TextMarkerTagger(searchServiceProvider, wpfTextView)) as ITagger<T>;
		}
	}

	sealed class TextMarkerTagger : ITagger<ITextMarkerTag>, ITextMarkerListener {
		readonly ISearchService searchService;

		public TextMarkerTagger(ISearchServiceProvider searchServiceProvider, IWpfTextView wpfTextView) {
			if (searchServiceProvider == null)
				throw new ArgumentNullException(nameof(searchServiceProvider));
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			this.searchService = searchServiceProvider.Get(wpfTextView);
			searchService.RegisterTextMarkerListener(this);
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
		void ITextMarkerListener.RaiseTagsChanged(SnapshotSpan span) => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));

		public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var span in searchService.GetSpans(spans))
				yield return new TagSpan<ITextMarkerTag>(span, searchTextMarkerTag);
		}
		static readonly ITextMarkerTag searchTextMarkerTag = new TextMarkerTag(ThemeClassificationTypeNameKeys.FindMatchHighlightMarker);
	}
}
