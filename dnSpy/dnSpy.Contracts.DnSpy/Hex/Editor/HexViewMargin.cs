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

using System;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex view margin
	/// </summary>
	public abstract class HexViewMargin : IDisposable {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewMargin() { }

		/// <summary>
		/// true if the margin is enabled
		/// </summary>
		public abstract bool Enabled { get; }

		/// <summary>
		/// Gets the size of the margin (width or height depending on whether it's a vertical or horizontal margin)
		/// </summary>
		public abstract double MarginSize { get; }

		/// <summary>
		/// Gets a <see cref="HexViewMargin"/> or null if it's not this margin or a child of this margin
		/// </summary>
		/// <param name="marginName">Name of margin</param>
		/// <returns></returns>
		public abstract HexViewMargin? GetHexViewMargin(string marginName);

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public void Dispose() => DisposeCore();

		/// <summary>
		/// Disposes this instance
		/// </summary>
		protected virtual void DisposeCore() { }
	}
}
