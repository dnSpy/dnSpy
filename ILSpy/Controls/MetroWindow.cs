/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using dnSpy.AsmEditor;

namespace ICSharpCode.ILSpy.Controls {
	public class MetroWindow : Window {
		public MetroWindow() {
			SetValue(winChrome_WindowChromeProperty, CreateWindowChromeObject());
			// Since the system menu had to be disabled, we must add this command
			var cmd = new RelayCommand(a => ShowSystemMenu(this), a => !IsFullScreen);
			InputBindings.Add(new KeyBinding(cmd, Key.Space, ModifierKeys.Alt));
		}

		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);
			WindowUtils.UpdateWin32Style(this);
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
			var obj = (DependencyObject)window.GetValue(winChrome_WindowChromeProperty);
			if (window.IsFullScreen)
				window.InitializeWindowCaptionAndResizeBorder(obj, false);
			else
				window.InitializeWindowCaptionAndResizeBorder(obj);

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

		public static readonly DependencyProperty IsHitTestVisibleInChromeProperty = DependencyProperty.RegisterAttached(
			"IsHitTestVisibleInChrome", typeof(bool), typeof(MetroWindow), new UIPropertyMetadata(false, OnIsHitTestVisibleInChromeChanged));

		static void OnIsHitTestVisibleInChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var elem = d as UIElement;
			if (elem != null)
				elem.SetValue(winChrome_IsHitTestVisibleInChromeProperty, e.NewValue);
		}

		public static void SetIsHitTestVisibleInChrome(UIElement element, bool value) {
			element.SetValue(IsHitTestVisibleInChromeProperty, value);
		}

		public static bool GetIsHitTestVisibleInChrome(UIElement element) {
			return (bool)element.GetValue(IsHitTestVisibleInChromeProperty);
		}

		public static readonly DependencyProperty MaximizedElementProperty = DependencyProperty.RegisterAttached(
			"MaximizedElement", typeof(bool), typeof(MetroWindow), new UIPropertyMetadata(false, OnMaximizedElementChanged));

		static void OnMaximizedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var border = d as Border;
			Debug.Assert(border != null);
			if (border == null)
				return;
			var win = Window.GetWindow(border);
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

			public MaximizedWindowFixer(Window win, Border border) {
				this.border = border;
				this.oldThickness = border.BorderThickness;
				win.StateChanged += win_StateChanged;
				border.Loaded += border_Loaded;
			}

			void border_Loaded(object sender, RoutedEventArgs e) {
				border.Loaded -= border_Loaded;
				UpdatePadding((MetroWindow)Window.GetWindow(border));
			}

			void win_StateChanged(object sender, EventArgs e) {
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
			var obj = (DependencyObject)win.GetValue(winChrome_WindowChromeProperty);
			if (obj == null)
				return;

			win.InitializeWindowCaptionAndResizeBorder(obj);
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

		static Type windowChromeType;
		static Type nonClientFrameEdgesType;
		static DependencyProperty winChrome_CaptionHeightProperty;
		static DependencyProperty winChrome_CornerRadiusProperty;
		static DependencyProperty winChrome_GlassFrameThicknessProperty;
		static DependencyProperty winChrome_NonClientFrameEdgesProperty;
		static DependencyProperty winChrome_ResizeBorderThicknessProperty;
		static DependencyProperty winChrome_WindowChromeProperty;
		static DependencyProperty winChrome_IsHitTestVisibleInChromeProperty;

		static MetroWindow() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroWindow), new FrameworkPropertyMetadata(typeof(MetroWindow)));
			Initialize();
		}

		// If these get updated, also update the templates if necessary
		static readonly CornerRadius CornerRadius = new CornerRadius(0, 0, 0, 0);
		static readonly Thickness GlassFrameThickness = new Thickness(0);
		static readonly int NonClientFrameEdges = 0; // 0=None
													 // NOTE: Keep these in sync: CaptionHeight + ResizeBorderThickness.Top = GridCaptionHeight
		static readonly double CaptionHeight = 20;
		static readonly Thickness ResizeBorderThickness = new Thickness(10, 10, 5, 5);
		public static readonly GridLength GridCaptionHeight = new GridLength(CaptionHeight + ResizeBorderThickness.Top, GridUnitType.Pixel);

		static void Initialize() {
			// Available in .NET 4.5. Also available in Microsoft.Windows.Shell.dll (Microsoft Ribbon for WPF)
			var asm = typeof(Window).Assembly;
			var ns = "System.Windows.Shell.";
			windowChromeType = asm.GetType(ns + "WindowChrome", false, false);
			if (windowChromeType == null) {
				asm = Assembly.Load("Microsoft.Windows.Shell, Version=3.5.41019.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				ns = "Microsoft.Windows.Shell.";
				windowChromeType = asm.GetType(ns + "WindowChrome", false, false);
			}
			if (windowChromeType == null)
				throw new ApplicationException("Could not find WindowChrome class");

			nonClientFrameEdgesType = asm.GetType(ns + "NonClientFrameEdges", false, false);
			winChrome_CaptionHeightProperty = (DependencyProperty)windowChromeType.GetField("CaptionHeightProperty").GetValue(null);
			winChrome_CornerRadiusProperty = (DependencyProperty)windowChromeType.GetField("CornerRadiusProperty").GetValue(null);
			winChrome_GlassFrameThicknessProperty = (DependencyProperty)windowChromeType.GetField("GlassFrameThicknessProperty").GetValue(null);
			winChrome_NonClientFrameEdgesProperty = (DependencyProperty)windowChromeType.GetField("NonClientFrameEdgesProperty").GetValue(null);
			winChrome_ResizeBorderThicknessProperty = (DependencyProperty)windowChromeType.GetField("ResizeBorderThicknessProperty").GetValue(null);
			winChrome_WindowChromeProperty = (DependencyProperty)windowChromeType.GetField("WindowChromeProperty").GetValue(null);
			winChrome_IsHitTestVisibleInChromeProperty = (DependencyProperty)windowChromeType.GetField("IsHitTestVisibleInChromeProperty").GetValue(null);
		}

		protected override void OnStateChanged(EventArgs e) {
			base.OnStateChanged(e);
			if (WindowState == WindowState.Normal)
				ClearValue(Window.WindowStateProperty);

			var obj = (DependencyObject)GetValue(winChrome_WindowChromeProperty);
			switch (WindowState) {
			case WindowState.Normal:
				InitializeWindowCaptionAndResizeBorder(obj);
				ClearValue(Window.WindowStateProperty);
				break;

			case WindowState.Minimized:
			case WindowState.Maximized:
				InitializeWindowCaptionAndResizeBorder(obj, false);
				break;

			default:
				break;
			}
		}

		DependencyObject CreateWindowChromeObject() {
			var ctor = windowChromeType.GetConstructor(new Type[0]);
			var obj = (DependencyObject)ctor.Invoke(new object[0]);

			obj.SetValue(winChrome_CornerRadiusProperty, CornerRadius);
			obj.SetValue(winChrome_GlassFrameThicknessProperty, GlassFrameThickness);
			obj.SetValue(winChrome_NonClientFrameEdgesProperty, Enum.ToObject(nonClientFrameEdgesType, NonClientFrameEdges));
			InitializeWindowCaptionAndResizeBorder(obj);

			return obj;
		}

		void InitializeWindowCaptionAndResizeBorder(DependencyObject obj) {
			InitializeWindowCaptionAndResizeBorder(obj, UseResizeBorder);
		}

		void InitializeWindowCaptionAndResizeBorder(DependencyObject obj, bool useResizeBorder) {
			if (useResizeBorder) {
				obj.SetValue(winChrome_CaptionHeightProperty, CaptionHeight);
				obj.SetValue(winChrome_ResizeBorderThicknessProperty, ResizeBorderThickness);
			}
			else {
				if (IsFullScreen)
					obj.SetValue(winChrome_CaptionHeightProperty, 0d);
				else
					obj.SetValue(winChrome_CaptionHeightProperty, GridCaptionHeight.Value);
				obj.SetValue(winChrome_ResizeBorderThicknessProperty, new Thickness(0));
			}
		}

		static void ShowSystemMenu(object o) {
			var depo = o as DependencyObject;
			if (depo == null)
				return;
			var win = Window.GetWindow(depo);
			if (win == null)
				return;

			var p = win.PointToScreen(new Point(0, GridCaptionHeight.Value));
			WindowUtils.ShowSystemMenu(win, p);
		}
	}

	static class WindowUtils {
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

		public static void UpdateWin32Style(MetroWindow window) {
			const int GWL_STYLE = -16;
			const int WS_SYSMENU = 0x00080000;

			IntPtr hWnd = new WindowInteropHelper(window).Handle;

			// The whole title bar is restyled. We must hide the system menu or Windows
			// will sometimes paint the title bar for us.
			SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		public static void ShowSystemMenu(Window window, Point p) {
			var hWnd = new WindowInteropHelper(window).Handle;
			if (hWnd == IntPtr.Zero)
				return;
			if (!IsWindow(hWnd))
				return;

			var hMenu = GetSystemMenu(hWnd, false);
			uint res = TrackPopupMenuEx(hMenu, 0x100, (int)p.X, (int)p.Y, hWnd, IntPtr.Zero);
			if (res != 0)
				PostMessage(hWnd, 0x112, new IntPtr(res), IntPtr.Zero);
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
