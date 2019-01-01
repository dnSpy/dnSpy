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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Disassembly;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Implemented by a .NET engine, see <see cref="DbgRuntime.InternalRuntime"/>
	/// </summary>
	public interface IDbgDotNetRuntime {
		/// <summary>
		/// Gets the dispatcher
		/// </summary>
		DbgDotNetDispatcher Dispatcher { get; }

		/// <summary>
		/// Gets the feature flags
		/// </summary>
		DbgDotNetRuntimeFeatures Features { get; }

		/// <summary>
		/// Gets the module id
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		ModuleId GetModuleId(DbgModule module);

		/// <summary>
		/// Gets the module data or <see cref="DbgDotNetRawModuleBytes.None"/>
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		DbgDotNetRawModuleBytes GetRawModuleBytes(DbgModule module);

		/// <summary>
		/// Translates a method token from the original dynamic module's metadata to the saved module metadata used by the expression compiler
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="methodToken">Method token</param>
		/// <param name="metadataMethodToken">New method token</param>
		/// <param name="metadataLocalVarSigTok">New method body local variables signature token</param>
		/// <returns></returns>
		bool TryGetMethodToken(DbgModule module, int methodToken, out int metadataMethodToken, out int metadataLocalVarSigTok);

		/// <summary>
		/// Gets the current method or null if it's not a normal IL frame
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		DmdMethodBase GetFrameMethod(DbgEvaluationInfo evalInfo);

		/// <summary>
		/// Loads the address of an instance or a static field or returns null if it's not supported
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="field">Field</param>
		/// <returns></returns>
		DbgDotNetValue LoadFieldAddress(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field);

		/// <summary>
		/// Loads an instance or a static field
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="field">Field</param>
		/// <returns></returns>
		DbgDotNetValueResult LoadField(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field);

		/// <summary>
		/// Stores a value in a field. Returns null or an error message
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="field">Field</param>
		/// <param name="value">Value to store: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <returns></returns>
		string StoreField(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field, object value);

		/// <summary>
		/// Calls an instance or a static method
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="obj">Instance object or null if it's a static method</param>
		/// <param name="method">Method</param>
		/// <param name="arguments">Arguments: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="invokeOptions">Invoke options</param>
		/// <returns></returns>
		DbgDotNetValueResult Call(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions);

		/// <summary>
		/// Creates a new instance of a type by calling its constructor
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="ctor">Constructor</param>
		/// <param name="arguments">Arguments: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="invokeOptions">Invoke options</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateInstance(DbgEvaluationInfo evalInfo, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions);

		/// <summary>
		/// Creates a new instance of a type. All fields are initialized to 0 or null. The constructor isn't called.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="type">Type to create</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateInstanceNoConstructor(DbgEvaluationInfo evalInfo, DmdType type);

		/// <summary>
		/// Creates an SZ array
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="elementType">Element type</param>
		/// <param name="length">Length of the array</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateSZArray(DbgEvaluationInfo evalInfo, DmdType elementType, int length);

		/// <summary>
		/// Creates a multi-dimensional array
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="elementType">Element type</param>
		/// <param name="dimensionInfos">Dimension infos</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateArray(DbgEvaluationInfo evalInfo, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos);

		/// <summary>
		/// Gets aliases
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		DbgDotNetAliasInfo[] GetAliases(DbgEvaluationInfo evalInfo);

		/// <summary>
		/// Gets all exceptions
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationInfo evalInfo);

		/// <summary>
		/// Gets all return values
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		DbgDotNetReturnValueInfo[] GetReturnValues(DbgEvaluationInfo evalInfo);

		/// <summary>
		/// Gets an exception or null
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="id">Exception id, eg. <see cref="DbgDotNetRuntimeConstants.ExceptionId"/></param>
		/// <returns></returns>
		DbgDotNetValue GetException(DbgEvaluationInfo evalInfo, uint id);

		/// <summary>
		/// Gets a stowed exception or null
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="id">Stowed exception id, eg. <see cref="DbgDotNetRuntimeConstants.StowedExceptionId"/></param>
		/// <returns></returns>
		DbgDotNetValue GetStowedException(DbgEvaluationInfo evalInfo, uint id);

		/// <summary>
		/// Gets a return value or null
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="id">Return value id, eg. <see cref="DbgDotNetRuntimeConstants.LastReturnValueId"/></param>
		/// <returns></returns>
		DbgDotNetValue GetReturnValue(DbgEvaluationInfo evalInfo, uint id);

		/// <summary>
		/// Gets a local value
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Metadata index of local</param>
		/// <returns></returns>
		DbgDotNetValueResult GetLocalValue(DbgEvaluationInfo evalInfo, uint index);

		/// <summary>
		/// Gets a parameter value
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Metadata index of parameter</param>
		/// <returns></returns>
		DbgDotNetValueResult GetParameterValue(DbgEvaluationInfo evalInfo, uint index);

		/// <summary>
		/// Writes a new local value. Returns an error message or null.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Metadata index of parameter</param>
		/// <param name="targetType">Type of the local</param>
		/// <param name="value">New value: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <returns></returns>
		string SetLocalValue(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value);

		/// <summary>
		/// Writes a new parameter value. Returns an error message or null.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Metadata index of parameter</param>
		/// <param name="targetType">Type of the parameter</param>
		/// <param name="value">New value: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <returns></returns>
		string SetParameterValue(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value);

		/// <summary>
		/// Gets the address of a local value or null if it's not supported
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Metadata index of local</param>
		/// <param name="targetType">Type of the local</param>
		/// <returns></returns>
		DbgDotNetValue GetLocalValueAddress(DbgEvaluationInfo evalInfo, uint index, DmdType targetType);

		/// <summary>
		/// Gets the address of a parameter value or null if it's not supported
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Metadata index of local</param>
		/// <param name="targetType">Type of the parameter</param>
		/// <returns></returns>
		DbgDotNetValue GetParameterValueAddress(DbgEvaluationInfo evalInfo, uint index, DmdType targetType);

		/// <summary>
		/// Creates a simple value (a primitive number or a string, or arrays of those types)
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="value">A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateValue(DbgEvaluationInfo evalInfo, object value);

		/// <summary>
		/// Boxes the value type
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="value">Value to box</param>
		/// <returns></returns>
		DbgDotNetValueResult Box(DbgEvaluationInfo evalInfo, object value);

		/// <summary>
		/// Returns true if it's possible to create an object id
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		bool CanCreateObjectId(DbgDotNetValue value);

		/// <summary>
		/// Creates an object id or returns null
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <param name="id">Unique id</param>
		/// <returns></returns>
		DbgDotNetObjectId CreateObjectId(DbgDotNetValue value, uint id);

		/// <summary>
		/// Checks if an object id and a value refer to the same data
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		bool Equals(DbgDotNetObjectId objectId, DbgDotNetValue value);

		/// <summary>
		/// Gets the hash code of an object id
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <returns></returns>
		int GetHashCode(DbgDotNetObjectId objectId);

		/// <summary>
		/// Gets the hash code of a value created by this runtime
		/// </summary>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		int GetHashCode(DbgDotNetValue value);

		/// <summary>
		/// Gets an object ID's value or null if there was an error
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="objectId">Object id created by this class</param>
		/// <returns></returns>
		DbgDotNetValue GetValue(DbgEvaluationInfo evalInfo, DbgDotNetObjectId objectId);

		/// <summary>
		/// Checks if two values are equal. Returns null if it's unknown.
		/// </summary>
		/// <param name="a">Value #1</param>
		/// <param name="b">Value #2</param>
		/// <returns></returns>
		bool? Equals(DbgDotNetValue a, DbgDotNetValue b);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="frame">Frame</param>
		/// <param name="nativeCode">Updated with the native code if successful</param>
		/// <returns></returns>
		bool TryGetNativeCode(DbgStackFrame frame, out DbgDotNetNativeCode nativeCode);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="nativeCode">Updated with the native code if successful</param>
		/// <returns></returns>
		bool TryGetNativeCode(DmdMethodBase method, out DbgDotNetNativeCode nativeCode);

		/// <summary>
		/// Tries to get a symbol
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="result">Updated with the symbol if successful</param>
		/// <returns></returns>
		bool TryGetSymbol(ulong address, out SymbolResolverResult result);
	}

	/// <summary>
	/// Invoke options
	/// </summary>
	[Flags]
	public enum DbgDotNetInvokeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Non-virtual call
		/// </summary>
		NonVirtual				= 0x00000001,
	}

	/// <summary>
	/// .NET runtime features
	/// </summary>
	[Flags]
	public enum DbgDotNetRuntimeFeatures {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Object IDs are supported
		/// </summary>
		ObjectIds				= 0x00000001,

		/// <summary>
		/// Calling generic methods isn't supported
		/// </summary>
		NoGenericMethods		= 0x00000002,

		/// <summary>
		/// <see cref="DbgDotNetValue.LoadIndirect"/> and <see cref="DbgDotNetValue.StoreIndirect(DbgEvaluationInfo, object)"/>
		/// isn't supported for pointers.
		/// </summary>
		NoDereferencePointers	= 0x00000004,

		/// <summary>
		/// Async step with object ids isn't supported
		/// </summary>
		NoAsyncStepObjectId		= 0x00000008,

		/// <summary>
		/// It's possible to get the native code of jitted managed methods
		/// </summary>
		NativeMethodBodies		= 0x00000010,
	}

	/// <summary>
	/// Constants
	/// </summary>
	public static class DbgDotNetRuntimeConstants {
		/// <summary>
		/// Exception ID
		/// </summary>
		public const uint ExceptionId = 1;

		/// <summary>
		/// Stowed exception ID
		/// </summary>
		public const uint StowedExceptionId = 1;

		/// <summary>
		/// ID of last return value
		/// </summary>
		public const uint LastReturnValueId = 0;
	}

	/// <summary>
	/// Contains .NET module data information
	/// </summary>
	public readonly struct DbgDotNetRawModuleBytes {
		/// <summary>
		/// No .NET module data is available
		/// </summary>
		public static readonly DbgDotNetRawModuleBytes None = default;

		/// <summary>
		/// true if it's file layout, false if it's memory layout
		/// </summary>
		public bool IsFileLayout { get; }

		/// <summary>
		/// Raw bytes of the .NET module
		/// </summary>
		public byte[] RawBytes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rawBytes">Raw bytes of the .NET module</param>
		/// <param name="isFileLayout">true if it's file layout, false if it's memory layout</param>
		public DbgDotNetRawModuleBytes(byte[] rawBytes, bool isFileLayout) {
			IsFileLayout = isFileLayout;
			RawBytes = rawBytes ?? throw new ArgumentNullException(nameof(rawBytes));
		}
	}
}
