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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Properties;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineLocalsProviderImpl : DbgEngineLocalsValueNodeProvider {
		readonly DbgModuleReferenceProvider dbgModuleReferenceProvider;
		readonly DbgDotNetExpressionCompiler expressionCompiler;
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetILInterpreter dnILInterpreter;

		public DbgEngineLocalsProviderImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, DbgDotNetExpressionCompiler expressionCompiler, DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetILInterpreter dnILInterpreter) {
			this.dbgModuleReferenceProvider = dbgModuleReferenceProvider ?? throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			this.expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));
			this.dnILInterpreter = dnILInterpreter ?? throw new ArgumentNullException(nameof(dnILInterpreter));
		}

		public override DbgEngineLocalsValueNodeInfo[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetNodesCore(context, frame, options, localsOptions, cancellationToken);
			return dispatcher.Invoke(() => GetNodesCore(context, frame, options, localsOptions, cancellationToken));
		}

		enum ValueInfoKind {
			CompiledExpression,
			DecompilerGeneratedVariable,
		}

		abstract class ValueInfo {
			public abstract ValueInfoKind Kind { get; }
			public abstract bool IsParameter { get; }
		}

		sealed class CompiledExpressionValueInfo : ValueInfo {
			public override ValueInfoKind Kind => ValueInfoKind.CompiledExpression;
			public override bool IsParameter => compiledExpressions[index].ImageName == PredefinedDbgValueNodeImageNames.Parameter || compiledExpressions[index].ImageName == PredefinedDbgValueNodeImageNames.This;
			public bool IsCompilerGenerated => (compiledExpressions[index].ResultFlags & DbgDotNetCompiledExpressionResultFlags.CompilerGenerated) != 0;
			public ref DbgDotNetCompiledExpressionResult CompiledExpressionResult => ref compiledExpressions[index];

			readonly DbgDotNetCompiledExpressionResult[] compiledExpressions;
			readonly int index;

			public CompiledExpressionValueInfo(DbgDotNetCompiledExpressionResult[] compiledExpressions, int index) {
				this.compiledExpressions = compiledExpressions;
				this.index = index;
			}
		}

		sealed class DecompilerGeneratedVariableValueInfo : ValueInfo {
			public override ValueInfoKind Kind => ValueInfoKind.DecompilerGeneratedVariable;
			public override bool IsParameter => false;
			public string Name { get; }
			public DecompilerGeneratedVariableValueInfo(string name) =>
				Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		sealed class GetNodesState {
			public struct Key {
				readonly int decompilerOptionsVersion;
				readonly DbgValueNodeEvaluationOptions valueNodeEvaluationOptions;
				readonly DbgLocalsValueNodeEvaluationOptions localsValueNodeEvaluationOptions;
				// NOTE: DbgModule isn't part of this struct because the state is attached to the module.
				readonly int methodToken;
				readonly int methodVersion;
				readonly DbgModuleReference[] moduleReferences;
				readonly MethodDebugScope scope;
				public Key(int decompilerOptionsVersion, DbgValueNodeEvaluationOptions valueNodeEvaluationOptions, DbgLocalsValueNodeEvaluationOptions localsValueNodeEvaluationOptions, int methodToken, int methodVersion, DbgModuleReference[] moduleReferences, MethodDebugScope scope) {
					this.decompilerOptionsVersion = decompilerOptionsVersion;
					this.valueNodeEvaluationOptions = valueNodeEvaluationOptions;
					this.localsValueNodeEvaluationOptions = localsValueNodeEvaluationOptions;
					this.methodToken = methodToken;
					this.methodVersion = methodVersion;
					this.moduleReferences = moduleReferences;
					this.scope = scope;
				}
				public bool Equals(Key other) =>
					scope == other.scope &&
					moduleReferences == other.moduleReferences &&
					methodToken == other.methodToken &&
					methodVersion == other.methodVersion &&
					decompilerOptionsVersion == other.decompilerOptionsVersion &&
					valueNodeEvaluationOptions == other.valueNodeEvaluationOptions &&
					localsValueNodeEvaluationOptions == other.localsValueNodeEvaluationOptions;
			}
			public Key CachedKey;
			public ValueInfo[] CachedValueInfos;
			public byte[] CachedAssemblyBytes;
			public DbgDotNetILInterpreterState CachedILInterpreterState;
			public int CachedDecompilerGeneratedCount;
			public int CachedCompilerGeneratedCount;
		}

		DbgEngineLocalsValueNodeInfo[] GetNodesCore(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, CancellationToken cancellationToken) {
			DbgEngineLocalsValueNodeInfo[] valueNodes = null;
			try {
				var refsResult = dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, frame);
				if (refsResult.ErrorMessage != null)
					return Array.Empty<DbgEngineLocalsValueNodeInfo>();

				var languageDebugInfo = context.TryGetLanguageDebugInfo();
				if (languageDebugInfo == null)
					return Array.Empty<DbgEngineLocalsValueNodeInfo>();
				var methodDebugInfo = languageDebugInfo.MethodDebugInfo;
				var module = frame.Module ?? throw new InvalidOperationException();

				// Since we attach this to the module, the module doesn't have to be part of Key
				var state = StateWithKey<GetNodesState>.GetOrCreate(module, this);
				var localsOptionsKey = localsOptions & ~(DbgLocalsValueNodeEvaluationOptions.ShowCompilerGeneratedVariables | DbgLocalsValueNodeEvaluationOptions.ShowDecompilerGeneratedVariables);
				var key = new GetNodesState.Key(methodDebugInfo.DecompilerOptionsVersion, options, localsOptionsKey,
						methodDebugInfo.Method.MDToken.ToInt32(), languageDebugInfo.MethodVersion,
						refsResult.ModuleReferences, GetScope(methodDebugInfo.Scope, languageDebugInfo.ILOffset));

				ValueInfo[] valueInfos;
				byte[] assemblyBytes;
				int compilerGeneratedCount;
				int decompilerGeneratedCount;
				if (key.Equals(state.CachedKey)) {
					valueInfos = state.CachedValueInfos;
					assemblyBytes = state.CachedAssemblyBytes;
					decompilerGeneratedCount = state.CachedDecompilerGeneratedCount;
					compilerGeneratedCount = state.CachedCompilerGeneratedCount;
				}
				else {
					var evalOptions = DbgEvaluationOptions.None;
					if ((options & DbgValueNodeEvaluationOptions.NoFuncEval) != 0)
						evalOptions |= DbgEvaluationOptions.NoFuncEval;
					if ((options & DbgValueNodeEvaluationOptions.RawView) != 0)
						evalOptions |= DbgEvaluationOptions.RawView;
					if ((options & DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers) != 0)
						evalOptions |= DbgEvaluationOptions.HideCompilerGeneratedMembers;
					if ((options & DbgValueNodeEvaluationOptions.RespectHideMemberAttributes) != 0)
						evalOptions |= DbgEvaluationOptions.RespectHideMemberAttributes;
					if ((options & DbgValueNodeEvaluationOptions.PublicMembers) != 0)
						evalOptions |= DbgEvaluationOptions.PublicMembers;
					if ((options & DbgValueNodeEvaluationOptions.NoHideRoots) != 0)
						evalOptions |= DbgEvaluationOptions.NoHideRoots;

					var compilationResult = expressionCompiler.CompileGetLocals(context, frame, refsResult.ModuleReferences, evalOptions, cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();
					if (compilationResult.IsError)
						return new[] { CreateInternalErrorNode(context, frame, compilationResult.ErrorMessage, cancellationToken) };

					decompilerGeneratedCount = GetDecompilerGeneratedVariablesCount(methodDebugInfo.Scope, languageDebugInfo.ILOffset);

					valueInfos = new ValueInfo[compilationResult.CompiledExpressions.Length + decompilerGeneratedCount];
					int valueInfosIndex = 0;
					compilerGeneratedCount = 0;
					for (int i = 0; i < compilationResult.CompiledExpressions.Length; i++, valueInfosIndex++) {
						if ((compilationResult.CompiledExpressions[i].ResultFlags & DbgDotNetCompiledExpressionResultFlags.CompilerGenerated) != 0)
							compilerGeneratedCount++;
						valueInfos[valueInfosIndex] = new CompiledExpressionValueInfo(compilationResult.CompiledExpressions, i);
					}

					if (decompilerGeneratedCount > 0) {
						var scope = methodDebugInfo.Scope;
						for (;;) {
							foreach (var local in scope.Locals) {
								if (local.IsDecompilerGenerated) {
									valueInfos[valueInfosIndex] = new DecompilerGeneratedVariableValueInfo(local.Name);
									valueInfosIndex++;
								}
							}

							bool found = false;
							foreach (var childScope in scope.Scopes) {
								if (childScope.Span.Start <= languageDebugInfo.ILOffset && languageDebugInfo.ILOffset < childScope.Span.End) {
									found = true;
									scope = childScope;
									break;
								}
							}
							if (!found)
								break;
						}
					}

					if (valueInfos.Length != valueInfosIndex)
						throw new InvalidOperationException();

					assemblyBytes = compilationResult.Assembly;
					state.CachedKey = key;
					state.CachedValueInfos = valueInfos;
					state.CachedAssemblyBytes = assemblyBytes;
					state.CachedILInterpreterState = null;
					state.CachedDecompilerGeneratedCount = decompilerGeneratedCount;
					state.CachedCompilerGeneratedCount = compilerGeneratedCount;
				}

				int count = valueInfos.Length;
				if ((localsOptions & DbgLocalsValueNodeEvaluationOptions.ShowCompilerGeneratedVariables) == 0)
					count -= compilerGeneratedCount;
				if ((localsOptions & DbgLocalsValueNodeEvaluationOptions.ShowDecompilerGeneratedVariables) == 0)
					count -= decompilerGeneratedCount;
				valueNodes = count == 0 ? Array.Empty<DbgEngineLocalsValueNodeInfo>() : new DbgEngineLocalsValueNodeInfo[count];
				var valueCreator = new DbgDotNetValueCreator(valueNodeFactory, dnILInterpreter, context, frame, options, assemblyBytes, cancellationToken);
				int w = 0;
				for (int i = 0; i < valueInfos.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					var valueInfo = valueInfos[i];

					DbgEngineLocalsValueNodeInfo valueNodeInfo;
					switch (valueInfo.Kind) {
					case ValueInfoKind.CompiledExpression:
						var compExpr = (CompiledExpressionValueInfo)valueInfo;
						if ((localsOptions & DbgLocalsValueNodeEvaluationOptions.ShowCompilerGeneratedVariables) == 0 && compExpr.IsCompilerGenerated)
							continue;
						valueNodeInfo = new DbgEngineLocalsValueNodeInfo(
							compExpr.IsParameter ? DbgLocalsValueNodeKind.Parameter : DbgLocalsValueNodeKind.Local,
							valueCreator.CreateValueNode(ref state.CachedILInterpreterState, ref compExpr.CompiledExpressionResult));
						break;

					case ValueInfoKind.DecompilerGeneratedVariable:
						if ((localsOptions & DbgLocalsValueNodeEvaluationOptions.ShowDecompilerGeneratedVariables) == 0)
							continue;
						var decGen = (DecompilerGeneratedVariableValueInfo)valueInfo;
						valueNodeInfo = new DbgEngineLocalsValueNodeInfo(DbgLocalsValueNodeKind.Local,
							valueNodeFactory.CreateError(context, frame,
							new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Local, decGen.Name)),
							dnSpy_Debugger_DotNet_Resources.DecompilerGeneratedVariablesCanNotBeEvaluated,
							decGen.Name, false, cancellationToken));
						break;

					default:
						throw new InvalidOperationException();
					}

					valueNodes[w++] = valueNodeInfo;
				}
				if (w != valueNodes.Length)
					throw new InvalidOperationException();

				return valueNodes;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				if (valueNodes != null)
					frame.Process.DbgManager.Close(valueNodes.Select(a => a.ValueNode).Where(a => a != null));
				return new[] { CreateInternalErrorNode(context, frame, PredefinedEvaluationErrorMessages.InternalDebuggerError, cancellationToken) };
			}
		}

		DbgEngineLocalsValueNodeInfo CreateInternalErrorNode(DbgEvaluationContext context, DbgStackFrame frame, string errorMessage, CancellationToken cancellationToken) =>
			new DbgEngineLocalsValueNodeInfo(DbgLocalsValueNodeKind.Error, valueNodeFactory.CreateError(context, frame, new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, "<error>")), errorMessage, "<internal.error>", false, cancellationToken));

		static MethodDebugScope GetScope(MethodDebugScope rootScope, uint offset) {
			var scope = rootScope;
			for (;;) {
				bool found = false;
				foreach (var childScope in scope.Scopes) {
					if (childScope.Span.Start <= offset && offset < childScope.Span.End) {
						found = true;
						scope = childScope;
						break;
					}
				}
				if (!found)
					return scope;
			}
		}

		static int GetDecompilerGeneratedVariablesCount(MethodDebugScope rootScope, uint offset) {
			var scope = rootScope;
			int count = 0;
			for (;;) {
				foreach (var local in scope.Locals) {
					if (local.IsDecompilerGenerated)
						count++;
				}

				bool found = false;
				foreach (var childScope in scope.Scopes) {
					if (childScope.Span.Start <= offset && offset < childScope.Span.End) {
						found = true;
						scope = childScope;
						break;
					}
				}
				if (!found)
					return count;
			}
		}
	}
}
