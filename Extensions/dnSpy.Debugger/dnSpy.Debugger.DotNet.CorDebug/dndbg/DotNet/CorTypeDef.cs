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

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.Utils;

namespace dndbg.DotNet {
	sealed class CorTypeDef : TypeDef, ICorHasCustomAttribute, ICorHasDeclSecurity, ICorTypeOrMethodDef {
		readonly CorModuleDef readerModule;
		readonly uint origRid;

		public bool CompletelyLoaded => completelyLoaded;
		bool completelyLoaded;

		public MDToken OriginalToken => new MDToken(MDToken.Table, origRid);

		public CorTypeDef(CorModuleDef readerModule, uint rid) {
			this.readerModule = readerModule;
			this.rid = rid;
			origRid = rid;
		}

		readonly object lockObj = new object();

		public void Initialize(bool calledFromLoadClass) {
			Debug.Assert(!CompletelyLoaded);
			try {
				if (Interlocked.Increment(ref initCounter) != 1) {
					Debug.Fail("Initialize() called recursively");
					return;
				}

				methodRidToOverrides = null;
				InitTypeDefProps_NoLock();
				InitClassLayout_NoLock();
				InitFields_NoLock();
				InitMethods_NoLock();
				InitCustomAttributes_NoLock();
				InitDeclSecurities_NoLock();
				InitGenericParams_NoLock();
				InitProperties_NoLock();
				InitEvents_NoLock();
				InitInterfaceImpls_NoLock();
				ResetBaseType();
				completelyLoaded |= calledFromLoadClass;
			}
			finally {
				Interlocked.Decrement(ref initCounter);
			}
		}
		int initCounter = 0;

		unsafe void InitTypeDefProps_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			InitializeName(MDAPI.GetUtf8Name(mdi, token), MDAPI.GetTypeDefName(mdi, token));
			Attributes = MDAPI.GetTypeDefAttributes(mdi, token) ?? 0;
		}

		void InitializeName(UTF8String? utf8Name, string? fullName) {
			Utils.SplitNameAndNamespace(utf8Name, fullName, out var ns, out var name);
			Namespace = ns;
			Name = name;
		}

