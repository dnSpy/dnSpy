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

using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts;

namespace dnSpy.MVVM {
	public sealed class InitDataTemplateAP : DependencyObject {
		public static readonly DependencyProperty InitializeProperty = DependencyProperty.RegisterAttached(
			"Initialize", typeof(bool), typeof(InitDataTemplateAP), new UIPropertyMetadata(false, InitializePropertyChangedCallback));

		public static void SetInitialize(FrameworkElement element, bool value) {
			element.SetValue(InitializeProperty, value);
		}

		public static bool GetInitialize(FrameworkElement element) {
			return (bool)element.GetValue(InitializeProperty);
		}

		sealed class MefState {
			public static readonly MefState Instance = new MefState();

			MefState() {
				Globals.App.CompositionContainer.ComposeParts(this);
			}

			[ImportMany(typeof(IInitializeDataTemplate))]
			public IInitializeDataTemplate[] entries = null;
		}

		static void InitializePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if ((bool)e.NewValue) {
				foreach (var elem in MefState.Instance.entries)
					elem.Initialize(d);
			}
		}
	}
}
