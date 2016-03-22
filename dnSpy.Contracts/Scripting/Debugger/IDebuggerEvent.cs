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
	/// An event
	/// </summary>
	public interface IDebuggerEvent {
		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the attributes
		/// </summary>
		EventAttributes Attributes { get; }

		/// <summary>
		/// true if it's special name
		/// </summary>
		bool IsSpecialName { get; }

		/// <summary>
		/// true if it's runtime special name
		/// </summary>
		bool IsRuntimeSpecialName { get; }

		/// <summary>
		/// Gets the event type
		/// </summary>
		IDebuggerType EventType { get; }

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
		/// Gets the adder method or null
		/// </summary>
		IDebuggerMethod Adder { get; }

		/// <summary>
		/// Gets the invoker method or null
		/// </summary>
		IDebuggerMethod Invoker { get; }

		/// <summary>
		/// Gets the remover method or null
		/// </summary>
		IDebuggerMethod Remover { get; }

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
