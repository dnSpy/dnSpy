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

namespace dnSpy.Contracts.Themes {
	/// <summary>
	/// Manages all <see cref="ITheme"/>s
	/// </summary>
	public interface IThemeService {
		/// <summary>
		/// Gets the current theme
		/// </summary>
		ITheme Theme { get; }

		/// <summary>
		/// Raised when <see cref="Theme"/> gets changed, and is raised before <see cref="ThemeChanged"/>
		/// and <see cref="ThemeChangedLowPriority"/>
		/// </summary>
		event EventHandler<ThemeChangedEventArgs> ThemeChangedHighPriority;

		/// <summary>
		/// Raised when <see cref="Theme"/> gets changed and is notified after <see cref="ThemeChangedHighPriority"/>
		/// and before <see cref="ThemeChangedLowPriority"/>
		/// </summary>
		event EventHandler<ThemeChangedEventArgs> ThemeChanged;

		/// <summary>
		/// Raised when <see cref="Theme"/> gets changed and is notified after <see cref="ThemeChangedHighPriority"/>
		/// and <see cref="ThemeChanged"/>
		/// </summary>
		event EventHandler<ThemeChangedEventArgs> ThemeChangedLowPriority;
	}
}
