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
using System.Collections.Immutable;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.DnSpy;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace dnSpy.Roslyn.Shared.Debugger.ExpressionCompiler {
	abstract class LanguageExpressionCompiler : DbgDotNetExpressionCompiler {
		const bool showAllLocals = false;

		protected abstract class EvalContextState {
			public DbgModuleReference[] LastModuleReferences;
			public ImmutableArray<MetadataBlock> LastMetadataBlocks;

			public DSEEMethodDebugInfo MethodDebugInfo;
			public object MethodDebugInfoKey;
		}

		T GetEvalContextState<T>(DbgStackFrame frame) where T : EvalContextState, new() {
			// We attach the state to the module, and not to the app domain, so the Roslyn compilation
			// doesn't get recreated everytime we change the module.
			var module = frame.Module;
			if (module == null)
				throw new InvalidOperationException();
			return module.GetOrCreateData<T>();
		}

		protected void GetCompileGetLocalsState<T>(DbgEvaluationContext context, DbgStackFrame frame, DbgModuleReference[] references, out DbgLanguageDebugInfo langDebugInfo, out MethodDef method, out int localVarSigTok, out T state, out ImmutableArray<MetadataBlock> metadataBlocks, out int methodVersion) where T : EvalContextState, new() {
			langDebugInfo = context.GetLanguageDebugInfo();
			method = langDebugInfo.MethodDebugInfo.Method;
			localVarSigTok = (int)(method.Body?.LocalVarSigTok ?? 0);

			state = GetEvalContextState<T>(frame);

			if (state.LastModuleReferences == references && !state.LastMetadataBlocks.IsDefault)
				metadataBlocks = state.LastMetadataBlocks;
			else {
				metadataBlocks = CreateMetadataBlock(references);
				state.LastModuleReferences = references;
				state.LastMetadataBlocks = metadataBlocks;
			}

			methodVersion = langDebugInfo.MethodVersion;
		}

		protected GetMethodDebugInfo CreateGetMethodDebugInfo(EvalContextState evalContextState, DbgLanguageDebugInfo langDebugInfo) {
			// If there's only one scope, we can cache the whole thing
			if (langDebugInfo.MethodDebugInfo.Scope.Scopes.Length == 0) {
				if (evalContextState.MethodDebugInfoKey != langDebugInfo.MethodDebugInfo.Method) {
					evalContextState.MethodDebugInfo = CreateMethodDebugInfo(langDebugInfo);
					evalContextState.MethodDebugInfoKey = langDebugInfo.MethodDebugInfo.Method;
				}
				return () => evalContextState.MethodDebugInfo;
			}
			evalContextState.MethodDebugInfo = default;
			evalContextState.MethodDebugInfoKey = null;
			return () => CreateMethodDebugInfo(langDebugInfo);
		}

		DSEEMethodDebugInfo CreateMethodDebugInfo(DbgLanguageDebugInfo langDebugInfo) {
			var info = new DSEEMethodDebugInfo();

			var stack = new List<MethodDebugScope>();
			var allScopes = new List<MethodDebugScope>();
			var containingScopes = new List<MethodDebugScope>();
			RoslynExpressionCompilerMethods.GetAllScopes(langDebugInfo.MethodDebugInfo.Scope, stack, allScopes, containingScopes, langDebugInfo.ILOffset);

			info.HoistedLocalScopeRecords = default;
			info.ImportRecordGroups = GetImports(langDebugInfo.MethodDebugInfo.Method.DeclaringType, langDebugInfo.MethodDebugInfo.Scope, out var defaultNamespaceName);
			info.ExternAliasRecords = default;
			info.DynamicLocalMap = default;
			info.TupleLocalMap = default;
			info.DefaultNamespaceName = defaultNamespaceName ?? string.Empty;
			info.LocalVariableNames = RoslynExpressionCompilerMethods.GetLocalNames(containingScopes, showAllLocals);
			info.LocalConstants = default;
			info.ReuseSpan = RoslynExpressionCompilerMethods.GetReuseSpan(allScopes, langDebugInfo.ILOffset);

			return info;
		}

		protected abstract ImmutableArray<ImmutableArray<DSEEImportRecord>> GetImports(TypeDef declaringType, MethodDebugScope scope, out string defaultNamespaceName);

		protected static void AddDSEEImportRecord(ImmutableArray<DSEEImportRecord>.Builder builder, ImportInfo info, ref string defaultNamespaceName) {
			switch (info.TargetKind) {
			case ImportInfoKind.Namespace:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.Namespace, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.Type:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.Type, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.NamespaceOrType:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.NamespaceOrType, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.Assembly:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.Assembly, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.XmlNamespace:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.XmlNamespace, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.MethodToken:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.MethodToken, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.CurrentNamespace:
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.CurrentNamespace, info.Alias, info.Target, info.ExternAlias));
				break;

			case ImportInfoKind.DefaultNamespace:
				defaultNamespaceName = info.Target ?? string.Empty;
				builder.Add(new DSEEImportRecord(DSEEImportTargetKind.DefaultNamespace, info.Alias, info.Target, info.ExternAlias));
				break;

			default:
				Debug.Fail($"Unknown import kind: {info.TargetKind}");
				break;
			}
		}

		protected DbgDotNetCompilationResult CreateCompilationResult(byte[] assembly, string typeName, DSEELocalAndMethod[] infos, string errorMessage) {
			Debug.Assert(errorMessage == null || (assembly == null || assembly.Length == 0));

			if (errorMessage != null)
				return new DbgDotNetCompilationResult(errorMessage);
			if (assembly == null || assembly.Length == 0)
				return new DbgDotNetCompilationResult(Array.Empty<byte>(), Array.Empty<DbgDotNetCompiledExpressionResult>());

			var compiledExpressions = new DbgDotNetCompiledExpressionResult[infos.Length];
			int w = 0;
			for (int i = 0; i < infos.Length; i++) {
				var info = infos[i];

				string imageName;
				DbgDotNetText displayName;
				object nameColor;
				switch (info.Kind) {
				case LocalAndMethodKind.Local:
					imageName = PredefinedDbgValueNodeImageNames.Local;
					nameColor = BoxedTextColor.Local;
					break;

				case LocalAndMethodKind.Parameter:
					imageName = PredefinedDbgValueNodeImageNames.Parameter;
					nameColor = BoxedTextColor.Parameter;
					break;

				case LocalAndMethodKind.This:
					imageName = PredefinedDbgValueNodeImageNames.This;
					nameColor = BoxedTextColor.Keyword;
					break;

				case LocalAndMethodKind.LocalConstant:
					imageName = PredefinedDbgValueNodeImageNames.Constant;
					nameColor = BoxedTextColor.Local;
					break;

				case LocalAndMethodKind.StaticLocal:
					imageName = PredefinedDbgValueNodeImageNames.Local;
					nameColor = BoxedTextColor.Local;
					break;

				case LocalAndMethodKind.ObjectAddress:
					imageName = PredefinedDbgValueNodeImageNames.ObjectAddress;
					nameColor = BoxedTextColor.Number;
					break;

				case LocalAndMethodKind.TypeVariables:
					Debug.Fail("Roslyn's EC shouldn't create type variables");
					continue;

				case LocalAndMethodKind.Exception:
				case LocalAndMethodKind.StowedException:
				case LocalAndMethodKind.ReturnValue:
				case LocalAndMethodKind.ObjectId:
				case LocalAndMethodKind.EEVariable:
					Debug.Fail("Should not happen since we didn't pass in any aliases");
					continue;

				default:
					Debug.Fail($"Unknown {nameof(LocalAndMethodKind)} value: {info.Kind}");
					imageName = PredefinedDbgValueNodeImageNames.Data;
					nameColor = BoxedTextColor.Text;
					break;
				}
				displayName = new DbgDotNetText(new DbgDotNetTextPart(nameColor, info.LocalDisplayName));

				var flags = DbgEvaluationResultFlags.None;
				if ((info.Flags & DkmClrCompilationResultFlags.PotentialSideEffect) != 0)
					flags |= DbgEvaluationResultFlags.SideEffects;
				if ((info.Flags & DkmClrCompilationResultFlags.ReadOnlyResult) != 0)
					flags |= DbgEvaluationResultFlags.ReadOnly;
				if ((info.Flags & DkmClrCompilationResultFlags.BoolResult) != 0)
					flags |= DbgEvaluationResultFlags.BooleanExpression;

				DbgDotNetCustomTypeInfo customTypeInfo;
				if (info.CustomTypeInfo != null)
					customTypeInfo = new DbgDotNetCustomTypeInfo(info.CustomTypeInfoId, info.CustomTypeInfo);
				else
					customTypeInfo = null;

				compiledExpressions[w++] = DbgDotNetCompiledExpressionResult.Create(typeName, info.MethodName, info.LocalName, displayName, flags, imageName, customTypeInfo, info.Index);
			}
			if (compiledExpressions.Length != w)
				Array.Resize(ref compiledExpressions, w);
			return new DbgDotNetCompilationResult(assembly, compiledExpressions);
		}

		static ImmutableArray<MetadataBlock> CreateMetadataBlock(DbgModuleReference[] references) {
			var builder = ImmutableArray.CreateBuilder<MetadataBlock>(references.Length);
			for (int i = 0; i < references.Length; i++) {
				var r = references[i];
				builder.Add(new MetadataBlock(r.ModuleVersionId, r.GenerationId, r.MetadataAddress, (int)r.MetadataSize));
			}
			return builder.ToImmutable();
		}
	}
}
