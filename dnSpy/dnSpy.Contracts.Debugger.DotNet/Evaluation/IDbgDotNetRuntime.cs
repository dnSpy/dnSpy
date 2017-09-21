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

using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
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
		/// <param name="value">Value to store</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string StoreField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, DbgDotNetValue value, CancellationToken cancellationToken);

		/// <summary>
		/// Calls an instance or a static method
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="method">Method</param>
		/// <param name="arguments">Arguments, simple types or <see cref="DbgDotNetValue"/>s</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a new instance of a type by calling its constructor
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="ctor">Constructor</param>
		/// <param name="arguments">Arguments, simple types or <see cref="DbgDotNetValue"/>s</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValueResult CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, CancellationToken cancellationToken);

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
		DbgDotNetEngineObjectId CreateObjectId(DbgDotNetValue value, uint id);

		/// <summary>
		/// Checks if an object id and a value refer to the same data
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <param name="value">Value created by this runtime</param>
		/// <returns></returns>
		bool Equals(DbgDotNetEngineObjectId objectId, DbgDotNetValue value);

		/// <summary>
		/// Gets the hash code of an object id
		/// </summary>
		/// <param name="objectId">Object id created by this class</param>
		/// <returns></returns>
		int GetHashCode(DbgDotNetEngineObjectId objectId);

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
		/// <param name="objectId">Object id created by this class</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		DbgDotNetValue GetValue(DbgEvaluationContext context, DbgDotNetEngineObjectId objectId, CancellationToken cancellationToken);
	}
}
