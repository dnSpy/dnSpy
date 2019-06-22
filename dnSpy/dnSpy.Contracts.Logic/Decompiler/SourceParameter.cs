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
	/// A parameter present in decompiled code
	/// </summary>
	public sealed class SourceParameter : ISourceVariable {
		/// <summary>
		/// The parameter
		/// </summary>
		public Parameter Parameter { get; }

		IVariable? ISourceVariable.Variable => Parameter;
		bool ISourceVariable.IsLocal => false;
		bool ISourceVariable.IsParameter => true;
		bool ISourceVariable.IsDecompilerGenerated => (Flags & SourceVariableFlags.DecompilerGenerated) != 0;

		/// <summary>
		/// Gets the name of the parameter the decompiler used. It could be different from the real name if the decompiler renamed it.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the parameter
		/// </summary>
		public TypeSig Type { get; }

		/// <summary>
		/// Gets the hoisted field or null if it's not a hoisted parameter
		/// </summary>
		public FieldDef? HoistedField { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public SourceVariableFlags Flags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parameter">Parameter</param>
		/// <param name="name">Name used by the decompiler</param>
		/// <param name="type">Type of local</param>
		/// <param name="flags">Flags</param>
		public SourceParameter(Parameter parameter, string name, TypeSig type, SourceVariableFlags flags) {
			Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parameter">Parameter</param>
		/// <param name="name">Name used by the decompiler</param>
		/// <param name="hoistedField">Hoisted field</param>
		/// <param name="flags">Flags</param>
		public SourceParameter(Parameter parameter, string name, FieldDef hoistedField, SourceVariableFlags flags) {
			Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			HoistedField = hoistedField ?? throw new ArgumentNullException(nameof(hoistedField));
			Type = hoistedField.FieldType;
			Flags = flags;
		}
	}
}
