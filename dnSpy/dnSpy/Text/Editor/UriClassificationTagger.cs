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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IViewTaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.Text)]
	sealed class UriClassificationTaggerProvider : IViewTaggerProvider {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;
		readonly IClassificationTag classificationTag;

		[ImportingConstructor]
		UriClassificationTaggerProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IThemeClassificationTypeService themeClassificationTypeService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			classificationTag = new ClassificationTag(themeClassificationTypeService.GetClassificationType(TextColor.Url));
		}

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag =>
			new UriClassificationTagger(classificationTag, buffer, viewTagAggregatorFactoryService.CreateTagAggregator<IUrlTag>(textView)) as ITagger<T>;
	}

	sealed class UriClassificationTagger : ITagger<IClassificationTag>, IDisposable {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly IClassificationTag classificationTag;
		readonly ITagAggregator<IUrlTag> tagAggregator;
		readonly ITextBuffer textBuffer;

		public UriClassificationTagger(IClassificationTag classificationTag, ITextBuffer textBuffer, ITagAggregator<IUrlTag> tagAggregator) {
			this.classificationTag = classificationTag ?? throw new ArgumentNullException(nameof(classificationTag));
			this.tagAggregator = tagAggregator ?? throw new ArgumentNullException(nameof(tagAggregator));
			this.textBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
		}

		void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e) {
			foreach (var span in e.Span.GetSpans(textBuffer))
				TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var tagSpan in tagAggregator.GetTags(spans)) {
				foreach (var span in tagSpan.Span.GetSpans(textBuffer))
					yield return new TagSpan<IClassificationTag>(span, classificationTag);
			}
		}

		public void Dispose() {
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
		}
	}
}
