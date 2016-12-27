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
	/// Provides <see cref="DotNetMethodBody"/> instances
	/// </summary>
	public abstract class DotNetMethodProvider : IBufferFileHeaders {
		/// <summary>
		/// Returns true if <paramref name="position"/> is probably within a method body
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract bool IsMethodPosition(HexPosition position);

		/// <summary>
		/// Gets a method or null if <paramref name="position"/> isn't within a method body
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract DotNetMethodBody GetMethodBody(HexPosition position);
	}
}
