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
using dnlib.DotNet;

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Method local info
	/// </summary>
	public readonly struct DbgLocal {
		/// <summary>
		/// Gets the local index or &lt; 0 if it's not in the metadata
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// Gets the name of the local
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the hoisted field or null if it's not a hoisted local
		/// </summary>
		public FieldDef? HoistedField { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgLocalFlags Flags { get; }

		/// <summary>
		/// true if this is a decompiler generated local
		/// </summary>
		public bool IsDecompilerGenerated => (Flags & DbgLocalFlags.DecompilerGenerated) != 0;

		/// <summary>
		/// true if this is a debugger hidden local
		/// </summary>
		public bool IsDebuggerHidden => (Flags & DbgLocalFlags.DebuggerHidden) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Index of local or &lt; 0 if it's not in the metadata</param>
		/// <param name="name">Name of the local</param>
		/// <param name="hoistedField">Hoisted field or null if it's not a hoisted local/parameter</param>
		/// <param name="flags">Local flags</param>
		public DbgLocal(int index, string name, FieldDef? hoistedField, DbgLocalFlags flags) {
			Index = index;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			HoistedField = hoistedField;
			Flags = flags;
		}
	}

	/// <summary>
	/// Locals flags
	/// </summary>
	[Flags]
	public enum DbgLocalFlags : uint {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Decompiler generated local
		/// </summary>
		DecompilerGenerated		= 0x00000001,

		/// <summary>
		/// Debugger hidden local
		/// </summary>
		DebuggerHidden			= 0x00000002,
	}
}
