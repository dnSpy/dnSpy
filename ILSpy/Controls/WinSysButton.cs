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
using System.Windows;

namespace ICSharpCode.ILSpy.Controls
{
	public enum WinSysType
	{
		Minimize,
		Maximize,
		Close,
	}

	public enum CurrentWinSysType
	{
		Minimize,
		Maximize,
		Restore,
		Close,
	}

	public class WinSysButton : TabButton
	{
		public static readonly DependencyProperty WinSysTypeProperty =
			DependencyProperty.Register("WinSysType", typeof(WinSysType), typeof(WinSysButton),
			new FrameworkPropertyMetadata(WinSysType.Minimize, OnWinSysTypeChanged));
		public static readonly DependencyProperty CurrentWinSysTypeProperty =
			DependencyProperty.Register("CurrentWinSysType", typeof(CurrentWinSysType), typeof(WinSysButton),
			new FrameworkPropertyMetadata(CurrentWinSysType.Minimize));

		public WinSysType WinSysType {
			get { return (WinSysType)GetValue(WinSysTypeProperty); }
			set { SetValue(WinSysTypeProperty, value); }
		}

		public CurrentWinSysType CurrentWinSysType {
			get { return (CurrentWinSysType)GetValue(CurrentWinSysTypeProperty); }
			set { SetValue(CurrentWinSysTypeProperty, value); }
		}

		Window window;

		static WinSysButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WinSysButton), new FrameworkPropertyMetadata(typeof(WinSysButton)));
		}

		public WinSysButton()
		{
			Loaded += WinSysButton_Loaded;
		}

		void WinSysButton_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= WinSysButton_Loaded;
			window = Window.GetWindow(this);
			window.StateChanged += window_StateChanged;
		}

		void window_StateChanged(object sender, EventArgs e)
		{
			OnWinSysTypeChanged(WinSysType);
		}

		static void OnWinSysTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((WinSysButton)d).OnWinSysTypeChanged((WinSysType)e.NewValue);
		}

		void OnWinSysTypeChanged(WinSysType newValue)
		{
			if (window == null)
				window = Window.GetWindow(this);
			switch (newValue) {
			case Controls.WinSysType.Minimize:
				CurrentWinSysType = Controls.CurrentWinSysType.Minimize;
				break;

			case Controls.WinSysType.Maximize:
				CurrentWinSysType =
					window.WindowState == WindowState.Maximized ?
					Controls.CurrentWinSysType.Restore :
					Controls.CurrentWinSysType.Maximize;
				break;

			case Controls.WinSysType.Close:
				CurrentWinSysType = Controls.CurrentWinSysType.Close;
				break;

			default:
				throw new ArgumentException("Invalid WinSysType");
			}
		}

		protected override void OnClick()
		{
			switch (CurrentWinSysType) {
			case Controls.CurrentWinSysType.Minimize:
				window.WindowState = WindowState.Minimized;
				break;

			case Controls.CurrentWinSysType.Maximize:
				window.WindowState = WindowState.Maximized;
				break;

			case Controls.CurrentWinSysType.Restore:
				window.WindowState = WindowState.Normal;
				break;

			case Controls.CurrentWinSysType.Close:
				window.Close();
				break;

			default:
				throw new ArgumentException("Invalid CurrentWinSysType");
			}
		}
	}
}
