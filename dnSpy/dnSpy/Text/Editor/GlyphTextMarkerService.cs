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
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	interface IGlyphTextMarkerImpl : IGlyphTextMarker {
		Func<ITextView, bool> TextViewFilter { get; }
		IGlyphTextMarkerHandler Handler { get; }
	}

	interface IGlyphTextMethodMarkerImpl : IGlyphTextMethodMarker, IGlyphTextMarkerImpl {
	}

	interface IGlyphTextDotNetTokenMarkerImpl : IGlyphTextDotNetTokenMarker, IGlyphTextMarkerImpl {
	}

	[Export(typeof(IGlyphTextMarkerService))]
	[Export(typeof(IGlyphTextMarkerServiceImpl))]
	sealed class GlyphTextMarkerService : IGlyphTextMarkerServiceImpl {
		public event EventHandler<GlyphTextMarkerAddedEventArgs>? MarkerAdded;
		public event EventHandler<GlyphTextMarkerRemovedEventArgs>? MarkerRemoved;
		public event EventHandler<GlyphTextMarkersRemovedEventArgs>? MarkersRemoved;
		public event EventHandler<GetGlyphTextMarkerAndSpanEventArgs>? GetGlyphTextMarkerAndSpan;

#pragma warning disable CS0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDsAdornmentLayers.GlyphTextMarker)]
		[Order(After = PredefinedDsAdornmentLayers.BottomLayer, Before = PredefinedDsAdornmentLayers.TopLayer)]
		[Order(Before = PredefinedAdornmentLayers.Selection, After = PredefinedAdornmentLayers.Outlining)]
		[Order(Before = PredefinedAdornmentLayers.TextMarker)]
		static AdornmentLayerDefinition? glyphTextMarkerAdornmentLayerDefinition;
#pragma warning restore CS0169

		public IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; }
		public IEditorFormatMapService EditorFormatMapService { get; }
		public IEnumerable<IGlyphTextMarkerImpl> AllMarkers => glyphTextMarkers;
		public Lazy<IGlyphTextMarkerMouseProcessorProvider, IGlyphTextMarkerMouseProcessorProviderMetadata>[] GlyphTextMarkerMouseProcessorProviders { get; }

		readonly HashSet<IGlyphTextMarkerImpl> glyphTextMarkers;

		[ImportingConstructor]
		GlyphTextMarkerService(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, [ImportMany] IEnumerable<Lazy<IGlyphTextMarkerMouseProcessorProvider, IGlyphTextMarkerMouseProcessorProviderMetadata>> glyphTextMarkerMouseProcessorProviders) {
			ViewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			EditorFormatMapService = editorFormatMapService;
			glyphTextMarkers = new HashSet<IGlyphTextMarkerImpl>();
			GlyphTextMarkerMouseProcessorProviders = Orderer.Order(glyphTextMarkerMouseProcessorProviders).ToArray();
		}

		IGlyphTextMethodMarker IGlyphTextMarkerService.AddMarker(ModuleTokenId tokenId, uint ilOffset, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag, IGlyphTextMarkerHandler? handler, Func<ITextView, bool>? textViewFilter) =>
			AddMarker(tokenId.Module, tokenId.Token, ilOffset, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);

		IGlyphTextMethodMarker AddMarker(ModuleId module, uint token, uint ilOffset, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag, IGlyphTextMarkerHandler? handler, Func<ITextView, bool>? textViewFilter) {
			var marker = new GlyphTextMethodMarker(module, token, ilOffset, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);
			glyphTextMarkers.Add(marker);
			MarkerAdded?.Invoke(this, new GlyphTextMarkerAddedEventArgs(marker));
			return marker;
		}

		IGlyphTextDotNetTokenMarker AddMarker(ModuleId module, uint token, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag, IGlyphTextMarkerHandler? handler, Func<ITextView, bool>? textViewFilter) {
			var marker = new GlyphTextDotNetTokenMarker(module, token, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);
			glyphTextMarkers.Add(marker);
			MarkerAdded?.Invoke(this, new GlyphTextMarkerAddedEventArgs(marker));
			return marker;
		}

		public IGlyphTextMarker AddMarker(GlyphTextMarkerLocationInfo location, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag, IGlyphTextMarkerHandler? handler, Func<ITextView, bool>? textViewFilter) {
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			switch (location) {
			case DotNetMethodBodyGlyphTextMarkerLocationInfo bodyLoc:
				return AddMarker(bodyLoc.Module, bodyLoc.Token, bodyLoc.ILOffset, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);
			case DotNetTokenGlyphTextMarkerLocationInfo tokenLoc:
				return AddMarker(tokenLoc.Module, tokenLoc.Token, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);
			default:
				throw new InvalidOperationException();
			}
		}

		sealed class NullGlyphTextMarkerHandler : IGlyphTextMarkerHandler {
			public static readonly NullGlyphTextMarkerHandler Instance = new NullGlyphTextMarkerHandler();
			IGlyphTextMarkerHandlerMouseProcessor? IGlyphTextMarkerHandler.MouseProcessor => null;
			FrameworkElement? IGlyphTextMarkerHandler.GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) => null;
			GlyphTextMarkerToolTip? IGlyphTextMarkerHandler.GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) => null;
			IEnumerable<GuidObject> IGlyphTextMarkerHandler.GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint) { yield break; }
		}

		sealed class GlyphTextMethodMarker : IGlyphTextMethodMarkerImpl {
			public ImageReference? GlyphImageReference { get; }
			public string? MarkerTypeName { get; }
			public string? SelectedMarkerTypeName { get; }
			public IClassificationType? ClassificationType { get; }
			public int ZIndex { get; }
			public object? Tag { get; }
			public IGlyphTextMarkerHandler Handler { get; }
			public Func<ITextView, bool> TextViewFilter { get; }
			public ModuleTokenId Method { get; }
			public uint ILOffset { get; }

			public GlyphTextMethodMarker(ModuleId module, uint token, uint ilOffset, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag, IGlyphTextMarkerHandler? handler, Func<ITextView, bool>? textViewFilter) {
				Method = new ModuleTokenId(module, token);
				ILOffset = ilOffset;
				GlyphImageReference = glyphImage is null || glyphImage.Value.IsDefault ? null : glyphImage;
				MarkerTypeName = markerTypeName;
				SelectedMarkerTypeName = selectedMarkerTypeName;
				ClassificationType = classificationType;
				ZIndex = zIndex;
				Tag = tag;
				Handler = handler ?? NullGlyphTextMarkerHandler.Instance;
				TextViewFilter = textViewFilter ?? defaultTextViewFilter;
			}
			static readonly Func<ITextView, bool> defaultTextViewFilter = a => true;
		}

		sealed class GlyphTextDotNetTokenMarker : IGlyphTextDotNetTokenMarkerImpl {
			public ImageReference? GlyphImageReference { get; }
			public string? MarkerTypeName { get; }
			public string? SelectedMarkerTypeName { get; }
			public IClassificationType? ClassificationType { get; }
			public int ZIndex { get; }
			public object? Tag { get; }
			public IGlyphTextMarkerHandler Handler { get; }
			public Func<ITextView, bool> TextViewFilter { get; }
			public ModuleId Module { get; }
			public uint Token { get; }

			public GlyphTextDotNetTokenMarker(ModuleId module, uint token, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag, IGlyphTextMarkerHandler? handler, Func<ITextView, bool>? textViewFilter) {
				Module = module;
				Token = token;
				GlyphImageReference = glyphImage is null || glyphImage.Value.IsDefault ? null : glyphImage;
				MarkerTypeName = markerTypeName;
				SelectedMarkerTypeName = selectedMarkerTypeName;
				ClassificationType = classificationType;
				ZIndex = zIndex;
				Tag = tag;
				Handler = handler ?? NullGlyphTextMarkerHandler.Instance;
				TextViewFilter = textViewFilter ?? defaultTextViewFilter;
			}
			static readonly Func<ITextView, bool> defaultTextViewFilter = a => true;
		}

		public void Remove(IGlyphTextMarker marker) {
			if (marker is null)
				throw new ArgumentNullException(nameof(marker));
			var markerImpl = (IGlyphTextMarkerImpl)marker;
			glyphTextMarkers.Remove(markerImpl);
			MarkerRemoved?.Invoke(this, new GlyphTextMarkerRemovedEventArgs(markerImpl));
		}

		public void Remove(IEnumerable<IGlyphTextMarker> markers) {
			if (markers is null)
				throw new ArgumentNullException(nameof(markers));
			var hash = new HashSet<IGlyphTextMarkerImpl>();
			foreach (var m in markers)
				hash.Add((IGlyphTextMarkerImpl)m);
			if (hash.Count == 0)
				return;
			foreach (var m in hash)
				glyphTextMarkers.Remove(m);
			MarkersRemoved?.Invoke(this, new GlyphTextMarkersRemovedEventArgs(hash));
		}

		public GlyphTextMarkerAndSpan[] GetMarkers(ITextView textView, SnapshotSpan span) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			if (textView.TextSnapshot != span.Snapshot)
				throw new ArgumentException();
			var e = new GetGlyphTextMarkerAndSpanEventArgs(textView, span);
			GetGlyphTextMarkerAndSpan?.Invoke(this, e);
			return e.Result ?? Array.Empty<GlyphTextMarkerAndSpan>();
		}

		public void SetDotNetSpanMap(ITextView textView, IDotNetSpanMap? map) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			var service = GlyphTextViewMarkerService.TryGet(textView);
			Debug2.Assert(!(service is null));
			service?.SetDotNetSpanMap(map);
		}
	}
}
