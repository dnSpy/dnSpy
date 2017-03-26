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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// <see cref="DbgEngineCodeBreakpoint"/> serializer. Use <see cref="ExportDbgEngineCodeBreakpointSerializerAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgEngineCodeBreakpointSerializer {
		/// <summary>
		/// Serializes <paramref name="breakpoint"/>
		/// </summary>
		/// <param name="section">Destination section</param>
		/// <param name="breakpoint">Breakpoint</param>
		public abstract void Serialize(ISettingsSection section, DbgEngineCodeBreakpoint breakpoint);

		/// <summary>
		/// Deserializes a breakpoint or returns null if it failed
		/// </summary>
		/// <param name="section">Serialized section</param>
		/// <returns></returns>
		public abstract DbgEngineCodeBreakpoint Deserialize(ISettingsSection section);
	}

	/// <summary>Metadata</summary>
	public interface IDbgEngineCodeBreakpointSerializerMetadata {
		/// <summary>See <see cref="ExportDbgEngineCodeBreakpointSerializerAttribute.Types"/></summary>
		string[] Types { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgEngineCodeBreakpointSerializer"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgEngineCodeBreakpointSerializerAttribute : ExportAttribute, IDbgEngineCodeBreakpointSerializerMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type (compared against <see cref="DbgEngineCodeBreakpoint.Type"/>), see <see cref="PredefinedDbgEngineCodeBreakpointTypes"/></param>
		public ExportDbgEngineCodeBreakpointSerializerAttribute(string type)
			: base(typeof(DbgEngineCodeBreakpointSerializer)) => Types = new[] { type ?? throw new ArgumentNullException(nameof(type)) };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="types">Types (compared against <see cref="DbgEngineCodeBreakpoint.Type"/>), see <see cref="PredefinedDbgEngineCodeBreakpointTypes"/></param>
		public ExportDbgEngineCodeBreakpointSerializerAttribute(string[] types)
			: base(typeof(DbgEngineCodeBreakpointSerializer)) => Types = types ?? throw new ArgumentNullException(nameof(types));

		/// <summary>
		/// Types of supported <see cref="DbgEngineCodeBreakpoint"/>s
		/// </summary>
		public string[] Types { get; }
	}
}
