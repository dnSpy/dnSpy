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

using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dndbg.DotNet {
	sealed class CorEventDef : EventDef, ICorHasCustomAttribute {
		readonly CorModuleDef readerModule;
		readonly uint origRid;
		readonly CorTypeDef ownerType;

		public MDToken OriginalToken {
			get { return new MDToken(MDToken.Table, origRid); }
		}

		public CorTypeDef OwnerType {
			get { return ownerType; }
		}

		public CorEventDef(CorModuleDef readerModule, uint rid, CorTypeDef ownerType) {
			this.readerModule = readerModule;
			this.rid = rid;
			this.origRid = rid;
			this.ownerType = ownerType;
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

				declaringType2 = ownerType;
				InitNameAndAttrs_NoLock();
				ResetMethods();
				InitCustomAttributes_NoLock();
			}
			finally {
				initCounter--;
			}
		}
		int initCounter;

		void InitCustomAttributes_NoLock() {
			customAttributes = null;
		}

		protected override void InitializeCustomAttributes() {
			readerModule.InitCustomAttributes(this, ref customAttributes, GenericParamContext.Create(ownerType));
		}

		void InitNameAndAttrs_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			Name = Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, OriginalToken.Raw), MDAPI.GetEventName(mdi, token) ?? string.Empty);
			Attributes = MDAPI.GetEventAttributes(mdi, token);
			uint eventType = MDAPI.GetEventTypeToken(mdi, token);
			EventType = readerModule.ResolveTypeDefOrRefInternal(eventType, new GenericParamContext(ownerType));
		}

		protected override void InitializeEventMethods_NoLock() {
			ThreadSafe.IList<MethodDef> newOtherMethods;
			ownerType.InitializeEvent(this, out addMethod, out invokeMethod, out removeMethod, out newOtherMethods);
			otherMethods = newOtherMethods;
		}
	}
}
