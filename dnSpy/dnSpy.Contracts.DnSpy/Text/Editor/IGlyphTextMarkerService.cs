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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text marker location base class
	/// </summary>
	public abstract class GlyphTextMarkerLocationInfo {
	}

	/// <summary>
	/// Method text marker location info
	/// </summary>
	public sealed class DotNetMethodBodyGlyphTextMarkerLocationInfo : GlyphTextMarkerLocationInfo {
		/// <summary>
		/// Module
		/// </summary>
		public ModuleId Module { get; }

		/// <summary>
		/// Token of method
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// Method offset
		/// </summary>
		public uint ILOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of method</param>
		/// <param name="ilOffset">Method offset</param>
		public DotNetMethodBodyGlyphTextMarkerLocationInfo(ModuleId module, uint token, uint ilOffset) {
			Module = module;
			Token = token;
			ILOffset = ilOffset;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of method</param>
		/// <param name="ilOffset">Method offset</param>
		public DotNetMethodBodyGlyphTextMarkerLocationInfo(ModuleId module, int token, uint ilOffset) {
			Module = module;
			Token = (uint)token;
			ILOffset = ilOffset;
		}
	}

	/// <summary>
	/// Method text marker location info
	/// </summary>
	public sealed class DotNetTokenGlyphTextMarkerLocationInfo : GlyphTextMarkerLocationInfo {
		/// <summary>
		/// Module
		/// </summary>
		public ModuleId Module { get; }

		/// <summary>
		/// Token of definition (type, method, field, property, event)
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of definition (type, method, field, property, event)</param>
		public DotNetTokenGlyphTextMarkerLocationInfo(ModuleId module, uint token) {
			Module = module;
			Token = token;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of definition (type, method, field, property, event)</param>
		public DotNetTokenGlyphTextMarkerLocationInfo(ModuleId module, int token) {
			Module = module;
			Token = (uint)token;
		}
	}

	/// <summary>
	/// Marks text and shows a glyph in the glyph margin
	/// </summary>
	public interface IGlyphTextMarkerService {
		/// <summary>
		/// Should be called whenever <paramref name="textView"/> gets a new <see cref="IDotNetSpanMap"/>
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="map">New map or null if none</param>
		void SetDotNetSpanMap(ITextView textView, IDotNetSpanMap? map);

		/// <summary>
		/// Adds a marker
		/// </summary>
		/// <param name="tokenId">Method token</param>
		/// <param name="ilOffset">Method offset</param>
		/// <param name="glyphImage">Image shown in the glyph margin or null if none</param>
		/// <param name="markerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) or null. It should have a background color and an optional foreground color for the border</param>
		/// <param name="selectedMarkerTypeName">Name of a <see cref="MarkerFormatDefinition"/> or null. It's used whenever the caret is inside the text marker.</param>
		/// <param name="classificationType">Classification type or null. Only the foreground color is needed. If it has a background color, it will hide the text markers shown in the text marker layer (eg. search result, highlighted reference)</param>
		/// <param name="zIndex">Z-index of <paramref name="glyphImage"/> and <paramref name="markerTypeName"/>, eg. <see cref="GlyphTextMarkerServiceZIndexes.EnabledBreakpoint"/></param>
		/// <param name="tag">User data</param>
		/// <param name="handler">Glyph handler or null</param>
		/// <param name="textViewFilter">Filters out non-supported text views</param>
		/// <returns></returns>
		IGlyphTextMethodMarker AddMarker(ModuleTokenId tokenId, uint ilOffset, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag = null, IGlyphTextMarkerHandler? handler = null, Func<ITextView, bool>? textViewFilter = null);

		/// <summary>
		/// Adds a marker
		/// </summary>
		/// <param name="location">Location</param>
		/// <param name="glyphImage">Image shown in the glyph margin or null if none</param>
		/// <param name="markerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) or null. It should have a background color and an optional foreground color for the border</param>
		/// <param name="selectedMarkerTypeName">Name of a <see cref="MarkerFormatDefinition"/> or null. It's used whenever the caret is inside the text marker.</param>
		/// <param name="classificationType">Classification type or null. Only the foreground color is needed. If it has a background color, it will hide the text markers shown in the text marker layer (eg. search result, highlighted reference)</param>
		/// <param name="zIndex">Z-index of <paramref name="glyphImage"/> and <paramref name="markerTypeName"/>, eg. <see cref="GlyphTextMarkerServiceZIndexes.EnabledBreakpoint"/></param>
		/// <param name="tag">User data</param>
		/// <param name="handler">Glyph handler or null</param>
		/// <param name="textViewFilter">Filters out non-supported text views</param>
		/// <returns></returns>
		IGlyphTextMarker AddMarker(GlyphTextMarkerLocationInfo location, ImageReference? glyphImage, string? markerTypeName, string? selectedMarkerTypeName, IClassificationType? classificationType, int zIndex, object? tag = null, IGlyphTextMarkerHandler? handler = null, Func<ITextView, bool>? textViewFilter = null);

		/// <summary>
		/// Removes a marker
		/// </summary>
		/// <param name="marker">Marker to remove</param>
		void Remove(IGlyphTextMarker marker);

		/// <summary>
		/// Removes markers
		/// </summary>
		/// <param name="markers">Markers to remove</param>
		void Remove(IEnumerable<IGlyphTextMarker> markers);

		/// <summary>
		/// Gets markers
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="span">Span</param>
		/// <returns></returns>
		GlyphTextMarkerAndSpan[] GetMarkers(ITextView textView, SnapshotSpan span);
	}

	/// <summary>
	/// Marker and its span in a <see cref="ITextView"/>
	/// </summary>
	public readonly struct GlyphTextMarkerAndSpan {
		/// <summary>
		/// Gets the marker
		/// </summary>
		public IGlyphTextMarker Marker { get; }

		/// <summary>
		/// Gets the span of the marker in the <see cref="ITextView"/>
		/// </summary>
		public SnapshotSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="marker">Marker</param>
		/// <param name="span">Span of the marker in the <see cref="ITextView"/></param>
		public GlyphTextMarkerAndSpan(IGlyphTextMarker marker, SnapshotSpan span) {
			if (span.Snapshot is null)
				throw new ArgumentException();
			Marker = marker ?? throw new ArgumentNullException(nameof(marker));
			Span = span;
		}
	}

	/// <summary>
	/// A marker created by <see cref="IGlyphTextMarkerService"/>
	/// </summary>
	public interface IGlyphTextMarker {
		/// <summary>
		/// Gets the image reference shown in the glyph margin or null if none
		/// </summary>
		ImageReference? GlyphImageReference { get; }

		/// <summary>
		/// Gets the name of the marker format definition or null if none
		/// </summary>
		string? MarkerTypeName { get; }

		/// <summary>
		/// Gets the name of the marker format definition to use whenever the caret is inside the span; it can be null
		/// </summary>
		string? SelectedMarkerTypeName { get; }

		/// <summary>
		/// Gets the classification type or null if none
		/// </summary>
		IClassificationType? ClassificationType { get; }

		/// <summary>
		/// Gets the z-index of <see cref="GlyphImageReference"/> and <see cref="MarkerTypeName"/>, eg. <see cref="GlyphTextMarkerServiceZIndexes.EnabledBreakpoint"/>
		/// </summary>
		int ZIndex { get; }

		/// <summary>
		/// Gets the user data
		/// </summary>
		object? Tag { get; }
	}

	/// <summary>
	/// A method marker created by <see cref="IGlyphTextMarkerService"/>
	/// </summary>
	public interface IGlyphTextMethodMarker : IGlyphTextMarker {
		/// <summary>
		/// Gets the method token
		/// </summary>
		ModuleTokenId Method { get; }

		/// <summary>
		/// Gets the IL offset
		/// </summary>
		uint ILOffset { get; }
	}

	/// <summary>
	/// A method marker created by <see cref="IGlyphTextMarkerService"/>
	/// </summary>
	public interface IGlyphTextDotNetTokenMarker : IGlyphTextMarker {
		/// <summary>
		/// Gets the module
		/// </summary>
		ModuleId Module { get; }

		/// <summary>
		/// Gets the token
		/// </summary>
		uint Token { get; }
	}

	/// <summary>
	/// Converts .NET tokens to spans
	/// </summary>
	public interface IDotNetSpanMap {
		/// <summary>
		/// Converts a method offset to a <see cref="Span"/> or returns null if the IL offset isn't present in the document
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of method</param>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		Span? ToSpan(ModuleId module, uint token, uint ilOffset);

		/// <summary>
		/// Converts a .NET module + token to a <see cref="Span"/> or returns null if the definition isn't present in the document
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of definition (type, method, field, property, event)</param>
		/// <returns></returns>
		Span? ToSpan(ModuleId module, uint token);
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerService"/> text marker tag
	/// </summary>
	public interface IGlyphTextMarkerTag : ITag {
		/// <summary>
		/// Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) (<see cref="IGlyphTextMarker.MarkerTypeName"/>)
		/// </summary>
		string MarkerTypeName { get; }

		/// <summary>
		/// Gets the name of the marker format definition to use whenever the caret is inside the span; it can be null (<see cref="IGlyphTextMarker.SelectedMarkerTypeName"/>)
		/// </summary>
		string? SelectedMarkerTypeName { get; }

		/// <summary>
		/// Gets the Z-index (<see cref="IGlyphTextMarker.ZIndex"/>)
		/// </summary>
		int ZIndex { get; }
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerService"/> text marker tag
	/// </summary>
	public sealed class GlyphTextMarkerTag : IGlyphTextMarkerTag {
		/// <summary>
		/// Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) (<see cref="IGlyphTextMarker.MarkerTypeName"/>)
		/// </summary>
		public string MarkerTypeName { get; }

		/// <summary>
		/// Gets the name of the marker format definition to use whenever the caret is inside the span; it can be null (<see cref="IGlyphTextMarker.SelectedMarkerTypeName"/>)
		/// </summary>
		public string? SelectedMarkerTypeName { get; }

		/// <summary>
		/// Gets the Z-index (<see cref="IGlyphTextMarker.ZIndex"/>)
		/// </summary>
		public int ZIndex { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="markerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) (<see cref="IGlyphTextMarker.MarkerTypeName"/>)</param>
		/// <param name="selectedMarkerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) (<see cref="IGlyphTextMarker.SelectedMarkerTypeName"/>)</param>
		/// <param name="zIndex">Z-index of this text marker (<see cref="IGlyphTextMarker.ZIndex"/>)</param>
		public GlyphTextMarkerTag(string markerTypeName, string? selectedMarkerTypeName, int zIndex) {
			MarkerTypeName = markerTypeName ?? throw new ArgumentNullException(nameof(markerTypeName));
			SelectedMarkerTypeName = selectedMarkerTypeName;
			ZIndex = zIndex;
		}
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerService"/> glyph tag
	/// </summary>
	public interface IGlyphTextMarkerGlyphTag : IGlyphTag {
		/// <summary>
		/// Gets the image reference (<see cref="IGlyphTextMarker.GlyphImageReference"/>)
		/// </summary>
		ImageReference ImageReference { get; }

		/// <summary>
		/// Gets the Z-index (<see cref="IGlyphTextMarker.ZIndex"/>)
		/// </summary>
		int ZIndex { get; }
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerService"/> glyph tag
	/// </summary>
	public sealed class GlyphTextMarkerGlyphTag : IGlyphTag {
		/// <summary>
		/// Image reference (<see cref="IGlyphTextMarker.GlyphImageReference"/>)
		/// </summary>
		public ImageReference ImageReference { get; }

		/// <summary>
		/// Z-index (<see cref="IGlyphTextMarker.ZIndex"/>)
		/// </summary>
		public int ZIndex { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageReference">Image reference (<see cref="IGlyphTextMarker.GlyphImageReference"/>)</param>
		/// <param name="zIndex">Z-index (<see cref="IGlyphTextMarker.ZIndex"/>)</param>
		public GlyphTextMarkerGlyphTag(ImageReference imageReference, int zIndex) {
			ImageReference = imageReference;
			ZIndex = zIndex;
		}
	}
}
