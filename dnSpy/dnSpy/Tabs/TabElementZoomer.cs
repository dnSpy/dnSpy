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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Controls;

namespace dnSpy.Tabs {
	sealed class TabElementZoomer : IDisposable {
		readonly List<CommandBinding> commandBindings;
		readonly List<KeyBinding> keyBindings;
		FrameworkElement zoomElement;
		IUIObjectProvider uiObjectProvider;
		IZoomable zoomable;

		public TabElementZoomer() {
			this.commandBindings = new List<CommandBinding>();
			this.keyBindings = new List<KeyBinding>();
			ICommand cmd;
			this.commandBindings.Add(new CommandBinding(cmd = new RoutedCommand("ZoomIncrease", typeof(TabElementZoomer)), (s, e) => ZoomIncrease(), (s, e) => e.CanExecute = true));
			keyBindings.Add(new KeyBinding(cmd, Key.OemPlus, ModifierKeys.Control));
			keyBindings.Add(new KeyBinding(cmd, Key.Add, ModifierKeys.Control));
			this.commandBindings.Add(new CommandBinding(cmd = new RoutedCommand("ZoomDecrease", typeof(TabElementZoomer)), (s, e) => ZoomDecrease(), (s, e) => e.CanExecute = true));
			keyBindings.Add(new KeyBinding(cmd, Key.OemMinus, ModifierKeys.Control));
			keyBindings.Add(new KeyBinding(cmd, Key.Subtract, ModifierKeys.Control));
			this.commandBindings.Add(new CommandBinding(cmd = new RoutedCommand("ZoomReset", typeof(TabElementZoomer)), (s, e) => ZoomReset(), (s, e) => e.CanExecute = true));
			keyBindings.Add(new KeyBinding(cmd, Key.D0, ModifierKeys.Control));
			keyBindings.Add(new KeyBinding(cmd, Key.NumPad0, ModifierKeys.Control));
		}

		public void InstallZoom(IUIObjectProvider provider, FrameworkElement elem) {
			var zoomable = (provider as IZoomableProvider)?.Zoomable ?? provider as IZoomable;
			if (zoomable != null)
				InstallScaleCore(provider, zoomable);
			else
				InstallScaleCore(provider, elem);
		}

		void InstallScaleCore(IUIObjectProvider provider, IZoomable zoomable) {
			UninstallScale();
			if (zoomable == null)
				return;
			this.zoomable = zoomable;
			SetZoomValue(zoomable.ZoomValue, force: true);
		}

		void InstallScaleCore(IUIObjectProvider provider, FrameworkElement elem) {
			UninstallScale();
			zoomElement = elem;
			uiObjectProvider = provider;
			if (zoomElement == null)
				return;
			zoomElement.PreviewMouseWheel += ZoomElement_PreviewMouseWheel;
			zoomElement.CommandBindings.AddRange(commandBindings);
			zoomElement.InputBindings.AddRange(keyBindings);
			SetZoomValue(ZoomValue, force: true);
		}

		void UninstallScale() {
			currentZoomValue = 1;
			zoomable = null;
			uiObjectProvider = null;
			if (zoomElement == null)
				return;
			if (metroWindow != null)
				metroWindow.WindowDpiChanged -= MetroWindow_WindowDpiChanged;
			zoomElement.Loaded -= ZoomElement_Loaded;
			zoomElement.PreviewMouseWheel -= ZoomElement_PreviewMouseWheel;
			foreach (var b in commandBindings)
				zoomElement.CommandBindings.Remove(b);
			foreach (var b in keyBindings)
				zoomElement.InputBindings.Remove(b);
			zoomElement = null;
		}

		void ZoomElement_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
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

		void ZoomIncrease() => ZoomValue = ZoomSelector.ZoomIn(ZoomValue * 100) / 100;
		void ZoomDecrease() => ZoomValue = ZoomSelector.ZoomOut(ZoomValue * 100) / 100;
		void ZoomReset() => ZoomValue = 1;

		public double ZoomValue {
			get { return zoomable?.ZoomValue ?? currentZoomValue; }
			set { SetZoomValue(value, force: false); }
		}
		double currentZoomValue = 1;
		MetroWindow metroWindow;

		void SetZoomValue(double value, bool force) {
			var newZoomValue = value;
			if (double.IsNaN(newZoomValue) || Math.Abs(newZoomValue - 1.0) < 0.05)
				newZoomValue = 1.0;

			if (newZoomValue < ZoomSelector.MinZoomLevel / 100)
				newZoomValue = ZoomSelector.MinZoomLevel / 100;
			else if (newZoomValue > ZoomSelector.MaxZoomLevel / 100)
				newZoomValue = ZoomSelector.MaxZoomLevel / 100;

			if (!force && currentZoomValue == newZoomValue)
				return;

			currentZoomValue = newZoomValue;

			if (zoomElement != null)
				AddScaleTransform();
		}

		void AddScaleTransform() {
			var mwin = GetWindow();
			if (mwin != null) {
				mwin.SetScaleTransform(zoomElement, currentZoomValue);
				DsImage.SetZoom(zoomElement, currentZoomValue);
			}
		}

		MetroWindow GetWindow() {
			Debug.Assert(zoomElement != null);
			if (metroWindow != null)
				return metroWindow;
			if (zoomElement == null)
				return null;

			var win = Window.GetWindow(zoomElement);
			metroWindow = win as MetroWindow;
			if (metroWindow != null) {
				metroWindow.WindowDpiChanged += MetroWindow_WindowDpiChanged;
				return metroWindow;
			}

			// zoomElement.IsLoaded can be true if we've moved a tool window, so always hook the
			// loaded event.
			zoomElement.Loaded -= ZoomElement_Loaded;
			zoomElement.Loaded += ZoomElement_Loaded;

			return null;
		}

		void MetroWindow_WindowDpiChanged(object sender, EventArgs e) {
			Debug.Assert(sender != null && sender == metroWindow);
			((MetroWindow)sender).SetScaleTransform(zoomElement, currentZoomValue);
		}

		void ZoomElement_Loaded(object sender, RoutedEventArgs e) {
			var fe = (FrameworkElement)sender;
			fe.Loaded -= ZoomElement_Loaded;
			AddScaleTransform();
		}

		public void Dispose() => UninstallScale();
	}
}
