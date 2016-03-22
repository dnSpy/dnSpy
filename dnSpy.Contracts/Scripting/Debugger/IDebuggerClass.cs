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
using SR = System.Reflection;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A non-instantiated type (see <c>ICorDebugClass</c>)
	/// </summary>
	public interface IDebuggerClass {
		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the namespace or null if none
		/// </summary>
		string Namespace { get; }

		/// <summary>
		/// Gets the full name, which is the same as calling <see cref="object.ToString"/>
		/// </summary>
		string FullName { get; }

		/// <summary>
		/// Gets the type attributes
		/// </summary>
		TypeAttributes Attributes { get; }

		/// <summary>
		/// Gets the visibility
		/// </summary>
		TypeAttributes Visibility { get; }

		/// <summary>
		/// true if not public
		/// </summary>
		bool IsNotPublic { get; }

		/// <summary>
		/// true if public
		/// </summary>
		bool IsPublic { get; }

		/// <summary>
		/// true if nested public
		/// </summary>
		bool IsNestedPublic { get; }

		/// <summary>
		/// true if nested private
		/// </summary>
		bool IsNestedPrivate { get; }

		/// <summary>
		/// true if nested family
		/// </summary>
		bool IsNestedFamily { get; }

		/// <summary>
		/// true if nested assembly
		/// </summary>
		bool IsNestedAssembly { get; }

		/// <summary>
		/// true if nested family and assembly
		/// </summary>
		bool IsNestedFamilyAndAssembly { get; }

		/// <summary>
		/// true if nested family or assembly
		/// </summary>
		bool IsNestedFamilyOrAssembly { get; }

		/// <summary>
		/// Gets the layout
		/// </summary>
		TypeAttributes Layout { get; }

		/// <summary>
		/// true if auto layout
		/// </summary>
		bool IsAutoLayout { get; }

		/// <summary>
		/// true if sequential layout
		/// </summary>
		bool IsSequentialLayout { get; }

		/// <summary>
		/// true if explicit layout
		/// </summary>
		bool IsExplicitLayout { get; }

		/// <summary>
		/// true if it's an interface
		/// </summary>
		bool IsInterface { get; }

		/// <summary>
		/// true if it's a class or struct
		/// </summary>
		bool IsClass { get; }

		/// <summary>
		/// true if it's abstract
		/// </summary>
		bool IsAbstract { get; }

		/// <summary>
		/// true if it's sealed
		/// </summary>
		bool IsSealed { get; }

		/// <summary>
		/// true if's special name
		/// </summary>
		bool IsSpecialName { get; }

		/// <summary>
		/// true if it's an import
		/// </summary>
		bool IsImport { get; }

		/// <summary>
		/// true if it's serializable
		/// </summary>
		bool IsSerializable { get; }

		/// <summary>
		/// true if it's windows runtime
		/// </summary>
		bool IsWindowsRuntime { get; }

		/// <summary>
		/// Gets the string format
		/// </summary>
		TypeAttributes StringFormat { get; }

		/// <summary>
		/// true if it's an ANSI string class
		/// </summary>
		bool IsAnsiClass { get; }

		/// <summary>
		/// true if it's a unicode string class
		/// </summary>
		bool IsUnicodeClass { get; }

		/// <summary>
		/// true if it's an auto string class
		/// </summary>
		bool IsAutoClass { get; }

		/// <summary>
		/// true if it's a custom string format class
		/// </summary>
		bool IsCustomFormatClass { get; }

		/// <summary>
		/// true if it's before field init
		/// </summary>
		bool IsBeforeFieldInit { get; }

		/// <summary>
		/// true if it's a forwarder
		/// </summary>
		bool IsForwarder { get; }

		/// <summary>
		/// true if it's a runtime special name
		/// </summary>
		bool IsRuntimeSpecialName { get; }

		/// <summary>
		/// true if it has a security descriptor
		/// </summary>
		bool HasSecurity { get; }

		/// <summary>
		/// Gets the token
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets the module
		/// </summary>
		IDebuggerModule Module { get; }

		/// <summary>
		/// Gets the base type or null
		/// </summary>
		IDebuggerType BaseType { get; }

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
		/// Gets all methods declared in this class
		/// </summary>
		IDebuggerMethod[] Methods { get; }

		/// <summary>
		/// Gets all fields declared in this class
		/// </summary>
		IDebuggerField[] Fields { get; }

		/// <summary>
		/// Gets all properties declared in this class
		/// </summary>
		IDebuggerProperty[] Properties { get; }

		/// <summary>
		/// Gets all events declared in this class
		/// </summary>
		IDebuggerEvent[] Events { get; }

		/// <summary>
		/// Returns all methods
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerMethod[] GetMethods(bool checkBaseClasses = true);

		/// <summary>
		/// Finds methods
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerMethod[] GetMethods(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Finds a method. If only one method is found, it's returned, else the method that takes
		/// no arguments is returned, or null if it doesn't exist.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Finds a method with a certain signature. Base classes are also searched.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="argTypes">Argument types. This can be <see cref="System.Type"/>s or strings
		/// with the full name of the argument types</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string name, params object[] argTypes);

		/// <summary>
		/// Finds a method with a certain signature.
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <param name="argTypes">Argument types. This can be <see cref="System.Type"/>s or strings
		/// with the full name of the argument types</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string name, bool checkBaseClasses, params object[] argTypes);

		/// <summary>
		/// Returns all constructors
		/// </summary>
		/// <returns></returns>
		IDebuggerMethod[] GetConstructors();

		/// <summary>
		/// Returns the default constructor or null if not found
		/// </summary>
		/// <returns></returns>
		IDebuggerMethod GetConstructor();

		/// <summary>
		/// Returns a constructor
		/// </summary>
		/// <param name="argTypes">Argument types. This can be <see cref="System.Type"/>s or strings
		/// with the full name of the argument types</param>
		/// <returns></returns>
		IDebuggerMethod GetConstructor(params object[] argTypes);

		/// <summary>
		/// Returns all fields
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerField[] GetFields(bool checkBaseClasses = true);

		/// <summary>
		/// Finds fields
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerField[] GetFields(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerField GetField(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Returns all properties
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerProperty[] GetProperties(bool checkBaseClasses = true);

		/// <summary>
		/// Finds properties
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerProperty[] GetProperties(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Returns all events
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerEvent[] GetEvents(bool checkBaseClasses = true);

		/// <summary>
		/// Finds events
		/// </summary>
		/// <param name="name">Event name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerEvent[] GetEvents(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Finds a event
		/// </summary>
		/// <param name="name">Event name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerField GetField(SR.FieldInfo field);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(SR.MethodBase method);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="prop">Property</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(SR.PropertyInfo prop);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="evt">Event</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(SR.EventInfo evt);

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

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="token">Token of field</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IStackFrame frame, uint token);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IStackFrame frame, IDebuggerField field);

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="name">Field name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerValue ReadStaticField(IStackFrame frame, string name, bool checkBaseClasses = true);
	}
}
