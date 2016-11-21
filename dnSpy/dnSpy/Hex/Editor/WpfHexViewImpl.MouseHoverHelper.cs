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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	sealed partial class WpfHexViewImpl {
		sealed class MouseHoverHelper {
			readonly WpfHexViewImpl owner;
			readonly List<MouseHoverHandler> handlers;
			readonly DispatcherTimer timer;
			LinePosition? position;

			struct LinePosition : IEquatable<LinePosition> {
				public HexBufferLine Line { get; }
				public int Column { get; }
				public LinePosition(HexBufferLine line, int column) {
					Line = line;
					Column = column;
				}

				public static bool operator ==(LinePosition a, LinePosition b) => a.Equals(b);
				public static bool operator !=(LinePosition a, LinePosition b) => !a.Equals(b);
				public bool Equals(LinePosition other) => EqualLines(Line, other.Line) && Column == other.Column;
				public override bool Equals(object obj) => obj is LinePosition && Equals((LinePosition)obj);
				public override int GetHashCode() => (Line?.GetHashCode() ?? 0) ^ Column.GetHashCode();
				static bool EqualLines(HexBufferLine a, HexBufferLine b) {
					if ((object)a == b)
						return true;
					if ((object)a == null || (object)b == null)
						return false;
					return a.LineProvider == b.LineProvider && a.BufferSpan.Equals(b.BufferSpan);
				}
			}

			public MouseHoverHelper(WpfHexViewImpl owner) {
				this.owner = owner;
				this.handlers = new List<MouseHoverHandler>();
				this.timer = new DispatcherTimer(DispatcherPriority.Normal, owner.VisualElement.Dispatcher);
				timer.Tick += Timer_Tick;
				owner.VisualElement.MouseDown += WpfTextView_MouseDown;
				owner.VisualElement.MouseLeftButtonDown += WpfTextView_MouseLeftButtonDown;
				owner.VisualElement.MouseRightButtonDown += WpfTextView_MouseRightButtonDown;
				owner.VisualElement.MouseLeave += WpfTextView_MouseLeave;
				owner.VisualElement.MouseMove += WpfTextView_MouseMove;
				owner.VisualElement.IsVisibleChanged += WpfTextView_IsVisibleChanged;
			}

			void WpfTextView_MouseMove(object sender, MouseEventArgs e) {
				if (owner.IsClosed || owner.IsMouseOverOverlayLayerElement(e)) {
					ClearMouseHoverPositionAndStopTimer();
					return;
				}
				if (e.LeftButton != MouseButtonState.Released || e.RightButton != MouseButtonState.Released || e.MiddleButton != MouseButtonState.Released)
					return;

				var loc = HexMouseLocation.TryCreateTextOnly(owner, e);
				LinePosition? newPosition;

				if (loc == null)
					newPosition = null;
				else if (loc.Position > loc.HexViewLine.TextSpan.End)
					newPosition = null;
				else if (!(loc.HexViewLine.TextTop <= loc.Point.Y && loc.Point.Y < loc.HexViewLine.TextBottom))
					newPosition = null;
				else if (!(loc.HexViewLine.TextLeft <= loc.Point.X && loc.Point.X < loc.HexViewLine.TextRight))
					newPosition = null;
				else
					newPosition = new LinePosition(loc.HexViewLine.BufferLine, loc.Position);

				if (newPosition != position) {
					position = newPosition;
					StopTimer();
					foreach (var h in handlers)
						h.Raised = false;
					UpdateTimer();
				}
			}

			void WpfTextView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				if (owner.IsClosed || !owner.VisualElement.IsVisible)
					ClearMouseHoverPositionAndStopTimer();
				else
					UpdateTimer();
			}

			void WpfTextView_MouseDown(object sender, MouseButtonEventArgs e) => StopTimer();
			void WpfTextView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => StopTimer();
			void WpfTextView_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => StopTimer();
			void WpfTextView_MouseLeave(object sender, MouseEventArgs e) => ClearMouseHoverPositionAndStopTimer();

			public event EventHandler<HexMouseHoverEventArgs> MouseHover {
				add {
					if (owner.IsClosed)
						return;
					if (value == null)
						return;
					handlers.Add(new MouseHoverHandler(value));
					UpdateTimer();
				}
				remove {
					if (value == null)
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
			Stopwatch timerStart;

			void Timer_Tick(object sender, EventArgs e) {
				if (owner.IsClosed || !owner.VisualElement.IsVisible || position == null || !owner.BufferLines.BufferSpan.Contains(position.Value.Line.BufferSpan)) {
					ClearMouseHoverPositionAndStopTimer();
					return;
				}
				Debug.Assert(timerStart != null);
				var list = GetHandlersToNotify();
				if (list != null) {
					var mhe = new HexMouseHoverEventArgs(owner, position.Value.Line, position.Value.Column);
					foreach (var h in list) {
						h.Raised = true;
						h.Handler(owner, mhe);
					}
				}
				UpdateTimer();
			}

			long GetElapsedTimerStartTicks() {
				if (timerStart == null)
					return 0;
				return timerStart.ElapsedMilliseconds * 10000;
			}

			List<MouseHoverHandler> GetHandlersToNotify() {
				List<MouseHoverHandler> list = null;
				long elapsedTicks = GetElapsedTimerStartTicks();
				foreach (var h in handlers) {
					if (h.Raised)
						continue;
					// If it's close enough to the requested time, notify the handler.
					if (h.DelayTicks - h.DelayTicks / 10 <= elapsedTicks) {
						if (list == null)
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
					if (timerStart == null)
						timerStart = Stopwatch.StartNew();
					timer.Interval = TimeSpan.FromTicks(Math.Max(10000, ticksLeft));
					timer.Start();
				}
			}

			long GetTicksLeft() {
				if (position == null)
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
				owner.VisualElement.MouseDown -= WpfTextView_MouseDown;
				owner.VisualElement.MouseLeftButtonDown -= WpfTextView_MouseLeftButtonDown;
				owner.VisualElement.MouseRightButtonDown -= WpfTextView_MouseRightButtonDown;
				owner.VisualElement.MouseLeave -= WpfTextView_MouseLeave;
				owner.VisualElement.MouseMove -= WpfTextView_MouseMove;
				owner.VisualElement.IsVisibleChanged -= WpfTextView_IsVisibleChanged;
			}

			sealed class MouseHoverHandler {
				public long DelayTicks { get; }
				public EventHandler<HexMouseHoverEventArgs> Handler { get; }
				public bool Raised { get; set; }
				const int DEFAULT_DELAY_MILLISECS = 150;
				const int MIN_DELAY_MILLISECS = 50;

				public MouseHoverHandler(EventHandler<HexMouseHoverEventArgs> handler) {
					if (handler == null)
						throw new ArgumentNullException(nameof(handler));
					Handler = handler;
					var attr = handler.Method.GetCustomAttributes(typeof(HexMouseHoverAttribute), false).FirstOrDefault() as HexMouseHoverAttribute;
					DelayTicks = Math.Max(MIN_DELAY_MILLISECS, attr?.Delay ?? DEFAULT_DELAY_MILLISECS) * 10000L;
				}
			}
		}
	}
}
