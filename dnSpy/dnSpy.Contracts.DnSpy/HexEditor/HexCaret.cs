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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace dnSpy.Contracts.HexEditor {
	sealed class HexCaret : Control, IHexLayer {
		public static readonly double DEFAULT_ORDER = 3000;

		public static readonly DependencyProperty InactiveCaretForegroundProperty =
			DependencyProperty.Register(nameof(InactiveCaretForeground), typeof(Brush), typeof(HexCaret),
			new FrameworkPropertyMetadata(Brushes.Black));

		public Brush InactiveCaretForeground {
			get { return (Brush)GetValue(InactiveCaretForegroundProperty); }
			set { SetValue(InactiveCaretForegroundProperty, value); }
		}

		[DllImport("user32")]
		static extern int GetCaretBlinkTime();

		static HexCaret() {
			SystemEvents.UserPreferenceChanged += (s, e) => {
				if (e.Category == UserPreferenceCategory.Keyboard)
					caretBlinkTime = GetCaretBlinkTime();
            };
			caretBlinkTime = GetCaretBlinkTime();
		}
		static int caretBlinkTime;

		public double Order => DEFAULT_ORDER;

		public HexBoxPosition Position => position;
		HexBoxPosition position;
		double horizOffset;
		DispatcherTimer timer;

		public HexCaret() {
			this.hexByteInfo = caretInfos[0];
			this.asciiInfo = caretInfos[1];
			UpdateInstallTimer();
			IsVisibleChanged += HexCaret_IsVisibleChanged;
			Loaded += (s, e) => UpdateInstallTimer();
			Unloaded += (s, e) => StopTimer();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property == ForegroundProperty || e.Property == InactiveCaretForegroundProperty)
				Redraw();
		}

		void HexCaret_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible)
				UpdateInstallTimer();
			else
				StopTimer();
		}

		void UpdateInstallTimer() {
			if (Visibility != Visibility.Visible) {
				StopTimer();
				return;
			}

			bool cont = oldCaretBlinkTime != caretBlinkTime ||
				(caretBlinkTime > 0 && timer == null);
			if (!cont)
				return;
			oldCaretBlinkTime = caretBlinkTime;

			StopTimer();

			if (caretBlinkTime > 0)
				timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, caretBlinkTime), DispatcherPriority.Normal, TimerHandler, Dispatcher);
		}
		int oldCaretBlinkTime = -1;

		void StopTimer() {
			if (timer != null) {
				timer.Stop();
				timer = null;
			}
		}

		void TimerHandler(object sender, EventArgs e) {
			blinkIsVisible = !blinkIsVisible;
			Redraw();
		}
		bool blinkIsVisible = false;

		void Redraw() => InvalidateVisual();

		protected override void OnRender(DrawingContext drawingContext) {
			base.OnRender(drawingContext);

			UpdateInstallTimer();

			bool visible = blinkIsVisible || caretBlinkTime <= 0;

			CreateGeometry();
			foreach (var info in caretInfos) {
				var geo = visible ? info.Geometry : info.BlinkHiddenGeometry;
				if (geo != null)
					drawingContext.DrawGeometry(info.IsActive ? Foreground : InactiveCaretForeground, null, geo);
			}
		}

		public Rect? HexRect => hexByteInfo.Rect;
		public Rect? AsciiRect => asciiInfo.Rect;

		sealed class CaretInfo {
			public Geometry Geometry;
			public Geometry BlinkHiddenGeometry;
			public Rect? Rect;
			public bool IsActive;

			public void Initialize(Rect? rect, bool isActive) {
				this.Rect = rect;
				this.IsActive = isActive;
			}
		}
        readonly CaretInfo[] caretInfos = new CaretInfo[] {
			new CaretInfo(),
			new CaretInfo(),
		};
		readonly CaretInfo hexByteInfo;
		readonly CaretInfo asciiInfo;

		void CreateGeometry() {
			if (geometriesCreated)
				return;
			geometriesCreated = true;

			Initialize(hexByteInfo, Position.Kind == HexBoxPositionKind.HexByte);
			Initialize(asciiInfo, Position.Kind == HexBoxPositionKind.Ascii);
		}
		bool geometriesCreated = false;

		void Initialize(CaretInfo info, bool hasFocus) {
			if (info.Rect == null) {
				info.Geometry = null;
				info.BlinkHiddenGeometry = null;
			}
			else {
				var rect = info.Rect.Value;
				if (!hasFocus) {
					double height = Math.Min(rect.Height, 2.0);
					rect = new Rect(rect.X, rect.Y + rect.Height - height, rect.Width, height);
				}
				info.Geometry = new RectangleGeometry(new Rect(rect.X - horizOffset, rect.Y, rect.Width, rect.Height));
				info.Geometry.Freeze();
				info.BlinkHiddenGeometry = hasFocus ? null : info.Geometry;
			}
		}

		public void SetCaret(HexBoxPosition position, double horizOffset, Rect? hexByteCaret, Rect? asciiCaret) {
			if (position == Position && this.horizOffset == horizOffset && hexByteInfo.Rect == hexByteCaret && asciiInfo.Rect == asciiCaret)
				return;
			this.position = position;
			this.horizOffset = horizOffset;
			hexByteInfo.Initialize(hexByteCaret, position.Kind == HexBoxPositionKind.HexByte);
			asciiInfo.Initialize(asciiCaret, position.Kind == HexBoxPositionKind.Ascii);
			geometriesCreated = false;

			// Make sure caret doesn't blink when it's moving. It looks weird when quickly moving up/down
			blinkIsVisible = true;

			Redraw();
		}
	}
}
