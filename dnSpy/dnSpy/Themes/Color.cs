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

using System.Diagnostics;

namespace dnSpy.Themes {
	[DebuggerDisplay("{ColorInfo.ColorType}")]
	sealed class Color {
		/// <summary>
		/// Color info
		/// </summary>
		public readonly ColorInfo ColorInfo;

		/// <summary>
		/// Original color with no inherited properties. If this one or any of its properties
		/// get modified, <see cref="Theme.RecalculateInheritedColorProperties()"/> must be
		/// called.
		/// </summary>
		public ThemeColor OriginalColor;

		/// <summary>
		/// Color with inherited properties, but doesn't include inherited default text (because
		/// it messes up with selection in text editor). See also <see cref="InheritedColor"/>
		/// </summary>
		public ThemeColor TextInheritedColor;

		/// <summary>
		/// Color with inherited properties. See also <see cref="TextInheritedColor"/>
		/// </summary>
		public ThemeColor InheritedColor;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		public Color(ColorInfo colorInfo) => ColorInfo = colorInfo;
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
	}
}
