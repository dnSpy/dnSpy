/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

namespace dnSpy.Contracts.Disassembly.Viewer {
	/// <summary>
	/// A reference in the disassembled code
	/// </summary>
	public readonly struct DisassemblyReferenceInfo {
		/// <summary>
		/// Gets the reference or null
		/// </summary>
		public object Reference { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DisassemblyReferenceFlags Flags { get; }

		/// <summary>
		/// true if it's a local definition or reference, eg. a label
		/// </summary>
		public bool IsLocal => (Flags & DisassemblyReferenceFlags.Local) != 0;

		/// <summary>
		/// true if it's a definition
		/// </summary>
		public bool IsDefinition => (Flags & DisassemblyReferenceFlags.Definition) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference or null</param>
		/// <param name="flags">Flags</param>
		public DisassemblyReferenceInfo(object reference, DisassemblyReferenceFlags flags) {
			Reference = reference;
			Flags = flags;
		}
	}

	/// <summary>
	/// <see cref="DisassemblyReferenceInfo"/> flags
	/// </summary>
	public enum DisassemblyReferenceFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// It's a definition if set, else it's a reference to the definition
		/// </summary>
		Definition					= 0x00000001,

		/// <summary>
		/// It's a local definition or reference, eg. a label
		/// </summary>
		Local						= 0x00000002,
	}
}
