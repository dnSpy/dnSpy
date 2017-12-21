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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes {
	/// <summary>
	/// Creates value nodes. Use <see cref="ExportDbgDotNetValueNodeFactoryAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgDotNetValueNodeFactory {
		/// <summary>
		/// Creates a value node
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Frame</param>
		/// <param name="name">Name</param>
		/// <param name="value">Value</param>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Options</param>
		/// <param name="expression">Expression</param>
		/// <param name="imageName">Image name, see <see cref="PredefinedDbgValueNodeImageNames"/></param>
		/// <param name="isReadOnly">true if it's a read-only value</param>
		/// <param name="causesSideEffects">true if the expression causes side effects</param>
		/// <param name="expectedType">Expected type</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, in DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an exception value node
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Frame</param>
		/// <param name="id">Exception id</param>
		/// <param name="value">Value</param>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode CreateException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an exception value node
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Frame</param>
		/// <param name="id">Stowed exception id</param>
		/// <param name="value">Value</param>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode CreateStowedException(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a return value node
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Frame</param>
		/// <param name="id">Return value id</param>
		/// <param name="value">Value</param>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Options</param>
		/// <param name="method">Method</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode CreateReturnValue(DbgEvaluationContext context, DbgStackFrame frame, uint id, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgValueNodeEvaluationOptions options, DmdMethodBase method, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an error value node
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Frame</param>
		/// <param name="name">Name</param>
		/// <param name="errorMessage">Error message</param>
		/// <param name="expression">Expression</param>
		/// <param name="causesSideEffects">true if the expression causes side effects</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode CreateError(DbgEvaluationContext context, DbgStackFrame frame, in DbgDotNetText name, string errorMessage, string expression, bool causesSideEffects, CancellationToken cancellationToken);

		/// <summary>
		/// Creates type variables value node
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="frame">Frame</param>
		/// <param name="typeVariableInfos">Type variables</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgDotNetValueNode CreateTypeVariables(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetTypeVariableInfo[] typeVariableInfos, CancellationToken cancellationToken);
	}

	/// <summary>Metadata</summary>
	public interface IDbgDotNetValueNodeFactoryMetadata {
		/// <summary>See <see cref="ExportDbgDotNetValueNodeFactoryAttribute.LanguageGuid"/></summary>
		string LanguageGuid { get; }
		/// <summary>See <see cref="ExportDbgDotNetValueNodeFactoryAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgDotNetValueNodeFactory"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgDotNetValueNodeFactoryAttribute : ExportAttribute, IDbgDotNetValueNodeFactoryMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="languageGuid">Language GUID, see <see cref="DbgDotNetLanguageGuids"/></param>
		/// <param name="order">Order</param>
		public ExportDbgDotNetValueNodeFactoryAttribute(string languageGuid, double order = double.MaxValue)
			: base(typeof(DbgDotNetValueNodeFactory)) {
			LanguageGuid = languageGuid ?? throw new ArgumentNullException(nameof(languageGuid));
			Order = order;
		}

		/// <summary>
		/// Language GUID, see <see cref="DbgDotNetLanguageGuids"/>
		/// </summary>
		public string LanguageGuid { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}

	/// <summary>
	/// Contains the generic parameter and type
	/// </summary>
	public readonly struct DbgDotNetTypeVariableInfo {
		/// <summary>
		/// Gets the generic parameter type
		/// </summary>
		public DmdType GenericParameterType { get; }

		/// <summary>
		/// Gets the generic argument type
		/// </summary>
		public DmdType GenericArgumentType { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="genericParameterType">Generic parameter type</param>
		/// <param name="genericArgumentType">Generic argument type</param>
		public DbgDotNetTypeVariableInfo(DmdType genericParameterType, DmdType genericArgumentType) {
			GenericParameterType = genericParameterType ?? throw new ArgumentNullException(nameof(genericParameterType));
			GenericArgumentType = genericArgumentType ?? throw new ArgumentNullException(nameof(genericArgumentType));
		}
	}
}
