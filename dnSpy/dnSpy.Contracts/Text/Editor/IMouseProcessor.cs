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
	/// Mouse processor, see also <see cref="IMouseProcessor2"/>
	/// </summary>
	public interface IMouseProcessor {
		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessDragEnter(DragEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessDragLeave(DragEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessDragOver(DragEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessDrop(DragEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessGiveFeedback(GiveFeedbackEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseDown(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseEnter(MouseEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseLeave(MouseEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseMove(MouseEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseRightButtonDown(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseRightButtonUp(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseUp(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessMouseWheel(MouseWheelEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessQueryContinueDrag(QueryContinueDragEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessDragEnter(DragEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessDragLeave(DragEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessDragOver(DragEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessDrop(DragEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessGiveFeedback(GiveFeedbackEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseDown(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseEnter(MouseEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseLeave(MouseEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseMove(MouseEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseRightButtonDown(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseRightButtonUp(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseUp(MouseButtonEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessMouseWheel(MouseWheelEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessQueryContinueDrag(QueryContinueDragEventArgs e);
	}
}
