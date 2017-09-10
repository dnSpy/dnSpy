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
using System.Linq;
using System.Threading;
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
	sealed class DbgEngineLocalsProviderImpl : DbgEngineValueNodeProvider {
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

		public override DbgEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.Invoke(() => GetNodesCore(context, frame, options, cancellationToken));

		public override void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.BeginInvoke(() => callback(GetNodesCore(context, frame, options, cancellationToken)));

		enum ValueInfoKind {
			CompiledExpression,
			DecompilerGeneratedVariable,
		}

		abstract class ValueInfo {
			public int ArrayIndex { get; }
			public abstract ValueInfoKind Kind { get; }
			public abstract bool IsParameter { get; }
			public abstract int Index { get; }
			protected ValueInfo(int arrayIndex) => ArrayIndex = arrayIndex;
		}

		sealed class CompiledExpressionValueInfo : ValueInfo {
			public override ValueInfoKind Kind => ValueInfoKind.CompiledExpression;
			public override bool IsParameter => compiledExpressions[index].ImageName == PredefinedDbgValueNodeImageNames.Parameter;
			public override int Index => compiledExpressions[index].Index;
			public ref DbgDotNetCompiledExpressionResult CompiledExpressionResult => ref compiledExpressions[index];

			readonly DbgDotNetCompiledExpressionResult[] compiledExpressions;
			readonly int index;

			public CompiledExpressionValueInfo(int arrayIndex, DbgDotNetCompiledExpressionResult[] compiledExpressions, int index) : base(arrayIndex) {
				this.compiledExpressions = compiledExpressions;
				this.index = index;
			}
		}

		sealed class DecompilerGeneratedVariableValueInfo : ValueInfo {
			public override ValueInfoKind Kind => ValueInfoKind.DecompilerGeneratedVariable;
			public override bool IsParameter => false;
			public override int Index => -1;
			public string Name { get; }
			public DecompilerGeneratedVariableValueInfo(int arrayIndex, string name) : base(arrayIndex) =>
				Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		sealed class GetNodesState {
			public struct Key {
				public readonly int DecompilerOptionsVersion;
				public readonly DbgValueNodeEvaluationOptions ValueNodeEvaluationOptions;
				// NOTE: DbgModule isn't part of this struct because the state is attached to the module.
				public readonly int MethodToken;
				public readonly int MethodVersion;
				public readonly DbgModuleReference[] ModuleReferences;
				public readonly MethodDebugScope Scope;
				public Key(int decompilerOptionsVersion, DbgValueNodeEvaluationOptions valueNodeEvaluationOptions, int methodToken, int methodVersion, DbgModuleReference[] moduleReferences, MethodDebugScope scope) {
					DecompilerOptionsVersion = decompilerOptionsVersion;
					ValueNodeEvaluationOptions = valueNodeEvaluationOptions;
					MethodToken = methodToken;
					MethodVersion = methodVersion;
					ModuleReferences = moduleReferences;
					Scope = scope;
				}
				public bool Equals(Key other) =>
					Scope == other.Scope &&
					ModuleReferences == other.ModuleReferences &&
					MethodToken == other.MethodToken &&
					MethodVersion == other.MethodVersion &&
					DecompilerOptionsVersion == other.DecompilerOptionsVersion &&
					ValueNodeEvaluationOptions == other.ValueNodeEvaluationOptions;
			}
			public Key CachedKey;
			public ValueInfo[] CachedValueInfos;
			public byte[] CachedAssemblyBytes;
			public DbgDotNetILInterpreterState CachedILInterpreterState;
		}

		DbgEngineValueNode[] GetNodesCore(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			DbgEngineValueNode[] valueNodes = null;
			try {
				var references = dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, frame);

				var languageDebugInfo = context.GetLanguageDebugInfo();
				var methodDebugInfo = languageDebugInfo.MethodDebugInfo;
				var module = frame.Module ?? throw new InvalidOperationException();

				// Since we attach this to the module, the module doesn't have to be part of Key
				var state = StateWithKey<GetNodesState>.GetOrCreate(module, this);
				var key = new GetNodesState.Key(methodDebugInfo.DecompilerOptionsVersion, options,
						methodDebugInfo.Method.MDToken.ToInt32(), languageDebugInfo.MethodVersion,
						references, GetScope(methodDebugInfo.Scope, languageDebugInfo.ILOffset));

				ValueInfo[] valueInfos;
				byte[] assemblyBytes;
				if (key.Equals(state.CachedKey)) {
					valueInfos = state.CachedValueInfos;
					assemblyBytes = state.CachedAssemblyBytes;
				}
				else {
					var evalOptions = DbgEvaluationOptions.None;
					if ((options & DbgValueNodeEvaluationOptions.NoFuncEval) != 0)
						evalOptions |= DbgEvaluationOptions.NoFuncEval;
					if ((options & DbgValueNodeEvaluationOptions.RawView) != 0)
						evalOptions |= DbgEvaluationOptions.RawView;

					var compilationResult = expressionCompiler.CompileGetLocals(context, frame, references, evalOptions, cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();
					if (compilationResult.IsError)
						return new[] { CreateInternalErrorNode(context, compilationResult.ErrorMessage) };

					int decompilerGeneratedCount = GetDecompilerGeneratedVariablesCount(methodDebugInfo.Scope, languageDebugInfo.ILOffset);

					valueInfos = new ValueInfo[compilationResult.CompiledExpressions.Length + decompilerGeneratedCount];
					int valueInfosIndex = 0;
					for (int i = 0; i < compilationResult.CompiledExpressions.Length; i++, valueInfosIndex++)
						valueInfos[valueInfosIndex] = new CompiledExpressionValueInfo(valueInfosIndex, compilationResult.CompiledExpressions, i);

					if (decompilerGeneratedCount > 0) {
						var scope = methodDebugInfo.Scope;
						for (;;) {
							foreach (var local in scope.Locals) {
								if (local.IsDecompilerGenerated) {
									valueInfos[valueInfosIndex] = new DecompilerGeneratedVariableValueInfo(valueInfosIndex, local.Name);
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
				}

				valueNodes = valueInfos.Length == 0 ? Array.Empty<DbgEngineValueNode>() : new DbgEngineValueNode[valueInfos.Length];
				var valueCreator = new DbgDotNetValueCreator(valueNodeFactory, dnILInterpreter, context, frame, options, assemblyBytes, cancellationToken);
				for (int i = 0; i < valueNodes.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					var valueInfo = valueInfos[i];

					DbgEngineValueNode valueNode;
					switch (valueInfo.Kind) {
					case ValueInfoKind.CompiledExpression:
						var compExpr = (CompiledExpressionValueInfo)valueInfo;
						valueNode = valueCreator.CreateValueNode(ref state.CachedILInterpreterState, ref compExpr.CompiledExpressionResult);
						break;

					case ValueInfoKind.DecompilerGeneratedVariable:
						var decGen = (DecompilerGeneratedVariableValueInfo)valueInfo;
						valueNode = valueNodeFactory.CreateError(context,
							new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Local, decGen.Name)),
							dnSpy_Debugger_DotNet_Resources.DecompilerGeneratedVariablesCanNotBeEvaluated,
							decGen.Name);
						break;

					default:
						throw new InvalidOperationException();
					}

					valueNodes[i] = valueNode;
				}

				return valueNodes;
			}
			catch {
				if (valueNodes != null)
					frame.Process.DbgManager.Close(valueNodes.Where(a => a != null));
				return new[] { CreateInternalErrorNode(context, dnSpy_Debugger_DotNet_Resources.InternalDebuggerError) };
			}
		}

		DbgEngineValueNode CreateInternalErrorNode(DbgEvaluationContext context, string errorMessage) =>
			valueNodeFactory.CreateError(context, new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, "<error>")), errorMessage, "<internal.error>");

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
