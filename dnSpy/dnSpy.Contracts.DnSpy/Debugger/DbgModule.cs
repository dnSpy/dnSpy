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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// A module in a process
	/// </summary>
	public abstract class DbgModule : DbgObject {
		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// true if <see cref="Address"/> and <see cref="Size"/> are valid
		/// </summary>
		public bool HasAddress => Address != 0 && Size != 0;

		/// <summary>
		/// Address of module. Only valid if <see cref="HasAddress"/> is true
		/// </summary>
		public abstract ulong Address { get; }

		/// <summary>
		/// Size of module. Only valid if <see cref="HasAddress"/> is true
		/// </summary>
		public abstract uint Size { get; }

		/// <summary>
		/// Image layout
		/// </summary>
		public abstract DbgImageLayout ImageLayout { get; }

		/// <summary>
		/// Name of module
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Filename if it exists on disk, else it could be any longer name
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// Real filename of module if it exists on disk. It could be different from <see cref="Filename"/>
		/// if it's an NGEN'd .NET module. This filename would then be the name of the *.ni.dll file.
		/// </summary>
		public abstract string RealFilename { get; }

		/// <summary>
		/// true if it's an in-memory module
		/// </summary>
		public abstract bool IsInMemory { get; }

		/// <summary>
		/// Load order of this module
		/// </summary>
		public abstract int Order { get; }

		/// <summary>
		/// Timestamp of module (eg. as found in the PE header)
		/// </summary>
		public abstract DateTime Timestamp { get; }
	}
}
