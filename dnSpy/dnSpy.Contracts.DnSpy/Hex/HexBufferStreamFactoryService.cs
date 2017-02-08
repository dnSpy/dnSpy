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

using System;
using System.IO;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Creates <see cref="HexBufferStream"/>s
	/// </summary>
	public abstract class HexBufferStreamFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferStreamFactoryService() { }

		/// <summary>
		/// Creates a <see cref="HexBufferStream"/>
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public HexBufferStream Create(string filename) {
			if (filename == null)
				throw new ArgumentNullException(nameof(filename));
			return Create(File.ReadAllBytes(filename), filename);
		}

		/// <summary>
		/// Creates a <see cref="HexBufferStream"/>
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="name">Name, can be anything and is usually the filename</param>
		/// <returns></returns>
		public abstract HexBufferStream Create(byte[] data, string name);

		/// <summary>
		/// Creates a <see cref="HexBufferStream"/>
		/// </summary>
		/// <param name="simpleStream">Underlying stream</param>
		/// <param name="disposeStream">true if the returned stream owns <paramref name="simpleStream"/> and
		/// disposes it when the returned stream gets disposed</param>
		/// <returns></returns>
		public abstract HexCachedBufferStream CreateCached(HexSimpleBufferStream simpleStream, bool disposeStream);

		/// <summary>
		/// Creates a process stream
		/// </summary>
		/// <param name="hProcess">Process handle</param>
		/// <param name="name">Name or null to use the default name</param>
		/// <param name="isReadOnly">true if it's read only</param>
		/// <param name="isVolatile">true if the memory can be changed by other code</param>
		/// <returns></returns>
		public HexCachedBufferStream CreateCachedProcessStream(IntPtr hProcess, string name = null, bool isReadOnly = false, bool isVolatile = true) {
			var simpleStream = CreateSimpleProcessStream(hProcess, name, isReadOnly, isVolatile);
			return CreateCached(simpleStream, disposeStream: true);
		}

		/// <summary>
		/// Creates a process stream
		/// </summary>
		/// <param name="hProcess">Process handle</param>
		/// <param name="name">Name or null to use the default name</param>
		/// <param name="isReadOnly">true if it's read only</param>
		/// <param name="isVolatile">true if the memory can be changed by other code</param>
		/// <returns></returns>
		public abstract HexSimpleBufferStream CreateSimpleProcessStream(IntPtr hProcess, string name = null, bool isReadOnly = false, bool isVolatile = true);
	}
}
