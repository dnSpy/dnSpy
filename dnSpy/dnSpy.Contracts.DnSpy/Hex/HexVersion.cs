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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// <see cref="Buffer"/> version
	/// </summary>
	public abstract class HexVersion {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexVersion() { }

		/// <summary>
		/// Gets the <see cref="Buffer"/>
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// Gets all hex changes or null if this is the latest version
		/// </summary>
		public abstract NormalizedHexChangeCollection Changes { get; }

		/// <summary>
		/// Next version or null if this is the latest version
		/// </summary>
		public abstract HexVersion Next { get; }

		/// <summary>
		/// Version number
		/// </summary>
		public abstract int VersionNumber { get; }

		/// <summary>
		/// Re-iterated version number used by undo/redo
		/// </summary>
		public abstract int ReiteratedVersionNumber { get; }
	}
}
