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
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;
using dnSpy.Decompiler.Utils;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineLanguageImpl : DbgEngineLanguage {
		public override string Name { get; }
		public override string DisplayName { get; }
		public override DbgEngineExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgEngineFormatter Formatter { get; }
		public override DbgEngineLocalsValueNodeProvider LocalsProvider { get; }
		public override DbgEngineValueNodeProvider AutosProvider { get; }
		public override DbgEngineValueNodeProvider ExceptionsProvider { get; }
		public override DbgEngineValueNodeProvider ReturnValuesProvider { get; }
		public override DbgEngineValueNodeProvider TypeVariablesProvider { get; }
		public override DbgEngineValueNodeFactory ValueNodeFactory { get; }

		readonly DbgMetadataService dbgMetadataService;
		readonly DbgDotNetExpressionCompiler expressionCompiler;
		readonly IDecompiler decompiler;
		readonly IDebuggerDisplayAttributeEvaluator debuggerDisplayAttributeEvaluator;

		public DbgEngineLanguageImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, string name, string displayName, DbgDotNetExpressionCompiler expressionCompiler, DbgMetadataService dbgMetadataService, IDecompiler decompiler, DbgDotNetFormatter formatter, DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetILInterpreter dnILInterpreter, DbgObjectIdService objectIdService, IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper) {
			if (dbgModuleReferenceProvider == null)
				throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));
			if (valueNodeFactory == null)
				throw new ArgumentNullException(nameof(valueNodeFactory));
			if (dnILInterpreter == null)
				throw new ArgumentNullException(nameof(dnILInterpreter));
			if (objectIdService == null)
				throw new ArgumentNullException(nameof(objectIdService));
			if (predefinedEvaluationErrorMessagesHelper == null)
				throw new ArgumentNullException(nameof(predefinedEvaluationErrorMessagesHelper));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			this.dbgMetadataService = dbgMetadataService ?? throw new ArgumentNullException(nameof(dbgMetadataService));
			this.expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
			this.decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
			var expressionEvaluator = new DbgEngineExpressionEvaluatorImpl(dbgModuleReferenceProvider, expressionCompiler, dnILInterpreter, objectIdService, predefinedEvaluationErrorMessagesHelper);
			ExpressionEvaluator = expressionEvaluator;
			Formatter = new DbgEngineFormatterImpl(formatter);
			LocalsProvider = new DbgEngineLocalsProviderImpl(dbgModuleReferenceProvider, expressionCompiler, valueNodeFactory, dnILInterpreter);
			AutosProvider = new DbgEngineAutosProviderImpl(valueNodeFactory);
			ExceptionsProvider = new DbgEngineExceptionsProviderImpl(valueNodeFactory);
			ReturnValuesProvider = new DbgEngineReturnValuesProviderImpl(valueNodeFactory);
			TypeVariablesProvider = new DbgEngineTypeVariablesProviderImpl(valueNodeFactory);
			ValueNodeFactory = new DbgEngineValueNodeFactoryImpl(expressionEvaluator, valueNodeFactory, formatter);
			debuggerDisplayAttributeEvaluator = expressionEvaluator;
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

		readonly struct DbgLanguageDebugInfoKey {
			readonly uint token;
			readonly DbgModule module;
			readonly ModuleId moduleId;
			readonly int refreshedVersion;

			public DbgLanguageDebugInfoKey(DbgModule module, uint token) {
				this.token = token;
				moduleId = default;
				this.module = module;
				refreshedVersion = module.RefreshedVersion;
			}

			public DbgLanguageDebugInfoKey(in ModuleId moduleId, uint token) {
				this.token = token;
				this.moduleId = moduleId;
				module = null;
				refreshedVersion = 0;
			}

			public bool Equals(DbgLanguageDebugInfoKey other) =>
				token == other.token &&
				module == other.module &&
				refreshedVersion == other.refreshedVersion &&
				moduleId == other.moduleId;
		}

		sealed class RuntimeState {
			public readonly object LockObj = new object();
			public const int MAX_CACHED_DEBUG_INFOS = 5;
			public readonly List<(DbgLanguageDebugInfoKey key, DbgLanguageDebugInfo debugInfo)> DebugInfos = new List<(DbgLanguageDebugInfoKey key, DbgLanguageDebugInfo debugInfo)>(MAX_CACHED_DEBUG_INFOS);
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location, CancellationToken cancellationToken) {
			Debug.Assert(context.Runtime.GetDotNetRuntime() != null);

			IDebuggerDisplayAttributeEvaluatorUtils.Initialize(context, debuggerDisplayAttributeEvaluator);
			// Needed by DebuggerRuntimeImpl (calls expressionCompiler.TryGetAliasInfo())
			context.GetOrCreateData(() => expressionCompiler);

			if ((context.Options & DbgEvaluationContextOptions.NoMethodBody) == 0 && location is IDbgDotNetCodeLocation loc) {
				var state = StateWithKey<RuntimeState>.GetOrCreate(context.Runtime, decompiler);
				var debugInfo = GetOrCreateDebugInfo(context, state, loc, cancellationToken);
				if (debugInfo != null)
					DbgLanguageDebugInfoExtensions.SetLanguageDebugInfo(context, debugInfo);
			}
		}

		DbgLanguageDebugInfo GetOrCreateDebugInfo(DbgEvaluationContext context, RuntimeState state, IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
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

			var debugInfo = CreateDebugInfo(context, location, cancellationToken);
			if (debugInfo == null)
				return null;
			lock (state.LockObj) {
				if (debugInfos.Count == RuntimeState.MAX_CACHED_DEBUG_INFOS)
					debugInfos.RemoveAt(0);
				debugInfos.Add((key, debugInfo));
			}
			return debugInfo;
		}

		DbgLanguageDebugInfo CreateDebugInfo(DbgEvaluationContext context, IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
			const DbgLoadModuleOptions options = DbgLoadModuleOptions.AutoLoaded;
			ModuleDef mdModule;
			if (location.DbgModule is DbgModule dbgModule)
				mdModule = dbgMetadataService.TryGetMetadata(dbgModule, options);
			else {
				dbgModule = null;
				mdModule = dbgMetadataService.TryGetMetadata(location.Module, options);
			}
			Debug.Assert(mdModule != null);
			if (mdModule == null)
				return null;
			cancellationToken.ThrowIfCancellationRequested();

			var method = mdModule.ResolveToken(location.Token) as MethodDef;
			// Could be null if it's a dynamic assembly. It will get refreshed later and we'll get called again.
			if (method == null)
				return null;

			if (!StateMachineHelpers.TryGetKickoffMethod(method, out var containingMethod))
				containingMethod = method;

			var runtime = context.Runtime.GetDotNetRuntime();
			int methodToken, localVarSigTok;
			if (dbgModule == null || !runtime.TryGetMethodToken(dbgModule, (int)location.Token, out methodToken, out localVarSigTok)) {
				methodToken = (int)location.Token;
				localVarSigTok = (int)(method.Body?.LocalVarSigTok ?? 0);
			}

			var decContext = new DecompilationContext {
				CancellationToken = cancellationToken,
				CalculateBinSpans = true,
			};
			var methodDebugInfo = TryCompileAndGetDebugInfo(containingMethod, location.Token, decContext, cancellationToken);
			if (methodDebugInfo == null && containingMethod != method) {
				// The decompiler can't decompile the iterator / async method, try again,
				// but only decompile the MoveNext method
				methodDebugInfo = TryCompileAndGetDebugInfo(method, location.Token, decContext, cancellationToken);
			}
			if (methodDebugInfo == null && method.Body == null) {
				var scope = new MethodDebugScope(new BinSpan(0, 0), Array.Empty<MethodDebugScope>(), Array.Empty<SourceLocal>(), Array.Empty<ImportInfo>(), Array.Empty<MethodDebugConstant>());
				methodDebugInfo = new MethodDebugInfo(-1, method, null, Array.Empty<SourceStatement>(), scope, null, null);
			}
			if (methodDebugInfo == null)
				return null;

			// We don't support EnC so the version is always 1
			const int methodVersion = 1;
			return new DbgLanguageDebugInfo(methodDebugInfo, methodToken, localVarSigTok, methodVersion, location.Offset);
		}

		MethodDebugInfo TryCompileAndGetDebugInfo(MethodDef method, uint methodToken, DecompilationContext decContext, CancellationToken cancellationToken) {
			var output = DecompilerOutputImplCache.Alloc();
			output.Initialize(methodToken);
			//TODO: Whenever the decompiler options change, we need to invalidate our cache and every
			//		single DbgLanguageDebugInfo instance.
			decompiler.Decompile(method, output, decContext);
			var methodDebugInfo = output.TryGetMethodDebugInfo();
			DecompilerOutputImplCache.Free(ref output);
			cancellationToken.ThrowIfCancellationRequested();
			return methodDebugInfo;
		}
	}
}
