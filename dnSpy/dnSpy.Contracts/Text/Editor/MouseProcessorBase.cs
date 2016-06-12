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

using System.Windows;
using System.Windows.Input;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IMouseProcessor"/> base class
	/// </summary>
	public abstract class MouseProcessorBase : IMouseProcessor {
		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDragEnter(DragEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDragLeave(DragEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDragOver(DragEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDrop(DragEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessGiveFeedback(GiveFeedbackEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseEnter(MouseEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseLeave(MouseEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseMove(MouseEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseRightButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseRightButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseWheel(MouseWheelEventArgs e) { }

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessQueryContinueDrag(QueryContinueDragEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDragEnter(DragEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDragLeave(DragEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDragOver(DragEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDrop(DragEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessGiveFeedback(GiveFeedbackEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseEnter(MouseEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseLeave(MouseEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseMove(MouseEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseRightButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseRightButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseWheel(MouseWheelEventArgs e) { }

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessQueryContinueDrag(QueryContinueDragEventArgs e) { }
	}
}
