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

using System.Windows;
using System.Windows.Input;

namespace dnSpy.Text.Editor {
	class DefaultMouseProcessor {
		public virtual void OnDragEnter(object? sender, DragEventArgs e) { }
		public virtual void OnDragLeave(object? sender, DragEventArgs e) { }
		public virtual void OnDragOver(object? sender, DragEventArgs e) { }
		public virtual void OnDrop(object? sender, DragEventArgs e) { }
		public virtual void OnGiveFeedback(object? sender, GiveFeedbackEventArgs e) { }
		public virtual void OnMouseDown(object? sender, MouseButtonEventArgs e) { }
		public virtual void OnMouseEnter(object? sender, MouseEventArgs e) { }
		public virtual void OnMouseLeave(object? sender, MouseEventArgs e) { }
		public virtual void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e) { }
		public virtual void OnMouseLeftButtonUp(object? sender, MouseButtonEventArgs e) { }
		public virtual void OnMouseMove(object? sender, MouseEventArgs e) { }
		public virtual void OnMouseRightButtonDown(object? sender, MouseButtonEventArgs e) { }
		public virtual void OnMouseRightButtonUp(object? sender, MouseButtonEventArgs e) { }
		public virtual void OnMouseUp(object? sender, MouseButtonEventArgs e) { }
		public virtual void OnMouseWheel(object? sender, MouseWheelEventArgs e) { }
		public virtual void OnQueryContinueDrag(object? sender, QueryContinueDragEventArgs e) { }
		public virtual void OnManipulationCompleted(object? sender, ManipulationCompletedEventArgs e) { }
		public virtual void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e) { }
		public virtual void OnManipulationInertiaStarting(object? sender, ManipulationInertiaStartingEventArgs e) { }
		public virtual void OnManipulationStarting(object? sender, ManipulationStartingEventArgs e) { }
		public virtual void OnStylusSystemGesture(object? sender, StylusSystemGestureEventArgs e) { }
		public virtual void OnTouchDown(object? sender, TouchEventArgs e) { }
		public virtual void OnTouchUp(object? sender, TouchEventArgs e) { }
	}
}
