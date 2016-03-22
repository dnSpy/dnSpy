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

using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A field
	/// </summary>
	public interface IDebuggerField {
		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the attributes
		/// </summary>
		FieldAttributes Attributes { get; }

		/// <summary>
		/// Gets the access
		/// </summary>
		FieldAttributes Access { get; }

		/// <summary>
		/// true if compiler controlled / private scope
		/// </summary>
		bool IsCompilerControlled { get; }

		/// <summary>
		/// true if compiler controlled / private scope
		/// </summary>
		bool IsPrivateScope { get; }

		/// <summary>
		/// true if private
		/// </summary>
		bool IsPrivate { get; }

		/// <summary>
		/// true if family and assembly
		/// </summary>
		bool IsFamilyAndAssembly { get; }

		/// <summary>
		/// true if assembly
		/// </summary>
		bool IsAssembly { get; }

		/// <summary>
		/// treu if family
		/// </summary>
		bool IsFamily { get; }

		/// <summary>
		/// true if family or assembly
		/// </summary>
		bool IsFamilyOrAssembly { get; }

		/// <summary>
		/// true if public
		/// </summary>
		bool IsPublic { get; }

		/// <summary>
		/// true if static
		/// </summary>
		bool IsStatic { get; }

		/// <summary>
		/// true if init only
		/// </summary>
		bool IsInitOnly { get; }

		/// <summary>
		/// true if literal
		/// </summary>
		bool IsLiteral { get; }

		/// <summary>
		/// true if not serialized
		/// </summary>
		bool IsNotSerialized { get; }

		/// <summary>
		/// true if special name
		/// </summary>
		bool IsSpecialName { get; }

		/// <summary>
		/// true if P/Invoke implementation
		/// </summary>
		bool IsPinvokeImpl { get; }

		/// <summary>
		/// true if runtime special name
		/// </summary>
		bool IsRuntimeSpecialName { get; }

		/// <summary>
		/// true if it has a marshal attribute
		/// </summary>
		bool HasFieldMarshal { get; }

		/// <summary>
		/// true if it has a default value
		/// </summary>
		bool HasDefault { get; }

		/// <summary>
		/// true if it has an RVA
		/// </summary>
		bool HasFieldRVA { get; }

		/// <summary>
		/// Gets the method signature. It's currently using custom <see cref="TypeDef"/>,
		/// <see cref="TypeRef"/> and <see cref="TypeSpec"/> instances that don't reveal all
		/// information available in the metadata.
		/// </summary>
		FieldSig FieldSig { get; }

		/// <summary>
		/// Owner module
		/// </summary>
		IDebuggerModule Module { get; }

		/// <summary>
		/// Owner class
		/// </summary>
		IDebuggerClass Class { get; }

		/// <summary>
		/// Token of method
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets the constant
		/// </summary>
		object Constant { get; }

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="type">Declaring type. Can be null if it's not a generic type</param>
		/// <returns></returns>
		IDebuggerValue ReadStatic(IStackFrame frame, IDebuggerType type = null);

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="flags">Flags</param>
		void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		string ToString(TypeFormatFlags flags);
	}
}
