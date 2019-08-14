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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class DbgCodeLocationSerializerService {
		public abstract bool CanSerialize(DbgCodeLocation location);
		public abstract void Serialize(ISettingsSection section, DbgCodeLocation location);
		public abstract DbgCodeLocation? Deserialize(ISettingsSection? section);
	}

	[Export(typeof(DbgCodeLocationSerializerService))]
	sealed class DbgCodeLocationSerializerServiceImpl : DbgCodeLocationSerializerService {
		readonly Lazy<DbgCodeLocationSerializer, IDbgCodeLocationSerializerMetadata>[] dbgCodeLocationSerializers;

		[ImportingConstructor]
		DbgCodeLocationSerializerServiceImpl([ImportMany] IEnumerable<Lazy<DbgCodeLocationSerializer, IDbgCodeLocationSerializerMetadata>> dbgCodeLocationSerializers) =>
			this.dbgCodeLocationSerializers = dbgCodeLocationSerializers.ToArray();

		Lazy<DbgCodeLocationSerializer, IDbgCodeLocationSerializerMetadata>? TryGetSerializer(string type) {
			foreach (var lz in dbgCodeLocationSerializers) {
				if (Array.IndexOf(lz.Metadata.Types, type) >= 0)
					return lz;
			}
			return null;
		}

		public override bool CanSerialize(DbgCodeLocation location) => !(TryGetSerializer(location.Type) is null);

		public override void Serialize(ISettingsSection section, DbgCodeLocation location) {
			if (section is null)
				throw new ArgumentNullException(nameof(section));
			if (location is null)
				throw new ArgumentNullException(nameof(location));

			var bpType = location.Type;
			var serializer = TryGetSerializer(bpType);
			Debug2.Assert(!(serializer is null));
			if (serializer is null)
				return;

			section.Attribute("__BPT", bpType);
			serializer.Value.Serialize(section, location);
		}

		public override DbgCodeLocation? Deserialize(ISettingsSection? section) {
			if (section is null)
				return null;

			var typeFullName = section.Attribute<string>("__BPT");
			Debug2.Assert(!(typeFullName is null));
			if (typeFullName is null)
				return null;
			var serializer = TryGetSerializer(typeFullName);
			Debug2.Assert(!(serializer is null));
			if (serializer is null)
				return null;

			return serializer.Value.Deserialize(section);
		}
	}
}
