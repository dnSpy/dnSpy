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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// A local or parameter present in decompiled code
	/// </summary>
	public interface ISourceVariable {
		/// <summary>
		/// Gets the real local or parameter or null if it's a decompiler generated variable
		/// </summary>
		IVariable? Variable { get; }

		/// <summary>
		/// true if this is a local
		/// </summary>
		bool IsLocal { get; }

		/// <summary>
		/// true if this is a parameter
		/// </summary>
		bool IsParameter { get; }

		/// <summary>
		/// Gets the name of the variable the decompiler used. It could be different from the real name if the decompiler renamed it.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the type of the variable
		/// </summary>
		TypeSig Type { get; }

		/// <summary>
		/// Gets the hoisted field or null if it's not a hoisted local/parameter
		/// </summary>
		FieldDef? HoistedField { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		SourceVariableFlags Flags { get; }

		/// <summary>
		/// true if this is a decompiler generated variable
		/// </summary>
		bool IsDecompilerGenerated { get; }
	}

	/// <summary>
	/// <see cref="ISourceVariable"/> flags
	/// </summary>
	[Flags]
	public enum SourceVariableFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Decompiler generated variable
		/// </summary>
		DecompilerGenerated		= 0x00000001,

		/// <summary>
		/// Readonly reference, eg. a 'ref readonly' local
		/// </summary>
		ReadOnlyReference		= 0x00000002,
	}
}
