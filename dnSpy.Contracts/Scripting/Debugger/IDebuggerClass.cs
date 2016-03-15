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

using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A non-instantiated type (see <c>ICorDebugClass</c>)
	/// </summary>
	public interface IDebuggerClass {
		/// <summary>
		/// Gets the token
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets the module
		/// </summary>
		IDebuggerModule Module { get; }

		/// <summary>
		/// true if this is <see cref="System.Enum"/>
		/// </summary>
		bool IsSystemEnum { get; }

		/// <summary>
		/// true if this is <see cref="System.ValueType"/>
		/// </summary>
		bool IsSystemValueType { get; }

		/// <summary>
		/// true if this is <see cref="System.Object"/>
		/// </summary>
		bool IsSystemObject { get; }

		/// <summary>
		/// Returns true if it's a System.XXX type in the corlib (eg. mscorlib)
		/// </summary>
		/// <param name="name">Name (not including namespace)</param>
		/// <returns></returns>
		bool IsSystem(string name);

		/// <summary>
		/// Mark all methods in the type as user code
		/// </summary>
		/// <param name="jmc">true to set user code</param>
		/// <returns></returns>
		bool SetJustMyCode(bool jmc);

		/// <summary>
		/// Returns true if an attribute is present
		/// </summary>
		/// <param name="attributeName">Full name of attribute type</param>
		/// <returns></returns>
		bool HasAttribute(string attributeName);

		/// <summary>
		/// Creates a <see cref="IDebuggerType"/>
		/// </summary>
		/// <param name="isValueType">true if it's value type, false if it's a reference type</param>
		/// <param name="typeArgs">Generic type arguments or null</param>
		/// <returns></returns>
		IDebuggerType ToType(bool isValueType, IDebuggerType[] typeArgs = null);

		/// <summary>
		/// Creates a <see cref="IDebuggerType"/> (reference type)
		/// </summary>
		/// <param name="typeArgs">Generic type arguments or null</param>
		/// <returns></returns>
		IDebuggerType ToRefType(IDebuggerType[] typeArgs = null);

		/// <summary>
		/// Creates a <see cref="IDebuggerType"/> (value type)
		/// </summary>
		/// <param name="typeArgs">Generic type arguments or null</param>
		/// <returns></returns>
		IDebuggerType ToValueType(IDebuggerType[] typeArgs = null);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="token">Token of field</param>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		IDebuggerValue GetStaticFieldValue(uint token, IStackFrame frame);

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <returns></returns>
		IDebuggerFunction FindMethod(string name);

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
