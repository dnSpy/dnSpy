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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;

namespace dnSpy.Contracts.Debugger.Disassembly {
	/// <summary>
	/// Returns native method bodies. Use <see cref="ExportDbgRuntimeNativeCodeProviderAttribute"/> to
	/// export an instance.
	/// </summary>
	public abstract class DbgRuntimeNativeCodeProvider {
		/// <summary>
		/// Checks if it's possible to get native code
		/// </summary>
		/// <param name="frame">Stack frame</param>
		/// <returns></returns>
		public abstract bool CanGetNativeCode(DbgStackFrame frame);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="frame">Stack frame</param>
		/// <param name="options">Options</param>
		/// <param name="result">Native code if successful</param>
		/// <returns></returns>
		public abstract bool TryGetNativeCode(DbgStackFrame frame, DbgNativeCodeOptions options, out GetNativeCodeResult result);

		/// <summary>
		/// Checks if it's possible to get native code
		/// </summary>
		/// <param name="boundBreakpoint">A bound breakpoint</param>
		/// <returns></returns>
		public abstract bool CanGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="boundBreakpoint">A bound breakpoint</param>
		/// <param name="options">Options</param>
		/// <param name="result">Native code if successful</param>
		/// <returns></returns>
		public abstract bool TryGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, DbgNativeCodeOptions options, out GetNativeCodeResult result);

		/// <summary>
		/// Checks if it's possible to get native code
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="location">Code location</param>
		/// <returns></returns>
		public abstract bool CanGetNativeCode(DbgRuntime runtime, DbgCodeLocation location);

		/// <summary>
		/// Tries to get the native code
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="location">Code location</param>
		/// <param name="options">Options</param>
		/// <param name="result">Native code if successful</param>
		/// <returns></returns>
		public abstract bool TryGetNativeCode(DbgRuntime runtime, DbgCodeLocation location, DbgNativeCodeOptions options, out GetNativeCodeResult result);
	}

	/// <summary>Metadata</summary>
	public interface IDbgRuntimeNativeCodeProviderMetadata {
		/// <summary>See <see cref="ExportDbgRuntimeNativeCodeProviderAttribute.Guid"/></summary>
		string? Guid { get; }
		/// <summary>See <see cref="ExportDbgRuntimeNativeCodeProviderAttribute.RuntimeKindGuid"/></summary>
		string? RuntimeKindGuid { get; }
		/// <summary>See <see cref="ExportDbgRuntimeNativeCodeProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgRuntimeNativeCodeProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgRuntimeNativeCodeProviderAttribute : ExportAttribute, IDbgRuntimeNativeCodeProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">Runtime GUID or null, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <param name="runtimeKindGuid">Runtime kind GUID or null, see <see cref="PredefinedDbgRuntimeKindGuids"/></param>
		/// <param name="order">Order</param>
		public ExportDbgRuntimeNativeCodeProviderAttribute(string? guid, string? runtimeKindGuid, double order = double.MaxValue)
			: base(typeof(DbgRuntimeNativeCodeProvider)) {
			Guid = guid;
			RuntimeKindGuid = runtimeKindGuid;
			Order = order;
		}

		/// <summary>
		/// Gets the runtime GUID or null, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public string? Guid { get; }

		/// <summary>
		/// Gets the runtime kind GUID or null, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public string? RuntimeKindGuid { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
