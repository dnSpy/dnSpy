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
	/// Creates <see cref="HexBuffer"/>s
	/// </summary>
	public abstract class HexBufferFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferFactoryService() { }

		/// <summary>
		/// Raised when a new <see cref="HexBuffer"/> has been created
		/// </summary>
		public abstract event EventHandler<HexBufferCreatedEventArgs> HexBufferCreated;

		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public abstract HexBuffer Create(string filename);

		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="name">Name, can be anything and is usually the filename</param>
		/// <returns></returns>
		public abstract HexBuffer Create(byte[] data, string name);

		/// <summary>
		/// Creates a new <see cref="HexBuffer"/>
		/// </summary>
		/// <param name="stream">Stream to use</param>
		/// <returns></returns>
		public abstract HexBuffer Create(HexBufferStream stream);
	}
}
