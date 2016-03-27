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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using dnSpy.Shared.MVVM;

namespace dnSpy.Shared.Controls {
	public class MetroWindow : Window {
		public static readonly RoutedCommand FullScreenCommand = new RoutedCommand("FullScreen", typeof(MetroWindow));

		public MetroWindow() {
			SetValue(WindowChrome.WindowChromeProperty, CreateWindowChromeObject());
			// Since the system menu had to be disabled, we must add this command
			var cmd = new RelayCommand(a => ShowSystemMenu(this), a => !IsFullScreen);
			InputBindings.Add(new KeyBinding(cmd, Key.Space, ModifierKeys.Alt));
		}

		public bool DisableDpiScaleAtStartup { get; set; }

		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);

			var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			Debug.Assert(hwndSource != null);
			if (hwndSource != null) {
				hwndSource.AddHook(WndProc);
				wpfDpi = 96.0 * hwndSource.CompositionTarget.TransformToDevice.M11;

				var w = Width;
				var h = Height;
				WindowDPI = GetDpi(hwndSource.Handle, (int)wpfDpi);

				// For some reason, we can't initialize the non-fit-to-size property, so always force
				// manual mode. When we're here, we should already have a valid Width and Height
				Debug.Assert(h > 0 && !double.IsNaN(h));
				Debug.Assert(w > 0 && !double.IsNaN(w));
				SizeToContent = SizeToContent.Manual;

				double scale = DisableDpiScaleAtStartup ? 1 : WpfPixelScaleFactor;
				Width = w * scale;
				Height = h * scale;

				if (WindowStartupLocation == WindowStartupLocation.CenterOwner || WindowStartupLocation == WindowStartupLocation.CenterScreen) {
					Left -= (w * scale - w) / 2;
					Top -= (h * scale - h) / 2;
				}
			}

