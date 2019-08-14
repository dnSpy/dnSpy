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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dndbg.DotNet;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.Impl;

namespace dnSpy.Debugger.DotNet.CorDebug.Metadata {
	[Export(typeof(DbgDynamicModuleProviderFactory))]
	sealed class DbgDynamicModuleProviderFactoryImpl : DbgDynamicModuleProviderFactory {
		public override DbgDynamicModuleProvider? Create(DbgRuntime runtime) {
			var engine = DbgEngineImpl.TryGetEngine(runtime);
			if (!(engine is null))
				return runtime.GetOrCreateData(() => new DbgDynamicModuleProviderImpl(engine));

			return null;
		}
	}

	sealed class DbgDynamicModuleProviderImpl : DbgDynamicModuleProvider {
		public override event EventHandler<ClassLoadedEventArgs>? ClassLoaded;

		readonly DbgEngineImpl engine;

		public DbgDynamicModuleProviderImpl(DbgEngineImpl engine) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			engine.ClassLoaded += DbgEngineImpl_ClassLoaded;
		}

		void DbgEngineImpl_ClassLoaded(object? sender, ClassLoadedEventArgs e) => ClassLoaded?.Invoke(this, e);
		public override void BeginInvoke(Action callback) => engine.CorDebugThread(callback);
		T Invoke<T>(Func<T> callback) => engine.InvokeCorDebugThread(callback);

		sealed class DynamicModuleData {
			public LastValidRids LastValidRids;
			public CorModuleDef? Metadata;
			public ModuleId ModuleId;
		}

		public override ModuleDef? GetDynamicMetadata(DbgModule module, out ModuleId moduleId) {
			var data = module.GetOrCreateData<DynamicModuleData>();
			if (!(data.Metadata is null)) {
				moduleId = data.ModuleId;
				return data.Metadata;
			}
			var info = Invoke(() => {
				if (!(data.Metadata is null))
					return (metadata: data.Metadata, moduleId: data.ModuleId);
				var info2 = engine.GetDynamicMetadata_EngineThread(module);
				if (!(info2.metadata is null)) {
					// DsDotNetDocumentBase sets EnableTypeDefFindCache to true and that property accesses the
					// Types property. It must be initialized in the correct thread.
					_ = info2.metadata.Types;
					info2.metadata.DisableMDAPICalls = true;
				}
				return info2;
			});
			data.ModuleId = info.moduleId;
			data.Metadata = info.metadata;
			moduleId = info.moduleId;
			return data.Metadata;
		}

		CorModuleDef? TryGetDynamicMetadata(DbgModule module) => module.GetOrCreateData<DynamicModuleData>().Metadata;

		public override void LoadEverything(DbgModule[] modules, bool started) {
			engine.VerifyCorDebugThread();
			foreach (var module in modules) {
				var md = TryGetDynamicMetadata(module);
				if (!(md is null))
					md.DisableMDAPICalls = !started;
			}
		}

