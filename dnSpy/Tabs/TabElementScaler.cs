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
using System.Windows;
using System.Windows.Input;
using dnSpy.Shared.Controls;

namespace dnSpy.Tabs {
	sealed class TabElementScaler : IDisposable {
		readonly List<CommandBinding> commandBindings;
		readonly List<KeyBinding> keyBindings;
		FrameworkElement scaleElement;

		public TabElementScaler() {
			this.commandBindings = new List<CommandBinding>();
			this.keyBindings = new List<KeyBinding>();
			ICommand cmd;
			this.commandBindings.Add(new CommandBinding(cmd = new RoutedCommand("ZoomIncrease", typeof(TabElementScaler)), (s, e) => ZoomIncrease(), (s, e) => e.CanExecute = true));
			keyBindings.Add(new KeyBinding(cmd, Key.OemPlus, ModifierKeys.Control));
			keyBindings.Add(new KeyBinding(cmd, Key.Add, ModifierKeys.Control));
			this.commandBindings.Add(new CommandBinding(cmd = new RoutedCommand("ZoomDecrease", typeof(TabElementScaler)), (s, e) => ZoomDecrease(), (s, e) => e.CanExecute = true));
			keyBindings.Add(new KeyBinding(cmd, Key.OemMinus, ModifierKeys.Control));
			keyBindings.Add(new KeyBinding(cmd, Key.Subtract, ModifierKeys.Control));
			this.commandBindings.Add(new CommandBinding(cmd = new RoutedCommand("ZoomReset", typeof(TabElementScaler)), (s, e) => ZoomReset(), (s, e) => e.CanExecute = true));
			keyBindings.Add(new KeyBinding(cmd, Key.D0, ModifierKeys.Control));
			keyBindings.Add(new KeyBinding(cmd, Key.NumPad0, ModifierKeys.Control));
		}

		public void InstallScale(FrameworkElement elem) {
			UninstallScale();
			scaleElement = elem;
			if (scaleElement == null)
				return;
			// A scrollviewer will prevent our code from getting called so use AddHandler()
			scaleElement.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(ScaleElement_MouseWheel), true);
			scaleElement.CommandBindings.AddRange(commandBindings);
			scaleElement.InputBindings.AddRange(keyBindings);
			ScaleValue = ScaleValue;
		}

		void UninstallScale() {
			if (scaleElement == null)
				return;
			scaleElement.Loaded -= ScaleElement_Loaded;
			scaleElement.RemoveHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(ScaleElement_MouseWheel));
			foreach (var b in commandBindings)
				scaleElement.CommandBindings.Remove(b);
			foreach (var b in keyBindings)
				scaleElement.InputBindings.Remove(b);
			scaleElement = null;
		}

		void ScaleElement_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (Keyboard.Modifiers != ModifierKeys.Control)
				return;

			ZoomMouseWheel(e.Delta);
			e.Handled = true;
		}

		void ZoomMouseWheel(int delta) {
			if (delta > 0)
				ZoomIncrease();
			else if (delta < 0)
				ZoomDecrease();
		}

		const double MIN_ZOOM = 0.2;
		const double MAX_ZOOM = 4.0;

		void ZoomIncrease() {
			var scale = ScaleValue;
			scale *= 1.1;
			ScaleValue = scale;
		}

		void ZoomDecrease() {
			var scale = ScaleValue;
			scale /= 1.1;
			ScaleValue = scale;
		}

		void ZoomReset() {
			ScaleValue = 1;
		}

		public double ScaleValue {
			get { return currentScaleValue; }
			set {
				var scale = value;
				if (double.IsNaN(scale) || Math.Abs(scale - 1.0) < 0.05)
					scale = 1.0;

				if (scale < MIN_ZOOM)
					scale = MIN_ZOOM;
				else if (scale > MAX_ZOOM)
					scale = MAX_ZOOM;

				currentScaleValue = scale;

				if (scaleElement != null)
					AddScaleTransform();
			}
		}
		double currentScaleValue = 1;
		MetroWindow metroWindow;

		void AddScaleTransform() {
			var mwin = GetWindow();
			if (mwin != null)
				mwin.SetScaleTransform(scaleElement, currentScaleValue);
		}

		MetroWindow GetWindow() {
			Debug.Assert(scaleElement != null);
			if (metroWindow != null)
				return metroWindow;
			if (scaleElement == null)
				return null;

			var win = Window.GetWindow(scaleElement);
			metroWindow = win as MetroWindow;
			if (metroWindow != null) {
				metroWindow.WindowDPIChanged += MetroWindow_WindowDPIChanged;
				return metroWindow;
			}

			Debug.Assert(!scaleElement.IsLoaded);
			if (!scaleElement.IsLoaded)
				scaleElement.Loaded += ScaleElement_Loaded;
			return null;
		}

		void MetroWindow_WindowDPIChanged(object sender, EventArgs e) {
			Debug.Assert(sender != null && sender == metroWindow);
			((MetroWindow)sender).SetScaleTransform(scaleElement, currentScaleValue);
		}

		void ScaleElement_Loaded(object sender, RoutedEventArgs e) {
			var fe = (FrameworkElement)sender;
			fe.Loaded -= ScaleElement_Loaded;
			AddScaleTransform();
		}

		public void Dispose() {
			UninstallScale();
		}
	}
}
