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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Editor {
	interface IGlyphTextMarkerServiceImpl : IGlyphTextMarkerService {
		IThemeManager ThemeManager { get; }
		IImageManager ImageManager { get; }
		IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; }
		IEditorFormatMapService EditorFormatMapService { get; }
		IEnumerable<IGlyphTextMarkerImpl> AllMarkers { get; }
		event EventHandler<GlyphTextMarkerAddedEventArgs> MarkerAdded;
		event EventHandler<GlyphTextMarkerRemovedEventArgs> MarkerRemoved;
		event EventHandler<GlyphTextMarkersRemovedEventArgs> MarkersRemoved;
	}

	abstract class GlyphTextMarkerEventArgs : EventArgs {
		public IGlyphTextMarkerImpl Marker { get; }

		protected GlyphTextMarkerEventArgs(IGlyphTextMarkerImpl marker) {
			if (marker == null)
				throw new ArgumentNullException(nameof(marker));
			Marker = marker;
		}
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

		public GlyphTextMarkersRemovedEventArgs(HashSet<IGlyphTextMarkerImpl> markers) {
			if (markers == null)
				throw new ArgumentNullException(nameof(markers));
			Markers = markers;
		}
	}
}
