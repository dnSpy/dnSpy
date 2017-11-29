/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
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
		/// <param name="module"></param>
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
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DmdMethodBase GetFrameMethod(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken);

		/// <summary>
		/// Loads the address of an instance or a static field or returns null if it's not supported
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="field">Field</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue LoadFieldAddress(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken);

		/// <summary>
		/// Loads an instance or a static field
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="field">Field</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult LoadField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken);

		/// <summary>
		/// Stores a value in a field. Returns null or an error message
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="field">Field</param>
		/// <param name="value">Value to store: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string StoreField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, object value, CancellationToken cancellationToken);

		/// <summary>
		/// Calls an instance or a static method
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="method">Method</param>
		/// <param name="arguments">Arguments: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="invokeOptions">Invoke options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a new instance of a type by calling its constructor
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="ctor">Constructor</param>
		/// <param name="arguments">Arguments: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="invokeOptions">Invoke options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a new instance of a type. All fields are initialized to 0 or null. The constructor isn't called.
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="type">Type to create</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateInstanceNoConstructor(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an SZ array
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="elementType">Element type</param>
		/// <param name="length">Length of the array</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateSZArray(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, int length, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a multi-dimensional array
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="elementType">Element type</param>
		/// <param name="dimensionInfos">Dimension infos</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateArray(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos, CancellationToken cancellationToken);

		/// <summary>
		/// Gets aliases
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetAliasInfo[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all exceptions
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all return values
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetReturnValueInfo[] GetReturnValues(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken);

		/// <summary>
		/// Gets an exception or null
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="id">Exception id, eg. <see cref="DbgDotNetRuntimeConstants.ExceptionId"/></param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetException(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a stowed exception or null
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="id">Stowed exception id, eg. <see cref="DbgDotNetRuntimeConstants.StowedExceptionId"/></param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetStowedException(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a return value or null
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="id">Return value id, eg. <see cref="DbgDotNetRuntimeConstants.LastReturnValueId"/></param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetReturnValue(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a local value
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of local</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult GetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a parameter value
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of parameter</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult GetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken);

		/// <summary>
		/// Writes a new local value. Returns an error message or null.
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of parameter</param>
		/// <param name="targetType">Type of the local</param>
		/// <param name="value">New value: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string SetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, object value, CancellationToken cancellationToken);

		/// <summary>
		/// Writes a new parameter value. Returns an error message or null.
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of parameter</param>
		/// <param name="targetType">Type of the parameter</param>
		/// <param name="value">New value: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string SetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, object value, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the address of a local value or null if it's not supported
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of local</param>
		/// <param name="targetType">Type of the local</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetLocalValueAddress(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken);

		/// <summary>
		/// Gets the address of a parameter value or null if it's not supported
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of local</param>
		/// <param name="targetType">Type of the parameter</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetParameterValueAddress(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a simple value (a primitive number or a string, or arrays of those types)
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="value">A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetCreateValueResult CreateValue(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken);

		/// <summary>
		/// Boxes the value type
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="value">Value to box</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetCreateValueResult Box(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken);

		/// <summary>
		/// Returns true if object IDs are supported by this runtime
		/// </summary>
		bool SupportsObjectIds { get; }

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
		/// Gets an object ID's value
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="objectId">Object id created by this class</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetValue(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetObjectId objectId, CancellationToken cancellationToken);

		/// <summary>
		/// Checks if two values are equal. Returns null if it's unknown.
		/// </summary>
		/// <param name="a">Value #1</param>
		/// <param name="b">Value #2</param>
		/// <returns></returns>
		bool? Equals(DbgDotNetValue a, DbgDotNetValue b);
	}

	/// <summary>
	/// Invoke options
	/// </summary>
	[Flags]
	public enum DbgDotNetInvokeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Non-virtual call
		/// </summary>
		NonVirtual			= 0x00000001,
	}

	/// <summary>
	/// .NET runtime features
	/// </summary>
	public enum DbgDotNetRuntimeFeatures {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Calling generic methods isn't supported
		/// </summary>
		NoGenericMethods	= 0x00000001,
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
	/// Contains the created value or an error message
	/// </summary>
	public struct DbgDotNetCreateValueResult {
		/// <summary>
		/// Gets the value or null if there was an error
		/// </summary>
		public DbgDotNetValue Value { get; }

		/// <summary>
		/// Gets the error message or null
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="errorMessage">Error message</param>
		public DbgDotNetCreateValueResult(string errorMessage) {
			Value = null;
			Error = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public DbgDotNetCreateValueResult(DbgDotNetValue value) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Error = null;
		}
	}

	/// <summary>
	/// Contains .NET module data information
	/// </summary>
	public struct DbgDotNetRawModuleBytes {
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
