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

using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorMethodSpec : MethodSpec, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		/*readonly*/ GenericParamContext gpContext;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorMethodSpec(CorModuleDef readerModule, uint rid, GenericParamContext gpContext) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			this.gpContext = gpContext;
			Initialize_NoLock();
		}

		void Initialize_NoLock() {
			var mdi2 = readerModule.MetaDataImport2;
			uint token = OriginalToken.Raw;

			var instData = MDAPI.GetMethodSpecProps(mdi2, token, out uint method);
			this.method = readerModule.ResolveToken(method, gpContext) as IMethodDefOrRef;
			instantiation = readerModule.ReadSignature(instData, gpContext);
		}

		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, gpContext);
	}
}
