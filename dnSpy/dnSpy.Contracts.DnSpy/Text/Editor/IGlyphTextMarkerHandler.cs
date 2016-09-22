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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Handles <see cref="IGlyphTextMarker"/> events
	/// </summary>
	public interface IGlyphTextMarkerHandler {
		/// <summary>
		/// Gets the mouse processor or null
		/// </summary>
		IGlyphTextMarkerHandlerMouseProcessor MouseProcessor { get; }

		/// <summary>
		/// Creates context menu objects
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="marginRelativePoint">Position of the mouse pointer relative to the glyph margin</param>
		/// <returns></returns>
		IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint);

		/// <summary>
		/// Gets the tool tip content or null if the next handler should be checked
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <returns></returns>
		GlyphTextMarkerToolTip GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker);

		/// <summary>
		/// Gets the popup content or null if the next handler should be checked. The popup content is
		/// shown above the line (eg. breakpoint toolbar settings popup)
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <returns></returns>
		FrameworkElement GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker);
	}

	/// <summary>
	/// Contains the tooltip content and style that is shown when hovering over the glyph in the glyph margin
	/// </summary>
	public sealed class GlyphTextMarkerToolTip {
		/// <summary>
		/// Tooltip content, a <see cref="string"/> or a UI element
		/// </summary>
		public object Content { get; }

		/// <summary>
		/// Tooltip style or null. Can be the key of a style in the resources (eg. a <see cref="string"/>) or a <see cref="System.Windows.Style"/> instance
		/// </summary>
		public object Style { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="content">Text content to show in the tooltip</param>
		/// <param name="style">Tooltip style or null. Can be the key of a style in the resources (eg. a <see cref="string"/>) or a <see cref="System.Windows.Style"/> instance</param>
		public GlyphTextMarkerToolTip(string content, object style = null) {
			Content = content;
			Style = style;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="content">Content to show in the tooltip</param>
		/// <param name="style">Tooltip style or null. Can be the key of a style in the resources (eg. a <see cref="string"/>) or a <see cref="System.Windows.Style"/> instance</param>
		public GlyphTextMarkerToolTip(object content, object style) {
			Content = content;
			Style = style;
		}
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerHandler"/> context
	/// </summary>
	public interface IGlyphTextMarkerHandlerContext {
		/// <summary>
		/// Gets the glyph margin
		/// </summary>
		IWpfTextViewMargin Margin { get; }

		/// <summary>
		/// Gets the text view host
		/// </summary>
		IWpfTextViewHost Host { get; }

		/// <summary>
		/// Gets the text view
		/// </summary>
		IWpfTextView TextView { get; }

		/// <summary>
		/// Gets the line
		/// </summary>
		IWpfTextViewLine Line { get; }
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerHandler"/> mouse processor (see also <see cref="GlyphTextMarkerHandlerMouseProcessorBase"/>)
	/// </summary>
	public interface IGlyphTextMarkerHandlerMouseProcessor {
		/// <summary>
		/// Mouse down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseDown(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseUp(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse left button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseLeftButtonDown(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse left button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseLeftButtonUp(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse right button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseRightButtonDown(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse right button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseRightButtonUp(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse move handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseMove(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseEventArgs e);
	}

	/// <summary>
	/// Abstract class implementing <see cref="IGlyphTextMarkerHandlerMouseProcessor"/>
	/// </summary>
	public abstract class GlyphTextMarkerHandlerMouseProcessorBase : IGlyphTextMarkerHandlerMouseProcessor {
		/// <summary>
		/// Constructor
		/// </summary>
		protected GlyphTextMarkerHandlerMouseProcessorBase() { }

		/// <summary>
		/// Default mouse down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseDown(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseUp(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse left button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseLeftButtonDown(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse left button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseLeftButtonUp(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse right button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseRightButtonDown(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse right button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseRightButtonUp(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse move handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marker">Marker</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseMove(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, MouseEventArgs e) { }
	}
}
