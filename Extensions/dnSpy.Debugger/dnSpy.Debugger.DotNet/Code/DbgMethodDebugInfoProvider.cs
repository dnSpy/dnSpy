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
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.Decompiler.Utils;

namespace dnSpy.Debugger.DotNet.Code {
	readonly struct MethodDebugInfoResult {
		public int MethodVersion { get; }
		public DbgMethodDebugInfo DebugInfoOrNull { get; }
		public DbgMethodDebugInfo StateMachineDebugInfoOrNull { get; }
		public MethodDebugInfoResult(int methodVersion, DbgMethodDebugInfo debugInfo, DbgMethodDebugInfo stateMachineDebugInfoOrNull) {
			if (methodVersion < 1)
				throw new ArgumentOutOfRangeException(nameof(methodVersion));
			MethodVersion = methodVersion;
			DebugInfoOrNull = debugInfo;
			StateMachineDebugInfoOrNull = stateMachineDebugInfoOrNull;
		}
	}

	abstract class DbgMethodDebugInfoProvider {
		public abstract MethodDebugInfoResult GetMethodDebugInfo(IDecompiler decompiler, DbgModule module, uint token, CancellationToken cancellationToken);
		public abstract MethodDebugInfoResult GetMethodDebugInfo(DbgRuntime runtime, IDecompiler decompiler, IDbgDotNetCodeLocation location, CancellationToken cancellationToken);
	}

	[Export(typeof(DbgMethodDebugInfoProvider))]
	sealed class DbgMethodDebugInfoProviderImpl : DbgMethodDebugInfoProvider {
		const DbgLoadModuleOptions loadModuleOptions = DbgLoadModuleOptions.AutoLoaded;
		const int MAX_CACHED_DEBUG_INFOS = 5;
		readonly DbgMetadataService dbgMetadataService;

		[ImportingConstructor]
		DbgMethodDebugInfoProviderImpl(DbgMetadataService dbgMetadataService) => this.dbgMetadataService = dbgMetadataService;

		public override MethodDebugInfoResult GetMethodDebugInfo(IDecompiler decompiler, DbgModule module, uint token, CancellationToken cancellationToken) {
			var mdModule = dbgMetadataService.TryGetMetadata(module, loadModuleOptions);
			var key = new MethodDebugInfoResultKey(module, token);
			return GetMethodDebugInfo(module.Runtime, key, decompiler, mdModule, token, cancellationToken);
		}

		public override MethodDebugInfoResult GetMethodDebugInfo(DbgRuntime runtime, IDecompiler decompiler, IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
			ModuleDef mdModule;
			MethodDebugInfoResultKey key;
			if (location.DbgModule is DbgModule dbgModule) {
				key = new MethodDebugInfoResultKey(dbgModule, location.Token);
				mdModule = dbgMetadataService.TryGetMetadata(dbgModule, loadModuleOptions);
			}
			else {
				key = new MethodDebugInfoResultKey(location.Module, location.Token);
				dbgModule = null;
				mdModule = dbgMetadataService.TryGetMetadata(location.Module, loadModuleOptions);
			}
			return GetMethodDebugInfo(runtime, key, decompiler, mdModule, location.Token, cancellationToken);
		}

		readonly struct MethodDebugInfoResultKey {
			readonly uint token;
			readonly DbgModule module;
			readonly ModuleId moduleId;
			readonly int refreshedVersion;

			public MethodDebugInfoResultKey(DbgModule module, uint token) {
				this.token = token;
				moduleId = default;
				this.module = module;
				refreshedVersion = module.RefreshedVersion;
			}

			public MethodDebugInfoResultKey(ModuleId moduleId, uint token) {
				this.token = token;
				this.moduleId = moduleId;
				module = null;
				refreshedVersion = 0;
			}

			public bool Equals(MethodDebugInfoResultKey other) =>
				token == other.token &&
				module == other.module &&
				refreshedVersion == other.refreshedVersion &&
				moduleId == other.moduleId;
		}

		sealed class RuntimeState {
			public readonly object LockObj = new object();
			public readonly List<(MethodDebugInfoResultKey key, MethodDebugInfoResult result)> DebugInfos = new List<(MethodDebugInfoResultKey key, MethodDebugInfoResult result)>(MAX_CACHED_DEBUG_INFOS);
		}

