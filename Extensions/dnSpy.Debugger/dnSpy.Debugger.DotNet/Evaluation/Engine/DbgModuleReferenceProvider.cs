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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Properties;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgModuleReferenceProvider {
		/// <summary>
		/// Gets the module references or an empty array if <paramref name="frame"/> is an unsupported frame with no .NET module
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		public abstract GetModuleReferencesResult GetModuleReferences(DbgRuntime runtime, DbgStackFrame frame);

		public abstract GetModuleReferencesResult GetModuleReferences(DbgRuntime runtime, DmdModule module);
	}

	struct GetModuleReferencesResult {
		public DbgModuleReference[] ModuleReferences { get; }
		public string ErrorMessage { get; }

		public GetModuleReferencesResult(string errorMessage) {
			ModuleReferences = null;
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}

		public GetModuleReferencesResult(DbgModuleReference[] moduleReferences) {
			ModuleReferences = moduleReferences ?? throw new ArgumentNullException(nameof(moduleReferences));
			ErrorMessage = null;
		}
	}

	[Export(typeof(DbgModuleReferenceProvider))]
	sealed class DbgModuleReferenceProviderImpl : DbgModuleReferenceProvider {
		readonly DbgRawMetadataService dbgRawMetadataService;

		[ImportingConstructor]
		DbgModuleReferenceProviderImpl(DbgRawMetadataService dbgRawMetadataService) => this.dbgRawMetadataService = dbgRawMetadataService;

		sealed class RuntimeState : IDisposable {
			public struct Key : IEquatable<Key> {
				public readonly bool IsFileLayout;
				public readonly ulong Address;
				public readonly uint Size;
				public Key(bool isFileLayout, ulong address, uint size) {
					IsFileLayout = isFileLayout;
					Address = address;
					Size = size;
				}
				public bool Equals(Key other) => Address == other.Address && Size == other.Size && IsFileLayout == other.IsFileLayout;
				public override bool Equals(object obj) => obj is Key other && Equals(other);
				public override int GetHashCode() => Address.GetHashCode();
			}
			public readonly Dictionary<Key, DbgModuleReferenceImpl> ModuleReferences = new Dictionary<Key, DbgModuleReferenceImpl>();
			public readonly List<DbgModuleReferenceImpl> OtherModuleReferences = new List<DbgModuleReferenceImpl>();

			readonly DbgRuntime runtime;

			RuntimeState(DbgRuntime runtime) => this.runtime = runtime;

			public static RuntimeState GetRuntimeState(DbgRuntime runtime) {
				if (runtime.TryGetData(out RuntimeState state))
					return state;
				return CreateRuntimeState(runtime);
			}

			static RuntimeState CreateRuntimeState(DbgRuntime runtime) => runtime.GetOrCreateData(() => new RuntimeState(runtime));

			public void Dispose() {
				var refs = ModuleReferences.Values.ToArray();
				ModuleReferences.Clear();
				runtime.Process.DbgManager.Close(refs);
				refs = OtherModuleReferences.ToArray();
				OtherModuleReferences.Clear();
				runtime.Process.DbgManager.Close(refs);
			}
		}

		sealed class ModuleReferencesState {
			public DbgModuleReference[] ModuleReferences;

			/// <summary>
			/// Referenced assemblies that have been loaded, including all their current modules. If an
			/// assembly gets unloaded or if it loads/unloads a module, we need to invalidate the cached data.
			/// </summary>
			public readonly List<AssemblyInfo> AssemblyInfos = new List<AssemblyInfo>();

			/// <summary>
			/// Referenced assemblies that haven't been loaded yet. If one of these get loaded,
			/// we need to invalidate the cached data.
			/// </summary>
			public readonly HashSet<IDmdAssemblyName> NonLoadedAssemblies = new HashSet<IDmdAssemblyName>(DmdMemberInfoEqualityComparer.DefaultMember);
		}

		struct AssemblyInfo {
			public DmdAssembly Assembly;
			public ModuleInfo[] Modules;
			public AssemblyInfo(DmdAssembly assembly) {
				Assembly = assembly;
				var modules = assembly.GetModules();
				var infos = new ModuleInfo[modules.Length];
				for (int i = 0; i < infos.Length; i++)
					infos[i] = new ModuleInfo(modules[i]);
				Modules = infos;
			}
		}

		struct ModuleInfo {
			public DmdModule Module;
			public DbgModule DebuggerModuleOrNull;
			public int DynamicModuleVersion;
			public int DebuggerModuleVersion;
			public ModuleInfo(DmdModule module) {
				Module = module;
				DebuggerModuleOrNull = module.GetDebuggerModule();
				DynamicModuleVersion = module.DynamicModuleVersion;
				DebuggerModuleVersion = DebuggerModuleOrNull?.RefreshedVersion ?? -1;
			}
		}

		sealed class DbgModuleReferenceImpl : DbgModuleReference {
			public override IntPtr MetadataAddress => dbgRawMetadata.MetadataAddress;
			public override uint MetadataSize => (uint)dbgRawMetadata.MetadataSize;
			public override Guid ModuleVersionId { get; }
			public override Guid GenerationId => generationId;

			readonly DbgRawMetadata dbgRawMetadata;
			Guid generationId;
			int refreshedVersion;
#if DEBUG
			readonly string toStringValue;
#endif

			public DbgModuleReferenceImpl(DbgRawMetadata dbgRawMetadata, Guid moduleVersionId, DmdModule moduleForToString, int refreshedVersion) {
				this.dbgRawMetadata = dbgRawMetadata;
				ModuleVersionId = moduleVersionId;
				this.refreshedVersion = refreshedVersion;
#if DEBUG
				toStringValue = $"{moduleForToString.Assembly.FullName} [{moduleForToString.FullyQualifiedName}]";
#endif
			}

			internal void Update(int version) {
				if (refreshedVersion != version) {
					refreshedVersion = version;
					generationId = Guid.NewGuid();
					dbgRawMetadata.UpdateMemory();
				}
			}

			protected override void CloseCore(DbgDispatcher dispatcher) => dbgRawMetadata.Release();
#if DEBUG
			public override string ToString() => toStringValue;
#endif
		}

		public override GetModuleReferencesResult GetModuleReferences(DbgRuntime runtime, DbgStackFrame frame) {
			var reflectionModule = frame.Module?.GetReflectionModule();
			if (reflectionModule == null)
				return new GetModuleReferencesResult(dnSpy_Debugger_DotNet_Resources.CantEvaluateWhenCurrentFrameIsNative);
			return GetModuleReferences(runtime, reflectionModule);
		}

		public override GetModuleReferencesResult GetModuleReferences(DbgRuntime runtime, DmdModule module) {
			// Not thread safe since all callers should call it on the correct engine thread
			runtime.GetDotNetRuntime().Dispatcher.VerifyAccess();

			if (module.TryGetData(out ModuleReferencesState state)) {
				if (CanReuse(module.AppDomain, state))
					return new GetModuleReferencesResult(state.ModuleReferences);
			}
			else
				state = module.GetOrCreateData<ModuleReferencesState>();

			InitializeState(runtime, module.Assembly, state);
			return new GetModuleReferencesResult(state.ModuleReferences);
		}

		sealed class IntrinsicsAssemblyState {
			public readonly DbgModuleReference ModuleReference;
			public readonly DmdAssembly Assembly;
			public IntrinsicsAssemblyState(DbgModuleReference moduleReference, DmdAssembly assembly) {
				ModuleReference = moduleReference ?? throw new ArgumentNullException(nameof(moduleReference));
				Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			}
		}

		IntrinsicsAssemblyState GetIntrinsicsAssemblyState(DbgRuntime runtime, DmdAppDomain appDomain) {
			if (appDomain.TryGetData(out IntrinsicsAssemblyState state))
				return state;
			return GetOrCreateIntrinsicsAssemblyState(runtime, appDomain);
		}

		IntrinsicsAssemblyState GetOrCreateIntrinsicsAssemblyState(DbgRuntime runtime, DmdAppDomain appDomain) {
			var info = new IntrinsicsAssemblyBuilder(appDomain.CorLib.GetName().FullName).Create();
			const bool isFileLayout = true;
			const bool isInMemory = false;
			const bool isDynamic = false;
			var assembly = appDomain.CreateSyntheticAssembly(() => new DmdLazyMetadataBytesArray(info.assemblyBytes, isFileLayout), isInMemory, isDynamic, DmdModule.GetFullyQualifiedName(isInMemory, isDynamic, null), string.Empty, info.assemblySimpleName);
			var rawMD = dbgRawMetadataService.Create(runtime, isFileLayout, info.assemblyBytes);
			var modRef = new DbgModuleReferenceImpl(rawMD, assembly.ManifestModule.ModuleVersionId, assembly.ManifestModule, -1);
			RuntimeState.GetRuntimeState(runtime).OtherModuleReferences.Add(modRef);
			var state = new IntrinsicsAssemblyState(modRef, assembly);
			return appDomain.GetOrCreateData(() => {
				appDomain.Add(state.Assembly);
				return state;
			});
		}

		void InitializeState(DbgRuntime runtime, DmdAssembly assembly, ModuleReferencesState state) {
			state.AssemblyInfos.Clear();
			state.NonLoadedAssemblies.Clear();
			var appDomain = assembly.AppDomain;

			var intrinsicsState = GetIntrinsicsAssemblyState(runtime, appDomain);

			var hash = new HashSet<DmdAssembly>();
			var stack = new List<AssemblyInfo>();
			stack.Add(new AssemblyInfo(assembly));
			stack.Add(new AssemblyInfo(intrinsicsState.Assembly));
			stack.Add(new AssemblyInfo(appDomain.CorLib));

			while (stack.Count > 0) {
				var info = stack[stack.Count - 1];
				stack.RemoveAt(stack.Count - 1);
				if (!hash.Add(info.Assembly))
					continue;
				state.AssemblyInfos.Add(info);
				foreach (var modInfo in info.Modules) {
					foreach (var asmRef in modInfo.Module.GetReferencedAssemblies()) {
						var asm = appDomain.GetAssembly(asmRef);
						if (asm != null) {
							if (!hash.Contains(asm))
								stack.Add(new AssemblyInfo(asm));
						}
						else
							state.NonLoadedAssemblies.Add(asmRef);
					}
				}
			}

			var rtState = RuntimeState.GetRuntimeState(runtime);
			var modRefs = new List<DbgModuleReference>();
			foreach (var asmInfo in state.AssemblyInfos) {
				foreach (var modInfo in asmInfo.Modules) {
					if (modInfo.Module.IsDynamic) {
						//TODO: Re-generate the dynamic module MD if it has changed since last time. Store this info in the DmdModule/DbgModule
						Debug.Fail("NYI");
					}
					else {
						if (modInfo.Module.Assembly == intrinsicsState.Assembly) {
							modRefs.Add(intrinsicsState.ModuleReference);
							continue;
						}
						var module = modInfo.Module.GetDebuggerModule();
						Debug.Assert(module != null);
						if (module == null)
							throw new InvalidOperationException();
						Debug.Assert(module.HasAddress);
						if (!module.HasAddress)
							throw new InvalidOperationException();
						var key = new RuntimeState.Key(module.ImageLayout == DbgImageLayout.File, module.Address, module.Size);
						if (!rtState.ModuleReferences.TryGetValue(key, out var modRef)) {
							var rawMd = dbgRawMetadataService.Create(runtime, key.IsFileLayout, key.Address, (int)key.Size);
							if (rawMd.MetadataAddress == IntPtr.Zero) {
								rawMd.Release();
								continue;
							}
							else {
								try {
									modRef = new DbgModuleReferenceImpl(rawMd, modInfo.Module.ModuleVersionId, modInfo.Module, module.RefreshedVersion);
									rtState.ModuleReferences.Add(key, modRef);
								}
								catch {
									rawMd.Release();
									throw;
								}
							}
						}
						modRef.Update(module.RefreshedVersion);
						modRefs.Add(modRef);
					}
				}
			}

			state.ModuleReferences = modRefs.ToArray();
		}

		bool CanReuse(DmdAppDomain appDomain, ModuleReferencesState state) {
			foreach (var asmRef in state.NonLoadedAssemblies) {
				if (appDomain.GetAssembly(asmRef) != null)
					return false;
			}

			foreach (var info in state.AssemblyInfos) {
				if (!info.Assembly.IsLoaded)
					return false;

				var modules = info.Assembly.GetModules();
				if (!Equals(modules, info.Modules))
					return false;
			}

			return true;
		}

		static bool Equals(DmdModule[] a, ModuleInfo[] b) {
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				var info = b[i];
				var am = a[i];
				if (am != info.Module || a[i].DynamicModuleVersion != info.DynamicModuleVersion)
					return false;
				var dm = info.DebuggerModuleOrNull ?? am.GetDebuggerModule();
				if (dm != null && dm.RefreshedVersion != info.DebuggerModuleVersion)
					return false;
			}
			return true;
		}
	}
}
