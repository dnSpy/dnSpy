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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	/// <summary>
	/// .NET metadata token
	/// </summary>
	public class TokenData : UInt32Data {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Data span</param>
		public TokenData(HexBufferSpan span)
			: base(span) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="position">Position</param>
		public TokenData(HexBuffer buffer, HexPosition position)
			: this(new HexBufferSpan(buffer, new HexSpan(position, 4))) {
		}

		/// <summary>
		/// Writes the value
		/// </summary>
		/// <param name="formatter">Formatter</param>
		public override void WriteValue(HexFieldFormatter formatter) => formatter.WriteToken(ReadValue());
	}
}
