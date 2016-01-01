/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	sealed class CorMemberRef : MemberRef, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		/*readonly*/ GenericParamContext gpContext;

		public MDToken OriginalToken {
			get { return new MDToken(MDToken.Table, origRid); }
		}

		public CorMemberRef(CorModuleDef readerModule, uint rid, GenericParamContext gpContext) {
			this.readerModule = readerModule;
			this.rid = rid;
			this.origRid = rid;
			this.gpContext = gpContext;
			Initialize_NoLock();
		}

		void Initialize_NoLock() {
			this.module = readerModule;
			InitName_NoLock();
			InitClass_NoLock();	// Must be called before InitSignature_NoLock()
			InitSignature_NoLock();
		}

		protected override void InitializeCustomAttributes() {
			readerModule.InitCustomAttributes(this, ref customAttributes, gpContext);
		}

		void InitName_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetMemberRefName(mdi, token) ?? string.Empty);
		}

		void InitClass_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			uint clsToken = MDAPI.GetMemberRefClassToken(mdi, token);
			this.Class = readerModule.ResolveToken(clsToken, gpContext) as IMemberRefParent;
		}

		void InitSignature_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var sig = MDAPI.GetMemberRefSignatureBlob(mdi, token);
			Signature = readerModule.ReadSignature(sig, GetSignatureGenericParamContext(gpContext, this.@class));
		}
	}
}
