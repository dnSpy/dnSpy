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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	sealed partial class WpfHexViewImpl {
		sealed class MouseHoverHelper {
			readonly WpfHexViewImpl owner;
			readonly List<MouseHoverHandler> handlers;
			readonly DispatcherTimer timer;
			LinePosition? position;

			readonly struct LinePosition : IEquatable<LinePosition> {
				public HexBufferLine Line { get; }
				public int Column { get; }
				public LinePosition(HexBufferLine line, int column) {
					Line = line;
					Column = column;
				}

				public static bool operator ==(LinePosition a, LinePosition b) => a.Equals(b);
				public static bool operator !=(LinePosition a, LinePosition b) => !a.Equals(b);
				public bool Equals(LinePosition other) => EqualLines(Line, other.Line) && Column == other.Column;
				public override bool Equals(object? obj) => obj is LinePosition && Equals((LinePosition)obj);
				public override int GetHashCode() => (Line?.GetHashCode() ?? 0) ^ Column.GetHashCode();
				static bool EqualLines(HexBufferLine a, HexBufferLine b) {
					if ((object)a == b)
						return true;
					if (a is null || b is null)
						return false;
					return a.LineProvider == b.LineProvider && a.BufferSpan.Equals(b.BufferSpan);
				}
			}

			public MouseHoverHelper(WpfHexViewImpl owner) {
				this.owner = owner;
				handlers = new List<MouseHoverHandler>();
				timer = new DispatcherTimer(DispatcherPriority.Normal, owner.VisualElement.Dispatcher);
				timer.Tick += Timer_Tick;
				owner.VisualElement.MouseDown += WpfHexView_MouseDown;
				owner.VisualElement.MouseLeftButtonDown += WpfHexView_MouseLeftButtonDown;
				owner.VisualElement.MouseRightButtonDown += WpfHexView_MouseRightButtonDown;
				owner.VisualElement.MouseLeave += WpfHexView_MouseLeave;
				owner.VisualElement.MouseMove += WpfHexView_MouseMove;
				owner.VisualElement.IsVisibleChanged += WpfHexView_IsVisibleChanged;
			}

			void WpfHexView_MouseMove(object? sender, MouseEventArgs e) {
				if (owner.IsClosed || owner.IsMouseOverOverlayLayerElement(e)) {
					ClearMouseHoverPositionAndStopTimer();
					return;
				}
				if (e.LeftButton != MouseButtonState.Released || e.RightButton != MouseButtonState.Released || e.MiddleButton != MouseButtonState.Released)
					return;

				var loc = HexMouseLocation.TryCreateTextOnly(owner, e, fullLineHeight: false);
				LinePosition? newPosition;

				if (loc is null)
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

			void WpfHexView_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e) {
				if (owner.IsClosed || !owner.VisualElement.IsVisible)
					ClearMouseHoverPositionAndStopTimer();
				else
					UpdateTimer();
			}

			void WpfHexView_MouseDown(object? sender, MouseButtonEventArgs e) => StopTimer();
			void WpfHexView_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e) => StopTimer();
			void WpfHexView_MouseRightButtonDown(object? sender, MouseButtonEventArgs e) => StopTimer();
			void WpfHexView_MouseLeave(object? sender, MouseEventArgs e) => ClearMouseHoverPositionAndStopTimer();

			public event EventHandler<HexMouseHoverEventArgs>? MouseHover {
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
				if (owner.IsClosed || !owner.VisualElement.IsVisible || position is null || !owner.BufferLines.BufferSpan.Contains(position.Value.Line.BufferSpan)) {
					ClearMouseHoverPositionAndStopTimer();
					return;
				}
				Debug2.Assert(timerStart is not null);
				var list = GetHandlersToNotify();
				if (list is not null) {
					var mhe = new HexMouseHoverEventArgs(owner, position.Value.Line, position.Value.Column);
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
				owner.VisualElement.MouseDown -= WpfHexView_MouseDown;
				owner.VisualElement.MouseLeftButtonDown -= WpfHexView_MouseLeftButtonDown;
				owner.VisualElement.MouseRightButtonDown -= WpfHexView_MouseRightButtonDown;
				owner.VisualElement.MouseLeave -= WpfHexView_MouseLeave;
				owner.VisualElement.MouseMove -= WpfHexView_MouseMove;
				owner.VisualElement.IsVisibleChanged -= WpfHexView_IsVisibleChanged;
			}

			sealed class MouseHoverHandler {
				public long DelayTicks { get; }
				public EventHandler<HexMouseHoverEventArgs> Handler { get; }
				public bool Raised { get; set; }
				const int DEFAULT_DELAY_MILLISECS = 150;
				const int MIN_DELAY_MILLISECS = 50;

				public MouseHoverHandler(EventHandler<HexMouseHoverEventArgs> handler) {
					Handler = handler ?? throw new ArgumentNullException(nameof(handler));
					var attr = handler.Method.GetCustomAttributes(typeof(HexMouseHoverAttribute), false).FirstOrDefault() as HexMouseHoverAttribute;
					DelayTicks = Math.Max(MIN_DELAY_MILLISECS, attr?.Delay ?? DEFAULT_DELAY_MILLISECS) * 10000L;
				}
			}
		}
	}
}
