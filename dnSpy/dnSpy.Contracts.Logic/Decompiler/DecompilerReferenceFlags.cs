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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Flags used by <see cref="IDecompilerOutput.Write(string, object, DecompilerReferenceFlags, object)"/>
	/// </summary>
	[Flags]
	public enum DecompilerReferenceFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// It's a definition (method declaration, field declaration etc) if set, else it's a reference to the definition
		/// </summary>
		Definition					= 0x00000001,

		/// <summary>
		/// It's a local definition or reference, eg. a method parameter, method local, method label.
		/// </summary>
		Local						= 0x00000002,

		/// <summary>
		/// The code writes to the reference
		/// </summary>
		IsWrite						= 0x00000004,

		/// <summary>
		/// Reference shouldn't be highlighted
		/// </summary>
		Hidden						= 0x00000008,
	}
}
