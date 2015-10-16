/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Threading;
using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorDeclSecurity : DeclSecurity, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public MDToken OriginalToken {
			get { return new MDToken(MDToken.Table, origRid); }
		}

		public CorDeclSecurity(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			this.origRid = rid;
			Initialize_NoLock();
		}

		void Initialize_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;
			this.action = MDAPI.GetPermissionSetAction(mdi, token);
		}

		protected override void InitializeCustomAttributes() {
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());
		}

		protected override void InitializeSecurityAttributes() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;
			var data = MDAPI.GetPermissionSetBlob(mdi, token);
			var gpContext = new GenericParamContext();
			var tmp = DeclSecurityReader.Read(readerModule, data, gpContext);
			Interlocked.CompareExchange(ref securityAttributes, tmp, null);
		}

		public override byte[] GetBlob() {
			if (blob != null)
				return blob;
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;
			Interlocked.CompareExchange(ref blob, MDAPI.GetPermissionSetBlob(mdi, token), null);
			return blob;
		}
		byte[] blob;
	}
}
