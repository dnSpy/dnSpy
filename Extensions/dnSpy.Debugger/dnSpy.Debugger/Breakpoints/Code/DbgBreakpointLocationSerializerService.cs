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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class DbgBreakpointLocationSerializerService {
		public abstract void Serialize(ISettingsSection section, DbgBreakpointLocation breakpoint);
		public abstract DbgBreakpointLocation Deserialize(ISettingsSection section);
	}

	[Export(typeof(DbgBreakpointLocationSerializerService))]
	sealed class DbgBreakpointLocationSerializerServiceImpl : DbgBreakpointLocationSerializerService {
		readonly Lazy<DbgBreakpointLocationSerializer, IDbgBreakpointLocationSerializerMetadata>[] dbgBreakpointLocationSerializers;

		[ImportingConstructor]
		DbgBreakpointLocationSerializerServiceImpl([ImportMany] IEnumerable<Lazy<DbgBreakpointLocationSerializer, IDbgBreakpointLocationSerializerMetadata>> dbgBreakpointLocationSerializers) =>
			this.dbgBreakpointLocationSerializers = dbgBreakpointLocationSerializers.ToArray();

		Lazy<DbgBreakpointLocationSerializer, IDbgBreakpointLocationSerializerMetadata> TryGetSerializer(string type) {
			foreach (var lz in dbgBreakpointLocationSerializers) {
				if (Array.IndexOf(lz.Metadata.Types, type) >= 0)
					return lz;
			}
			return null;
		}

		public override void Serialize(ISettingsSection section, DbgBreakpointLocation breakpoint) {
			if (section == null)
				throw new ArgumentNullException(nameof(section));
			if (breakpoint == null)
				throw new ArgumentNullException(nameof(breakpoint));

			var bpType = breakpoint.Type;
			var serializer = TryGetSerializer(bpType);
			Debug.Assert(serializer != null);
			if (serializer == null)
				return;

			section.Attribute("__BPT", bpType);
			serializer.Value.Serialize(section, breakpoint);
		}

		public override DbgBreakpointLocation Deserialize(ISettingsSection section) {
			if (section == null)
				return null;

			var typeFullName = section.Attribute<string>("__BPT");
			Debug.Assert(typeFullName != null);
			if (typeFullName == null)
				return null;
			var serializer = TryGetSerializer(typeFullName);
			Debug.Assert(serializer != null);
			if (serializer == null)
				return null;

			return serializer.Value.Deserialize(section);
		}
	}
}