		public override IEnumerable<uint> GetModifiedTypes(DbgModule module) {
			engine.VerifyCorDebugThread();
			var data = module.GetOrCreateData<DynamicModuleData>();
			var cmod = TryGetDynamicMetadata(module);

			var hash = new HashSet<uint>();
			if (cmod is null)
				return hash;

			var oldLastValid = UpdateLastValidRids(data, cmod);
			var lastValid = data.LastValidRids;
			if (oldLastValid.Equals(lastValid))
				return hash;

			const uint TYPEDEF_TOKEN = 0x02000000;

			// Optimization if we loaded a big file
			if (oldLastValid.TypeDefRid == 0) {
				for (uint rid = 1; rid <= lastValid.TypeDefRid; rid++)
					hash.Add(TYPEDEF_TOKEN + rid);
				return hash;
			}

			var methodRids = new HashSet<uint>();
			var gpRids = new HashSet<uint>();
			for (uint rid = oldLastValid.TypeDefRid + 1; rid <= lastValid.TypeDefRid; rid++)
				hash.Add(TYPEDEF_TOKEN + rid);
			for (uint rid = oldLastValid.FieldRid + 1; rid <= lastValid.FieldRid; rid++) {
				var typeOwner = cmod.GetFieldOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.MethodRid + 1; rid <= lastValid.MethodRid; rid++) {
				methodRids.Add(rid);
				var typeOwner = cmod.GetMethodOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.ParamRid + 1; rid <= lastValid.ParamRid; rid++) {
				var methodOwner = cmod.GetParamOwnerToken(rid);
				if (methodRids.Contains(methodOwner.Rid))
					continue;
				var typeOwner = cmod.GetMethodOwnerToken(methodOwner.Rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.EventRid + 1; rid <= lastValid.EventRid; rid++) {
				var typeOwner = cmod.GetEventOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.PropertyRid + 1; rid <= lastValid.PropertyRid; rid++) {
				var typeOwner = cmod.GetPropertyOwnerToken(rid);
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.GenericParamRid + 1; rid <= lastValid.GenericParamRid; rid++) {
				gpRids.Add(rid);
				var ownerToken = cmod.GetGenericParamOwnerToken(rid);
				MDToken typeOwner;
				if (ownerToken.Table == Table.TypeDef)
					typeOwner = ownerToken;
				else if (ownerToken.Table == Table.Method) {
					if (methodRids.Contains(ownerToken.Rid))
						continue;
					typeOwner = cmod.GetMethodOwnerToken(ownerToken.Rid);
				}
				else
					continue;
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}
			for (uint rid = oldLastValid.GenericParamConstraintRid + 1; rid <= lastValid.GenericParamConstraintRid; rid++) {
				var gpOwner = cmod.GetGenericParamConstraintOwnerToken(rid);
				if (gpRids.Contains(gpOwner.Rid))
					continue;
				var ownerToken = cmod.GetGenericParamOwnerToken(gpOwner.Rid);
				MDToken typeOwner;
				if (ownerToken.Table == Table.TypeDef)
					typeOwner = ownerToken;
				else if (ownerToken.Table == Table.Method) {
					if (methodRids.Contains(ownerToken.Rid))
						continue;
					typeOwner = cmod.GetMethodOwnerToken(ownerToken.Rid);
				}
				else
					continue;
				if (typeOwner.Rid != 0)
					hash.Add(typeOwner.Raw);
			}

			return hash;
		}

		public override void InitializeNonLoadedClasses(DbgModule module, uint[] nonLoadedTokens) {
			engine.VerifyCorDebugThread();
			var cmod = TryGetDynamicMetadata(module);
			Debug2.Assert(!(cmod is null));
			if (cmod is null)
				return;
			foreach (uint token in nonLoadedTokens)
				cmod.ForceInitializeTypeDef(token & 0x00FFFFFF);
		}

		LastValidRids UpdateLastValidRids(DynamicModuleData data, CorModuleDef module) {
			var old = data.LastValidRids;

			// Linear search but shouldn't be a problem except the first time if we load a big file

			for (; ; data.LastValidRids.TypeDefRid++) {
				if (!module.IsValidToken(new MDToken(Table.TypeDef, data.LastValidRids.TypeDefRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.FieldRid++) {
				if (!module.IsValidToken(new MDToken(Table.Field, data.LastValidRids.FieldRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.MethodRid++) {
				if (!module.IsValidToken(new MDToken(Table.Method, data.LastValidRids.MethodRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.ParamRid++) {
				if (!module.IsValidToken(new MDToken(Table.Param, data.LastValidRids.ParamRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.EventRid++) {
				if (!module.IsValidToken(new MDToken(Table.Event, data.LastValidRids.EventRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.PropertyRid++) {
				if (!module.IsValidToken(new MDToken(Table.Property, data.LastValidRids.PropertyRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.GenericParamRid++) {
				if (!module.IsValidToken(new MDToken(Table.GenericParam, data.LastValidRids.GenericParamRid + 1).Raw))
					break;
			}
			for (; ; data.LastValidRids.GenericParamConstraintRid++) {
				if (!module.IsValidToken(new MDToken(Table.GenericParamConstraint, data.LastValidRids.GenericParamConstraintRid + 1).Raw))
					break;
			}

			return old;
		}
	}
}
