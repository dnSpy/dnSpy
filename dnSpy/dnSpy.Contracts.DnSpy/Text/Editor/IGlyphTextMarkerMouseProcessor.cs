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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Creates <see cref="IGlyphTextMarkerMouseProcessor"/>s. You must <see cref="ExportAttribute"/>
	/// this interface with a <see cref="NameAttribute"/>. Optional attributes: <see cref="OrderAttribute"/>,
	/// <see cref="TextViewRoleAttribute"/>.
	/// </summary>
	public interface IGlyphTextMarkerMouseProcessorProvider {
		/// <summary>
		/// Creates a <see cref="IGlyphTextMarkerMouseProcessor"/> or returns null
		/// </summary>
		/// <param name="wpfTextViewHost">Text view host</param>
		/// <param name="margin">Margin</param>
		/// <returns></returns>
		IGlyphTextMarkerMouseProcessor GetAssociatedMouseProcessor(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin);
	}

	/// <summary>
	/// <see cref="IGlyphTextMarkerMouseProcessor"/> context
	/// </summary>
	public interface IGlyphTextMarkerMouseProcessorContext {
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

		/// <summary>
		/// Sorted markers shown in the glyph margin. The first marker is the top most marker.
		/// </summary>
		IGlyphTextMarker[] Markers { get; }

		/// <summary>
		/// Gets the span provider
		/// </summary>
		IGlyphTextMarkerSpanProvider SpanProvider { get; }
	}

	/// <summary>
	/// <see cref="IGlyphTextMarker"/> mouse processor (see also <see cref="GlyphTextMarkerMouseProcessorBase"/>)
	/// </summary>
	public interface IGlyphTextMarkerMouseProcessor {
		/// <summary>
		/// Creates context menu objects
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marginRelativePoint">Position of the mouse pointer relative to the glyph margin</param>
		/// <returns></returns>
		IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerMouseProcessorContext context, Point marginRelativePoint);

		/// <summary>
		/// Mouse down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse left button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseLeftButtonDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse left button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseLeftButtonUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse right button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseRightButtonDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse right button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseRightButtonUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e);

		/// <summary>
		/// Mouse move handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseMove(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e);

		/// <summary>
		/// Mouse enter handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseEnter(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e);

		/// <summary>
		/// Mouse leave handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		void OnMouseLeave(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e);
	}

	/// <summary>
	/// Abstract class implementing <see cref="IGlyphTextMarkerMouseProcessor"/>
	/// </summary>
	public abstract class GlyphTextMarkerMouseProcessorBase : IGlyphTextMarkerMouseProcessor {
		/// <summary>
		/// Constructor
		/// </summary>
		protected GlyphTextMarkerMouseProcessorBase() { }

		/// <summary>
		/// Creates context menu objects
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="marginRelativePoint">Position of the mouse pointer relative to the glyph margin</param>
		/// <returns></returns>
		public virtual IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerMouseProcessorContext context, Point marginRelativePoint) {
			yield break;
		}

		/// <summary>
		/// Default mouse down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse left button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseLeftButtonDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse left button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseLeftButtonUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse right button down handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseRightButtonDown(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse right button up handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseRightButtonUp(IGlyphTextMarkerMouseProcessorContext context, MouseButtonEventArgs e) { }

		/// <summary>
		/// Default mouse move handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseMove(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) { }

		/// <summary>
		/// Default mouse enter handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseEnter(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) { }

		/// <summary>
		/// Default mouse leave handler
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="e">Mouse event args</param>
		public virtual void OnMouseLeave(IGlyphTextMarkerMouseProcessorContext context, MouseEventArgs e) { }
	}
}
