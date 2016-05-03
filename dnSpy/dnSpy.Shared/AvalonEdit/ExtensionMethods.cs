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

using System.Windows.Media;
using dnSpy.Contracts.Themes;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Shared.AvalonEdit {
	sealed class MyHighlightingBrush : HighlightingBrush {
		readonly Brush brush;

		MyHighlightingBrush(Brush brush) {
			this.brush = brush;
		}

		public static HighlightingBrush Create(Brush brush) {
			if (brush == null)
				return null;
			return new MyHighlightingBrush(brush);
		}

		public override Brush GetBrush(ITextRunConstructionContext context) => brush;
	}

	/// <summary>
	/// AvalonEdit extension methods
	/// </summary>
	public static class ExtensionMethods {
		/// <summary>
		/// Converts <paramref name="self"/> to a <see cref="HighlightingColor"/> instance or null
		/// if input is null
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static HighlightingColor ToHighlightingColor(this ITextColor self) {
			if (self == null)
				return null;
			var hl = new HighlightingColor {
				Name = "???",
				FontWeight = self.FontWeight,
				FontStyle = self.FontStyle,
				Underline = null,
				Foreground = MyHighlightingBrush.Create(self.Foreground),
				Background = MyHighlightingBrush.Create(self.Background),
			};
			hl.Freeze();
			return hl;
		}

		/// <summary>
		/// Creates a <see cref="HighlightingBrush"/> or returns null if input is null
		/// </summary>
		/// <param name="brush">Brush</param>
		/// <returns></returns>
		public static HighlightingBrush ToHighlightingBrush(this Brush brush) => MyHighlightingBrush.Create(brush);
	}
}
