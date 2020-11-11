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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveGlyphTextMarkerService)]
	[TagType(typeof(IClassificationTag))]
	sealed class GlyphTextViewMarkerClassificationTaggerProvider : IViewTaggerProvider {
		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;

		[ImportingConstructor]
		GlyphTextViewMarkerClassificationTaggerProvider(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl) => this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl;

		public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			var wpfTextView = textView as IWpfTextView;
			Debug2.Assert(wpfTextView is not null);
			if (wpfTextView is null)
				return null;
			if (textView.TextBuffer != buffer)
				return null;
			var service = GlyphTextViewMarkerService.GetOrCreate(glyphTextMarkerServiceImpl, wpfTextView);
			return GlyphTextViewMarkerClassificationTagger.GetOrCreate(service) as ITagger<T>;
		}
	}

	sealed class GlyphTextViewMarkerClassificationTagger : ITagger<IClassificationTag> {
		readonly GlyphTextViewMarkerService service;

		public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

		public GlyphTextViewMarkerClassificationTagger(GlyphTextViewMarkerService service) => this.service = service;

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) =>
			service.GetClassificationTags(spans);

		public static GlyphTextViewMarkerClassificationTagger GetOrCreate(GlyphTextViewMarkerService service) {
			if (service is null)
				throw new ArgumentNullException(nameof(service));
			return service.TextView.TextBuffer.Properties.GetOrCreateSingletonProperty(typeof(GlyphTextViewMarkerClassificationTagger), () => new GlyphTextViewMarkerClassificationTagger(service));
		}

		public void RaiseTagsChanged(SnapshotSpan span) {
			if (span.Snapshot is null)
				throw new ArgumentException();
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
		}
	}
}
