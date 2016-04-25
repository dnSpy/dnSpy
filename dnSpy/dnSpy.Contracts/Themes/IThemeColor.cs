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

using System.Windows;
using System.Windows.Media;

namespace dnSpy.Contracts.Themes {
	/// <summary>
	/// Theme color
	/// </summary>
	public interface IThemeColor {
		/// <summary>
		/// Name of color
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Font weight or null
		/// </summary>
		FontWeight? FontWeight { get; }

		/// <summary>
		/// Font style or null
		/// </summary>
		FontStyle? FontStyle { get; }

		/// <summary>
		/// Foreground (first) color or null
		/// </summary>
		Brush Foreground { get; }

		/// <summary>
		/// Background (second) color null
		/// </summary>
		Brush Background { get; }

		/// <summary>
		/// Third color or null
		/// </summary>
		Brush Color3 { get; }

		/// <summary>
		/// Fourth color or null
		/// </summary>
		Brush Color4 { get; }
	}
}
