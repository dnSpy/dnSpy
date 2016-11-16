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
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// Formatted line source
	/// </summary>
	public abstract class HexFormattedLineSource {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFormattedLineSource() { }

		/// <summary>
		/// Gets the default text properties
		/// </summary>
		public abstract TextRunProperties DefaultTextProperties { get; }

		/// <summary>
		/// Gets the sequencer
		/// </summary>
		public abstract HexAndAdornmentSequencer HexAndAdornmentSequencer { get; }

		/// <summary>
		/// Gets the base indentation
		/// </summary>
		public abstract double BaseIndentation { get; }

		/// <summary>
		/// Gets the width of a column
		/// </summary>
		public abstract double ColumnWidth { get; }

		/// <summary>
		/// Gets the nominal line height
		/// </summary>
		public abstract double LineHeight { get; }

		/// <summary>
		/// Gets the nominal height of the text above the baseline
		/// </summary>
		public abstract double TextHeightAboveBaseline { get; }

		/// <summary>
		/// Gets the nominal height of the text below the baseline
		/// </summary>
		public abstract double TextHeightBelowBaseline { get; }

		/// <summary>
		/// true to use <see cref="TextFormattingMode.Display"/> mode, false to use
		/// <see cref="TextFormattingMode.Ideal"/> mode
		/// </summary>
		public abstract bool UseDisplayMode { get; }

		/// <summary>
		/// Formats a line
		/// </summary>
		/// <param name="line">Buffer line</param>
		/// <returns></returns>
		public abstract HexFormattedLine FormatLineInVisualBuffer(HexBufferLine line);
	}
}
