/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Extension;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Initialize data template attached property
	/// </summary>
	public sealed class InitDataTemplateAP : DependencyObject {
		/// <summary>
		/// Initialize property
		/// </summary>
		public static readonly DependencyProperty InitializeProperty = DependencyProperty.RegisterAttached(
			"Initialize", typeof(bool), typeof(InitDataTemplateAP), new UIPropertyMetadata(false, InitializePropertyChangedCallback));

		/// <summary>
		/// Sets initialize value
		/// </summary>
		/// <param name="element">Element</param>
		/// <param name="value">New value</param>
		public static void SetInitialize(FrameworkElement element, bool value) => element.SetValue(InitializeProperty, value);

		/// <summary>
		/// Gets the initialize value
		/// </summary>
		/// <param name="element">Element</param>
		/// <returns></returns>
		public static bool GetInitialize(FrameworkElement element) => (bool)element.GetValue(InitializeProperty);

		[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions)]
		sealed class MefState : IAutoLoaded {
			internal static MefState Instance;

			[ImportingConstructor]
			MefState([ImportMany] Lazy<IInitializeDataTemplate>[] entries) {
				Instance = this;
				this.entries = entries;
			}

			internal Lazy<IInitializeDataTemplate>[] entries;
		}

		static void InitializePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if ((bool)e.NewValue) {
				foreach (var elem in MefState.Instance.entries)
					elem.Value.Initialize(d);
			}
		}
	}
}
