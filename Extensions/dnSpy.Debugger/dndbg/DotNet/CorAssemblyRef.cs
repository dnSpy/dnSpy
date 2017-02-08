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
	sealed class CorAssemblyRef : AssemblyRef, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorAssemblyRef(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			Initialize_NoLock();
		}

		void Initialize_NoLock() => InitAssemblyName_NoLock();
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());

		unsafe void InitAssemblyName_NoLock() {
			var mdai = readerModule.MetaDataAssemblyImport;
			uint token = OriginalToken.Raw;

			Name = MDAPI.GetAssemblyRefSimpleName(mdai, token) ?? string.Empty;
			string locale;
			Version = MDAPI.GetAssemblyRefVersionAndLocale(mdai, token, out locale) ?? new Version(0, 0, 0, 0);
			Culture = locale ?? string.Empty;
			Hash = MDAPI.GetAssemblyRefHash(mdai, token);
			AssemblyAttributes attrs;
			PublicKeyOrToken = MDAPI.GetAssemblyRefPublicKeyOrToken(mdai, token, out attrs) ?? new PublicKeyToken((byte[])null);
			Attributes = attrs;
		}
	}
}