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
		/// <param name="typeArgs">Generic type arguments or null</param>
		/// <returns></returns>
		IDebuggerType ToType(IDebuggerType[] typeArgs = null);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="token">Token of field</param>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		IDebuggerValue GetStaticFieldValue(uint token, IStackFrame frame);

		/// <summary>
		/// Returns all methods
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerFunction[] GetMethods(bool checkBaseClasses = true);

		/// <summary>
		/// Finds a method. If only one method is found, it's returned, else the method that takes
		/// no arguments is returned, or null if it doesn't exist.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerFunction FindMethod(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Finds a method with a certain signature. Base classes are also searched.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="argTypes">Argument types. This can be <see cref="System.Type"/>s or strings
		/// with the full name of the argument types</param>
		/// <returns></returns>
		IDebuggerFunction FindMethod(string name, params object[] argTypes);

		/// <summary>
		/// Finds a method with a certain signature.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <param name="argTypes">Argument types. This can be <see cref="System.Type"/>s or strings
		/// with the full name of the argument types</param>
		/// <returns></returns>
		IDebuggerFunction FindMethod(string name, bool checkBaseClasses, params object[] argTypes);

		/// <summary>
		/// Finds methods
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerFunction[] FindMethods(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Returns all constructors
		/// </summary>
		/// <returns></returns>
		IDebuggerFunction[] FindConstructors();

		/// <summary>
		/// Returns the default constructor or null if not found
		/// </summary>
		/// <returns></returns>
		IDebuggerFunction FindConstructor();

		/// <summary>
		/// Returns a constructor
		/// </summary>
		/// <param name="argTypes">Argument types. This can be <see cref="System.Type"/>s or strings
		/// with the full name of the argument types</param>
		/// <returns></returns>
		IDebuggerFunction FindConstructor(params object[] argTypes);

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
