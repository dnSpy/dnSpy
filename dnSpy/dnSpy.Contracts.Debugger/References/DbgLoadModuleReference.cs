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
using dnSpy.Contracts.Documents;

namespace dnSpy.Contracts.Debugger.References {
	/// <summary>
	/// If passed to <see cref="ReferenceNavigatorService.GoTo(object, object[])"/>, the module gets
	/// loaded and selected in the treeview
	/// </summary>
	public sealed class DbgLoadModuleReference {
		/// <summary>
		/// Gets the module
		/// </summary>
		public DbgModule Module { get; }

		/// <summary>
		/// true if the module should be read from memory and not from a file on disk
		/// </summary>
		public bool UseMemory { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="useMemory">true if the module should be read from memory and not from a file on disk</param>
		public DbgLoadModuleReference(DbgModule module, bool useMemory) {
			Module = module ?? throw new ArgumentNullException(nameof(module));
			UseMemory = useMemory;
		}
	}
}
