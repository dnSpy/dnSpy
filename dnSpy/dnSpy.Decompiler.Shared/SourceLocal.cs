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
using dnlib.DotNet.Emit;

namespace dnSpy.Decompiler.Shared {
	/// <summary>
	/// A local used in the decompiled code
	/// </summary>
	public struct SourceLocal {
		/// <summary>
		/// The local
		/// </summary>
		public Local Local { get; }

		/// <summary>
		/// Gets the name of the local that's used in the decompiled code
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local">Local</param>
		/// <param name="name">Name used by the decompiler</param>
		public SourceLocal(Local local, string name) {
			if (local == null)
				throw new ArgumentNullException(nameof(local));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			Local = local;
			Name = name;
		}
	}
}
