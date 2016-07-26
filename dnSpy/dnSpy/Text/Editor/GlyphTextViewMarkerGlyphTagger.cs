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
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[TextViewRole(PredefinedDnSpyTextViewRoles.GlyphTextMarkerServiceable)]
	[TagType(typeof(GlyphTextMarkerGlyphTag))]
	sealed class GlyphTextViewMarkerGlyphTaggerProvider : IViewTaggerProvider {
		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;

		[ImportingConstructor]
		GlyphTextViewMarkerGlyphTaggerProvider(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl) {
			this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl;
		}

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			var wpfTextView = textView as IWpfTextView;
			Debug.Assert(wpfTextView != null);
			if (wpfTextView == null)
				return null;
			if (textView.TextBuffer != buffer)
				return null;
			var service = GlyphTextViewMarkerService.GetOrCreate(glyphTextMarkerServiceImpl, wpfTextView);
			return GlyphTextViewMarkerGlyphTagger.GetOrCreate(service) as ITagger<T>;
		}
	}

	sealed class GlyphTextViewMarkerGlyphTagger : ITagger<GlyphTextMarkerGlyphTag> {
		readonly GlyphTextViewMarkerService service;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public GlyphTextViewMarkerGlyphTagger(GlyphTextViewMarkerService service) {
			this.service = service;
		}

		public IEnumerable<ITagSpan<GlyphTextMarkerGlyphTag>> GetTags(NormalizedSnapshotSpanCollection spans) =>
			service.GlyphTextViewMarkerGlyphTags(spans);

		public static GlyphTextViewMarkerGlyphTagger GetOrCreate(GlyphTextViewMarkerService service) {
			if (service == null)
				throw new ArgumentNullException(nameof(service));
			return service.TextView.TextBuffer.Properties.GetOrCreateSingletonProperty(typeof(GlyphTextViewMarkerGlyphTagger), () => new GlyphTextViewMarkerGlyphTagger(service));
		}

		public void RaiseTagsChanged(SnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
		}
	}

	[Export(typeof(IGlyphFactoryProvider))]
	[Name("dnSpy-GlyphTextViewMarkerGlyphFactoryProvider")]
	[TagType(typeof(GlyphTextMarkerGlyphTag))]
	[ContentType(ContentTypes.Text)]
	[Order(After = Priority.Default)]
	sealed class GlyphTextViewMarkerGlyphFactoryProvider : IGlyphFactoryProvider {
		readonly IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl;

		[ImportingConstructor]
		GlyphTextViewMarkerGlyphFactoryProvider(IGlyphTextMarkerServiceImpl glyphTextMarkerServiceImpl) {
			this.glyphTextMarkerServiceImpl = glyphTextMarkerServiceImpl;
		}

		static readonly string[] textViewRoles = new string[] {
			PredefinedTextViewRoles.Interactive,
			PredefinedDnSpyTextViewRoles.GlyphTextMarkerServiceable,
		};

		public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin) {
			if (!view.Roles.ContainsAny(textViewRoles))
				return null;
			var service = GlyphTextViewMarkerService.GetOrCreate(glyphTextMarkerServiceImpl, view);
			return new GlyphTextViewMarkerGlyphFactory(service);
		}
	}

	sealed class GlyphTextViewMarkerGlyphFactory : IGlyphFactory {
		readonly GlyphTextViewMarkerService service;

		public GlyphTextViewMarkerGlyphFactory(GlyphTextViewMarkerService service) {
			this.service = service;
		}

		public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag) {
			var glyphTag = tag as GlyphTextMarkerGlyphTag;
			if (glyphTag == null)
				return null;

			return service.GenerateGlyph(line, glyphTag);
		}
	}
}
