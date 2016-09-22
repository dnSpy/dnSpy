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
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Text.MEF;
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

	[Export(typeof(IGlyphTextMarkerService))]
	[Export(typeof(IGlyphTextMarkerServiceImpl))]
	sealed class GlyphTextMarkerService : IGlyphTextMarkerServiceImpl {
		public event EventHandler<GlyphTextMarkerAddedEventArgs> MarkerAdded;
		public event EventHandler<GlyphTextMarkerRemovedEventArgs> MarkerRemoved;
		public event EventHandler<GlyphTextMarkersRemovedEventArgs> MarkersRemoved;

#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDnSpyAdornmentLayers.GlyphTextMarker)]
		[Order(After = PredefinedDnSpyAdornmentLayers.BottomLayer, Before = PredefinedDnSpyAdornmentLayers.TopLayer)]
		[Order(Before = PredefinedAdornmentLayers.Selection, After = PredefinedAdornmentLayers.Outlining)]
		[Order(Before = PredefinedAdornmentLayers.TextMarker)]
		static AdornmentLayerDefinition glyphTextMarkerAdornmentLayerDefinition;
#pragma warning restore 0169

		public IThemeManager ThemeManager { get; }
		public IImageManager ImageManager { get; }
		public IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; }
		public IEditorFormatMapService EditorFormatMapService { get; }
		public IEnumerable<IGlyphTextMarkerImpl> AllMarkers => glyphTextMarkers;
		public Lazy<IGlyphTextMarkerMouseProcessorProvider, IGlyphTextMarkerMouseProcessorProviderMetadata>[] GlyphTextMarkerMouseProcessorProviders { get; }

		readonly IModuleIdProvider moduleIdProvider;
		readonly HashSet<IGlyphTextMarkerImpl> glyphTextMarkers;

		[ImportingConstructor]
		GlyphTextMarkerService(IModuleIdProvider moduleIdProvider, IThemeManager themeManager, IImageManager imageManager, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, IEditorFormatMapService editorFormatMapService, [ImportMany] IEnumerable<Lazy<IGlyphTextMarkerMouseProcessorProvider, IGlyphTextMarkerMouseProcessorProviderMetadata>> glyphTextMarkerMouseProcessorProviders) {
			this.moduleIdProvider = moduleIdProvider;
			ThemeManager = themeManager;
			ImageManager = imageManager;
			ViewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
			EditorFormatMapService = editorFormatMapService;
			this.glyphTextMarkers = new HashSet<IGlyphTextMarkerImpl>();
			this.GlyphTextMarkerMouseProcessorProviders = Orderer.Order(glyphTextMarkerMouseProcessorProviders).ToArray();
		}

		public IGlyphTextMethodMarker AddMarker(MethodDef method, uint ilOffset, ImageReference? glyphImage, string markerTypeName, string selectedMarkerTypeName, IClassificationType classificationType, int zIndex, object tag, IGlyphTextMarkerHandler handler, Func<ITextView, bool> textViewFilter) {
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			return AddMarker(new ModuleTokenId(moduleIdProvider.Create(method.Module), method.MDToken), ilOffset, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);
		}

		public IGlyphTextMethodMarker AddMarker(ModuleTokenId tokenId, uint ilOffset, ImageReference? glyphImage, string markerTypeName, string selectedMarkerTypeName, IClassificationType classificationType, int zIndex, object tag, IGlyphTextMarkerHandler handler, Func<ITextView, bool> textViewFilter) {
			var marker = new GlyphTextMethodMarker(tokenId, ilOffset, glyphImage, markerTypeName, selectedMarkerTypeName, classificationType, zIndex, tag, handler, textViewFilter);
			glyphTextMarkers.Add(marker);
			MarkerAdded?.Invoke(this, new GlyphTextMarkerAddedEventArgs(marker));
			return marker;
		}

		sealed class GlyphTextMethodMarker : IGlyphTextMethodMarkerImpl {
			public ImageReference? GlyphImageReference { get; }
			public string MarkerTypeName { get; }
			public string SelectedMarkerTypeName { get; }
			public IClassificationType ClassificationType { get; }
			public int ZIndex { get; }
			public object Tag { get; }
			public IGlyphTextMarkerHandler Handler { get; }
			public Func<ITextView, bool> TextViewFilter { get; }
			public ModuleTokenId Method { get; }
			public uint ILOffset { get; }

			public GlyphTextMethodMarker(ModuleTokenId method, uint ilOffset, ImageReference? glyphImage, string markerTypeName, string selectedMarkerTypeName, IClassificationType classificationType, int zIndex, object tag, IGlyphTextMarkerHandler handler, Func<ITextView, bool> textViewFilter) {
				Method = method;
				ILOffset = ilOffset;
				GlyphImageReference = glyphImage == null || glyphImage.Value.IsDefault ? null : glyphImage;
				MarkerTypeName = markerTypeName;
				SelectedMarkerTypeName = selectedMarkerTypeName;
				ClassificationType = classificationType;
				ZIndex = zIndex;
				Tag = tag;
				Handler = handler ?? NullGlyphTextMarkerHandler.Instance;
				TextViewFilter = textViewFilter ?? defaultTextViewFilter;
			}
			static readonly Func<ITextView, bool> defaultTextViewFilter = a => true;

			sealed class NullGlyphTextMarkerHandler : IGlyphTextMarkerHandler {
				public static readonly NullGlyphTextMarkerHandler Instance = new NullGlyphTextMarkerHandler();
				IGlyphTextMarkerHandlerMouseProcessor IGlyphTextMarkerHandler.MouseProcessor => null;
				FrameworkElement IGlyphTextMarkerHandler.GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) => null;
				string IGlyphTextMarkerHandler.GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) => null;
				IEnumerable<GuidObject> IGlyphTextMarkerHandler.GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint) { yield break; }
			}
		}

		public void Remove(IGlyphTextMarker marker) {
			if (marker == null)
				throw new ArgumentNullException(nameof(marker));
			var markerImpl = (IGlyphTextMarkerImpl)marker;
			glyphTextMarkers.Remove(markerImpl);
			MarkerRemoved?.Invoke(this, new GlyphTextMarkerRemovedEventArgs(markerImpl));
		}

		public void Remove(IEnumerable<IGlyphTextMarker> markers) {
			if (markers == null)
				throw new ArgumentNullException(nameof(markers));
			var hash = new HashSet<IGlyphTextMarkerImpl>();
			foreach (var m in markers)
				hash.Add((IGlyphTextMarkerImpl)m);
			foreach (var m in hash)
				glyphTextMarkers.Remove(m);
			MarkersRemoved?.Invoke(this, new GlyphTextMarkersRemovedEventArgs(hash));
		}

		public void SetMethodOffsetSpanMap(ITextView textView, IMethodOffsetSpanMap map) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var service = GlyphTextViewMarkerService.TryGet(textView);
			Debug.Assert(service != null);
			service?.SetMethodOffsetSpanMap(map);
		}
	}
}