		unsafe void InitClassLayout_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			bool b = MDAPI.GetClassLayout(mdi, token, out ushort packingSize, out uint classSize);
			if (!b)
				ClassLayout = null;
			else
				ClassLayout = readerModule.UpdateRowId(new ClassLayoutUser(packingSize, classSize));
		}

		unsafe Dictionary<uint, uint> CalculateFieldOffsets() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;
			var fieldRidToFieldOffset = new Dictionary<uint, uint>();

			var fieldOffsets = MDAPI.GetFieldOffsets(mdi, token);
			fieldRidToFieldOffset.Clear();
			if (fieldOffsets is not null) {
				foreach (var fo in fieldOffsets) {
					if (fo.Offset != uint.MaxValue)
						fieldRidToFieldOffset[fo.FieldToken & 0x00FFFFFF] = fo.Offset;
				}
			}
			return fieldRidToFieldOffset;
		}

		internal uint? GetFieldOffset(CorFieldDef cfd) {
			Debug2.Assert(fieldRidToFieldOffset is not null);
			if (fieldRidToFieldOffset is null)
				return null;
			if (fieldRidToFieldOffset.TryGetValue(cfd.OriginalToken.Rid, out uint fieldOffset))
				return fieldOffset;
			return null;
		}
		Dictionary<uint, uint>? fieldRidToFieldOffset;

		public void UpdateFields() {
			lock (lockObj) {
				if (!CompletelyLoaded)
					InitFields_NoLock();
			}
		}

		void InitFields_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			fieldRidToFieldOffset = CalculateFieldOffsets();

			fields?.Clear();

			var itemTokens = MDAPI.GetFieldTokens(mdi, token);
			var newItems = new MemberInfo<CorFieldDef>[itemTokens.Length];
			for (int i = 0; i < itemTokens.Length; i++) {
				uint itemRid = itemTokens[i] & 0x00FFFFFF;
				newItems[i] = readerModule.Register(new CorFieldDef(readerModule, itemRid, this), cmd => cmd.Initialize());
			}

			fields = new LazyList<FieldDef, MemberInfo<CorFieldDef>[]>(itemTokens.Length, this, newItems, (newItems2, index) => newItems2[index].Item);
		}

		public void UpdateMethods() {
			lock (lockObj) {
				if (!CompletelyLoaded)
					InitMethods_NoLock();
			}
		}

		void InitMethods_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			methods?.Clear();

			var itemTokens = MDAPI.GetMethodTokens(mdi, token);
			var newItems = new MemberInfo<CorMethodDef>[itemTokens.Length];
			for (int i = 0; i < itemTokens.Length; i++) {
				uint itemRid = itemTokens[i] & 0x00FFFFFF;
				newItems[i] = readerModule.Register(new CorMethodDef(readerModule, itemRid, this), cmd => cmd.Initialize());
			}

			methods = new LazyList<MethodDef, MemberInfo<CorMethodDef>[]>(itemTokens.Length, this, newItems, (newItems2, index) => newItems2[index].Item);
		}

		public void UpdateGenericParams() {
			lock (lockObj) {
				if (!CompletelyLoaded)
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

			genericParameters = new LazyList<GenericParam, MemberInfo<CorGenericParam>[]>(itemTokens.Length, this, newItems, (newItems2, index) => newItems2[index].Item);
		}

		public void UpdateProperties() {
			lock (lockObj) {
				if (!CompletelyLoaded)
					InitProperties_NoLock();
			}
		}

		void InitProperties_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			properties?.Clear();

			var itemTokens = MDAPI.GetPropertyTokens(mdi, token);
			var newItems = new MemberInfo<CorPropertyDef>[itemTokens.Length];
			for (int i = 0; i < itemTokens.Length; i++) {
				uint itemRid = itemTokens[i] & 0x00FFFFFF;
				newItems[i] = readerModule.Register(new CorPropertyDef(readerModule, itemRid, this), cmd => cmd.Initialize());
			}

			properties = new LazyList<PropertyDef, MemberInfo<CorPropertyDef>[]>(itemTokens.Length, this, newItems, (newItems2, index) => newItems2[index].Item);
		}

		public void UpdateEvents() {
			lock (lockObj) {
				if (!CompletelyLoaded)
					InitEvents_NoLock();
			}
		}

		void InitEvents_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			events?.Clear();

			var itemTokens = MDAPI.GetEventTokens(mdi, token);
			var newItems = new MemberInfo<CorEventDef>[itemTokens.Length];
			for (int i = 0; i < itemTokens.Length; i++) {
				uint itemRid = itemTokens[i] & 0x00FFFFFF;
				newItems[i] = readerModule.Register(new CorEventDef(readerModule, itemRid, this), cmd => cmd.Initialize());
			}

			events = new LazyList<EventDef, MemberInfo<CorEventDef>[]>(itemTokens.Length, this, newItems, (newItems2, index) => newItems2[index].Item);
		}

		void InitInterfaceImpls_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			interfaces?.Clear();

			var itemTokens = MDAPI.GetInterfaceImplTokens(mdi, token);
			interfaces = new LazyList<InterfaceImpl?, uint[]>(itemTokens.Length, itemTokens, (itemTokens2, index) => readerModule.ResolveInterfaceImpl(itemTokens2[index], new GenericParamContext(this)));
		}

		void InitCustomAttributes_NoLock() => customAttributes = null;
		protected override void InitializeCustomAttributes() =>
			readerModule.InitCustomAttributes(this, ref customAttributes, new GenericParamContext(this));
		void InitDeclSecurities_NoLock() => declSecurities = null;
		protected override void InitializeDeclSecurities() =>
			readerModule.InitDeclSecurities(this, ref declSecurities);

		unsafe protected override ITypeDefOrRef? GetBaseType_NoLock() {
			var mdi = readerModule.MetaDataImport;
			uint token = OriginalToken.Raw;

			uint tkExtends = MDAPI.GetTypeDefExtends(mdi, token);
			return readerModule.ResolveTypeDefOrRefInternal(tkExtends, GenericParamContext.Create(this));
		}

		protected override TypeDef? GetDeclaringType2_NoLock() => readerModule.GetEnclosingTypeDef(this);

		TypeDef? DeclaringType2_NoLock {
			get {
				if (!declaringType2_isInitialized) {
					declaringType2 = GetDeclaringType2_NoLock();
					declaringType2_isInitialized = true;
				}
				return declaringType2;
			}
		}

		protected override ModuleDef? GetModule2_NoLock() => DeclaringType2_NoLock is not null ? null : readerModule;

		internal void PrepareAutoInsert() {
			DeclaringType = null;
			DeclaringType2 = null;
			module2 = null;
			module2_isInitialized = true;
		}

		internal IList<MethodOverride> GetMethodOverrides(CorMethodDef cmd) {
			var gpContext = new GenericParamContext(this, cmd);

			var dict = methodRidToOverrides;
			if (dict is null)
				dict = InitializeMethodOverrides();

			if (dict.TryGetValue(cmd.OriginalToken.Rid, out var overrides)) {
				var newList = new List<MethodOverride>(overrides.Count);

				for (int i = 0; i < overrides.Count; i++) {
					var ovr = overrides[i];
					var newMethodBody = readerModule.ResolveToken(ovr.MethodBodyToken, gpContext) as IMethodDefOrRef;
					var newMethodDeclaration = readerModule.ResolveToken(ovr.MethodDeclarationToken, gpContext) as IMethodDefOrRef;
					Debug2.Assert(newMethodBody is not null && newMethodDeclaration is not null);
					if (newMethodBody is null || newMethodDeclaration is null)
						continue;
					newList.Add(new MethodOverride(newMethodBody, newMethodDeclaration));
				}
				return newList;
			}
			return new List<MethodOverride>();
		}

		struct MethodOverrideTokens {
			public readonly uint MethodBodyToken;
			public readonly uint MethodDeclarationToken;

			public MethodOverrideTokens(uint methodBodyToken, uint methodDeclarationToken) {
				MethodBodyToken = methodBodyToken;
				MethodDeclarationToken = methodDeclarationToken;
			}
		}

		Dictionary<uint, IList<MethodOverrideTokens>> InitializeMethodOverrides() {
			var newMethodRidToOverrides = new Dictionary<uint, IList<MethodOverrideTokens>>();

			var infos = MDAPI.GetMethodOverrides(readerModule.MetaDataImport, OriginalToken.Raw);
			for (uint i = 0; i < infos.Length; i++) {
				var info = infos[i];
				var methodBody = readerModule.ResolveToken(info.BodyToken) as IMethodDefOrRef;
				var methodDecl = readerModule.ResolveToken(info.DeclToken) as IMethodDefOrRef;
				if (methodBody is null || methodDecl is null)
					continue;

				var method = FindMethodImplMethod(methodBody);
				if (method is null || method.DeclaringType != this)
					continue;

				var cmd = method as CorMethodDef;
				uint rid = cmd is not null ? cmd.OriginalToken.Rid : method.Rid;
				if (!newMethodRidToOverrides.TryGetValue(rid, out var overrides))
					newMethodRidToOverrides[rid] = overrides = new List<MethodOverrideTokens>();
				overrides.Add(new MethodOverrideTokens(methodBody.MDToken.Raw, methodDecl.MDToken.Raw));
			}
			return methodRidToOverrides = newMethodRidToOverrides;
		}
		Dictionary<uint, IList<MethodOverrideTokens>>? methodRidToOverrides;

		internal void InitializeProperty(CorPropertyDef prop, out IList<MethodDef> getMethods, out IList<MethodDef> setMethods, out IList<MethodDef> otherMethods) {
			getMethods = new List<MethodDef>();
			setMethods = new List<MethodDef>();
			otherMethods = new List<MethodDef>();
			if (prop is null)
				return;

			var mdi = readerModule.MetaDataImport;
			uint token = prop.OriginalToken.Raw;
			MDAPI.GetPropertyGetterSetter(mdi, token, out uint getToken, out uint setToken);
			var otherTokens = MDAPI.GetPropertyOtherMethodTokens(mdi, token);

			var dict = CreateMethodDict();
			Add(dict, getMethods, getToken);
			Add(dict, setMethods, setToken);
			foreach (uint otherToken in otherTokens)
				Add(dict, otherMethods, otherToken);
		}

		Dictionary<uint, CorMethodDef> CreateMethodDict() {
			var dict = new Dictionary<uint, CorMethodDef>(Methods.Count);
			foreach (var m in Methods) {
				if (m is CorMethodDef cmd)
					dict[cmd.OriginalToken.Rid] = cmd;
			}
			return dict;
		}

		internal void InitializeEvent(CorEventDef evt, out MethodDef? addMethod, out MethodDef? invokeMethod, out MethodDef? removeMethod, out IList<MethodDef> otherMethods) {
			addMethod = null;
			invokeMethod = null;
			removeMethod = null;
			otherMethods = new List<MethodDef>();

			var mdi = readerModule.MetaDataImport;
			uint token = evt.OriginalToken.Raw;
			MDAPI.GetEventAddRemoveFireTokens(mdi, token, out uint addToken, out uint removeToken, out uint fireToken);
			var otherTokens = MDAPI.GetEventOtherMethodTokens(mdi, token);

			var dict = CreateMethodDict();
			addMethod = Lookup(dict, addToken);
			invokeMethod = Lookup(dict, fireToken);
			removeMethod = Lookup(dict, removeToken);
			foreach (uint otherToken in otherTokens)
				Add(dict, otherMethods, otherToken);
		}

		CorMethodDef? Lookup(Dictionary<uint, CorMethodDef> dict, uint token) {
			var mdToken = new MDToken(token);
			if (mdToken.Table != Table.Method)
				return null;
			dict.TryGetValue(mdToken.Rid, out var cmd);
			return cmd;
		}

		void Add(Dictionary<uint, CorMethodDef> dict, IList<MethodDef> methods, uint token) {
			var cmd = Lookup(dict, token);
			if (cmd is null || methods.Contains(cmd))
				return;
			methods.Add(cmd);
		}

		protected override void InitializeNestedTypes() {
			var list = readerModule.GetTypeDefNestedClassRids(this);
			var tmp = new LazyList<TypeDef?, uint[]>(list.Length, this, list, (list2, index) => readerModule.ResolveTypeDef(list2[index]));
			Interlocked.CompareExchange(ref nestedTypes, tmp, null!);
		}
	}
}
