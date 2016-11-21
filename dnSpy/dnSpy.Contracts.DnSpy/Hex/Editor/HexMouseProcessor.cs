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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Mouse processor
	/// </summary>
	public abstract class HexMouseProcessor {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexMouseProcessor() { }

		/// <summary>
		/// Mouse left button down preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseLeftButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse left button down postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse right button down preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseRightButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse right button down postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseRightButtonDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse left button up preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseLeftButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse left button up postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseLeftButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Right button up preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseRightButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Right button up postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseRightButtonUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse up preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse up postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseUp(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse down preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse down postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseDown(MouseButtonEventArgs e) { }

		/// <summary>
		/// Mouse move preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseMove(MouseEventArgs e) { }

		/// <summary>
		/// Mouse move postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseMove(MouseEventArgs e) { }

		/// <summary>
		/// Mouse wheel preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseWheel(MouseWheelEventArgs e) { }

		/// <summary>
		/// Mouse wheel postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseWheel(MouseWheelEventArgs e) { }

		/// <summary>
		/// Mouse enter preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseEnter(MouseEventArgs e) { }

		/// <summary>
		/// Mouse enter postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseEnter(MouseEventArgs e) { }

		/// <summary>
		/// Mouse leave preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessMouseLeave(MouseEventArgs e) { }

		/// <summary>
		/// Mouse leave postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessMouseLeave(MouseEventArgs e) { }

		/// <summary>
		/// Drag leave preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDragLeave(DragEventArgs e) { }

		/// <summary>
		/// Drag leave postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDragLeave(DragEventArgs e) { }

		/// <summary>
		/// Drag over preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDragOver(DragEventArgs e) { }

		/// <summary>
		/// Drag over postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDragOver(DragEventArgs e) { }

		/// <summary>
		/// Drag enter preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDragEnter(DragEventArgs e) { }

		/// <summary>
		/// Drag enter postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDragEnter(DragEventArgs e) { }

		/// <summary>
		/// Drop preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessDrop(DragEventArgs e) { }

		/// <summary>
		/// Drop postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessDrop(DragEventArgs e) { }

		/// <summary>
		/// Query continue drag preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessQueryContinueDrag(QueryContinueDragEventArgs e) { }

		/// <summary>
		/// Query continue drag postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessQueryContinueDrag(QueryContinueDragEventArgs e) { }

		/// <summary>
		/// Give feedback preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessGiveFeedback(GiveFeedbackEventArgs e) { }

		/// <summary>
		/// Give feedback postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessGiveFeedback(GiveFeedbackEventArgs e) { }

		/// <summary>
		/// Touch down preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessTouchDown(TouchEventArgs e) { }

		/// <summary>
		/// Touch down postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessTouchDown(TouchEventArgs e) { }

		/// <summary>
		/// Touch up preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessTouchUp(TouchEventArgs e) { }

		/// <summary>
		/// Touch up postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessTouchUp(TouchEventArgs e) { }

		/// <summary>
		/// Stylus system gesture preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessStylusSystemGesture(StylusSystemGestureEventArgs e) { }

		/// <summary>
		/// Stylus system gesture postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessStylusSystemGesture(StylusSystemGestureEventArgs e) { }

		/// <summary>
		/// Manipulation inertia starting preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e) { }

		/// <summary>
		/// Manipulation inertia starting postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e) { }

		/// <summary>
		/// Manipulation starting preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessManipulationStarting(ManipulationStartingEventArgs e) { }

		/// <summary>
		/// Manipulation starting postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessManipulationStarting(ManipulationStartingEventArgs e) { }

		/// <summary>
		/// Manipulation delta preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessManipulationDelta(ManipulationDeltaEventArgs e) { }

		/// <summary>
		/// Manipulation delta postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessManipulationDelta(ManipulationDeltaEventArgs e) { }

		/// <summary>
		/// Manipulation completed preprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PreprocessManipulationCompleted(ManipulationCompletedEventArgs e) { }

		/// <summary>
		/// Manipulation completed postprocess handler
		/// </summary>
		/// <param name="e">Event args</param>
		public virtual void PostprocessManipulationCompleted(ManipulationCompletedEventArgs e) { }
	}
}
