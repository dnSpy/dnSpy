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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed partial class WpfTextView {
		sealed class MouseHoverHelper {
			readonly WpfTextView owner;
			readonly List<MouseHoverHandler> handlers;
			readonly DispatcherTimer timer;
			int? position;

			public MouseHoverHelper(WpfTextView owner) {
				this.owner = owner;
				handlers = new List<MouseHoverHandler>();
				timer = new DispatcherTimer(DispatcherPriority.Normal, owner.Dispatcher);
				timer.Tick += Timer_Tick;
				owner.MouseDown += WpfTextView_MouseDown;
				owner.MouseLeftButtonDown += WpfTextView_MouseLeftButtonDown;
				owner.MouseRightButtonDown += WpfTextView_MouseRightButtonDown;
				owner.MouseLeave += WpfTextView_MouseLeave;
				owner.MouseMove += WpfTextView_MouseMove;
				owner.IsVisibleChanged += WpfTextView_IsVisibleChanged;
			}

			void WpfTextView_MouseMove(object? sender, MouseEventArgs e) {
				if (owner.IsClosed || owner.IsMouseOverOverlayLayerElement(e)) {
					ClearMouseHoverPositionAndStopTimer();
					return;
				}
				if (e.LeftButton != MouseButtonState.Released || e.RightButton != MouseButtonState.Released || e.MiddleButton != MouseButtonState.Released)
					return;

				var loc = MouseLocation.TryCreateTextOnly(owner, e, fullLineHeight: false);
				int? newPosition;

				if (loc is null)
					newPosition = null;
				else if (loc.Position.IsInVirtualSpace)
					newPosition = null;
				else if (!(loc.TextViewLine.TextTop <= loc.Point.Y && loc.Point.Y < loc.TextViewLine.TextBottom))
					newPosition = null;
				else if (!(loc.TextViewLine.TextLeft <= loc.Point.X && loc.Point.X < loc.TextViewLine.TextRight))
					newPosition = null;
				else {
					int pos = loc.Position.Position.Position;
					if (loc.Affinity == PositionAffinity.Predecessor && pos != 0)
						pos--;
					newPosition = pos;
				}

				if (newPosition != position) {
					position = newPosition;
					StopTimer();
					foreach (var h in handlers)
						h.Raised = false;
					UpdateTimer();
				}
			}

			void WpfTextView_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e) {
				if (owner.IsClosed || !owner.IsVisible)
					ClearMouseHoverPositionAndStopTimer();
				else
					UpdateTimer();
			}

			void WpfTextView_MouseDown(object? sender, MouseButtonEventArgs e) => StopTimer();
			void WpfTextView_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e) => StopTimer();
			void WpfTextView_MouseRightButtonDown(object? sender, MouseButtonEventArgs e) => StopTimer();
			void WpfTextView_MouseLeave(object? sender, MouseEventArgs e) => ClearMouseHoverPositionAndStopTimer();

			public event EventHandler<MouseHoverEventArgs>? MouseHover {
				add {
					if (owner.IsClosed)
						return;
					if (value is null)
						return;
					handlers.Add(new MouseHoverHandler(value));
					UpdateTimer();
				}
				remove {
					if (value is null)
						return;
					for (int i = 0; i < handlers.Count; i++) {
						if (handlers[i].Handler == value) {
							handlers.RemoveAt(i);
							if (handlers.Count == 0)
								ClearMouseHoverPositionAndStopTimer();
							break;
						}
					}
				}
			}

			void ClearMouseHoverPositionAndStopTimer() {
				StopTimer();
				position = null;
			}

			void StopTimer() {
				timer.Stop();
				timerStart = null;
			}
			Stopwatch? timerStart;

			void Timer_Tick(object? sender, EventArgs e) {
				if (owner.IsClosed || !owner.IsVisible || position is null || position.Value > owner.TextSnapshot.Length) {
					ClearMouseHoverPositionAndStopTimer();
					return;
				}
				Debug2.Assert(!(timerStart is null));
				var list = GetHandlersToNotify();
				if (!(list is null)) {
					var mhe = new MouseHoverEventArgs(owner, position.Value, owner.BufferGraph.CreateMappingPoint(new SnapshotPoint(owner.TextSnapshot, position.Value), PointTrackingMode.Positive));
					foreach (var h in list) {
						h.Raised = true;
						h.Handler(owner, mhe);
					}
				}
				UpdateTimer();
			}

			long GetElapsedTimerStartTicks() {
				if (timerStart is null)
					return 0;
				return timerStart.ElapsedMilliseconds * 10000;
			}

			List<MouseHoverHandler>? GetHandlersToNotify() {
				List<MouseHoverHandler>? list = null;
				long elapsedTicks = GetElapsedTimerStartTicks();
				foreach (var h in handlers) {
					if (h.Raised)
						continue;
					// If it's close enough to the requested time, notify the handler.
					if (h.DelayTicks - h.DelayTicks / 10 <= elapsedTicks) {
						if (list is null)
							list = new List<MouseHoverHandler>();
						list.Add(h);
					}
				}
				return list;
			}

			public void OnLayoutChanged() => ClearMouseHoverPositionAndStopTimer();

			void UpdateTimer() {
				var ticksLeft = GetTicksLeft();
				if (ticksLeft < 0)
					StopTimer();
				else {
					if (timerStart is null)
						timerStart = Stopwatch.StartNew();
					timer.Interval = TimeSpan.FromTicks(Math.Max(10000, ticksLeft));
					timer.Start();
				}
			}

			long GetTicksLeft() {
				if (position is null)
					return -1;
				bool found = false;
				long ticks = long.MaxValue;
				foreach (var h in handlers) {
					if (!h.Raised) {
						ticks = Math.Min(h.DelayTicks, ticks);
						found = true;
					}
				}
				if (!found)
					return -1;
				return Math.Max(1, ticks - GetElapsedTimerStartTicks());
			}

			public void OnClosed() {
				StopTimer();
				handlers.Clear();
				timer.Tick -= Timer_Tick;
				owner.MouseDown -= WpfTextView_MouseDown;
				owner.MouseLeftButtonDown -= WpfTextView_MouseLeftButtonDown;
				owner.MouseRightButtonDown -= WpfTextView_MouseRightButtonDown;
				owner.MouseLeave -= WpfTextView_MouseLeave;
				owner.MouseMove -= WpfTextView_MouseMove;
				owner.IsVisibleChanged -= WpfTextView_IsVisibleChanged;
			}

			sealed class MouseHoverHandler {
				public long DelayTicks { get; }
				public EventHandler<MouseHoverEventArgs> Handler { get; }
				public bool Raised { get; set; }
				const int DEFAULT_DELAY_MILLISECS = 150;
				const int MIN_DELAY_MILLISECS = 50;

				public MouseHoverHandler(EventHandler<MouseHoverEventArgs> handler) {
					Handler = handler ?? throw new ArgumentNullException(nameof(handler));
					var attr = handler.Method.GetCustomAttributes(typeof(MouseHoverAttribute), false).FirstOrDefault() as MouseHoverAttribute;
					DelayTicks = Math.Max(MIN_DELAY_MILLISECS, attr?.Delay ?? DEFAULT_DELAY_MILLISECS) * 10000L;
				}
			}
		}
	}
}
