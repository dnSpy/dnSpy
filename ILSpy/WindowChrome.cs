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
using ICSharpCode.ILSpy.AsmEditor;

namespace ICSharpCode.ILSpy
{
	public enum WindowChromeType
	{
		None,
		MainWindow,
		Dialog,
	}

	public sealed class WindowChrome : DependencyObject
	{
		public static ICommand ShowSystemMenuCommand {
			get { return new RelayCommand(a => ShowSystemMenu(a), a => true); }
		}

		public static readonly DependencyProperty WindowChromeTypeProperty = DependencyProperty.RegisterAttached(
			"WindowChromeType", typeof(WindowChromeType), typeof(WindowChrome), new UIPropertyMetadata(WindowChromeType.None, OnWindowChromeTypeChanged));

		static void OnWindowChromeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var win = d as Window;
			if (win == null)
				return;

			string normalStyle, chromeStyle;
			switch ((WindowChromeType)e.NewValue)
			{
			case WindowChromeType.None:
				return;

			case WindowChromeType.MainWindow:
				normalStyle = "MainWindowStyle";
				chromeStyle = "ChromeMainWindowStyle";
				break;

			case WindowChromeType.Dialog:
				normalStyle = "DialogWindowStyle";
				chromeStyle = "ChromeDialogWindowStyle";
				// Can't be set in a style since it's not a dep prop, so set it here
				win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				break;

			default: throw new ArgumentException("Invalid WindowChromeType value");
			}

			win.Style = (Style)win.FindResource(HasWindowChrome ? chromeStyle : normalStyle);
			AddWindowChrome(win);
		}

		public static void SetWindowChromeType(UIElement element, WindowChromeType value)
		{
			element.SetValue(WindowChromeTypeProperty, value);
		}

		public static WindowChromeType GetWindowChromeType(UIElement element)
		{
			return (WindowChromeType)element.GetValue(WindowChromeTypeProperty);
		}

		public static readonly DependencyProperty IsHitTestVisibleInChromeProperty = DependencyProperty.RegisterAttached(
			"IsHitTestVisibleInChrome", typeof(bool), typeof(WindowChrome), new UIPropertyMetadata(false, OnIsHitTestVisibleInChromeChanged));

		static void OnIsHitTestVisibleInChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!HasWindowChrome)
				return;

			var elem = d as UIElement;
			if (elem != null)
				elem.SetValue(winChrome_IsHitTestVisibleInChromeProperty, e.NewValue);
		}

		public static void SetIsHitTestVisibleInChrome(UIElement element, bool value)
		{
			element.SetValue(IsHitTestVisibleInChromeProperty, value);
		}

		public static bool GetIsHitTestVisibleInChrome(UIElement element)
		{
			return (bool)element.GetValue(IsHitTestVisibleInChromeProperty);
		}

		public static readonly DependencyProperty MaximizedElementProperty = DependencyProperty.RegisterAttached(
			"MaximizedElement", typeof(bool), typeof(WindowChrome), new UIPropertyMetadata(false, OnMaximizedElementChanged));

		static void OnMaximizedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
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
		sealed class MaximizedWindowFixer
		{
			readonly Border border;
			readonly Thickness oldThickness;

			public MaximizedWindowFixer(Window win, Border border)
			{
				this.border = border;
				this.oldThickness = border.BorderThickness;
				win.StateChanged += win_StateChanged;
				border.Loaded += border_Loaded;
			}

			void border_Loaded(object sender, RoutedEventArgs e)
			{
				border.Loaded -= border_Loaded;
				UpdatePadding(Window.GetWindow(border));
			}

			void win_StateChanged(object sender, EventArgs e)
			{
				UpdatePadding((Window)sender);
			}

			void UpdatePadding(Window window)
			{
				Debug.Assert(window != null);
				switch (window.WindowState) {
				default:
				case WindowState.Normal:
					border.ClearValue(Border.PaddingProperty);
					border.BorderThickness = oldThickness;
					break;

				case WindowState.Minimized:
				case WindowState.Maximized:
					const int magic = 2;
					border.Padding = new Thickness(
						SystemParameters.BorderWidth + border.BorderThickness.Left + magic,
						SystemParameters.BorderWidth + border.BorderThickness.Top + magic,
						SystemParameters.BorderWidth + border.BorderThickness.Right + magic,
						SystemParameters.BorderWidth + border.BorderThickness.Bottom + magic);
					border.BorderThickness = new Thickness(0);
					break;
				}
			}
		}

		public static void SetMaximizedElement(UIElement element, bool value)
		{
			element.SetValue(MaximizedElementProperty, value);
		}

		public static bool GetMaximizedElement(UIElement element)
		{
			return (bool)element.GetValue(MaximizedElementProperty);
		}

		public static readonly DependencyProperty ShowMenuButtonProperty = DependencyProperty.RegisterAttached(
			"ShowMenuButton", typeof(bool), typeof(WindowChrome), new UIPropertyMetadata(true));
		public static readonly DependencyProperty ShowMinimizeButtonProperty = DependencyProperty.RegisterAttached(
			"ShowMinimizeButton", typeof(bool), typeof(WindowChrome), new UIPropertyMetadata(true));
		public static readonly DependencyProperty ShowMaximizeButtonProperty = DependencyProperty.RegisterAttached(
			"ShowMaximizeButton", typeof(bool), typeof(WindowChrome), new UIPropertyMetadata(true));
		public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.RegisterAttached(
			"ShowCloseButton", typeof(bool), typeof(WindowChrome), new UIPropertyMetadata(true));

		public static void SetShowMenuButton(UIElement element, bool value)
		{
			element.SetValue(ShowMenuButtonProperty, value);
		}

		public static bool GetShowMenuButton(UIElement element)
		{
			return (bool)element.GetValue(ShowMenuButtonProperty);
		}

		public static void SetShowMinimizeButton(UIElement element, bool value)
		{
			element.SetValue(ShowMinimizeButtonProperty, value);
		}

		public static bool GetShowMinimizeButton(UIElement element)
		{
			return (bool)element.GetValue(ShowMinimizeButtonProperty);
		}

		public static void SetShowMaximizeButton(UIElement element, bool value)
		{
			element.SetValue(ShowMaximizeButtonProperty, value);
		}

		public static bool GetShowMaximizeButton(UIElement element)
		{
			return (bool)element.GetValue(ShowMaximizeButtonProperty);
		}

		public static void SetShowCloseButton(UIElement element, bool value)
		{
			element.SetValue(ShowCloseButtonProperty, value);
		}

		public static bool GetShowCloseButton(UIElement element)
		{
			return (bool)element.GetValue(ShowCloseButtonProperty);
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

		public static bool HasWindowChrome {
			get { return windowChromeType != null; }
		}

		static WindowChrome()
		{
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

		static void Initialize()
		{
			// Available in .NET 4.5. Also available in Microsoft.Windows.Shell.dll (Microsoft Ribbon for WPF)
			var asm = typeof(Window).Assembly;
			var ns = "System.Windows.Shell.";
			windowChromeType = asm.GetType(ns + "WindowChrome", false, false);
			if (windowChromeType == null) {
				try {
					asm = Assembly.Load("Microsoft.Windows.Shell, Version=3.5.41019.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					ns = "Microsoft.Windows.Shell.";
					windowChromeType = asm.GetType(ns + "WindowChrome", false, false);
				}
				catch {
				}
			}
			if (windowChromeType == null)
				return;

			nonClientFrameEdgesType = asm.GetType(ns + "NonClientFrameEdges", false, false);
			winChrome_CaptionHeightProperty = (DependencyProperty)windowChromeType.GetField("CaptionHeightProperty").GetValue(null);
			winChrome_CornerRadiusProperty = (DependencyProperty)windowChromeType.GetField("CornerRadiusProperty").GetValue(null);
			winChrome_GlassFrameThicknessProperty = (DependencyProperty)windowChromeType.GetField("GlassFrameThicknessProperty").GetValue(null);
			winChrome_NonClientFrameEdgesProperty = (DependencyProperty)windowChromeType.GetField("NonClientFrameEdgesProperty").GetValue(null);
			winChrome_ResizeBorderThicknessProperty = (DependencyProperty)windowChromeType.GetField("ResizeBorderThicknessProperty").GetValue(null);
			winChrome_WindowChromeProperty = (DependencyProperty)windowChromeType.GetField("WindowChromeProperty").GetValue(null);
			winChrome_IsHitTestVisibleInChromeProperty = (DependencyProperty)windowChromeType.GetField("IsHitTestVisibleInChromeProperty").GetValue(null);
		}

		static void AddWindowChrome(Window window)
		{
			if (!HasWindowChrome)
				return;

			var obj = CreateWindowChromeObject();
			window.SetValue(winChrome_WindowChromeProperty, obj);
			window.StateChanged += window_StateChanged;
		}

		static void window_StateChanged(object sender, EventArgs e)
		{
			var window = (Window)sender;
			var obj = (DependencyObject)window.GetValue(winChrome_WindowChromeProperty);
			switch (window.WindowState) {
			case WindowState.Normal:
				obj.SetValue(winChrome_CaptionHeightProperty, CaptionHeight);
				obj.SetValue(winChrome_ResizeBorderThicknessProperty, ResizeBorderThickness);
				break;

			case WindowState.Minimized:
			case WindowState.Maximized:
				obj.SetValue(winChrome_CaptionHeightProperty, CaptionHeight + ResizeBorderThickness.Top);
				obj.SetValue(winChrome_ResizeBorderThicknessProperty, new Thickness(0));
				break;

			default:
				break;
			}
		}

		static DependencyObject CreateWindowChromeObject()
		{
			var ctor = windowChromeType.GetConstructor(new Type[0]);
			var obj = (DependencyObject)ctor.Invoke(new object[0]);

			obj.SetValue(winChrome_CaptionHeightProperty, CaptionHeight);
			obj.SetValue(winChrome_CornerRadiusProperty, CornerRadius);
			obj.SetValue(winChrome_GlassFrameThicknessProperty, GlassFrameThickness);
			obj.SetValue(winChrome_NonClientFrameEdgesProperty, Enum.ToObject(nonClientFrameEdgesType, NonClientFrameEdges));
			obj.SetValue(winChrome_ResizeBorderThicknessProperty, ResizeBorderThickness);

			return obj;
		}

		[DllImport("user32")]
		static extern bool IsWindow(IntPtr hWnd);
		[DllImport("user32")]
		static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
		[DllImport("user32")]
		static extern uint TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);
		[DllImport("user32")]
		static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		static void ShowSystemMenu(object o)
		{
			var depo = o as DependencyObject;
			if (depo == null)
				return;
			var win = Window.GetWindow(depo);
			if (win == null)
				return;

			var p = win.PointToScreen(new Point(0, GridCaptionHeight.Value));

			var hWnd = new WindowInteropHelper(win).Handle;
			if (hWnd == IntPtr.Zero)
				return;
			if (!IsWindow(hWnd))
				return;

			var hMenu = GetSystemMenu(hWnd, false);
			uint res = TrackPopupMenuEx(hMenu, 0x100, (int)p.X, (int)p.Y, hWnd, IntPtr.Zero);
			if (res != 0)
				PostMessage(hWnd, 0x112, new IntPtr(res), IntPtr.Zero);
		}
	}
}
