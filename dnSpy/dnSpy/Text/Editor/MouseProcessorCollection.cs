/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class MouseProcessorCollection : IDisposable {
		readonly UIElement mouseElement;
		readonly UIElement manipulationElement;
		readonly DefaultMouseProcessor defaultMouseProcessor;
		readonly IMouseProcessor[] mouseProcessors;
		readonly Func<MouseEventArgs, bool> allowEvent;
		static readonly Func<MouseEventArgs, bool> defaultAllowEvent = a => true;

		public MouseProcessorCollection(UIElement mouseElement, UIElement manipulationElement, DefaultMouseProcessor defaultMouseProcessor, IMouseProcessor[] mouseProcessors, Func<MouseEventArgs, bool> allowEvent) {
			this.mouseElement = mouseElement ?? throw new ArgumentNullException(nameof(mouseElement));
			this.manipulationElement = manipulationElement;
			this.defaultMouseProcessor = defaultMouseProcessor ?? throw new ArgumentNullException(nameof(defaultMouseProcessor));
			this.mouseProcessors = mouseProcessors ?? throw new ArgumentNullException(nameof(mouseProcessors));
			this.allowEvent = allowEvent ?? defaultAllowEvent;
			mouseElement.AddHandler(UIElement.QueryContinueDragEvent, new QueryContinueDragEventHandler(MouseElement_QueryContinueDrag), true);
			mouseElement.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(MouseElement_MouseWheel), true);
			mouseElement.AddHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler(MouseElement_MouseUp), true);
			mouseElement.AddHandler(UIElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(MouseElement_MouseRightButtonUp), true);
			mouseElement.AddHandler(UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(MouseElement_MouseRightButtonDown), true);
			mouseElement.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(MouseElement_MouseMove), true);
			mouseElement.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(MouseElement_MouseLeftButtonUp), true);
			mouseElement.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(MouseElement_MouseLeftButtonDown), true);
			mouseElement.AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(MouseElement_MouseLeave), true);
			mouseElement.AddHandler(UIElement.MouseEnterEvent, new MouseEventHandler(MouseElement_MouseEnter), true);
			mouseElement.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(MouseElement_MouseDown), true);
			mouseElement.AddHandler(UIElement.GiveFeedbackEvent, new GiveFeedbackEventHandler(MouseElement_GiveFeedback), true);
			mouseElement.AddHandler(UIElement.DropEvent, new DragEventHandler(MouseElement_Drop), true);
			mouseElement.AddHandler(UIElement.DragOverEvent, new DragEventHandler(MouseElement_DragOver), true);
			mouseElement.AddHandler(UIElement.DragLeaveEvent, new DragEventHandler(MouseElement_DragLeave), true);
			mouseElement.AddHandler(UIElement.DragEnterEvent, new DragEventHandler(MouseElement_DragEnter), true);
			if (manipulationElement != null) {
				manipulationElement.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>(ManipulationElement_TouchUp), true);
				manipulationElement.AddHandler(UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>(ManipulationElement_TouchDown), true);
				manipulationElement.AddHandler(UIElement.StylusSystemGestureEvent, new StylusSystemGestureEventHandler(ManipulationElement_StylusSystemGesture), true);
				manipulationElement.AddHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(ManipulationElement_ManipulationStarting), true);
				manipulationElement.AddHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(ManipulationElement_ManipulationInertiaStarting), true);
				manipulationElement.AddHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(ManipulationElement_ManipulationDelta), true);
				manipulationElement.AddHandler(UIElement.ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(ManipulationElement_ManipulationCompleted), true);
			}
		}

		void MouseElement_DragEnter(object sender, DragEventArgs e) {
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

		void MouseElement_DragLeave(object sender, DragEventArgs e) {
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

		void MouseElement_DragOver(object sender, DragEventArgs e) {
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

		void MouseElement_Drop(object sender, DragEventArgs e) {
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

		void MouseElement_GiveFeedback(object sender, GiveFeedbackEventArgs e) {
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

		void MouseElement_MouseDown(object sender, MouseButtonEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseEnter(object sender, MouseEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseLeave(object sender, MouseEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseMove(object sender, MouseEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseUp(object sender, MouseButtonEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (!allowEvent(e))
				return;
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

		void MouseElement_QueryContinueDrag(object sender, QueryContinueDragEventArgs e) {
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

		void ManipulationElement_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e) {
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

		void ManipulationElement_ManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
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

		void ManipulationElement_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e) {
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

		void ManipulationElement_ManipulationStarting(object sender, ManipulationStartingEventArgs e) {
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

		void ManipulationElement_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e) {
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

		void ManipulationElement_TouchDown(object sender, TouchEventArgs e) {
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

		void ManipulationElement_TouchUp(object sender, TouchEventArgs e) {
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
			mouseElement.RemoveHandler(UIElement.QueryContinueDragEvent, new QueryContinueDragEventHandler(MouseElement_QueryContinueDrag));
			mouseElement.RemoveHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(MouseElement_MouseWheel));
			mouseElement.RemoveHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler(MouseElement_MouseUp));
			mouseElement.RemoveHandler(UIElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(MouseElement_MouseRightButtonUp));
			mouseElement.RemoveHandler(UIElement.MouseRightButtonDownEvent, new MouseButtonEventHandler(MouseElement_MouseRightButtonDown));
			mouseElement.RemoveHandler(UIElement.MouseMoveEvent, new MouseEventHandler(MouseElement_MouseMove));
			mouseElement.RemoveHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(MouseElement_MouseLeftButtonUp));
			mouseElement.RemoveHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(MouseElement_MouseLeftButtonDown));
			mouseElement.RemoveHandler(UIElement.MouseLeaveEvent, new MouseEventHandler(MouseElement_MouseLeave));
			mouseElement.RemoveHandler(UIElement.MouseEnterEvent, new MouseEventHandler(MouseElement_MouseEnter));
			mouseElement.RemoveHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(MouseElement_MouseDown));
			mouseElement.RemoveHandler(UIElement.GiveFeedbackEvent, new GiveFeedbackEventHandler(MouseElement_GiveFeedback));
			mouseElement.RemoveHandler(UIElement.DropEvent, new DragEventHandler(MouseElement_Drop));
			mouseElement.RemoveHandler(UIElement.DragOverEvent, new DragEventHandler(MouseElement_DragOver));
			mouseElement.RemoveHandler(UIElement.DragLeaveEvent, new DragEventHandler(MouseElement_DragLeave));
			mouseElement.RemoveHandler(UIElement.DragEnterEvent, new DragEventHandler(MouseElement_DragEnter));
			if (manipulationElement != null) {
				manipulationElement.RemoveHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>(ManipulationElement_TouchUp));
				manipulationElement.RemoveHandler(UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>(ManipulationElement_TouchDown));
				manipulationElement.RemoveHandler(UIElement.StylusSystemGestureEvent, new StylusSystemGestureEventHandler(ManipulationElement_StylusSystemGesture));
				manipulationElement.RemoveHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(ManipulationElement_ManipulationStarting));
				manipulationElement.RemoveHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(ManipulationElement_ManipulationInertiaStarting));
				manipulationElement.RemoveHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(ManipulationElement_ManipulationDelta));
				manipulationElement.RemoveHandler(UIElement.ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(ManipulationElement_ManipulationCompleted));
			}
			foreach (var k in mouseProcessors)
				(k as IDisposable)?.Dispose();
		}
	}
}
