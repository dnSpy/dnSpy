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
	/// Creates <see cref="WpfHexViewMargin"/>s
	/// </summary>
	public abstract class WpfHexViewMarginProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected WpfHexViewMarginProvider() { }

		/// <summary>
		/// Creates a margin or returns null
		/// </summary>
		/// <param name="wpfHexViewHost">WPF hex view host</param>
		/// <param name="marginContainer">Margin container</param>
		/// <returns></returns>
		public abstract WpfHexViewMargin? CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer);
	}
}
