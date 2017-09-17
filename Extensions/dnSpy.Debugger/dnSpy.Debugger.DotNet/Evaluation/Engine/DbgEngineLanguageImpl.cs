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
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineLanguageImpl : DbgEngineLanguage {
		public override string Name { get; }
		public override string DisplayName { get; }
		public override DbgEngineExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgEngineValueFormatter ValueFormatter { get; }
		public override DbgEngineFormatter Formatter { get; }
		public override DbgEngineLocalsValueNodeProvider LocalsProvider { get; }
		public override DbgEngineValueNodeProvider AutosProvider { get; }
		public override DbgEngineValueNodeProvider ExceptionsProvider { get; }
		public override DbgEngineValueNodeProvider ReturnValuesProvider { get; }
		public override DbgEngineValueNodeProvider TypeVariablesProvider { get; }
		public override DbgEngineValueNodeFactory ValueNodeFactory { get; }

		readonly DbgMetadataService dbgMetadataService;
		readonly IDecompiler decompiler;

		public DbgEngineLanguageImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, string name, string displayName, DbgDotNetExpressionCompiler expressionCompiler, DbgMetadataService dbgMetadataService, IDecompiler decompiler, DbgDotNetFormatter formatter, DbgDotNetEngineValueNodeFactory valueNodeFactory) {
			if (dbgModuleReferenceProvider == null)
				throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			if (expressionCompiler == null)
				throw new ArgumentNullException(nameof(expressionCompiler));
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			this.dbgMetadataService = dbgMetadataService ?? throw new ArgumentNullException(nameof(dbgMetadataService));
			this.decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
			var dnILInterpreter = new DbgDotNetILInterpreterImpl();
			ExpressionEvaluator = new DbgEngineExpressionEvaluatorImpl(expressionCompiler);
			ValueFormatter = new DbgEngineValueFormatterImpl();
			Formatter = new DbgEngineFormatterImpl(formatter);
			LocalsProvider = new DbgEngineLocalsProviderImpl(dbgModuleReferenceProvider, expressionCompiler, valueNodeFactory, dnILInterpreter);
			AutosProvider = new DbgEngineAutosProviderImpl();
			ExceptionsProvider = new DbgEngineExceptionsProviderImpl(valueNodeFactory);
			ReturnValuesProvider = new DbgEngineReturnValuesProviderImpl();
			TypeVariablesProvider = new DbgEngineTypeVariablesProviderImpl(valueNodeFactory);
			ValueNodeFactory = new DbgEngineValueNodeFactoryImpl();
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

		struct DbgLanguageDebugInfoKey {
			readonly uint token;
			readonly DbgModule module;
			/*readonly*/ ModuleId moduleId;

			public DbgLanguageDebugInfoKey(DbgModule module, uint token) {
				this.token = token;
				moduleId = default;
				this.module = module;
			}

			public DbgLanguageDebugInfoKey(ModuleId moduleId, uint token) {
				this.token = token;
				this.moduleId = moduleId;
				module = null;
			}

			public bool Equals(DbgLanguageDebugInfoKey other) =>
				token == other.token &&
				module == other.module &&
				moduleId == other.moduleId;
		}

		sealed class RuntimeState {
			public readonly object LockObj = new object();
			public const int MAX_CACHED_DEBUG_INFOS = 5;
			public readonly List<(DbgLanguageDebugInfoKey key, DbgLanguageDebugInfo debugInfo)> DebugInfos = new List<(DbgLanguageDebugInfoKey key, DbgLanguageDebugInfo debugInfo)>(MAX_CACHED_DEBUG_INFOS);
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location, CancellationToken cancellationToken) {
			Debug.Assert(context.Runtime.GetDotNetRuntime() != null);
			var loc = location as IDbgDotNetCodeLocation;
			if (loc == null) {
				// Could be a special frame, eg. managed to native frame
				return;
			}

			var state = StateWithKey<RuntimeState>.GetOrCreate(context.Runtime, decompiler);
			var debugInfo = GetOrCreateDebugInfo(state, loc, cancellationToken);
			if (debugInfo == null)
				return;
			DbgLanguageDebugInfoExtensions.SetLanguageDebugInfo(context, debugInfo);
		}

		DbgLanguageDebugInfo GetOrCreateDebugInfo(RuntimeState state, IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
			DbgLanguageDebugInfoKey key;
			if (location.DbgModule is DbgModule dbgModule)
				key = new DbgLanguageDebugInfoKey(dbgModule, location.Token);
			else
				key = new DbgLanguageDebugInfoKey(location.Module, location.Token);

			var debugInfos = state.DebugInfos;
			lock (state.LockObj) {
				for (int i = debugInfos.Count - 1; i >= 0; i--) {
					var info = debugInfos[i];
					if (info.key.Equals(key)) {
						if (i != debugInfos.Count - 1) {
							debugInfos.RemoveAt(i);
							debugInfos.Add(info);
						}
						return info.debugInfo;
					}
				}
			}

			var debugInfo = CreateDebugInfo(location, cancellationToken);
			if (debugInfo == null)
				return null;
			lock (state.LockObj) {
				if (debugInfos.Count == RuntimeState.MAX_CACHED_DEBUG_INFOS)
					debugInfos.RemoveAt(0);
				debugInfos.Add((key, debugInfo));
			}
			return debugInfo;
		}

		DbgLanguageDebugInfo CreateDebugInfo(IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
			const DbgLoadModuleOptions options = DbgLoadModuleOptions.AutoLoaded;
			ModuleDef mdModule;
			if (location.DbgModule is DbgModule dbgModule)
				mdModule = dbgMetadataService.TryGetMetadata(dbgModule, options);
			else
				mdModule = dbgMetadataService.TryGetMetadata(location.Module, options);
			Debug.Assert(mdModule != null);
			if (mdModule == null)
				return null;
			cancellationToken.ThrowIfCancellationRequested();

			var method = mdModule.ResolveToken(location.Token) as MethodDef;
			Debug.Assert(method != null);
			if (method == null)
				return null;

			var context = new DecompilationContext {
				CancellationToken = cancellationToken,
				CalculateBinSpans = true,
			};
			var output = DecompilerOutputImplCache.Alloc();
			output.Initialize(method.MDToken.Raw);
			//TODO: Whenever the decompiler options change, we need to invalidate our cache and every
			//		single DbgLanguageDebugInfo instance.
			decompiler.Decompile(method, output, context);
			var methodDebugInfo = output.TryGetMethodDebugInfo();
			DecompilerOutputImplCache.Free(ref output);
			cancellationToken.ThrowIfCancellationRequested();
			Debug.Assert(methodDebugInfo != null);
			if (methodDebugInfo == null)
				return null;

			// We don't support EnC so the version is always 1
			const int methodVersion = 1;
			return new DbgLanguageDebugInfo(methodDebugInfo, methodVersion, location.Offset);
		}
	}
}
