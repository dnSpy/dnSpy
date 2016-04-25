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
	sealed class CorTypeRef : TypeRef, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public MDToken OriginalToken {
			get { return new MDToken(MDToken.Table, origRid); }
		}

		public CorTypeRef(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			this.origRid = rid;
			Initialize_NoLock();
		}

		void Initialize_NoLock() {
			this.module = readerModule;
			InitNameAndScope_NoLock();
		}

		void InitNameAndScope_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			InitializeName(MDAPI.GetUtf8Name(mdi, token), MDAPI.GetTypeRefName(mdi, token));
		}

		void InitializeName(UTF8String utf8Name, string fullName) {
			UTF8String ns, name;
			Utils.SplitNameAndNamespace(utf8Name, fullName, out ns, out name);
			this.Namespace = ns;
			this.Name = name;
		}

		protected override void InitializeCustomAttributes() {
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext());
		}

		protected override IResolutionScope GetResolutionScope_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;
			uint tkResolutionScope = MDAPI.GetTypeRefResolutionScope(mdi, token);
			return readerModule.ResolveResolutionScope(tkResolutionScope);
		}
	}
}
