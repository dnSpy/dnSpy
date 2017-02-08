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
using System.ComponentModel;
using dnSpy.Contracts.Settings.AppearanceCategory;

namespace dnSpy.Contracts.Settings.Fonts {
	/// <summary>
	/// Theme font settings
	/// </summary>
	public abstract class ThemeFontSettings : INotifyPropertyChanged {
		/// <summary>
		/// Constructor
		/// </summary>
		protected ThemeFontSettings() { }

		/// <summary>
		/// Raised after a property is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propertyName">Property name</param>
		protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		/// <summary>
		/// Gets the name, eg. <see cref="AppearanceCategoryConstants.TextEditor"/>
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the font type
		/// </summary>
		public abstract FontType FontType { get; }

		/// <summary>
		/// Gets the active <see cref="FontSettings"/> instance
		/// </summary>
		public abstract FontSettings Active { get; }

		/// <summary>
		/// Gets theme settings
		/// </summary>
		/// <param name="themeGuid">Guid of theme</param>
		/// <returns></returns>
		public abstract FontSettings GetSettings(Guid themeGuid);
	}
}
