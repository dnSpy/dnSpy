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

using System;
using System.IO;
using System.Reflection;
using dnlib.DotNet;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A module in the debugged process
	/// </summary>
	public interface IDebuggerModule {
		/// <summary>
		/// Gets the module name instance
		/// </summary>
		ModuleName ModuleName { get; }

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		int UniqueId { get; }

		/// <summary>
		/// Gets the owner AppDomain
		/// </summary>
		IAppDomain AppDomain { get; }

		/// <summary>
		/// Gets the assembly
		/// </summary>
		IDebuggerAssembly Assembly { get; }

		/// <summary>
		/// true if this is the manifest module
		/// </summary>
		bool IsManifestModule { get; }

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the name from the MD, which is the same as <see cref="ModuleDef.Name"/>
		/// </summary>
		string DnlibName { get; }

		/// <summary>
		/// Gets the name of the module. If it's an in-memory module, the hash code is included to
		/// make it uniquer since <see cref="Name"/> could have any value.
		/// </summary>
		string UniquerName { get; }

		/// <summary>
		/// Gets the base address of the module or 0
		/// </summary>
		ulong Address { get; }

		/// <summary>
		/// Gets the size of the module or 0
		/// </summary>
		uint Size { get; }

		/// <summary>
		/// true if it's a dynamic module that can add/remove types
		/// </summary>
		bool IsDynamic { get; }

		/// <summary>
		/// true if this is an in-memory module
		/// </summary>
		bool IsInMemory { get; }

		/// <summary>
		/// true if the module has been unloaded
		/// </summary>
		bool HasUnloaded { get; }

		/// <summary>
		/// Resolves an assembly reference. If the assembly hasn't been loaded, or if
		/// <paramref name="asmRefToken"/> is invalid, null is returned.
		/// </summary>
		/// <param name="asmRefToken">Valid assembly reference token in this module</param>
		/// <returns></returns>
		IDebuggerAssembly ResolveAssembly(uint asmRefToken);

		/// <summary>
		/// Gets a method in this module
		/// </summary>
		/// <param name="token"><c>Method</c> token</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(uint token);

		/// <summary>
		/// Gets a field in this module
		/// </summary>
		/// <param name="token"><c>Field</c> token</param>
		/// <returns></returns>
		IDebuggerField GetField(uint token);

		/// <summary>
		/// Gets a property in this module
		/// </summary>
		/// <param name="token"><c>Property</c> token</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(uint token);

		/// <summary>
		/// Gets an event in this module
		/// </summary>
		/// <param name="token"><c>Event</c> token</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(uint token);

		/// <summary>
		/// Gets a type in this module
		/// </summary>
		/// <param name="token"><c>TypeDef</c> token</param>
		/// <returns></returns>
		IDebuggerClass GetClass(uint token);

		/// <summary>
		/// Gets a type in this module
		/// </summary>
		/// <param name="token"><c>TypeDef</c> token</param>
		/// <returns></returns>
		IDebuggerType GetType(uint token);

		/// <summary>
		/// Gets the value of a global field
		/// </summary>
		/// <param name="fdToken">Token of a global field</param>
		/// <returns></returns>
		IDebuggerValue GetGlobalVariableValue(uint fdToken);

		/// <summary>
		/// Set just my code flag
		/// </summary>
		/// <param name="isJustMyCode">true if it's user code</param>
		void SetJMCStatus(bool isJustMyCode);

		/// <summary>
		/// true if the memory layout is identical to file layout
		/// </summary>
		bool IsFileLayout { get; }

		/// <summary>
		/// true if the OS loader has mapped the executable in memory
		/// </summary>
		bool IsMemoryLayout { get; }

		/// <summary>
		/// Can be called to initialize cached PE data in case that data is destroyed by the program
		/// at runtime.
		/// </summary>
		void InitializePE();

		/// <summary>
		/// Converts <paramref name="rva"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		ulong RVAToAddress(uint rva);

		/// <summary>
		/// Converts <paramref name="offset"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		ulong OffsetToAddress(uint offset);

		/// <summary>
		/// Converts <paramref name="address"/> to an RVA
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		uint AddressToRVA(ulong address);

		/// <summary>
		/// Converts <paramref name="address"/> to a file offset
		/// </summary>
		/// <param name="address">Address</param>
		/// <returns></returns>
		uint AddressToOffset(ulong address);

		/// <summary>
		/// Converts <paramref name="rva"/> to a file offset
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		uint RVAToOffset(uint rva);

		/// <summary>
		/// Converts <paramref name="offset"/> to an RVA
		/// </summary>
		/// <param name="offset"></param>
		/// <returns></returns>
		uint OffsetToRVA(uint offset);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="array">Destination</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to read</param>
		void Read(uint rva, byte[] array, long index, uint count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="array">Destination</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to read</param>
		void Read(uint rva, byte[] array, long index, int count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		byte[] Read(uint rva, uint count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		byte[] Read(uint rva, int count);

		/// <summary>
		/// Writes data to memory in the debugged process. Returns the number of bytes written.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="array">Source</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		uint Write(uint rva, byte[] array, long index, uint count);

		/// <summary>
		/// Writes data to memory in the debugged process. Returns the number of bytes written.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="array">Source</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		int Write(uint rva, byte[] array, long index, int count);

		/// <summary>
		/// Writes data to memory in the debugged process. Throws if all bytes couldn't be written.
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="array">Source</param>
		void Write(uint rva, byte[] array);

		/// <summary>
		/// Reads a <see cref="bool"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		bool ReadBoolean(uint rva);

		/// <summary>
		/// Reads a <see cref="char"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		char ReadChar(uint rva);

		/// <summary>
		/// Reads a <see cref="sbyte"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		sbyte ReadSByte(uint rva);

		/// <summary>
		/// Reads a <see cref="byte"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		byte ReadByte(uint rva);

		/// <summary>
		/// Reads a <see cref="short"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		short ReadInt16(uint rva);

		/// <summary>
		/// Reads a <see cref="ushort"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		ushort ReadUInt16(uint rva);

		/// <summary>
		/// Reads a <see cref="int"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		int ReadInt32(uint rva);

		/// <summary>
		/// Reads a <see cref="uint"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		uint ReadUInt32(uint rva);

		/// <summary>
		/// Reads a <see cref="long"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		long ReadInt64(uint rva);

		/// <summary>
		/// Reads a <see cref="ulong"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		ulong ReadUInt64(uint rva);

		/// <summary>
		/// Reads a <see cref="float"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		float ReadSingle(uint rva);

		/// <summary>
		/// Reads a <see cref="double"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		double ReadDouble(uint rva);

		/// <summary>
		/// Reads a <see cref="decimal"/> from an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		decimal ReadDecimal(uint rva);

		/// <summary>
		/// Writes a <see cref="bool"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, bool value);

		/// <summary>
		/// Writes a <see cref="char"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, char value);

		/// <summary>
		/// Writes a <see cref="sbyte"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, sbyte value);

		/// <summary>
		/// Writes a <see cref="byte"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, byte value);

		/// <summary>
		/// Writes a <see cref="short"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, short value);

		/// <summary>
		/// Writes a <see cref="ushort"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, ushort value);

		/// <summary>
		/// Writes a <see cref="int"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, int value);

		/// <summary>
		/// Writes a <see cref="uint"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, uint value);

		/// <summary>
		/// Writes a <see cref="long"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, long value);

		/// <summary>
		/// Writes a <see cref="ulong"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, ulong value);

		/// <summary>
		/// Writes a <see cref="float"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, float value);

		/// <summary>
		/// Writes a <see cref="double"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, double value);

		/// <summary>
		/// Writes a <see cref="decimal"/> to an address in the debugged process
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="value">Value</param>
		void Write(uint rva, decimal value);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="array">Destination</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to read</param>
		void ReadOffset(uint offset, byte[] array, long index, uint count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="array">Destination</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to read</param>
		void ReadOffset(uint offset, byte[] array, long index, int count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		byte[] ReadOffset(uint offset, uint count);

		/// <summary>
		/// Reads memory from the debugged process. Unmapped memory is read as 0s.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		byte[] ReadOffset(uint offset, int count);

		/// <summary>
		/// Writes data to memory in the debugged process. Returns the number of bytes written.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="array">Source</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		uint WriteOffset(uint offset, byte[] array, long index, uint count);

		/// <summary>
		/// Writes data to memory in the debugged process. Returns the number of bytes written.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="array">Source</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		int WriteOffset(uint offset, byte[] array, long index, int count);

		/// <summary>
		/// Writes data to memory in the debugged process. Throws if all bytes couldn't be written.
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="array">Source</param>
		void WriteOffset(uint offset, byte[] array);

		/// <summary>
		/// Reads a <see cref="bool"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		bool ReadBooleanOffset(uint offset);

		/// <summary>
		/// Reads a <see cref="char"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		char ReadCharOffset(uint offset);

		/// <summary>
		/// Reads a <see cref="sbyte"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		sbyte ReadSByteOffset(uint offset);

		/// <summary>
		/// Reads a <see cref="byte"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		byte ReadByteOffset(uint offset);

		/// <summary>
		/// Reads a <see cref="short"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		short ReadInt16Offset(uint offset);

		/// <summary>
		/// Reads a <see cref="ushort"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		ushort ReadUInt16Offset(uint offset);

		/// <summary>
		/// Reads a <see cref="int"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		int ReadInt32Offset(uint offset);

		/// <summary>
		/// Reads a <see cref="uint"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		uint ReadUInt32Offset(uint offset);

		/// <summary>
		/// Reads a <see cref="long"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		long ReadInt64Offset(uint offset);

		/// <summary>
		/// Reads a <see cref="ulong"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		ulong ReadUInt64Offset(uint offset);

		/// <summary>
		/// Reads a <see cref="float"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		float ReadSingleOffset(uint offset);

		/// <summary>
		/// Reads a <see cref="double"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		double ReadDoubleOffset(uint offset);

		/// <summary>
		/// Reads a <see cref="decimal"/> from an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns></returns>
		decimal ReadDecimalOffset(uint offset);

		/// <summary>
		/// Writes a <see cref="bool"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, bool value);

		/// <summary>
		/// Writes a <see cref="char"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, char value);

		/// <summary>
		/// Writes a <see cref="sbyte"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, sbyte value);

		/// <summary>
		/// Writes a <see cref="byte"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, byte value);

		/// <summary>
		/// Writes a <see cref="short"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, short value);

		/// <summary>
		/// Writes a <see cref="ushort"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, ushort value);

		/// <summary>
		/// Writes a <see cref="int"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, int value);

		/// <summary>
		/// Writes a <see cref="uint"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, uint value);

		/// <summary>
		/// Writes a <see cref="long"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, long value);

		/// <summary>
		/// Writes a <see cref="ulong"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, ulong value);

		/// <summary>
		/// Writes a <see cref="float"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, float value);

		/// <summary>
		/// Writes a <see cref="double"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, double value);

		/// <summary>
		/// Writes a <see cref="decimal"/> to an address in the debugged process
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="value">Value</param>
		void WriteOffset(uint offset, decimal value);

		/// <summary>
		/// Save the module to a byte[]. Can't be called if it's a dynamic assembly (<see cref="IsDynamic"/> is true)
		/// </summary>
		/// <returns></returns>
		byte[] Save();

		/// <summary>
		/// Save the module to a stream. Can't be called if it's a dynamic assembly (<see cref="IsDynamic"/> is true)
		/// </summary>
		/// <param name="stream">Destination stream</param>
		void Save(Stream stream);

		/// <summary>
		/// Save the module to a file. Can't be called if it's a dynamic assembly (<see cref="IsDynamic"/> is true)
		/// </summary>
		/// <param name="filename">Filename</param>
		void Save(string filename);

		/// <summary>
		/// Finds a class
		/// </summary>
		/// <param name="className">Class name</param>
		/// <returns></returns>
		IDebuggerClass GetClass(string className);

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string className, string methodName);

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="className">Class name</param>
		/// <param name="fieldName">Field name</param>
		/// <returns></returns>
		IDebuggerField GetField(string className, string fieldName);

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="className">Class name</param>
		/// <param name="propertyName">Property name</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(string className, string propertyName);

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="className">Class name</param>
		/// <param name="eventName">Event name</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(string className, string eventName);

		/// <summary>
		/// Finds a type
		/// </summary>
		/// <param name="className">Class name</param>
		/// <returns></returns>
		IDebuggerType GetType(string className);

		/// <summary>
		/// Finds a type
		/// </summary>
		/// <param name="className">Class name</param>
		/// <param name="genericArguments">Generic arguments</param>
		/// <returns></returns>
		IDebuggerType GetType(string className, params IDebuggerType[] genericArguments);

		/// <summary>
		/// Finds a type
		/// </summary>
		/// <param name="type">A type that must exist in one of the loaded assemblies in the
		/// debugged process.</param>
		/// <returns></returns>
		IDebuggerType GetType(Type type);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerField GetField(FieldInfo field);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(MethodBase method);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="prop">Property</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(PropertyInfo prop);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="evt">Event</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(EventInfo evt);
	}
}
