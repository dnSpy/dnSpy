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
	/// A property
	/// </summary>
	public interface IDebuggerProperty {
		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the attributes
		/// </summary>
		PropertyAttributes Attributes { get; }

		/// <summary>
		/// true if special name
		/// </summary>
		bool IsSpecialName { get; }

		/// <summary>
		/// true if it has a special name
		/// </summary>
		bool IsRuntimeSpecialName { get; }

		/// <summary>
		/// true if it has a default value
		/// </summary>
		bool HasDefault { get; }

		/// <summary>
		/// Gets the property signature. It's currently using custom <see cref="TypeDef"/>,
		/// <see cref="TypeRef"/> and <see cref="TypeSpec"/> instances that don't reveal all
		/// information available in the metadata.
		/// </summary>
		PropertySig PropertySig { get; }

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
		/// Gets the getter method or null
		/// </summary>
		IDebuggerMethod Getter { get; }

		/// <summary>
		/// Gets the setter method or null
		/// </summary>
		IDebuggerMethod Setter { get; }

		/// <summary>
		/// Gets the other methods
		/// </summary>
		IDebuggerMethod[] OtherMethods { get; }

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
