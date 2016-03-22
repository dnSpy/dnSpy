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
	/// An instantiated type (<c>ICorDebugType</c>)
	/// </summary>
	public interface IDebuggerType {
		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the namespace
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
		/// Gets the <c>TypeDef</c> token or a nil token if <see cref="Class"/> is null
		/// </summary>
		uint Token { get; }

		/// <summary>
		/// Gets the element type, see also <see cref="TryGetPrimitiveType"/>
		/// </summary>
		CorElementType ElementType { get; }

		/// <summary>
		/// Returns the enum underlying type if it's an enum, else <see cref="ElementType"/> is returned
		/// </summary>
		CorElementType ElementTypeOrEnumUnderlyingType { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.SZArray"/> (but not a <see cref="CorElementType.Array"/>)
		/// </summary>
		bool IsSZArray { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.Array"/> (but not a <see cref="CorElementType.SZArray"/>)
		/// </summary>
		bool IsArray { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.SZArray"/> or a <see cref="CorElementType.Array"/>
		/// </summary>
		bool IsAnyArray { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.Ptr"/>
		/// </summary>
		bool IsPtr { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.ByRef"/>
		/// </summary>
		bool IsByRef { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.GenericInst"/>
		/// </summary>
		bool IsGenericInst { get; }

		/// <summary>
		/// true if it's a <see cref="CorElementType.FnPtr"/>
		/// </summary>
		bool IsFnPtr { get; }

		/// <summary>
		/// Gets the rank of the array
		/// </summary>
		uint Rank { get; }

		/// <summary>
		/// Gets the first type parameter
		/// </summary>
		IDebuggerType FirstTypeParameter { get; }

		/// <summary>
		/// Gets all type parameters. If it's a <see cref="CorElementType.Class"/> or a <see cref="CorElementType.ValueType"/>,
		/// then the returned parameters are the type parameters in the correct order. If it's
		/// a <see cref="CorElementType.FnPtr"/>, the first type is the return type, followed by
		/// all the method argument types in correct order. If it's a <see cref="CorElementType.Array"/>,
		/// <see cref="CorElementType.SZArray"/>, <see cref="CorElementType.ByRef"/> or a
		/// <see cref="CorElementType.Ptr"/>, the returned type is the inner type, eg. <c>int</c> if the
		/// type is <c>int[]</c>. In this case, <see cref="FirstTypeParameter"/> can be called instead.
		/// </summary>
		IDebuggerType[] TypeParameters { get; }

		/// <summary>
		/// Gets the non-instantiated type, only valid if <see cref="ElementType"/> is a
		/// <see cref="CorElementType.Class"/> or <see cref="CorElementType.ValueType"/>
		/// </summary>
		IDebuggerClass Class { get; }

		/// <summary>
		/// Gets the base type or null
		/// </summary>
		IDebuggerType BaseType { get; }

		/// <summary>
		/// true if it derives from <see cref="System.Enum"/>
		/// </summary>
		bool IsEnum { get; }

		/// <summary>
		/// true if it derives from <see cref="System.Enum"/> or <see cref="System.ValueType"/>
		/// </summary>
		bool IsValueType { get; }

		/// <summary>
		/// true if this class directly derives from <see cref="System.ValueType"/>
		/// </summary>
		bool DerivesFromSystemValueType { get; }

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
		/// true if this is <see cref="System.Nullable{T}"/>
		/// </summary>
		bool IsSystemNullable { get; }

		/// <summary>
		/// Returns the enum underlying type and shouldn't be called unless <see cref="IsEnum"/>
		/// is true. <see cref="CorElementType.End"/> is returned if the underlying type wasn't found.
		/// </summary>
		CorElementType EnumUnderlyingType { get; }

		/// <summary>
		/// Same as <see cref="ElementType"/> except that it tries to return a primitive element
		/// type (eg. <see cref="CorElementType.U4"/>) if it's a primitive type.
		/// </summary>
		CorElementType TryGetPrimitiveType();

		/// <summary>
		/// Returns true if it's a System.XXX type in the corlib (eg. mscorlib)
		/// </summary>
		/// <param name="name">Name (not including namespace)</param>
		/// <returns></returns>
		bool IsSystem(string name);

		/// <summary>
		/// Returns true if an attribute is present
		/// </summary>
		/// <param name="attributeName">Full name of attribute type</param>
		/// <returns></returns>
		bool HasAttribute(string attributeName);

		/// <summary>
		/// Creates a pointer type
		/// </summary>
		/// <returns></returns>
		IDebuggerType ToPtr();

		/// <summary>
		/// Creates a pointer type
		/// </summary>
		/// <returns></returns>
		IDebuggerType ToPointer();

		/// <summary>
		/// Creates a by-ref type
		/// </summary>
		/// <returns></returns>
		IDebuggerType ToByReference();

		/// <summary>
		/// Creates a by-ref type
		/// </summary>
		/// <returns></returns>
		IDebuggerType ToByRef();

		/// <summary>
		/// Creates a single-dimension zero-lower bound array type
		/// </summary>
		/// <returns></returns>
		IDebuggerType ToSZArray();

		/// <summary>
		/// Creates a multi-dimensional array type. If <paramref name="rank"/> is <c>1</c>, you most
		/// likely want to call <see cref="ToSZArray"/> instead of this method.
		/// </summary>
		/// <param name="rank">Number of dimensions</param>
		/// <returns></returns>
		IDebuggerType ToArray(int rank);

		/// <summary>
		/// Gets all methods declared in this type
		/// </summary>
		IDebuggerMethod[] Methods { get; }

		/// <summary>
		/// Gets all fields declared in this type
		/// </summary>
		IDebuggerField[] Fields { get; }

		/// <summary>
		/// Gets all properties declared in this type
		/// </summary>
		IDebuggerProperty[] Properties { get; }

		/// <summary>
		/// Gets all events declared in this type
		/// </summary>
		IDebuggerEvent[] Events { get; }

		/// <summary>
		/// Returns all methods
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerMethod[] GetMethods(bool checkBaseClasses = true);

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
		/// Finds methods
		/// </summary>
		/// <param name="name">Method name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerMethod[] GetMethods(string name, bool checkBaseClasses = true);

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
