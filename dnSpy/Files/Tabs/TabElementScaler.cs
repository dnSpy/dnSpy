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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace dnSpy.Files.Tabs {
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
			scaleElement.MouseWheel += ScaleElement_MouseWheel;
			scaleElement.CommandBindings.AddRange(commandBindings);
			scaleElement.InputBindings.AddRange(keyBindings);
			ScaleValue = ScaleValue;
		}

		void UninstallScale() {
			if (scaleElement == null)
				return;
			scaleElement.MouseWheel -= ScaleElement_MouseWheel;
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
			scale += scale / 10;
			ScaleValue = scale;
		}

		void ZoomDecrease() {
			var scale = ScaleValue;
			scale -= scale / 10;
			ScaleValue = scale;
		}

		void ZoomReset() {
			ScaleValue = 1;
		}

		public double ScaleValue {
			get { return currentScaleValue; }
			set {
				var scale = value;
				if (double.IsNaN(scale))
					scale = 1.0;
				if (scaleElement == null) {
				}
				else if (scale == 1) {
					scaleElement.LayoutTransform = Transform.Identity;
					scaleElement.ClearValue(TextOptions.TextFormattingModeProperty);
				}
				else {
					if (scale < MIN_ZOOM)
						scale = MIN_ZOOM;
					else if (scale > MAX_ZOOM)
						scale = MAX_ZOOM;

					// We must set it to Ideal or the text will be blurry
					TextOptions.SetTextFormattingMode(scaleElement, TextFormattingMode.Ideal);

					var st = new ScaleTransform(scale, scale);
					st.Freeze();
					scaleElement.LayoutTransform = st;
				}
				currentScaleValue = scale;
			}
		}
		double currentScaleValue = 1;

		public void Dispose() {
			UninstallScale();
		}
	}
}
