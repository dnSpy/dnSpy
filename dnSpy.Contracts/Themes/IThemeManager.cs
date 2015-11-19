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
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Themes {
	/// <summary>
	/// Manages all <see cref="ITheme"/>s
	/// </summary>
	public interface IThemeManager {
		/// <summary>
		/// Gets the current theme
		/// </summary>
		ITheme Theme { get; }

		/// <summary>
		/// Notified when <see cref="Theme"/> gets changed
		/// </summary>
		event EventHandler<ThemeChangedEventArgs> ThemeChanged;

		/// <summary>
		/// Notified when <see cref="Theme"/> gets changed. Gets notified before <see cref="ThemeChanged"/>
		/// and should normally not be used (it's currently only used by <see cref="IImageManager"/>)
		/// </summary>
		event EventHandler<ThemeChangedEventArgs> EarlyThemeChanged;
	}
}
