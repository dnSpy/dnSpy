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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class MouseProcessorCollection : IDisposable {
		readonly UIElement mouseElement;
		readonly UIElement manipulationElem;
		readonly DefaultMouseProcessor defaultMouseProcessor;
		readonly IMouseProcessor[] mouseProcessors;

		public MouseProcessorCollection(UIElement mouseElement, UIElement manipulationElem, DefaultMouseProcessor defaultMouseProcessor, IMouseProcessor[] mouseProcessors) {
			if (mouseElement == null)
				throw new ArgumentNullException(nameof(mouseElement));
			if (defaultMouseProcessor == null)
				throw new ArgumentNullException(nameof(defaultMouseProcessor));
			if (mouseProcessors == null)
				throw new ArgumentNullException(nameof(mouseProcessors));
			this.mouseElement = mouseElement;
			this.manipulationElem = manipulationElem;
			this.defaultMouseProcessor = defaultMouseProcessor;
			this.mouseProcessors = mouseProcessors;
			mouseElement.AddHandler(UIElement.QueryContinueDragEvent, new QueryContinueDragEventHandler(VisualElement_QueryContinueDrag), true);
			mouseElement.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(VisualElement_MouseWheel), true);
			mouseElement.AddHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler(VisualElement_MouseUp), true);
			mouseElement.AddHandler(UIElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(VisualElement_MouseRightButtonUp), true);
			mouseElement.AddHandler(UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(VisualElement_MouseRightButtonDown), true);
			mouseElement.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(VisualElement_MouseMove), true);
			mouseElement.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(VisualElement_MouseLeftButtonUp), true);
			mouseElement.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(VisualElement_MouseLeftButtonDown), true);
			mouseElement.AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(VisualElement_MouseLeave), true);
			mouseElement.AddHandler(UIElement.MouseEnterEvent, new MouseEventHandler(VisualElement_MouseEnter), true);
			mouseElement.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(VisualElement_MouseDown), true);
			mouseElement.AddHandler(UIElement.GiveFeedbackEvent, new GiveFeedbackEventHandler(VisualElement_GiveFeedback), true);
			mouseElement.AddHandler(UIElement.DropEvent, new DragEventHandler(VisualElement_Drop), true);
			mouseElement.AddHandler(UIElement.DragOverEvent, new DragEventHandler(VisualElement_DragOver), true);
			mouseElement.AddHandler(UIElement.DragLeaveEvent, new DragEventHandler(VisualElement_DragLeave), true);
			mouseElement.AddHandler(UIElement.DragEnterEvent, new DragEventHandler(VisualElement_DragEnter), true);
			if (manipulationElem != null) {
				manipulationElem.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>(VisualElement_TouchUp), true);
				manipulationElem.AddHandler(UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>(VisualElement_TouchDown), true);
				manipulationElem.AddHandler(UIElement.StylusSystemGestureEvent, new StylusSystemGestureEventHandler(VisualElement_StylusSystemGesture), true);
				manipulationElem.AddHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(VisualElement_ManipulationStarting), true);
				manipulationElem.AddHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(VisualElement_ManipulationInertiaStarting), true);
				manipulationElem.AddHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(VisualElement_ManipulationDelta), true);
				manipulationElem.AddHandler(UIElement.ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(VisualElement_ManipulationCompleted), true);
			}
		}

		void VisualElement_DragEnter(object sender, DragEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessDragEnter(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnDragEnter(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessDragEnter(e);
		}

		void VisualElement_DragLeave(object sender, DragEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessDragLeave(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnDragLeave(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessDragLeave(e);
		}

		void VisualElement_DragOver(object sender, DragEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessDragOver(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnDragOver(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessDragOver(e);
		}

		void VisualElement_Drop(object sender, DragEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessDrop(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnDrop(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessDrop(e);
		}

		void VisualElement_GiveFeedback(object sender, GiveFeedbackEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessGiveFeedback(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnGiveFeedback(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessGiveFeedback(e);
		}

		bool TryFocusMouseElement(bool handled) {
			if (!mouseElement.Focusable)
				return false;
			if (!handled || !mouseElement.IsKeyboardFocusWithin) {
				mouseElement.Focus();
				return true;
			}
			return false;
		}

		void VisualElement_MouseDown(object sender, MouseButtonEventArgs e) {
			bool focused = e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right && TryFocusMouseElement(e.Handled);
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseDown(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseDown(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseDown(e);
			e.Handled |= focused;
		}

		void VisualElement_MouseEnter(object sender, MouseEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseEnter(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseEnter(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseEnter(e);
		}

		void VisualElement_MouseLeave(object sender, MouseEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseLeave(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseLeave(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseLeave(e);
		}

		void VisualElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			bool focused = TryFocusMouseElement(e.Handled);
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseLeftButtonDown(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseLeftButtonDown(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseLeftButtonDown(e);
			e.Handled |= focused;
		}

		void VisualElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseLeftButtonUp(e);
			}
			// Always call it, since it captures the mouse
			defaultMouseProcessor.OnMouseLeftButtonUp(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseLeftButtonUp(e);
		}

		void VisualElement_MouseMove(object sender, MouseEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseMove(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseMove(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseMove(e);
		}

		void VisualElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			bool focused = TryFocusMouseElement(e.Handled);
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseRightButtonDown(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseRightButtonDown(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseRightButtonDown(e);
			e.Handled |= focused;
		}

		void VisualElement_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseRightButtonUp(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseRightButtonUp(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseRightButtonUp(e);
		}

		void VisualElement_MouseUp(object sender, MouseButtonEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseUp(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseUp(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseUp(e);
		}

		void VisualElement_MouseWheel(object sender, MouseWheelEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessMouseWheel(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnMouseWheel(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessMouseWheel(e);
		}

		void VisualElement_QueryContinueDrag(object sender, QueryContinueDragEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				m.PreprocessQueryContinueDrag(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnQueryContinueDrag(sender, e);
			foreach (var m in mouseProcessors)
				m.PostprocessQueryContinueDrag(e);
		}

		void VisualElement_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessManipulationCompleted(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnManipulationCompleted(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessManipulationCompleted(e);
		}

		void VisualElement_ManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessManipulationDelta(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnManipulationDelta(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessManipulationDelta(e);
		}

		void VisualElement_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessManipulationInertiaStarting(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnManipulationInertiaStarting(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessManipulationInertiaStarting(e);
		}

		void VisualElement_ManipulationStarting(object sender, ManipulationStartingEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessManipulationStarting(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnManipulationStarting(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessManipulationStarting(e);
		}

		void VisualElement_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessStylusSystemGesture(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnStylusSystemGesture(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessStylusSystemGesture(e);
		}

		void VisualElement_TouchDown(object sender, TouchEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessTouchDown(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnTouchDown(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessTouchDown(e);
		}

		void VisualElement_TouchUp(object sender, TouchEventArgs e) {
			foreach (var m in mouseProcessors) {
				if (e.Handled)
					break;
				(m as IMouseProcessor2)?.PreprocessTouchUp(e);
			}
			if (!e.Handled)
				defaultMouseProcessor.OnTouchUp(sender, e);
			foreach (var m in mouseProcessors)
				(m as IMouseProcessor2)?.PostprocessTouchUp(e);
		}

		public void Dispose() {
			mouseElement.RemoveHandler(UIElement.QueryContinueDragEvent, new QueryContinueDragEventHandler(VisualElement_QueryContinueDrag));
			mouseElement.RemoveHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(VisualElement_MouseWheel));
			mouseElement.RemoveHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler(VisualElement_MouseUp));
			mouseElement.RemoveHandler(UIElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(VisualElement_MouseRightButtonUp));
			mouseElement.RemoveHandler(UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(VisualElement_MouseRightButtonDown));
			mouseElement.RemoveHandler(UIElement.MouseMoveEvent, new MouseEventHandler(VisualElement_MouseMove));
			mouseElement.RemoveHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(VisualElement_MouseLeftButtonUp));
			mouseElement.RemoveHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(VisualElement_MouseLeftButtonDown));
			mouseElement.RemoveHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(VisualElement_MouseLeave));
			mouseElement.RemoveHandler(UIElement.MouseEnterEvent, new MouseEventHandler(VisualElement_MouseEnter));
			mouseElement.RemoveHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(VisualElement_MouseDown));
			mouseElement.RemoveHandler(UIElement.GiveFeedbackEvent, new GiveFeedbackEventHandler(VisualElement_GiveFeedback));
			mouseElement.RemoveHandler(UIElement.DropEvent, new DragEventHandler(VisualElement_Drop));
			mouseElement.RemoveHandler(UIElement.DragOverEvent, new DragEventHandler(VisualElement_DragOver));
			mouseElement.RemoveHandler(UIElement.DragLeaveEvent, new DragEventHandler(VisualElement_DragLeave));
			mouseElement.RemoveHandler(UIElement.DragEnterEvent, new DragEventHandler(VisualElement_DragEnter));
			if (manipulationElem != null) {
				manipulationElem.RemoveHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>(VisualElement_TouchUp));
				manipulationElem.RemoveHandler(UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>(VisualElement_TouchDown));
				manipulationElem.RemoveHandler(UIElement.StylusSystemGestureEvent, new StylusSystemGestureEventHandler(VisualElement_StylusSystemGesture));
				manipulationElem.RemoveHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(VisualElement_ManipulationStarting));
				manipulationElem.RemoveHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(VisualElement_ManipulationInertiaStarting));
				manipulationElem.RemoveHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(VisualElement_ManipulationDelta));
				manipulationElem.RemoveHandler(UIElement.ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(VisualElement_ManipulationCompleted));
			}
			foreach (var k in mouseProcessors)
				(k as IDisposable)?.Dispose();
		}
	}
}