		MethodDebugInfoResult GetMethodDebugInfo(DbgRuntime runtime, in MethodDebugInfoResultKey key, IDecompiler decompiler, ModuleDef mdModule, uint token, CancellationToken cancellationToken) {
			Debug.Assert(mdModule != null);
			if (mdModule == null)
				return default;

			var state = runtime.GetOrCreateData<RuntimeState>();

			var debugInfos = state.DebugInfos;
			lock (state.LockObj) {
				for (int i = debugInfos.Count - 1; i >= 0; i--) {
					var info = debugInfos[i];
					if (info.key.Equals(key)) {
						if ((info.result.DebugInfoOrNull != null && info.result.DebugInfoOrNull.DebugInfoVersion != decompiler.Settings.Version) ||
							(info.result.StateMachineDebugInfoOrNull != null && info.result.StateMachineDebugInfoOrNull.DebugInfoVersion != decompiler.Settings.Version)) {
							debugInfos.RemoveAt(i);
							continue;
						}
						if (i != debugInfos.Count - 1) {
							debugInfos.RemoveAt(i);
							debugInfos.Add(info);
						}
						return info.result;
					}
				}
			}

			var result = GetMethodDebugInfoNonCached(decompiler, mdModule, token, cancellationToken);
			if (result.DebugInfoOrNull == null)
				return default;
			lock (state.LockObj) {
				if (debugInfos.Count == MAX_CACHED_DEBUG_INFOS)
					debugInfos.RemoveAt(0);
				debugInfos.Add((key, result));
			}
			return result;
		}

		MethodDebugInfoResult GetMethodDebugInfoNonCached(IDecompiler decompiler, ModuleDef mdModule, uint token, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			var method = mdModule.ResolveToken(token) as MethodDef;
			// Could be null if it's a dynamic assembly. It will get refreshed later and we'll get called again.
			if (method == null)
				return default;

			if (!StateMachineHelpers.TryGetKickoffMethod(method, out var containingMethod))
				containingMethod = method;

			var decContext = new DecompilationContext {
				CancellationToken = cancellationToken,
				CalculateILSpans = true,
				// This is only needed when decompiling more than one body
				AsyncMethodBodyDecompilation = false,
			};
			var info = TryDecompileAndGetDebugInfo(decompiler, containingMethod, token, decContext, cancellationToken);
			if (info.debugInfo == null && containingMethod != method) {
				// The decompiler can't decompile the iterator / async method, try again,
				// but only decompile the MoveNext method
				info = TryDecompileAndGetDebugInfo(decompiler, method, token, decContext, cancellationToken);
			}
			if (info.debugInfo == null && method.Body == null) {
				var scope = new DbgMethodDebugScope(new DbgILSpan(0, 0), Array.Empty<DbgMethodDebugScope>(), Array.Empty<DbgLocal>(), Array.Empty<DbgImportInfo>());
				info = (new DbgMethodDebugInfo(DbgCompilerKind.Unknown, -1, method, null, Array.Empty<DbgSourceStatement>(), scope, null), null);
			}
			if (info.debugInfo == null)
				return default;

			// We don't support EnC so the version is always 1
			const int methodVersion = 1;
			return new MethodDebugInfoResult(methodVersion, info.debugInfo, info.stateMachineDebugInfoOrNull);
		}

		static class DecompilerOutputImplCache {
			static DecompilerOutputImpl instance;
			public static DecompilerOutputImpl Alloc() => Interlocked.Exchange(ref instance, null) ?? new DecompilerOutputImpl();
			public static void Free(ref DecompilerOutputImpl inst) {
				var tmp = inst;
				inst = null;
				tmp.Clear();
				instance = tmp;
			}
		}

		(DbgMethodDebugInfo debugInfo, DbgMethodDebugInfo stateMachineDebugInfoOrNull) TryDecompileAndGetDebugInfo(IDecompiler decompiler, MethodDef method, uint methodToken, DecompilationContext decContext, CancellationToken cancellationToken) {
			var output = DecompilerOutputImplCache.Alloc();
			output.Initialize(methodToken);
			decompiler.Decompile(method, output, decContext);
			var info = output.TryGetMethodDebugInfo();
			DecompilerOutputImplCache.Free(ref output);
			cancellationToken.ThrowIfCancellationRequested();
			return info;
		}
	}
}