			WindowUtils.UpdateWin32Style(this);
		}

		static bool? canCall_GetDpi = null;
		static int GetDpi(IntPtr hWnd, int defaultValue) {
			if (canCall_GetDpi == false)
				return defaultValue;
			try {
				var res = GetDpi_Win81(hWnd);
				canCall_GetDpi = true;
				return res;
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canCall_GetDpi = false;
			return defaultValue;
		}

		static int GetDpi_Win81(IntPtr hWnd) {
			const int MONITOR_DEFAULTTONEAREST = 0x00000002;
			var hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
			int dpiX, dpiY;
			const int MDT_EFFECTIVE_DPI = 0;
			int hr = GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
			Debug.Assert(hr == 0);
			if (hr != 0)
				return 96;
			return dpiX;
		}

		struct RECT {
			public int left, top, right, bottom;
			RECT(bool dummy) { left = top = right = bottom = 0; }// disable compiler warning
		}

		[DllImport("user32", SetLastError = true)]
		static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
		[DllImport("shcore", SetLastError = true)]
		static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out int dpiX, out int dpiY);
		[DllImport("user32", SetLastError = true)]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			const int WM_DPICHANGED = 0x02E0;

			if (msg == WM_DPICHANGED) {
				int newDpi = (ushort)(wParam.ToInt64() >> 16);
				var rect = Marshal.PtrToStructure<RECT>(lParam);

				WindowDPI = newDpi;

				const int SWP_NOZORDER = 0x0004;
				const int SWP_NOACTIVATE = 0x0010;
				const int SWP_NOOWNERZORDER = 0x0200;
				bool b = SetWindowPos(hwnd, IntPtr.Zero, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
				Debug.Assert(b);

				handled = true;
				return IntPtr.Zero;
			}

			return IntPtr.Zero;
		}

		double SafeWpfPixelScaleFactor {
			get {
				if (wpfDpi == 0)
					return 1;
				return WpfPixelScaleFactor;
			}
		}

		double WpfPixelScaleFactor {
			get {
				Debug.Assert(wpfDpi != 0);
				if (wpfDpi == 0)
					return 1;
				return windowDpi / wpfDpi;
			}
		}

		int WindowDPI {
			get { return windowDpi; }
			set {
				if (windowDpi != value) {
					var origScale = SafeWpfPixelScaleFactor;
					if (origScale == 0)
						origScale = 1;
					windowDpi = value;
					var relScale = WpfPixelScaleFactor / origScale;
					MinWidth *= relScale;
					MinHeight *= relScale;
					MaxWidth *= relScale;
					MaxHeight *= relScale;
					UpdateWindowChromeProperties();
					ScaleWindow(WpfPixelScaleFactor);

					if (WindowDPIChanged != null)
						WindowDPIChanged(this, EventArgs.Empty);
				}
			}
		}
		int windowDpi;
		double wpfDpi;

		public event EventHandler WindowDPIChanged;

		void UpdateWindowChromeProperties() {
			var wc = (WindowChrome)GetValue(WindowChrome.WindowChromeProperty);
			Debug.Assert(wc != null);
			if (wc != null) {
				bool useResizeBorder = !(IsFullScreen || WindowState == WindowState.Maximized || WindowState == WindowState.Minimized);
				InitializeWindowCaptionAndResizeBorder(wc, useResizeBorder);
			}
		}

		void ScaleWindow(double scale) {
			var border = GetVisualChild(0) as Border;
			Debug.Assert(border != null);
			var vc = border == null ? null : VisualTreeHelper.GetChild(border, 0);
			Debug.Assert(vc != null);
			if (vc == null)
				return;

			SetScaleTransform(this, vc, scale);
		}

		public static ICommand ShowSystemMenuCommand {
			get { return new RelayCommand(a => ShowSystemMenu(a), a => true); }
		}

		public event EventHandler IsFullScreenChanged;

		public static readonly DependencyProperty IsFullScreenProperty =
			DependencyProperty.Register("IsFullScreen", typeof(bool), typeof(MetroWindow),
			new FrameworkPropertyMetadata(false, OnIsFullScreenChanged));

		public bool IsFullScreen {
			get { return (bool)GetValue(IsFullScreenProperty); }
			set { SetValue(IsFullScreenProperty, value); }
		}

		static void OnIsFullScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var window = (MetroWindow)d;
			var wc = (WindowChrome)window.GetValue(WindowChrome.WindowChromeProperty);
			if (window.IsFullScreen)
				window.InitializeWindowCaptionAndResizeBorder(wc, false);
			else
				window.InitializeWindowCaptionAndResizeBorder(wc);

			if (window.IsFullScreenChanged != null)
				window.IsFullScreenChanged(window, EventArgs.Empty);
		}

		public static readonly DependencyProperty SystemMenuImageProperty =
			DependencyProperty.Register("SystemMenuImage", typeof(ImageSource), typeof(MetroWindow),
			new FrameworkPropertyMetadata(null));

		public ImageSource SystemMenuImage {
			get { return (ImageSource)GetValue(SystemMenuImageProperty); }
			set { SetValue(SystemMenuImageProperty, value); }
		}

		public static readonly DependencyProperty MaximizedElementProperty = DependencyProperty.RegisterAttached(
			"MaximizedElement", typeof(bool), typeof(MetroWindow), new UIPropertyMetadata(false, OnMaximizedElementChanged));

		static void OnMaximizedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var border = d as Border;
			Debug.Assert(border != null);
			if (border == null)
				return;
			var win = Window.GetWindow(border) as MetroWindow;
			if (win == null)
				return;

			new MaximizedWindowFixer(win, border);
		}

		// When the window is maximized, the part of the window where the (in our case, hidden) resize
		// border is located, is hidden. Add a padding to a border element whose value exactly equals
		// the border width and reset it when it's not maximized.
		sealed class MaximizedWindowFixer {
			readonly Border border;
			readonly Thickness oldThickness;
			readonly MetroWindow metroWindow;

			public MaximizedWindowFixer(MetroWindow metroWindow, Border border) {
				this.border = border;
				this.oldThickness = border.BorderThickness;
				this.metroWindow = metroWindow;
				metroWindow.StateChanged += MetroWindow_StateChanged;
				border.Loaded += border_Loaded;
			}

			void border_Loaded(object sender, RoutedEventArgs e) {
				border.Loaded -= border_Loaded;
				UpdatePadding(metroWindow);
			}

			void MetroWindow_StateChanged(object sender, EventArgs e) {
				UpdatePadding((MetroWindow)sender);
			}

			void UpdatePadding(MetroWindow window) {
				Debug.Assert(window != null);

				var state = window.IsFullScreen ? WindowState.Maximized : window.WindowState;
				switch (state) {
				default:
				case WindowState.Normal:
					border.ClearValue(Border.PaddingProperty);
					border.BorderThickness = oldThickness;
					break;

				case WindowState.Minimized:
				case WindowState.Maximized:
					double magicx, magicy;

					magicx = magicy = 10;//TODO: Figure out how this value is calculated (it's not ResizeBorderThickness.Left/Top)

					double deltax = magicx - SystemParameters.ResizeFrameVerticalBorderWidth;
					double deltay = magicy - SystemParameters.ResizeFrameHorizontalBorderHeight;
					Debug.Assert(deltax >= 0 && deltay >= 0);
					if (deltax < 0)
						deltax = 0;
					if (deltay < 0)
						deltay = 0;

					border.Padding = new Thickness(
						SystemParameters.BorderWidth + border.BorderThickness.Left + deltax,
						SystemParameters.BorderWidth + border.BorderThickness.Top + deltay,
						SystemParameters.BorderWidth + border.BorderThickness.Right + deltax,
						SystemParameters.BorderWidth + border.BorderThickness.Bottom + deltay);
					border.BorderThickness = new Thickness(0);
					break;
				}
			}
		}

		public static void SetMaximizedElement(UIElement element, bool value) {
			element.SetValue(MaximizedElementProperty, value);
		}

		public static bool GetMaximizedElement(UIElement element) {
			return (bool)element.GetValue(MaximizedElementProperty);
		}

		public static readonly DependencyProperty UseResizeBorderProperty =
			DependencyProperty.Register("UseResizeBorder", typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true, OnUseResizeBorderChanged));

		public bool UseResizeBorder {
			get { return (bool)GetValue(UseResizeBorderProperty); }
			set { SetValue(UseResizeBorderProperty, value); }
		}

		static void OnUseResizeBorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var win = (MetroWindow)d;
			var wc = (WindowChrome)win.GetValue(WindowChrome.WindowChromeProperty);
			if (wc == null)
				return;

			win.InitializeWindowCaptionAndResizeBorder(wc);
		}

		public static void SetUseResizeBorder(UIElement element, bool value) {
			element.SetValue(UseResizeBorderProperty, value);
		}

		public static bool GetUseResizeBorder(UIElement element) {
			return (bool)element.GetValue(UseResizeBorderProperty);
		}

		public static readonly DependencyProperty IsDebuggingProperty =
			DependencyProperty.Register("IsDebugging", typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		public bool IsDebugging {
			get { return (bool)GetValue(IsDebuggingProperty); }
			set { SetValue(IsDebuggingProperty, value); }
		}

		public static readonly DependencyProperty ActiveCaptionProperty =
			DependencyProperty.Register("ActiveCaption", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ActiveCaptionTextProperty =
			DependencyProperty.Register("ActiveCaptionText", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ActiveDebuggingBorderProperty =
			DependencyProperty.Register("ActiveDebuggingBorder", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ActiveDefaultBorderProperty =
			DependencyProperty.Register("ActiveDefaultBorder", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty InactiveBorderProperty =
			DependencyProperty.Register("InactiveBorder", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty InactiveCaptionProperty =
			DependencyProperty.Register("InactiveCaption", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty InactiveCaptionTextProperty =
			DependencyProperty.Register("InactiveCaptionText", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ButtonInactiveBorderProperty =
			DependencyProperty.Register("ButtonInactiveBorder", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ButtonInactiveGlyphProperty =
			DependencyProperty.Register("ButtonInactiveGlyph", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ButtonHoverInactiveProperty =
			DependencyProperty.Register("ButtonHoverInactive", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ButtonHoverInactiveBorderProperty =
			DependencyProperty.Register("ButtonHoverInactiveBorder", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));
		public static readonly DependencyProperty ButtonHoverInactiveGlyphProperty =
			DependencyProperty.Register("ButtonHoverInactiveGlyph", typeof(Brush), typeof(MetroWindow),
			new UIPropertyMetadata(null));

		public Brush ActiveCaption {
			get { return (Brush)GetValue(ActiveCaptionProperty); }
			set { SetValue(ActiveCaptionProperty, value); }
		}
		public Brush ActiveCaptionText {
			get { return (Brush)GetValue(ActiveCaptionTextProperty); }
			set { SetValue(ActiveCaptionTextProperty, value); }
		}
		public Brush ActiveDebuggingBorder {
			get { return (Brush)GetValue(ActiveDebuggingBorderProperty); }
			set { SetValue(ActiveDebuggingBorderProperty, value); }
		}
		public Brush ActiveDefaultBorder {
			get { return (Brush)GetValue(ActiveDefaultBorderProperty); }
			set { SetValue(ActiveDefaultBorderProperty, value); }
		}
		public Brush InactiveBorder {
			get { return (Brush)GetValue(InactiveBorderProperty); }
			set { SetValue(InactiveBorderProperty, value); }
		}
		public Brush InactiveCaption {
			get { return (Brush)GetValue(InactiveCaptionProperty); }
			set { SetValue(InactiveCaptionProperty, value); }
		}
		public Brush InactiveCaptionText {
			get { return (Brush)GetValue(InactiveCaptionTextProperty); }
			set { SetValue(InactiveCaptionTextProperty, value); }
		}
		public Brush ButtonInactiveBorder {
			get { return (Brush)GetValue(ButtonInactiveBorderProperty); }
			set { SetValue(ButtonInactiveBorderProperty, value); }
		}
		public Brush ButtonInactiveGlyph {
			get { return (Brush)GetValue(ButtonInactiveGlyphProperty); }
			set { SetValue(ButtonInactiveGlyphProperty, value); }
		}
		public Brush ButtonHoverInactive {
			get { return (Brush)GetValue(ButtonHoverInactiveProperty); }
			set { SetValue(ButtonHoverInactiveProperty, value); }
		}
		public Brush ButtonHoverInactiveBorder {
			get { return (Brush)GetValue(ButtonHoverInactiveBorderProperty); }
			set { SetValue(ButtonHoverInactiveBorderProperty, value); }
		}
		public Brush ButtonHoverInactiveGlyph {
			get { return (Brush)GetValue(ButtonHoverInactiveGlyphProperty); }
			set { SetValue(ButtonHoverInactiveGlyphProperty, value); }
		}

		public static readonly DependencyProperty ShowMenuButtonProperty =
			DependencyProperty.Register("ShowMenuButton", typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));
		public static readonly DependencyProperty ShowMinimizeButtonProperty =
			DependencyProperty.Register("ShowMinimizeButton", typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));
		public static readonly DependencyProperty ShowMaximizeButtonProperty =
			DependencyProperty.Register("ShowMaximizeButton", typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));
		public static readonly DependencyProperty ShowCloseButtonProperty =
			DependencyProperty.Register("ShowCloseButton", typeof(bool), typeof(MetroWindow),
			new UIPropertyMetadata(true));

		public bool ShowMenuButton {
			get { return (bool)GetValue(ShowMenuButtonProperty); }
			set { SetValue(ShowMenuButtonProperty, value); }
		}

		public bool ShowMinimizeButton {
			get { return (bool)GetValue(ShowMinimizeButtonProperty); }
			set { SetValue(ShowMinimizeButtonProperty, value); }
		}

		public bool ShowMaximizeButton {
			get { return (bool)GetValue(ShowMaximizeButtonProperty); }
			set { SetValue(ShowMaximizeButtonProperty, value); }
		}

		public bool ShowCloseButton {
			get { return (bool)GetValue(ShowCloseButtonProperty); }
			set { SetValue(ShowCloseButtonProperty, value); }
		}

		static MetroWindow() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroWindow), new FrameworkPropertyMetadata(typeof(MetroWindow)));
		}

		// If these get updated, also update the templates if necessary
		static readonly CornerRadius CornerRadius = new CornerRadius(0, 0, 0, 0);
		static readonly Thickness GlassFrameThickness = new Thickness(0);
		// NOTE: Keep these in sync: CaptionHeight + ResizeBorderThickness.Top = GridCaptionHeight
		static readonly double CaptionHeight = 20;
		static readonly Thickness ResizeBorderThickness = new Thickness(10, 10, 5, 5);
		public static readonly GridLength GridCaptionHeight = new GridLength(CaptionHeight + ResizeBorderThickness.Top, GridUnitType.Pixel);

		protected override void OnStateChanged(EventArgs e) {
			base.OnStateChanged(e);
			if (WindowState == WindowState.Normal)
				ClearValue(Window.WindowStateProperty);

			var wc = (WindowChrome)GetValue(WindowChrome.WindowChromeProperty);
			switch (WindowState) {
			case WindowState.Normal:
				InitializeWindowCaptionAndResizeBorder(wc);
				ClearValue(Window.WindowStateProperty);
				break;

			case WindowState.Minimized:
			case WindowState.Maximized:
				InitializeWindowCaptionAndResizeBorder(wc, false);
				break;

			default:
				break;
			}
		}

		WindowChrome CreateWindowChromeObject() {
			var wc = new WindowChrome {
				CornerRadius = CornerRadius,
				GlassFrameThickness = GlassFrameThickness,
				NonClientFrameEdges = NonClientFrameEdges.None,
			};
			InitializeWindowCaptionAndResizeBorder(wc);
			return wc;
		}

		void InitializeWindowCaptionAndResizeBorder(WindowChrome wc) {
			InitializeWindowCaptionAndResizeBorder(wc, UseResizeBorder);
		}

		void InitializeWindowCaptionAndResizeBorder(WindowChrome wc, bool useResizeBorder) {
			var scale = SafeWpfPixelScaleFactor;
			if (useResizeBorder) {
				wc.CaptionHeight = CaptionHeight * scale;
				wc.ResizeBorderThickness = new Thickness(
					ResizeBorderThickness.Left * scale,
					ResizeBorderThickness.Top * scale,
					ResizeBorderThickness.Right * scale,
					ResizeBorderThickness.Bottom * scale);
			}
			else {
				if (IsFullScreen)
					wc.CaptionHeight = 0d;
				else
					wc.CaptionHeight = GridCaptionHeight.Value * scale;
				wc.ResizeBorderThickness = new Thickness(0);
			}
		}

		static void ShowSystemMenu(object o) {
			var depo = o as DependencyObject;
			if (depo == null)
				return;
			var win = Window.GetWindow(depo);
			if (win == null)
				return;

			var mwin = win as MetroWindow;
			Debug.Assert(mwin != null);
			var scale = mwin == null ? 0 : mwin.WpfPixelScaleFactor;

			var p = win.PointToScreen(new Point(0 * scale, GridCaptionHeight.Value * scale));
			WindowUtils.ShowSystemMenu(win, p);
		}

		public void SetScaleTransform(DependencyObject vc, double scale) {
			SetScaleTransform(vc, vc, scale);
		}

		void SetScaleTransform(DependencyObject textObj, DependencyObject vc, double scale) {
			if (vc == null || textObj == null)
				return;

			if (scale == 1) {
				vc.SetValue(LayoutTransformProperty, Transform.Identity);
				if (WindowDPI == 96)
					TextOptions.SetTextFormattingMode(textObj, TextFormattingMode.Display);
				else
					TextOptions.SetTextFormattingMode(textObj, TextFormattingMode.Ideal);
			}
			else {
				var st = new ScaleTransform(scale, scale);
				st.Freeze();
				vc.SetValue(LayoutTransformProperty, st);

				// We must set it to Ideal or the text will be blurry
				TextOptions.SetTextFormattingMode(textObj, TextFormattingMode.Ideal);
			}
		}
	}

	public static class WindowUtils {
		[DllImport("user32")]
		static extern bool IsWindow(IntPtr hWnd);
		[DllImport("user32")]
		static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
		[DllImport("user32")]
		static extern uint TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);
		[DllImport("user32")]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32")]
		extern static int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32")]
		extern static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		internal static void UpdateWin32Style(MetroWindow window) {
			const int GWL_STYLE = -16;
			const int WS_SYSMENU = 0x00080000;

			IntPtr hWnd = new WindowInteropHelper(window).Handle;

			// The whole title bar is restyled. We must hide the system menu or Windows
			// will sometimes paint the title bar for us.
			SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		internal static void ShowSystemMenu(Window window, Point p) {
			var hWnd = new WindowInteropHelper(window).Handle;
			if (hWnd == IntPtr.Zero)
				return;
			if (!IsWindow(hWnd))
				return;

			var hMenu = GetSystemMenu(hWnd, false);
			uint res = TrackPopupMenuEx(hMenu, 0x100, (int)p.X, (int)p.Y, hWnd, IntPtr.Zero);
			if (res != 0)
				PostMessage(hWnd, 0x112, IntPtr.Size == 4 ? new IntPtr((int)res) : new IntPtr(res), IntPtr.Zero);
		}

		public static void SetState(Window window, WindowState state) {
			switch (state) {
			case WindowState.Normal:
				Restore(window);
				break;

			case WindowState.Minimized:
				Minimize(window);
				break;

			case WindowState.Maximized:
				Maximize(window);
				break;
			}
		}

		public static void Minimize(Window window) {
			window.WindowState = WindowState.Minimized;
		}

		public static void Maximize(Window window) {
			window.WindowState = WindowState.Maximized;
		}

		public static void Restore(Window window) {
			window.ClearValue(Window.WindowStateProperty);
		}
	}
}
