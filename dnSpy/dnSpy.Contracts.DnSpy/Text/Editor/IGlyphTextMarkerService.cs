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
using dnlib.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Marks text and shows a glyph in the glyph margin
	/// </summary>
	public interface IGlyphTextMarkerService {
		/// <summary>
		/// Should be called whenever <paramref name="textView"/> gets a new <see cref="IMethodOffsetSpanMap"/>
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="map">New map or null if none</param>
		void SetMethodOffsetSpanMap(ITextView textView, IMethodOffsetSpanMap map);

		/// <summary>
		/// Adds a marker
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="ilOffset">Method offset</param>
		/// <param name="glyphImage">Image shown in the glyph margin or null if none</param>
		/// <param name="markerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) or null. It should have a background color and an optional foreground color for the border</param>
		/// <param name="classificationType">Classification type or null. Only the foreground color is needed. If it has a background color, it will hide the text markers shown in the text marker layer (eg. search result, highlighted reference)</param>
		/// <param name="zIndex">Z-index of <paramref name="glyphImage"/> and <paramref name="markerTypeName"/></param>
		/// <param name="tag">User data</param>
		/// <param name="handler">Glyph handler or null</param>
		/// <param name="textViewFilter">Filters out non-supported text views</param>
		/// <returns></returns>
		IGlyphTextMethodMarker AddMarker(MethodDef method, uint ilOffset, ImageReference? glyphImage, string markerTypeName, IClassificationType classificationType, int zIndex, object tag = null, IGlyphTextMarkerHandler handler = null, Func<ITextView, bool> textViewFilter = null);

		/// <summary>
		/// Adds a marker
		/// </summary>
		/// <param name="tokenId">Method token</param>
		/// <param name="ilOffset">Method offset</param>
		/// <param name="glyphImage">Image shown in the glyph margin or null if none</param>
		/// <param name="markerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) or null. It should have a background color and an optional foreground color for the border</param>
		/// <param name="classificationType">Classification type or null. Only the foreground color is needed. If it has a background color, it will hide the text markers shown in the text marker layer (eg. search result, highlighted reference)</param>
		/// <param name="zIndex">Z-index of <paramref name="glyphImage"/> and <paramref name="markerTypeName"/></param>
		/// <param name="tag">User data</param>
		/// <param name="handler">Glyph handler or null</param>
		/// <param name="textViewFilter">Filters out non-supported text views</param>
		/// <returns></returns>
		IGlyphTextMethodMarker AddMarker(ModuleTokenId tokenId, uint ilOffset, ImageReference? glyphImage, string markerTypeName, IClassificationType classificationType, int zIndex, object tag = null, IGlyphTextMarkerHandler handler = null, Func<ITextView, bool> textViewFilter = null);

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
		string MarkerTypeName { get; }

		/// <summary>
		/// Gets the classification type or null if none
		/// </summary>
		IClassificationType ClassificationType { get; }

		/// <summary>
		/// Gets the z-index of <see cref="GlyphImageReference"/> and <see cref="MarkerTypeName"/>
		/// </summary>
		int ZIndex { get; }

		/// <summary>
		/// Gets the user data
		/// </summary>
		object Tag { get; }
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
	/// Converts method IL offsets to <see cref="Span"/>s
	/// </summary>
	public interface IMethodOffsetSpanMap {
		/// <summary>
		/// Converts a method offset to a <see cref="Span"/> or returns null if the IL offset isn't present in the document
		/// </summary>
		/// <param name="method">Method token</param>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		Span? ToSpan(ModuleTokenId method, uint ilOffset);
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
		/// Gets the Z-index (<see cref="IGlyphTextMarker.ZIndex"/>)
		/// </summary>
		public int ZIndex { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="markerTypeName">Name of a <see cref="MarkerFormatDefinition"/> (or an <see cref="EditorFormatDefinition"/>) (<see cref="IGlyphTextMarker.MarkerTypeName"/>)</param>
		/// <param name="zIndex">Z-index of this text marker (<see cref="IGlyphTextMarker.ZIndex"/>)</param>
		public GlyphTextMarkerTag(string markerTypeName, int zIndex) {
			if (markerTypeName == null)
				throw new ArgumentNullException(nameof(markerTypeName));
			MarkerTypeName = markerTypeName;
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
