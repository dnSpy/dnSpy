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
		/// Gets the module id
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		ModuleId GetModuleId(DbgModule module);

		/// <summary>
		/// Gets the current method or null if it's not a normal IL frame
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DmdMethodBase GetFrameMethod(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken);

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
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a new instance of a type by calling its constructor
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="ctor">Constructor</param>
		/// <param name="arguments">Arguments: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, CancellationToken cancellationToken);

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
		/// Gets a local value or null if the local doesn't exist or if it's not possible to read it (eg. optimized code)
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="index">Metadata index of local</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult GetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a parameter value or null if the parameter doesn't exist or if it's not possible to read it (eg. optimized code)
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
		/// Creates a simple value (a primitive number or a string, or arrays of those types)
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="value">A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetCreateValueResult CreateValue(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken);

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
}
