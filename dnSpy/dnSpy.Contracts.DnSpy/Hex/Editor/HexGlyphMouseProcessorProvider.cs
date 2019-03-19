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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Creates <see cref="HexMouseProcessor"/>s
	/// </summary>
	public abstract class HexGlyphMouseProcessorProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexGlyphMouseProcessorProvider() { }

		/// <summary>
		/// Creates a mouse processor or returns null
		/// </summary>
		/// <param name="wpfHexViewHost">Hex view host</param>
		/// <param name="margin">Margin</param>
		/// <returns></returns>
		public abstract HexMouseProcessor GetAssociatedMouseProcessor(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin margin);
	}
}
