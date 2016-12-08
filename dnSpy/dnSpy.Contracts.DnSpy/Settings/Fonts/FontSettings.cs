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
using System.ComponentModel;
using System.Windows.Media;

namespace dnSpy.Contracts.Settings.Fonts {
	/// <summary>
	/// Font settings
	/// </summary>
	public abstract class FontSettings : INotifyPropertyChanged {
		/// <summary>
		/// Constructor
		/// </summary>
		protected FontSettings() { }

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
		/// Gets the owner
		/// </summary>
		public abstract ThemeFontSettings ThemeFontSettings { get; }

		/// <summary>
		/// Gets the theme guid
		/// </summary>
		public abstract Guid ThemeGuid { get; }

		/// <summary>
		/// Gets the font type
		/// </summary>
		public FontType FontType => ThemeFontSettings.FontType;

		/// <summary>
		/// Gets/sets the font family
		/// </summary>
		public abstract FontFamily FontFamily { get; set; }

		/// <summary>
		/// Gets/sets the font size
		/// </summary>
		public abstract double FontSize { get; set; }
	}
}
