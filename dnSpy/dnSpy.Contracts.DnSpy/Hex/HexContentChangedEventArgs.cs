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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex content changed event args
	/// </summary>
	public sealed class HexContentChangedEventArgs : HexVersionChangedEventArgs {
		/// <summary>
		/// Gets the changes
		/// </summary>
		public NormalizedHexChangeCollection Changes => BeforeVersion.Changes;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="beforeVersion">Version before the change</param>
		/// <param name="afterVersion">Version after the change</param>
		/// <param name="editTag">Edit tag</param>
		public HexContentChangedEventArgs(HexVersion beforeVersion, HexVersion afterVersion, object editTag)
			: base(beforeVersion, afterVersion, editTag) {
		}
	}
}
