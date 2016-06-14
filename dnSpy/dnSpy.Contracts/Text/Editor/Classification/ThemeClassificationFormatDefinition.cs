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
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Text.Editor.Classification {
	/// <summary>
	/// Theme classification definition
	/// </summary>
	public abstract class ThemeClassificationFormatDefinition : ClassificationFormatDefinition {
		readonly ColorType colorType;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="colorType">Color type</param>
		protected ThemeClassificationFormatDefinition(ColorType colorType) {
			this.colorType = colorType;
		}

		/// <summary>
		/// Gets the foreground brush or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public override Brush GetForeground(ITheme theme) => theme.GetExplicitColor(colorType).Foreground;

		/// <summary>
		/// Gets the background brush or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public override Brush GetBackground(ITheme theme) => theme.GetExplicitColor(colorType).Background;

		/// <summary>
		/// Gets the bold value or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public override bool? GetIsBold(ITheme theme) {
			var tc = theme.GetExplicitColor(colorType);
			if (tc.FontWeight == null)
				return null;
			return tc.FontWeight.Value == FontWeights.Bold;
		}

		/// <summary>
		/// Gets the italic value or null
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public override bool? GetIsItalic(ITheme theme) {
			var tc = theme.GetExplicitColor(colorType);
			if (tc.FontStyle == null)
				return null;
			return tc.FontStyle.Value == FontStyles.Italic;
		}
	}
}
