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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// A local present in decompiled code
	/// </summary>
	public sealed class SourceLocal : ISourceVariable {
		/// <summary>
		/// The local or null
		/// </summary>
		public Local Local { get; }

		IVariable ISourceVariable.Variable => Local;
		bool ISourceVariable.IsLocal => true;
		bool ISourceVariable.IsParameter => false;

		/// <summary>
		/// Gets the name of the local the decompiler used. It could be different from the real name if the decompiler renamed it.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the local
		/// </summary>
		public TypeSig Type { get; }

		/// <summary>
		/// Gets the hoisted field or null if it's not a hoisted local/parameter
		/// </summary>
		public FieldDef HoistedField { get; }

		/// <summary>
		/// true if this is a decompiler generated local
		/// </summary>
		public bool IsDecompilerGenerated => Local == null && HoistedField == null;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local">Local or null</param>
		/// <param name="name">Name used by the decompiler</param>
		/// <param name="type">Type of local</param>
		public SourceLocal(Local local, string name, TypeSig type) {
			Local = local;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="local">Local or null</param>
		/// <param name="name">Name used by the decompiler</param>
		/// <param name="hoistedField">Hoisted field</param>
		public SourceLocal(Local local, string name, FieldDef hoistedField) {
			Local = local;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			HoistedField = hoistedField ?? throw new ArgumentNullException(nameof(hoistedField));
			Type = hoistedField.FieldType;
		}
	}
}
