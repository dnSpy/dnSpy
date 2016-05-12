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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Color
	/// </summary>
	public struct Color {
		/// <summary>
		/// Color or null
		/// </summary>
		public ITextColor TextColor { get; }

		/// <summary>
		/// Color or null
		/// </summary>
		public ColorType? ColorType { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="color">Color</param>
		public Color(ITextColor color) {
			if (color == null)
				throw new ArgumentNullException(nameof(color));
			TextColor = color;
			ColorType = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="color">Color</param>
		public Color(ColorType color) {
			TextColor = null;
			ColorType = color;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="foreground">Foreground color or null</param>
		/// <param name="background">Background color or null</param>
		/// <param name="fontWeight">Font weight or null</param>
		/// <param name="fontStyle">Font style or null</param>
		public Color(Brush foreground, Brush background = null, FontWeight? fontWeight = null, FontStyle? fontStyle = null) {
			TextColor = new TextColor(foreground, background, fontWeight, fontStyle);
			ColorType = null;
		}

		/// <summary>
		/// Gets the color
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public ITextColor ToTextColor(ITheme theme) => TextColor ?? theme.GetTextColor(ColorType.Value);
	}
}
