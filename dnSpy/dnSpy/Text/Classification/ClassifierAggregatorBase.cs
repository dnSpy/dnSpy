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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Tagging;

namespace dnSpy.Text.Classification {
	abstract class ClassifierAggregatorBase : IClassifier, IDisposable {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITagAggregator<IClassificationTag> tagAggregator;
		readonly ITextBuffer textBuffer;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		protected ClassifierAggregatorBase(ITagAggregator<IClassificationTag> tagAggregator, IContentTypeRegistryService contentTypeRegistryService, ITextBuffer textBuffer) {
			if (tagAggregator == null)
				throw new ArgumentNullException(nameof(tagAggregator));
			if (contentTypeRegistryService == null)
				throw new ArgumentNullException(nameof(contentTypeRegistryService));
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.tagAggregator = tagAggregator;
			this.textBuffer = textBuffer;
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
		}

		void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e) {
			if (ClassificationChanged == null)
				return;
			foreach (var span in e.Span.GetSpans(textBuffer))
				ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {
			return Array.Empty<ClassificationSpan>();//TODO:
		}

		public void Dispose() {
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			(tagAggregator as IDisposable)?.Dispose();
		}
	}
}
