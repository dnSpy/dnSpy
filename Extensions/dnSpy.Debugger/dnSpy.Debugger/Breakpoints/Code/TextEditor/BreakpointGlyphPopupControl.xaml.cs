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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	sealed partial class BreakpointGlyphPopupControl : UserControl {
		readonly BreakpointGlyphPopupVM vm;
		readonly FrameworkElement glyphMargin;
		readonly IInputElement focusedElement;
		bool loaded;
		DispatcherTimer? timer;
		Storyboard? fadeOutStoryboard;

		public BreakpointGlyphPopupControl(BreakpointGlyphPopupVM vm, FrameworkElement glyphMargin) {
			InitializeComponent();
			DataContext = vm;
			this.vm = vm;
			this.glyphMargin = glyphMargin;
			focusedElement = Keyboard.FocusedElement;
			Loaded += BreakpointGlyphPopupControl_Loaded;
			Unloaded += BreakpointGlyphPopupControl_Unloaded;
		}

		void RemoveFadeOut() {
			if (!(fadeOutStoryboard is null)) {
				fadeOutStoryboard.Completed -= FadeOutStoryboard_Completed;
				fadeOutStoryboard.Stop();
				fadeOutStoryboard = null;
			}
		}

		void FadeOut() {
			StopTimer();
			var animation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1)));
			fadeOutStoryboard = new Storyboard();
			fadeOutStoryboard.Children.Add(animation);
			Storyboard.SetTarget(animation, this);
			Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
			fadeOutStoryboard.Completed += FadeOutStoryboard_Completed;
			fadeOutStoryboard.Begin();
		}

		void FadeOutStoryboard_Completed(object? sender, EventArgs e) => ClosePopup();

		void BreakpointGlyphPopupControl_Loaded(object? sender, RoutedEventArgs e) {
			if (loaded)
				return;
			loaded = true;
			if (!(focusedElement is null))
				focusedElement.LostKeyboardFocus += FocusedElement_LostKeyboardFocus;
			glyphMargin.MouseEnter += GlyphMargin_MouseEnter;
			glyphMargin.MouseLeave += GlyphMargin_MouseLeave;
			vm.BeforeExecuteCommand += BreakpointGlyphPopupVM_BeforeExecuteCommand;
			StartTimerIfNeeded();
		}

		void BreakpointGlyphPopupControl_Unloaded(object? sender, RoutedEventArgs e) => ClosePopup();
		void FocusedElement_LostKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) => ClosePopup();
		void BreakpointGlyphPopupVM_BeforeExecuteCommand(object? sender, EventArgs e) => ClosePopup();
		void Timer_Tick(object? sender, EventArgs e) => FadeOut();
		void GlyphMargin_MouseEnter(object? sender, MouseEventArgs e) => StopTimer();
		void GlyphMargin_MouseLeave(object? sender, MouseEventArgs e) => StartTimerIfNeeded();

		protected override void OnMouseEnter(MouseEventArgs e) {
			base.OnMouseEnter(e);
			StopTimer();
		}

		protected override void OnMouseLeave(MouseEventArgs e) {
			base.OnMouseLeave(e);
			StartTimerIfNeeded();
		}

		bool IsMouseWithinControls => glyphMargin.IsMouseOver || IsMouseOver;

		void StopTimer() {
			RemoveFadeOut();
			if (timer is null)
				return;
			timer.Stop();
			timer.Tick -= Timer_Tick;
			timer = null;
		}

		void StartTimerIfNeeded() {
			if (IsMouseWithinControls)
				return;
			if (!(timer is null))
				return;
			timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
			timer.Tick += Timer_Tick;
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Start();
		}

		void ClosePopup() {
			StopTimer();
			if (Parent is Popup popup)
				popup.IsOpen = false;

			if (!(focusedElement is null))
				focusedElement.LostKeyboardFocus -= FocusedElement_LostKeyboardFocus;
			glyphMargin.MouseEnter -= GlyphMargin_MouseEnter;
			glyphMargin.MouseLeave -= GlyphMargin_MouseLeave;
			vm.BeforeExecuteCommand -= BreakpointGlyphPopupVM_BeforeExecuteCommand;
			Loaded -= BreakpointGlyphPopupControl_Loaded;
			Unloaded -= BreakpointGlyphPopupControl_Unloaded;
		}
	}
}
