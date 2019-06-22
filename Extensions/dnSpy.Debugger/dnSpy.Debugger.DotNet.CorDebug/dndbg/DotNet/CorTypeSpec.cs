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
	sealed class CorTypeSpec : TypeSpec, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		/*readonly*/ GenericParamContext gpContext;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorTypeSpec(CorModuleDef readerModule, uint rid, GenericParamContext gpContext) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			this.gpContext = gpContext;
		}

		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, gpContext);

		protected override TypeSig? GetTypeSigAndExtraData_NoLock(out byte[]? extraData) {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var sigData = MDAPI.GetTypeSpecSignatureBlob(mdi, token);
			var sig = readerModule.ReadTypeSignature(sigData, gpContext, out extraData);
			if (!(sig is null))
				sig.Rid = origRid;
			return sig;
		}
	}
}
