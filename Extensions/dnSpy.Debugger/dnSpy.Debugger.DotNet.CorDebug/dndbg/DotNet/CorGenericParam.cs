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

using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.Utils;

namespace dndbg.DotNet {
	sealed class CorGenericParam : GenericParam, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		readonly new ICorTypeOrMethodDef owner;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);
		public new ICorTypeOrMethodDef Owner => owner;

		public CorGenericParam(CorModuleDef readerModule, uint rid, ICorTypeOrMethodDef owner) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
			this.owner = owner;
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

				base.owner = owner;
				InitGenericParamProps_NoLock();
				InitGenericParamConstraints_NoLock();
				InitCustomAttributes_NoLock();
			}
			finally {
				initCounter--;
			}
		}
		int initCounter;

		static GenericParamContext GetGenericParamContext(ITypeOrMethodDef tmOwner) {
			if (tmOwner is MethodDef md)
				return GenericParamContext.Create(md);
			return new GenericParamContext(tmOwner as TypeDef);
		}

		void InitCustomAttributes_NoLock() => customAttributes = null;
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, GetGenericParamContext(owner));

		void InitGenericParamProps_NoLock() {
			var mdi2 = readerModule.MetaDataImport2;
			uint token = OriginalToken.Raw;

			Name = MDAPI.GetGenericParamName(mdi2, token) ?? string.Empty;
			Kind = null;
			MDAPI.GetGenericParamNumAndAttrs(mdi2, token, out number, out var attrs);
			attributes = (int)attrs;
		}

		public void UpdateGenericParamConstraints() {
			lock (lockObj) {
				if (!owner.CompletelyLoaded)
					InitGenericParamConstraints_NoLock();
			}
		}

		void InitGenericParamConstraints_NoLock() {
			var mdi2 = readerModule.MetaDataImport2;
			uint token = OriginalToken.Raw;

			// Don't clear the list to prevent recursive init
			//genericParamConstraints?.Clear();

			var itemTokens = MDAPI.GetGenericParamConstraintTokens(mdi2, token);
			genericParamConstraints = new LazyList<GenericParamConstraint>(itemTokens.Length, this, itemTokens, (itemTokens2, index) => readerModule.ResolveGenericParamConstraintDontCache(itemTokens[index], GetGenericParamContext(owner)));
		}
	}
}
