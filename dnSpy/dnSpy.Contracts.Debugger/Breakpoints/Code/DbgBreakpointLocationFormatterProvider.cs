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
using dnSpy.Contracts.Debugger.Code;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code {
	/// <summary>
	/// Creates <see cref="DbgCodeLocation"/> formatters. Use <see cref="ExportDbgBreakpointLocationFormatterProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgBreakpointLocationFormatterProvider {
		/// <summary>
		/// Returns a formatter or null
		/// </summary>
		/// <param name="location">Breakpoint location</param>
		/// <returns></returns>
		public abstract DbgBreakpointLocationFormatter? Create(DbgCodeLocation location);
	}

	/// <summary>Metadata</summary>
	public interface IDbgBreakpointLocationFormatterProviderMetadata {
		/// <summary>See <see cref="ExportDbgBreakpointLocationFormatterProviderAttribute.Types"/></summary>
		string[] Types { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgBreakpointLocationFormatterProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgBreakpointLocationFormatterProviderAttribute : ExportAttribute, IDbgBreakpointLocationFormatterProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type (compared against <see cref="DbgCodeLocation.Type"/>), see <see cref="PredefinedDbgCodeLocationTypes"/></param>
		public ExportDbgBreakpointLocationFormatterProviderAttribute(string type)
			: base(typeof(DbgBreakpointLocationFormatterProvider)) => Types = new[] { type ?? throw new ArgumentNullException(nameof(type)) };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="types">Types (compared against <see cref="DbgCodeLocation.Type"/>), see <see cref="PredefinedDbgCodeLocationTypes"/></param>
		public ExportDbgBreakpointLocationFormatterProviderAttribute(string[] types)
			: base(typeof(DbgBreakpointLocationFormatterProvider)) => Types = types ?? throw new ArgumentNullException(nameof(types));

		/// <summary>
		/// Types (compared against <see cref="DbgCodeLocation.Type"/>), see <see cref="PredefinedDbgCodeLocationTypes"/>
		/// </summary>
		public string[] Types { get; }
	}
}
