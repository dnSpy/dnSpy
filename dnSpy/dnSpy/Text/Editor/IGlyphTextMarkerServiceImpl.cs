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
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Editor {
	interface IGlyphTextMarkerServiceImpl : IGlyphTextMarkerService {
		IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; }
		IEditorFormatMapService EditorFormatMapService { get; }
		IEnumerable<IGlyphTextMarkerImpl> AllMarkers { get; }
		Lazy<IGlyphTextMarkerMouseProcessorProvider, IGlyphTextMarkerMouseProcessorProviderMetadata>[] GlyphTextMarkerMouseProcessorProviders { get; }
		event EventHandler<GlyphTextMarkerAddedEventArgs> MarkerAdded;
		event EventHandler<GlyphTextMarkerRemovedEventArgs> MarkerRemoved;
		event EventHandler<GlyphTextMarkersRemovedEventArgs> MarkersRemoved;
		event EventHandler<GetGlyphTextMarkerAndSpanEventArgs> GetGlyphTextMarkerAndSpan;
	}

	abstract class GlyphTextMarkerEventArgs : EventArgs {
		public IGlyphTextMarkerImpl Marker { get; }

		protected GlyphTextMarkerEventArgs(IGlyphTextMarkerImpl marker) => Marker = marker ?? throw new ArgumentNullException(nameof(marker));
	}

	sealed class GlyphTextMarkerAddedEventArgs : GlyphTextMarkerEventArgs {
		public GlyphTextMarkerAddedEventArgs(IGlyphTextMarkerImpl marker)
			: base(marker) {
		}
	}

	sealed class GlyphTextMarkerRemovedEventArgs : GlyphTextMarkerEventArgs {
		public GlyphTextMarkerRemovedEventArgs(IGlyphTextMarkerImpl marker)
			: base(marker) {
		}
	}

	sealed class GlyphTextMarkersRemovedEventArgs : EventArgs {
		public HashSet<IGlyphTextMarkerImpl> Markers { get; }

		public GlyphTextMarkersRemovedEventArgs(HashSet<IGlyphTextMarkerImpl> markers) => Markers = markers ?? throw new ArgumentNullException(nameof(markers));
	}

	sealed class GetGlyphTextMarkerAndSpanEventArgs : EventArgs {
		public ITextView TextView { get; }
		public SnapshotSpan Span { get; }
		public GlyphTextMarkerAndSpan[] Result { get; set; }
		public GetGlyphTextMarkerAndSpanEventArgs(ITextView textView, SnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			TextView = textView ?? throw new ArgumentNullException(nameof(textView));
			Span = span;
		}
	}

	sealed class GetFirstGlyphTextMarkerAndSpanEventArgs : EventArgs {
		public ITextView TextView { get; }
		public SnapshotSpan Span { get; }
		public GlyphTextMarkerAndSpan? Result { get; set; }
		public GetFirstGlyphTextMarkerAndSpanEventArgs(ITextView textView, SnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			TextView = textView ?? throw new ArgumentNullException(nameof(textView));
			Span = span;
		}
	}
}
