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

using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorExportedType : ExportedType, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		new uint implementation;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorExportedType(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			Initialize_NoLock();
		}

		void Initialize_NoLock() {
			module = readerModule;
			InitNameAndAttrs_NoLock();
		}

		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());

		void InitNameAndAttrs_NoLock() {
			var mdai = readerModule.MetaDataAssemblyImport;
			uint token = OriginalToken.Raw;
			Utils.SplitNameAndNamespace(null, MDAPI.GetExportedTypeName(mdai, token), out var ns, out var name);
			TypeName = name;
			TypeNamespace = ns;

			MDAPI.GetExportedTypeProps(mdai, token, out implementation, out typeDefId, out var attrs);
			attributes = (int)attrs;
		}

		protected override IImplementation? GetImplementation_NoLock() => readerModule.ResolveToken(implementation) as IImplementation;
	}
}
