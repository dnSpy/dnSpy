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

using System.Collections.Generic;
using System.IO;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A value in the debugged process. This value gets neutered (invalid) as soon as the debugged
	/// process continues.
	/// </summary>
	public interface IDebuggerValue {
		/// <summary>
		/// true if it supports reading and writing its value (<c>ICorDebugGenericValue</c>)
		/// </summary>
		bool CanReadWrite { get; }

		/// <summary>
		/// true if it's a reference (<c>ICorDebugReferenceValue</c>)
		/// </summary>
		bool IsReference { get; }

		/// <summary>
		/// true if it's a handle (<c>ICorDebugHandleValue</c>)
		/// </summary>
		bool IsHandle { get; }

		/// <summary>
		/// true if it's a heap (<c>ICorDebugHeapValue</c>, <c>ICorDebugArrayValue</c>,
		/// <c>ICorDebugBoxValue</c>, <c>ICorDebugStringValue</c>)
		/// </summary>
		bool IsHeap { get; }

		/// <summary>
		/// true if it's an array (<c>ICorDebugArrayValue</c>)
		/// </summary>
		bool IsArray { get; }

		/// <summary>
		/// true if it's a boxed value (<c>ICorDebugBoxValue</c>)
		/// </summary>
		bool IsBox { get; }

		/// <summary>
		/// true if it's a string (<c>ICorDebugStringValue</c>)
		/// </summary>
		bool IsString { get; }

		/// <summary>
		/// true if it's an object (<c>ICorDebugObjectValue</c>)
		/// </summary>
		bool IsObject { get; }

		/// <summary>
		/// true if it's a context (<c>ICorDebugContext</c>)
		/// </summary>
		bool IsContext { get; }

		/// <summary>
		/// true if it's an RCW (<c>ICorDebugComObjectValue</c>)
		/// </summary>
		bool IsComObject { get; }

		/// <summary>
		/// true if it's an exception object (<c>ICorDebugExceptionObjectValue</c>)
		/// </summary>
		bool IsExceptionObject { get; }

		/// <summary>
		/// Gets the element type
		/// </summary>
		CorElementType ElementType { get; }

		/// <summary>
		/// Returns the enum underlying type if it's an enum, else <see cref="ElementType"/> is returned
		/// </summary>
		CorElementType ElementTypeOrEnumUnderlyingType { get; }

		/// <summary>
		/// Gets the size of the value
		/// </summary>
		ulong Size { get; }

		/// <summary>
		/// Gets the address of the value or 0 if it's not available, eg. it could be in a register
		/// </summary>
		ulong Address { get; }

		/// <summary>
		/// Gets the class if <see cref="IsObject"/> or <see cref="IsContext"/>, else null
		/// </summary>
		IDebuggerClass Class { get; }

		/// <summary>
		/// Gets the type or null
		/// </summary>
		IDebuggerType Type { get; }

		/// <summary>
		/// true if it's a reference and it's null
		/// </summary>
		bool IsNull { get; }

		/// <summary>
		/// Gets/sets the address to which the reference points (see <see cref="IsReference"/>)
		/// </summary>
		ulong ReferenceAddress { get; set; }

		/// <summary>
		/// Gets the handle type if it's a handle value (<see cref="IsHandle"/>)
		/// </summary>
		DebugHandleType HandleType { get; }

		/// <summary>
		/// Gets the dereferenced value to which the reference (<see cref="IsReference"/>) points or null
		/// </summary>
		IDebuggerValue DereferencedValue { get; }

		/// <summary>
		/// Gets the type of the array's elements or <see cref="CorElementType.End"/> if it's not an array
		/// </summary>
		CorElementType ArrayElementType { get; }

		/// <summary>
		/// Gets the rank of the array or 0 if it's not an array
		/// </summary>
		uint Rank { get; }

		/// <summary>
		/// Gets the number of elements in the array or 0 if it's not an array
		/// </summary>
		uint ArrayCount { get; }

		/// <summary>
		/// Gets the dimensions or null if it's not an array
		/// </summary>
		uint[] Dimensions { get; }

		/// <summary>
		/// true if the array has base indices
		/// </summary>
		bool HasBaseIndicies { get; }

		/// <summary>
		/// Gets all base indicies or null if it's not an array
		/// </summary>
		uint[] BaseIndicies { get; }

		/// <summary>
		/// Gets the boxed object value or null if none. The return value has <see cref="IsObject"/> set to true
		/// </summary>
		IDebuggerValue BoxedValue { get; }

		/// <summary>
		/// Gets the length of the string in characters or 0 if it's not a string (<see cref="IsString"/>)
		/// </summary>
		uint StringLength { get; }

		/// <summary>
		/// Gets the string or null if it's not a string (<see cref="IsString"/>)
		/// </summary>
		string String { get; }

		/// <summary>
		/// true if this is an object value (<see cref="IsObject"/>) and it's a value type
		/// </summary>
		bool IsValueClass { get; }

		/// <summary>
		/// Gets all <see cref="ExceptionObjectStackFrame"/>s if <see cref="IsExceptionObject"/> is true
		/// </summary>
		IEnumerable<ExceptionObjectStackFrame> ExceptionObjectStackFrames { get; }

		/// <summary>
		/// Gets the value. Only values of simple types are currently returned: boolean, integers,
		/// floating points, decimal, string and null.
		/// </summary>
		ValueResult Value { get; }

		/// <summary>
		/// true if the value has been neutered, eg. because Continue() was called
		/// </summary>
		bool IsNeutered { get; }

		/// <summary>
		/// Disposes the handle if it's a handle (<see cref="IsHandle"/>)
		/// </summary>
		/// <returns></returns>
		bool DisposeHandle();

		/// <summary>
		/// Gets the value at a specified index in the array or null. The array is treated as a
		/// zero-based, single-dimensional array
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns></returns>
		IDebuggerValue GetElementAtPosition(uint index);

		/// <summary>
		/// Gets the value at a specified index in the array or null. The array is treated as a
		/// zero-based, single-dimensional array
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns></returns>
		IDebuggerValue GetElementAtPosition(int index);

		/// <summary>
		/// Gets the value at the specified indices or null
		/// </summary>
		/// <param name="indices">Indices into the array</param>
		/// <returns></returns>
		IDebuggerValue GetElement(uint[] indices);

		/// <summary>
		/// Gets the value at the specified indices or null
		/// </summary>
		/// <param name="indices">Indices into the array</param>
		/// <returns></returns>
		IDebuggerValue GetElement(int[] indices);

		/// <summary>
		/// Gets the value of a field or null if it's not an object (<see cref="IsObject"/>)
		/// </summary>
		/// <param name="cls">Class</param>
		/// <param name="token">Token of field in <paramref name="cls"/></param>
		/// <returns></returns>
		IDebuggerValue GetFieldValue(IDebuggerClass cls, uint token);

		/// <summary>
		/// Gets the value of a field. Returns null if field wasn't found or there was another error
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerValue GetFieldValue(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Creates a handle to this heap value (<see cref="IsHeap"/>). The returned value is a
		/// handle value (<see cref="IsHandle"/>).
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		IDebuggerValue CreateHandle(DebugHandleType type);

		/// <summary>
		/// Writes a new value. Can be called if <see cref="CanReadWrite"/> is true
		/// </summary>
		/// <param name="data">Data</param>
		/// <returns></returns>
		bool Write(byte[] data);

		/// <summary>
		/// Reads the data. Can be called if <see cref="CanReadWrite"/> is true. Returns null if there
		/// was an error.
		/// </summary>
		/// <returns></returns>
		byte[] Read();

		/// <summary>
		/// Reads a <see cref="bool"/>
		/// </summary>
		/// <returns></returns>
		bool ReadBoolean();

		/// <summary>
		/// Reads a <see cref="char"/>
		/// </summary>
		/// <returns></returns>
		char ReadChar();

		/// <summary>
		/// Reads a <see cref="sbyte"/>
		/// </summary>
		/// <returns></returns>
		sbyte ReadSByte();

		/// <summary>
		/// Reads a <see cref="short"/>
		/// </summary>
		/// <returns></returns>
		short ReadInt16();

		/// <summary>
		/// Reads a <see cref="int"/>
		/// </summary>
		/// <returns></returns>
		int ReadInt32();

		/// <summary>
		/// Reads a <see cref="long"/>
		/// </summary>
		/// <returns></returns>
		long ReadInt64();

		/// <summary>
		/// Reads a <see cref="byte"/>
		/// </summary>
		/// <returns></returns>
		byte ReadByte();

		/// <summary>
		/// Reads a <see cref="ushort"/>
		/// </summary>
		/// <returns></returns>
		ushort ReadUInt16();

		/// <summary>
		/// Reads a <see cref="uint"/>
		/// </summary>
		/// <returns></returns>
		uint ReadUInt32();

		/// <summary>
		/// Reads a <see cref="ulong"/>
		/// </summary>
		/// <returns></returns>
		ulong ReadUInt64();

		/// <summary>
		/// Reads a <see cref="float"/>
		/// </summary>
		/// <returns></returns>
		float ReadSingle();

		/// <summary>
		/// Reads a <see cref="double"/>
		/// </summary>
		/// <returns></returns>
		double ReadDouble();

		/// <summary>
		/// Reads a <see cref="decimal"/>
		/// </summary>
		/// <returns></returns>
		decimal ReadDecimal();

		/// <summary>
		/// Writes a <see cref="bool"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(bool value);

		/// <summary>
		/// Writes a <see cref="char"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(char value);

		/// <summary>
		/// Writes a <see cref="sbyte"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(sbyte value);

		/// <summary>
		/// Writes a <see cref="short"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(short value);

		/// <summary>
		/// Writes a <see cref="int"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(int value);

		/// <summary>
		/// Writes a <see cref="long"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(long value);

		/// <summary>
		/// Writes a <see cref="byte"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(byte value);

		/// <summary>
		/// Writes a <see cref="ushort"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(ushort value);

		/// <summary>
		/// Writes a <see cref="uint"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(uint value);

		/// <summary>
		/// Writes a <see cref="ulong"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(ulong value);

		/// <summary>
		/// Writes a <see cref="float"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(float value);

		/// <summary>
		/// Writes a <see cref="double"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(double value);

		/// <summary>
		/// Writes a <see cref="decimal"/>
		/// </summary>
		/// <param name="value">Value</param>
		void Write(decimal value);

		/// <summary>
		/// Gets the nullable value's value field. Returns true if it's a nullable type, false if
		/// it's not a nullable type.
		/// </summary>
		/// <param name="value">Updated with the value of the nullable field or null if the nullable
		/// is null or if it's not a nullable value</param>
		/// <returns></returns>
		bool GetNullableValue(out IDebuggerValue value);

		/// <summary>
		/// Reads an instance field. To read a static field, see eg. <see cref="IDebuggerType.ReadStaticField(IStackFrame, IDebuggerField)"/>
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerValue Read(IDebuggerField field);

		/// <summary>
		/// Reads an instance field. To read a static field, see eg. <see cref="IDebuggerType.ReadStaticField(IStackFrame, uint)"/>
		/// </summary>
		/// <param name="token">Field token</param>
		/// <returns></returns>
		IDebuggerValue Read(uint token);

		/// <summary>
		/// Reads an instance field. To read a static field, see eg. <see cref="IDebuggerType.ReadStaticField(IStackFrame, string, bool)"/>
		/// </summary>
		/// <param name="name">Field name</param>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		IDebuggerValue Read(string name, bool checkBaseClasses = true);

		/// <summary>
		/// Calls an instance method in the debugged process
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <param name="method">Instance method to call</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// Use <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerThread thread, IDebuggerMethod method, params object[] args);

		/// <summary>
		/// Calls an instance method in the debugged process
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments
		/// (<see cref="Type"/> or <see cref="IDebuggerType"/> instances)</param>
		/// <param name="method">Instance method to call</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// Use <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, IDebuggerMethod method, params object[] args);

		/// <summary>
		/// Calls an instance method in the debugged process
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// Use <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerThread thread, string modName, string className, string methodName, params object[] args);

		/// <summary>
		/// Calls an instance method in the debugged process
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Method token</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// Use <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerThread thread, string modName, uint token, params object[] args);

		/// <summary>
		/// Calls an instance method in the debugged process
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments
		/// (<see cref="Type"/> or <see cref="IDebuggerType"/> instances)</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// Use <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, string modName, string className, string methodName, params object[] args);

		/// <summary>
		/// Calls an instance method in the debugged process
		/// </summary>
		/// <param name="thread">Thread to use</param>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments
		/// (<see cref="Type"/> or <see cref="IDebuggerType"/> instances)</param>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Method token</param>
		/// <param name="args">Arguments, either simple types (ints, doubles, strings, null),
		/// arrays (<c>int[], string[])</c>, <see cref="IDebuggerValue"/>, <see cref="Type"/>,
		/// <see cref="IDebuggerType"/> instances.
		/// Use <see cref="Box"/> to box values.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, string modName, uint token, params object[] args);

		/// <summary>
		/// Reads the data but doesn't return internal values such as array length if it's an array.
		/// </summary>
		/// <returns></returns>
		byte[] SaveData();

		/// <summary>
		/// Reads the data but doesn't return internal values such as array length if it's an array.
		/// </summary>
		/// <param name="stream">Destination stream</param>
		void SaveData(Stream stream);

		/// <summary>
		/// Reads the data but doesn't return internal values such as array length if it's an array.
		/// </summary>
		/// <param name="filename">Filename</param>
		void SaveData(string filename);

		/// <summary>
		/// Returns the address of the first element in the array
		/// </summary>
		/// <returns></returns>
		ulong GetArrayDataAddress();

		/// <summary>
		/// Returns the address of the first element in the array
		/// </summary>
		/// <param name="elemSize">Element size</param>
		/// <returns></returns>
		ulong GetArrayDataAddress(out ulong elemSize);

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="valueResult">Value result</param>
		/// <param name="flags">Flags</param>
		void Write(ISyntaxHighlightOutput output, ValueResult valueResult, TypeFormatFlags flags = TypeFormatFlags.Default);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="valueResult">Value result</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		string ToString(ValueResult valueResult, TypeFormatFlags flags = TypeFormatFlags.Default);

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="flags">Flags</param>
		void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags = TypeFormatFlags.Default);

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="type">Type</param>
		/// <param name="flags">Flags</param>
		void WriteType(ISyntaxHighlightOutput output, IDebuggerType type, TypeFormatFlags flags = TypeFormatFlags.Default);

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="cls">Class</param>
		/// <param name="flags">Flags</param>
		void WriteType(ISyntaxHighlightOutput output, IDebuggerClass cls, TypeFormatFlags flags = TypeFormatFlags.Default);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		string ToString(TypeFormatFlags flags);
	}
}
