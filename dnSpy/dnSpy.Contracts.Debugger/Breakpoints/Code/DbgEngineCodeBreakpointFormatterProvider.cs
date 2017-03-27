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

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// Creates <see cref="DbgEngineCodeBreakpoint"/> formatters. Use <see cref="ExportDbgEngineCodeBreakpointFormatterProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgEngineCodeBreakpointFormatterProvider {
		/// <summary>
		/// Returns a formatter or null
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <returns></returns>
		public abstract DbgEngineCodeBreakpointFormatter Create(DbgEngineCodeBreakpoint breakpoint);
	}

	/// <summary>Metadata</summary>
	public interface IDbgEngineCodeBreakpointFormatterProviderMetadata {
		/// <summary>See <see cref="ExportDbgEngineCodeBreakpointFormatterProviderAttribute.Types"/></summary>
		string[] Types { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgEngineCodeBreakpointFormatterProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgEngineCodeBreakpointFormatterProviderAttribute : ExportAttribute, IDbgEngineCodeBreakpointFormatterProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type (compared against <see cref="DbgEngineCodeBreakpoint.Type"/>), see <see cref="PredefinedDbgEngineCodeBreakpointTypes"/></param>
		public ExportDbgEngineCodeBreakpointFormatterProviderAttribute(string type)
			: base(typeof(DbgEngineCodeBreakpointFormatterProvider)) => Types = new[] { type ?? throw new ArgumentNullException(nameof(type)) };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="types">Types (compared against <see cref="DbgEngineCodeBreakpoint.Type"/>), see <see cref="PredefinedDbgEngineCodeBreakpointTypes"/></param>
		public ExportDbgEngineCodeBreakpointFormatterProviderAttribute(string[] types)
			: base(typeof(DbgEngineCodeBreakpointFormatterProvider)) => Types = types ?? throw new ArgumentNullException(nameof(types));

		/// <summary>
		/// Types (compared against <see cref="DbgEngineCodeBreakpoint.Type"/>), see <see cref="PredefinedDbgEngineCodeBreakpointTypes"/>
		/// </summary>
		public string[] Types { get; }
	}
}
