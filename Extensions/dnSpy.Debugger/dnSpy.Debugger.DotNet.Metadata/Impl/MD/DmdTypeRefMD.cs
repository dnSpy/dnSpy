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
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdTypeRefMD : DmdTypeRef {
		public override DmdTypeScope TypeScope { get; }
		public override string? MetadataNamespace { get; }
		public override string? MetadataName { get; }

		readonly DmdEcma335MetadataReader reader;
		readonly int declTypeToken;

		public DmdTypeRefMD(DmdEcma335MetadataReader reader, uint rid, IList<DmdCustomModifier>? customModifiers) : base(reader.Module, rid, customModifiers) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

			bool b = reader.TablesStream.TryReadTypeRefRow(rid, out var row);
			Debug.Assert(b);
			var ns = reader.StringsStream.Read(row.Namespace);
			MetadataNamespace = string.IsNullOrEmpty(ns) ? null : ns;
			MetadataName = reader.StringsStream.ReadNoNull(row.Name);

			if (!CodedToken.ResolutionScope.Decode(row.ResolutionScope, out uint resScopeToken))
				resScopeToken = uint.MaxValue;
			switch (resScopeToken >> 24) {
			case 0x00:
				TypeScope = new DmdTypeScope(reader.Module);
				break;

			case 0x01:
				TypeScope = DmdTypeScope.Invalid;
				declTypeToken = (int)resScopeToken;
				break;

			case 0x1A:
				reader.TablesStream.TryReadModuleRefRow(resScopeToken & 0x00FFFFFF, out var moduleRefRow);
				var moduleName = reader.StringsStream.ReadNoNull(moduleRefRow.Name);
				TypeScope = new DmdTypeScope(reader.GetName(), moduleName);
				break;

			case 0x23:
				TypeScope = new DmdTypeScope(reader.ReadAssemblyName(resScopeToken & 0x00FFFFFF));
				break;

			default:
				TypeScope = DmdTypeScope.Invalid;
				break;
			}
		}

		protected override int GetDeclaringTypeRefToken() => declTypeToken;
		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => AppDomain.Intern(new DmdTypeRefMD(reader, Rid, VerifyCustomModifiers(customModifiers)));
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.Intern(new DmdTypeRefMD(reader, Rid, null));
	}
}
