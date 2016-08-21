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

using System.Diagnostics;
using System.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using dnlib.Utils;

namespace dndbg.DotNet {
	sealed class CorMethodDef : MethodDef, ICorHasCustomAttribute, ICorHasDeclSecurity, ICorTypeOrMethodDef {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		readonly CorTypeDef ownerType;
		uint origRva;
		MethodAttributes origAttributes;
		MethodImplAttributes origImplAttributes;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);
		public CorTypeDef OwnerType => ownerType;
		public bool CompletelyLoaded => OwnerType.CompletelyLoaded;
		protected override bool CanFreeMethodBody => canFreeMethodBody && !readerModule.DisableMDAPICalls;
		bool canFreeMethodBody;

		public CorMethodDef(CorModuleDef readerModule, uint rid, CorTypeDef ownerType) {
			this.readerModule = readerModule;
			this.rid = rid;
			this.origRid = rid;
			this.ownerType = ownerType;
			this.canFreeMethodBody = true;
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

				if (parameterList == null)
					parameterList = new ParameterList(this, ownerType);

				declaringType2 = ownerType;
				InitNameAndAttributes_NoLock();
				InitRVA_NoLock();
				InitCustomAttributes_NoLock();
				InitDeclSecurities_NoLock();
				InitGenericParams_NoLock();
				InitSignature_NoLock();
				Debug.Assert(CanFreeMethodBody, "Can't free method body");
				FreeMethodBody();
				InitParamDefs_NoLock();
				ResetImplMap();
				semAttrs = 0;
				overrides = null;
				this.parameterList.UpdateParameterTypes();
				canFreeMethodBody = !ownerType.CompletelyLoaded;
			}
			finally {
				initCounter--;
			}
		}
		int initCounter;

		void InitNameAndAttributes_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetMethodName(mdi, token) ?? string.Empty);
			MethodAttributes attrs;
			MethodImplAttributes implAttrs;
			MDAPI.GetMethodAttributes(mdi, token, out attrs, out implAttrs);
			Attributes = attrs;
			ImplAttributes = implAttrs;
			origAttributes = attrs;
			origImplAttributes = implAttrs;
		}

		void InitRVA_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			origRva = MDAPI.GetRVA(mdi, token) ?? 0;
			RVA = (RVA)origRva;
		}

		void InitSignature_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var sig = MDAPI.GetMethodSignatureBlob(mdi, token);
			Signature = readerModule.ReadSignature(sig, new GenericParamContext(ownerType, this));
		}

		void InitCustomAttributes_NoLock() => customAttributes = null;
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext(ownerType, this));
		void InitDeclSecurities_NoLock() => declSecurities = null;
		protected override void InitializeDeclSecurities() =>
			readerModule.InitDeclSecurities(this, ref declSecurities);
		protected override MethodBody GetMethodBody_NoLock() =>
			readerModule.ReadMethodBody(this, origRva, origAttributes, origImplAttributes, new GenericParamContext(ownerType, this));

		public void UpdateParams() {
			lock (lockObj) {
				if (!ownerType.CompletelyLoaded)
					InitParamDefs_NoLock();
			}
		}

		void InitParamDefs_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			paramDefs?.Clear();

			var itemTokens = MDAPI.GetParamTokens(mdi, token);
			var newItems = new MemberInfo<CorParamDef>[itemTokens.Length];
			for (int i = 0; i < itemTokens.Length; i++) {
				uint itemRid = itemTokens[i] & 0x00FFFFFF;
				newItems[i] = readerModule.Register(new CorParamDef(readerModule, itemRid, this), cmd => cmd.Initialize());
			}

			paramDefs = new LazyList<ParamDef>(itemTokens.Length, this, itemTokens, (itemTokens2, index) => newItems[index].Item);
		}

		public void UpdateGenericParams() {
			lock (lockObj) {
				if (!ownerType.CompletelyLoaded)
					InitGenericParams_NoLock();
			}
		}

		void InitGenericParams_NoLock() {
			var mdi2 = readerModule.MetaDataImport2;
			uint token = OriginalToken.Raw;

			genericParameters?.Clear();

			var itemTokens = MDAPI.GetGenericParamTokens(mdi2, token);
			var newItems = new MemberInfo<CorGenericParam>[itemTokens.Length];
			for (int i = 0; i < itemTokens.Length; i++) {
				uint itemRid = itemTokens[i] & 0x00FFFFFF;
				newItems[i] = readerModule.Register(new CorGenericParam(readerModule, itemRid, this), cmd => cmd.Initialize());
			}

			genericParameters = new LazyList<GenericParam>(itemTokens.Length, this, itemTokens, (itemTokens2, index) => newItems[index].Item);
		}

		protected override void InitializeOverrides() {
			var tmp = ownerType.GetMethodOverrides(this);
			Interlocked.CompareExchange(ref overrides, tmp, null);
		}

		protected override ImplMap GetImplMap_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			var name = MDAPI.GetPinvokeMapName(mdi, token);
			if (name == null)
				return null;
			PInvokeAttributes attrs;
			uint moduleToken;
			if (!MDAPI.GetPinvokeMapProps(mdi, token, out attrs, out moduleToken))
				return null;
			var mr = readerModule.ResolveToken(moduleToken) as ModuleRef;
			if (mr == null)
				return null;

			return readerModule.UpdateRowId(new ImplMapUser(mr, name, attrs));
		}

		protected override void InitializeSemanticsAttributes() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;
			var tokens = MDAPI.GetMethodSemanticsTokens(mdi, token);
			if (tokens.Length > 0)
				semAttrs = (int)MDAPI.GetMethodSemanticsAttributes(mdi, token, tokens[0]) | SEMATTRS_INITD;
			else
				semAttrs = SEMATTRS_INITD;
		}
	}
}
