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

using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;

namespace dndbg.DotNet {
	sealed class CorParamDef : ParamDef, ICorHasCustomAttribute, ICorHasFieldMarshal {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		readonly CorMethodDef ownerMethod;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);
		public CorMethodDef OwnerMethod => ownerMethod;

		public CorParamDef(CorModuleDef readerModule, uint rid, CorMethodDef ownerMethod) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			this.ownerMethod = ownerMethod;
		}

		public bool MustInitialize {
			get { lock (lockObj) return mustInitialize; }
			set { lock (lockObj) mustInitialize = value; }
		}
		bool mustInitialize;
		readonly object lockObj = new object();

		public void Initialize() {
			lock (lockObj) {
				if (!mustInitialize)
					return;
				Initialize_NoLock();
				mustInitialize = false;
			}
		}

		void Initialize_NoLock() {
			try {
				if (initCounter++ != 0) {
					Debug.Fail("Initialize() called recursively");
					return;
				}

				declaringMethod = ownerMethod;
				InitNameAndAttributes_NoLock();
				ResetConstant();
				ResetMarshalType();
				InitCustomAttributes_NoLock();
			}
			finally {
				initCounter--;
			}
		}
		int initCounter;

		void InitNameAndAttributes_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetParamName(mdi, token) ?? string.Empty);
			MDAPI.GetParamSeqAndAttrs(mdi, token, out uint seq, out var attrs);
			Sequence = (ushort)seq;
			Attributes = attrs;
		}

		void InitCustomAttributes_NoLock() => customAttributes = null;
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext(ownerMethod.OwnerType, ownerMethod));

		protected override Constant? GetConstant_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var c = MDAPI.GetParamConstant(mdi, token, out var etype);
			if (etype == CorElementType.End)
				return null;
			return readerModule.UpdateRowId(new ConstantUser(c, (ElementType)etype));
		}

		protected override MarshalType? GetMarshalType_NoLock() =>
			readerModule.ReadMarshalType(this, new GenericParamContext(ownerMethod.OwnerType, ownerMethod));
	}
}
