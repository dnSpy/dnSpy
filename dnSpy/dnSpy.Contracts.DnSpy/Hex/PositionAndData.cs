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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Position and data
	/// </summary>
	public struct PositionAndData {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Data == null;

		/// <summary>
		/// Gets the position
		/// </summary>
		public HexBufferPoint Position { get; }

		/// <summary>
		/// Gets the data
		/// </summary>
		public byte[] Data { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="data">Data</param>
		public PositionAndData(HexBufferPoint position, byte[] data) {
			if (position.IsDefault)
				throw new ArgumentException();
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			Position = position;
			Data = data;
		}
	}
}
