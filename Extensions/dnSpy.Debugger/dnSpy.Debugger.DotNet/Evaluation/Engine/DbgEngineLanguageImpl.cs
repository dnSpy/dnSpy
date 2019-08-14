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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Code;
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;

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

		readonly DbgMethodDebugInfoProvider dbgMethodDebugInfoProvider;
		readonly IDecompiler decompiler;

		readonly DbgDotNetExpressionCompiler expressionCompiler;
		readonly IDebuggerDisplayAttributeEvaluator debuggerDisplayAttributeEvaluator;

		public DbgEngineLanguageImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, string name, string displayName, DbgDotNetExpressionCompiler expressionCompiler, DbgMethodDebugInfoProvider dbgMethodDebugInfoProvider, IDecompiler decompiler, DbgDotNetFormatter formatter, DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetILInterpreter dnILInterpreter, DbgAliasProvider dbgAliasProvider, IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper) {
			if (dbgModuleReferenceProvider is null)
				throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			if (formatter is null)
				throw new ArgumentNullException(nameof(formatter));
			if (valueNodeFactory is null)
				throw new ArgumentNullException(nameof(valueNodeFactory));
			if (dnILInterpreter is null)
				throw new ArgumentNullException(nameof(dnILInterpreter));
			if (dbgAliasProvider is null)
				throw new ArgumentNullException(nameof(dbgAliasProvider));
			if (predefinedEvaluationErrorMessagesHelper is null)
				throw new ArgumentNullException(nameof(predefinedEvaluationErrorMessagesHelper));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			this.dbgMethodDebugInfoProvider = dbgMethodDebugInfoProvider ?? throw new ArgumentNullException(nameof(dbgMethodDebugInfoProvider));
			this.expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
			this.decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
			var expressionEvaluator = new DbgEngineExpressionEvaluatorImpl(dbgModuleReferenceProvider, expressionCompiler, dnILInterpreter, dbgAliasProvider, predefinedEvaluationErrorMessagesHelper);
			ExpressionEvaluator = expressionEvaluator;
			Formatter = new DbgEngineFormatterImpl(formatter);
			LocalsProvider = new DbgEngineLocalsProviderImpl(dbgModuleReferenceProvider, expressionCompiler, valueNodeFactory, dnILInterpreter, dbgAliasProvider);
			AutosProvider = new DbgEngineAutosProviderImpl(valueNodeFactory);
			ExceptionsProvider = new DbgEngineExceptionsProviderImpl(valueNodeFactory);
			ReturnValuesProvider = new DbgEngineReturnValuesProviderImpl(valueNodeFactory);
			TypeVariablesProvider = new DbgEngineTypeVariablesProviderImpl(valueNodeFactory);
			ValueNodeFactory = new DbgEngineValueNodeFactoryImpl(expressionEvaluator, valueNodeFactory, formatter);
			debuggerDisplayAttributeEvaluator = expressionEvaluator;
		}

		readonly struct DbgLanguageDebugInfoKey {
			readonly uint token;
			readonly DbgModule? module;
			readonly ModuleId moduleId;
			readonly int refreshedVersion;

			public DbgLanguageDebugInfoKey(DbgModule module, uint token) {
				this.token = token;
				moduleId = default;
				this.module = module;
				refreshedVersion = module.RefreshedVersion;
			}

			public DbgLanguageDebugInfoKey(ModuleId moduleId, uint token) {
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

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation? location, CancellationToken cancellationToken) {
			Debug2.Assert(!(context.Runtime.GetDotNetRuntime() is null));

			IDebuggerDisplayAttributeEvaluatorUtils.Initialize(context, debuggerDisplayAttributeEvaluator);
			// Needed by DebuggerRuntimeImpl (calls expressionCompiler.TryGetAliasInfo())
			context.GetOrCreateData(() => expressionCompiler);

			if ((context.Options & DbgEvaluationContextOptions.NoMethodBody) == 0 && location is IDbgDotNetCodeLocation loc) {
				var state = StateWithKey<RuntimeState>.GetOrCreate(context.Runtime, decompiler);
				var debugInfo = GetOrCreateDebugInfo(context, state, loc, cancellationToken);
				if (!(debugInfo is null))
					DbgLanguageDebugInfoExtensions.SetLanguageDebugInfo(context, debugInfo);
			}
		}

		//TODO: If decompiler settings change, we need to invalidate the cached data in DbgEvaluationContext, see decompiler.Settings.VersionChanged
		DbgLanguageDebugInfo? GetOrCreateDebugInfo(DbgEvaluationContext context, RuntimeState state, IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
			DbgLanguageDebugInfoKey key;
			if (location.DbgModule is DbgModule dbgModule)
				key = new DbgLanguageDebugInfoKey(dbgModule, location.Token);
			else
				key = new DbgLanguageDebugInfoKey(location.Module, location.Token);

			var debugInfos = state.DebugInfos;
			lock (state.LockObj) {
				if (debugInfos.Count > 0 && debugInfos[0].debugInfo.MethodDebugInfo.DebugInfoVersion != decompiler.Settings.Version)
					debugInfos.Clear();
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
			if (debugInfo is null)
				return null;
			lock (state.LockObj) {
				if (debugInfos.Count == RuntimeState.MAX_CACHED_DEBUG_INFOS)
					debugInfos.RemoveAt(0);
				debugInfos.Add((key, debugInfo));
			}
			return debugInfo;
		}

		DbgLanguageDebugInfo? CreateDebugInfo(DbgEvaluationContext context, IDbgDotNetCodeLocation location, CancellationToken cancellationToken) {
			var result = dbgMethodDebugInfoProvider.GetMethodDebugInfo(context.Runtime, decompiler, location, cancellationToken);
			if (result.DebugInfo is null)
				return null;

			var runtime = context.Runtime.GetDotNetRuntime();
			if (location.DbgModule is null || !runtime.TryGetMethodToken(location.DbgModule, (int)location.Token, out int methodToken, out int localVarSigTok)) {
				methodToken = (int)location.Token;
				localVarSigTok = (int)((result.StateMachineDebugInfo ?? result.DebugInfo)?.Method.Body?.LocalVarSigTok ?? 0);
			}

			return new DbgLanguageDebugInfo(result.DebugInfo, methodToken, localVarSigTok, result.MethodVersion, location.Offset);
		}
	}
}
