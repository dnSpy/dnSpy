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
using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorAssemblyDef : AssemblyDef, ICorHasCustomAttribute, ICorHasDeclSecurity {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorAssemblyDef(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			Initialize_NoLock();
		}

		void Initialize_NoLock() => InitAssemblyName_NoLock();
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());
		protected override void InitializeDeclSecurities() =>
			readerModule.InitDeclSecurities(this, ref declSecurities);

		unsafe void InitAssemblyName_NoLock() {
			var mdai = readerModule.MetaDataAssemblyImport;
			uint token = OriginalToken.Raw;

			Name = MDAPI.GetAssemblySimpleName(mdai, token) ?? string.Empty;
			string locale;
			Version = MDAPI.GetAssemblyVersionAndLocale(mdai, token, out locale) ?? new Version(0, 0, 0, 0);
			Culture = locale ?? string.Empty;
			HashAlgorithm = MDAPI.GetAssemblyHashAlgorithm(mdai, token) ?? AssemblyHashAlgorithm.SHA1;
			Attributes = MDAPI.GetAssemblyAttributes(mdai, token) ?? AssemblyAttributes.None;
			PublicKey = MDAPI.GetAssemblyPublicKey(mdai, token) ?? new PublicKey((byte[])null);
		}
	}
}
