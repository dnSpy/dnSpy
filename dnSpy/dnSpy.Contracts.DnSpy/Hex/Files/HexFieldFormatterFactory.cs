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

using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Creates <see cref="HexFieldFormatter"/>s
	/// </summary>
	public abstract class HexFieldFormatterFactory {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFieldFormatterFactory() { }

		/// <summary>
		/// Creates a formatter
		/// </summary>
		/// <param name="writer">Text writer</param>
		/// <returns></returns>
		public HexFieldFormatter Create(HexTextWriter writer) =>
			Create(writer, HexFieldFormatterOptions.None, HexNumberOptions.HexCSharp | HexNumberOptions.MinimumDigits, HexNumberOptions.HexCSharp);

		/// <summary>
		/// Creates a formatter
		/// </summary>
		/// <param name="writer">Text writer</param>
		/// <param name="options">Options</param>
		/// <param name="arrayIndexOptions">Array index options</param>
		/// <param name="valueNumberOptions">Value number options</param>
		/// <returns></returns>
		public abstract HexFieldFormatter Create(HexTextWriter writer, HexFieldFormatterOptions options, HexNumberOptions arrayIndexOptions, HexNumberOptions valueNumberOptions);
	}
}
