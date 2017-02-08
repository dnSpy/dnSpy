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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// WPF hex view line
	/// </summary>
	public abstract class WpfHexViewLine : HexViewLine {
		/// <summary>
		/// Constructor
		/// </summary>
		protected WpfHexViewLine() { }

		/// <summary>
		/// Gets all text lines
		/// </summary>
		public abstract ReadOnlyCollection<TextLine> TextLines { get; }

		/// <summary>
		/// Gets the visible area
		/// </summary>
		public abstract Rect VisibleArea { get; }

		/// <summary>
		/// Gets the character formatting
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <returns></returns>
		public abstract TextRunProperties GetCharacterFormatting(int linePosition);
	}
}
